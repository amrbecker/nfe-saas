using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Services;
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
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EmitirNFeCommandHandler> _logger;

    public EmitirNFeCommandHandler(
        IEmpresaRepository empresaRepo,
        INotaFiscalRepository notaRepo,
        ISefazService sefaz,
        IXmlNFeService xmlService,
        IImpostoCalculoService impostoService,
        IAuditService auditService,
        IUnitOfWork uow,
        ILogger<EmitirNFeCommandHandler> logger)
    {
        _empresaRepo = empresaRepo;
        _notaRepo = notaRepo;
        _sefaz = sefaz;
        _xmlService = xmlService;
        _impostoService = impostoService;
        _auditService = auditService;
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

        // Validate CNPJ of the empresa
        if (!CnpjValidator.Validar(empresa.Cnpj))
            return new EmitirNFeResult(false, null, null, null, $"CNPJ da empresa inválido: {empresa.Cnpj}");

        // Validate recipient
        var destErro = ValidarDestinatario(request.Dados.Destinatario);
        if (destErro != null)
            return new EmitirNFeResult(false, null, null, null, destErro);

        // CRT vs CST/CSOSN: Simples Nacional (CRT 1 ou 2) usa CSOSN; Regime Normal (CRT 3) usa CST.
        var isSimples = empresa.RegimeTributario != RegimeTributario.RegimeNormal;
        for (var i = 0; i < request.Dados.Itens.Count; i++)
        {
            var imp = request.Dados.Itens[i].Impostos;
            if (isSimples && !imp.CsosnIcms.HasValue)
                return new EmitirNFeResult(false, null, null, null,
                    $"CSOSN obrigatório para o item {request.Dados.Itens[i].CodigoProduto} (empresa Simples Nacional).");
            if (!isSimples && imp.CsosnIcms.HasValue)
                return new EmitirNFeResult(false, null, null, null,
                    $"Empresa em Regime Normal não pode emitir CSOSN no item {request.Dados.Itens[i].CodigoProduto}; use CST.");
        }

        // Validate CFOP, NCM and CST scope per item
        var isInterestadual = !string.Equals(empresa.Uf, request.Dados.Destinatario.Uf, StringComparison.OrdinalIgnoreCase);
        foreach (var item in request.Dados.Itens)
        {
            if (!CfopValidator.Existe(item.Cfop))
                return new EmitirNFeResult(false, null, null, null, $"CFOP inválido: {item.Cfop}");

            if (request.Dados.TipoOperacao == TipoOperacao.Saida &&
                !CfopValidator.ValidarParaSaida(item.Cfop, isInterestadual))
                return new EmitirNFeResult(false, null, null, null,
                    $"CFOP {item.Cfop} não é válido para operação de saída {(isInterestadual ? "interestadual" : "intraestadual")}");

            if (!NcmValidator.Validar(item.Ncm))
                return new EmitirNFeResult(false, null, null, null,
                    $"NCM inválido para o item {item.CodigoProduto}: deve ter 8 dígitos.");
        }

        // Dedup pré-check (UI / cliente). O índice único no banco é a proteção final em caso de race.
        var serieCheck = request.Dados.Tipo == TipoNota.NFe ? empresa.SerieNFe : empresa.SerieNFCe;
        var proximoNumero = (request.Dados.Tipo == TipoNota.NFe ? empresa.UltimoNumeronFe : empresa.UltimoNumeronFCe) + 1;
        var existente = await _notaRepo.GetBySerieNumeroAsync(empresa.Id, request.Dados.Tipo, serieCheck, proximoNumero, empresa.AmbienteSefaz, cancellationToken);
        if (existente != null)
            return new EmitirNFeResult(false, null, null, null,
                $"Já existe uma nota fiscal {request.Dados.Tipo} série {serieCheck} número {proximoNumero} para esta empresa neste ambiente.");

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
                if (imp.CsosnIcms.HasValue)
                    item.SetIcmsSimples(imp.OrigemMercadoria, imp.CsosnIcms.Value, icms.BaseCalculo, icms.Aliquota);
                else
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

                // IPI (opcional — calcula apenas se alíquota foi informada)
                if (imp.AliquotaIpi.HasValue && imp.AliquotaIpi.Value > 0)
                {
                    var ipi = _impostoService.CalcularIpi(valorBase, imp.AliquotaIpi.Value);
                    item.SetIpi(imp.CstIpi ?? "50", ipi.BaseCalculo, ipi.Aliquota);
                }

                // FCP (opcional — calcula sobre BC ICMS do item)
                if (imp.AliquotaFcp.HasValue && imp.AliquotaFcp.Value > 0)
                {
                    var fcp = _impostoService.CalcularFcp(icms.BaseCalculo, imp.AliquotaFcp.Value);
                    item.SetFcp(fcp.BaseCalculo, fcp.Aliquota);
                }

                // DIFAL (auto-detectado para operação interestadual a destinatário sem IE — não contribuinte)
                var destSemIe = string.IsNullOrWhiteSpace(request.Dados.Destinatario.InscricaoEstadual);
                if (isInterestadual && destSemIe && imp.AliquotaInternaUfDestino.HasValue && imp.AliquotaInternaUfDestino.Value > 0)
                {
                    var difal = _impostoService.CalcularDifal(valorBase, imp.AliquotaInternaUfDestino.Value, imp.AliquotaIcms);
                    item.SetDifal(difal.BaseCalculo, difal.AliquotaInterna, difal.AliquotaInterestadual);
                }

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

            // Audit
            var auditDetalhes = resultado.Sucesso
                ? $"Autorizada. Protocolo: {resultado.Protocolo}"
                : $"Rejeitada: {resultado.MensagemErro}";
            await _auditService.RegistrarAsync(empresa.Id,
                resultado.Sucesso ? "NFe.Autorizada" : "NFe.Rejeitada",
                request.UsuarioId, resultado.ChaveAcesso, auditDetalhes,
                ct: cancellationToken);

            return resultado.Sucesso
                ? new EmitirNFeResult(true, nota.Id, resultado.ChaveAcesso, resultado.Protocolo, null)
                : new EmitirNFeResult(false, nota.Id, null, null, resultado.MensagemErro);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao emitir nota fiscal para empresa {EmpresaId}", request.EmpresaId);
            return new EmitirNFeResult(false, null, null, null, "Ocorreu um erro interno ao processar a nota fiscal. Tente novamente.");
        }
    }

    private static string? ValidarDestinatario(DestinatarioDto dest)
    {
        if (dest.TipoPessoa != TipoPessoa.Estrangeiro)
        {
            if (string.IsNullOrWhiteSpace(dest.CpfCnpj))
                return "CPF/CNPJ do destinatário é obrigatório.";

            var digits = CnpjValidator.ApenasDigitos(dest.CpfCnpj);
            if (digits.Length == 14 && !CnpjValidator.Validar(dest.CpfCnpj))
                return $"CNPJ do destinatário inválido: {dest.CpfCnpj}";
            if (digits.Length == 11 && !CnpjValidator.ValidarCpf(dest.CpfCnpj))
                return $"CPF do destinatário inválido: {dest.CpfCnpj}";
            if (digits.Length != 11 && digits.Length != 14)
                return $"CPF/CNPJ do destinatário com formato inválido: {dest.CpfCnpj}";
        }

        if (string.IsNullOrWhiteSpace(dest.RazaoSocial))
            return "Razão social do destinatário é obrigatória.";
        if (string.IsNullOrWhiteSpace(dest.Logradouro))
            return "Logradouro do destinatário é obrigatório.";
        if (string.IsNullOrWhiteSpace(dest.Numero))
            return "Número do destinatário é obrigatório.";
        if (string.IsNullOrWhiteSpace(dest.Bairro))
            return "Bairro do destinatário é obrigatório.";
        if (string.IsNullOrWhiteSpace(dest.Cidade))
            return "Cidade do destinatário é obrigatória.";
        if (string.IsNullOrWhiteSpace(dest.Uf) || !IeValidator.UfValida(dest.Uf))
            return $"UF do destinatário inválida: {dest.Uf}";
        if (string.IsNullOrWhiteSpace(dest.Cep) || dest.Cep.Where(char.IsDigit).Count() != 8)
            return "CEP do destinatário inválido (deve ter 8 dígitos).";
        if (string.IsNullOrWhiteSpace(dest.CodigoMunicipio))
            return "Código IBGE do município do destinatário é obrigatório.";

        return null;
    }
}
