using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NfeSaas.Application.Commands.EmitirNFe;

public record EmitirNFeCommand(Guid EmpresaId, Guid UsuarioId, EmitirNotaFiscalDto Dados) : IRequest<EmitirNFeResult>;

public record EmitirNFeResult(bool Sucesso, Guid? NotaFiscalId, string? ChaveAcesso, string? Protocolo, string? MensagemErro);

public class EmitirNFeCommandHandler : IRequestHandler<EmitirNFeCommand, EmitirNFeResult>
{
    private readonly IEmpresaRepository _empresaRepo;
    private readonly INotaFiscalRepository _notaRepo;
    private readonly ISefazService _sefaz;
    private readonly IXmlNFeService _xmlService;
    private readonly IImpostoCalculoService _impostoService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EmitirNFeCommandHandler> _logger;

    public EmitirNFeCommandHandler(
        IEmpresaRepository empresaRepo,
        INotaFiscalRepository notaRepo,
        ISefazService sefaz,
        IXmlNFeService xmlService,
        IImpostoCalculoService impostoService,
        IUnitOfWork uow,
        ILogger<EmitirNFeCommandHandler> logger)
    {
        _empresaRepo = empresaRepo;
        _notaRepo = notaRepo;
        _sefaz = sefaz;
        _xmlService = xmlService;
        _impostoService = impostoService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<EmitirNFeResult> Handle(EmitirNFeCommand request, CancellationToken cancellationToken)
    {
        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, cancellationToken);
        if (empresa == null)
            return new EmitirNFeResult(false, null, null, null, "Empresa não encontrada.");

        if (!empresa.CertificadoValido())
            return new EmitirNFeResult(false, null, null, null, "Certificado digital inválido ou expirado.");

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var dados = request.Dados;
            var numero = dados.Tipo == TipoNota.NFe ? empresa.ProximoNumeroNFe() : empresa.ProximoNumeroNFCe();
            var serie = dados.Tipo == TipoNota.NFe ? empresa.SerieNFe : empresa.SerieNFCe;

            var nota = NotaFiscal.Criar(empresa.Id, dados.Tipo, serie, numero,
                dados.Finalidade, dados.TipoOperacao, empresa.AmbienteSefaz);

            var dest = dados.Destinatario;
            nota.SetDestinatario(dest.CpfCnpj, dest.RazaoSocial, dest.Email, dest.TipoPessoa,
                dest.Logradouro, dest.Numero, dest.Bairro, dest.Cidade, dest.Uf,
                dest.Cep, dest.CodigoMunicipio, dest.InscricaoEstadual);

            var tr = dados.Transporte;
            nota.SetTransporte(tr.ModalidadeFrete, tr.TransportadoraCpfCnpj, tr.TransportadoraRazaoSocial,
                tr.Frete, tr.Seguro);

            nota.SetPagamento(dados.Pagamento.FormaPagamento, dados.Pagamento.Valor);
            nota.SetInformacoesAdicionais(dados.InformacoesAdicionais);

            for (int i = 0; i < dados.Itens.Count; i++)
            {
                var itemDto = dados.Itens[i];
                var item = ItemNotaFiscal.Criar(nota.Id, i + 1, itemDto.CodigoProduto,
                    itemDto.Descricao, itemDto.Ncm, itemDto.Cfop, itemDto.Unidade,
                    itemDto.Quantidade, itemDto.ValorUnitario, itemDto.Desconto);

                if (!string.IsNullOrEmpty(itemDto.CodigoEan)) item.SetCodigoEan(itemDto.CodigoEan);
                if (!string.IsNullOrEmpty(itemDto.Cest)) item.SetCest(itemDto.Cest);

                var imp = itemDto.Impostos;
                var valorBase = (itemDto.Quantidade * itemDto.ValorUnitario) - itemDto.Desconto;

                var icms = _impostoService.CalcularIcms(valorBase, imp.AliquotaIcms, imp.PercentualReducaoIcms);
                item.SetIcms(imp.OrigemMercadoria, imp.CstIcms, icms.BaseCalculo, icms.Aliquota);

                if (imp.AplicarSt && imp.MvaIcmsSt.HasValue && imp.AliquotaInternaIcmsSt.HasValue)
                {
                    var icmsSt = _impostoService.CalcularIcmsSt(valorBase, imp.MvaIcmsSt.Value,
                        imp.AliquotaInternaIcmsSt.Value, imp.AliquotaIcms);
                    item.SetIcmsSt(icmsSt.BaseCalculo, icmsSt.Aliquota);
                }

                var pis = _impostoService.CalcularPis(valorBase, imp.AliquotaPis);
                item.SetPis(imp.CstPis, pis.BaseCalculo, pis.Aliquota);

                var cofins = _impostoService.CalcularCofins(valorBase, imp.AliquotaCofins);
                item.SetCofins(imp.CstCofins, cofins.BaseCalculo, cofins.Aliquota);

                nota.AdicionarItem(item);
            }

            // Gerar e assinar XML
            var xml = _xmlService.GerarXmlNFe(nota, empresa);
            var xmlAssinado = _xmlService.AssinarXml(xml, empresa.CertificadoBytes!, empresa.CertificadoSenha!);

            if (!_xmlService.ValidarXml(xmlAssinado, out var errosXml))
                return new EmitirNFeResult(false, null, null, null, string.Join("; ", errosXml));

            nota.MarcarEnviada(xmlAssinado);

            await _notaRepo.AddAsync(nota, cancellationToken);
            await _empresaRepo.UpdateAsync(empresa, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            // Enviar para SEFAZ
            var resultado = await _sefaz.EnviarNFeAsync(nota, empresa, cancellationToken);

            if (resultado.Sucesso)
            {
                nota.Autorizar(resultado.ChaveAcesso!, resultado.Protocolo!, resultado.XmlRetorno!);
                _logger.LogInformation("NF-e {Numero} autorizada. Chave: {Chave}", nota.Numero, resultado.ChaveAcesso);
            }
            else
            {
                nota.Rejeitar(resultado.MensagemErro ?? "Erro desconhecido");
                _logger.LogWarning("NF-e {Numero} rejeitada: {Motivo}", nota.Numero, resultado.MensagemErro);
            }

            await _notaRepo.UpdateAsync(nota, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitAsync(cancellationToken);

            return resultado.Sucesso
                ? new EmitirNFeResult(true, nota.Id, resultado.ChaveAcesso, resultado.Protocolo, null)
                : new EmitirNFeResult(false, nota.Id, null, null, resultado.MensagemErro);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao emitir nota fiscal para empresa {EmpresaId}", request.EmpresaId);
            return new EmitirNFeResult(false, null, null, null, $"Erro interno: {ex.Message}");
        }
    }
}
