using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Assistente.Automacoes;

// Automações da Ori (docs/assistente/AUTOMACOES.md). Todas PREPARAM dados para o formulário; nenhuma persiste
// nota, cliente ou produto — quem conclui é o usuário, clicando em Emitir/Salvar.

public record ItemSugeridoDto(int NumeroItem, string Codigo, string Descricao, string Ncm, string? Cest, string Cfop,
    string Unidade, decimal ValorUnitario, int OrigemMercadoria);

public record DivergenciaProdutoDto(Guid ProdutoId, string Codigo, string Campo, string ValorCadastro, string ValorNota);

public record CadastroSugeridoDto(Guid NotaId, int NumeroNota, bool DestinatarioNovo, DestinatarioDto? Destinatario,
    List<ItemSugeridoDto> ProdutosNovos, List<DivergenciaProdutoDto> Divergencias);

public record ClienteExistenteDto(Guid Id, string Nome);

/// <summary>A5 — dados para "Emitir igual": destinatário, itens, frete, pagamento; sem número, série, datas nem chave.</summary>
public record PrepararNotaQuery(Guid EmpresaId, Guid NotaId) : IRequest<EmitirNotaFiscalDto?>;

/// <summary>A1/A2/A4 — destinatário e produtos digitados à mão e divergências com o cadastro.</summary>
public record CadastroSugeridoQuery(Guid EmpresaId, Guid NotaId) : IRequest<CadastroSugeridoDto?>;

/// <summary>A3 — destinatário já cadastrado com esse CPF/CNPJ.</summary>
public record DestinatarioExistenteQuery(Guid EmpresaId, string Documento) : IRequest<ClienteExistenteDto?>;

public class AutomacoesHandlers :
    IRequestHandler<PrepararNotaQuery, EmitirNotaFiscalDto?>,
    IRequestHandler<CadastroSugeridoQuery, CadastroSugeridoDto?>,
    IRequestHandler<DestinatarioExistenteQuery, ClienteExistenteDto?>,
    IRequestHandler<GetPadroesPreenchimentoQuery, PadroesPreenchimentoDto>
{
    public const int NotasAnalisadasPadroes = 10;
    public const double LimiarPadrao = 0.8;

    private readonly INotaFiscalRepository _notas;
    private readonly IClienteRepository _clientes;
    private readonly IProdutoRepository _produtos;
    private readonly IConsultasAssistenteRepository _consultas;

    public AutomacoesHandlers(INotaFiscalRepository notas, IClienteRepository clientes, IProdutoRepository produtos,
        IConsultasAssistenteRepository consultas)
    {
        _notas = notas;
        _clientes = clientes;
        _produtos = produtos;
        _consultas = consultas;
    }

    private async Task<NotaFiscal?> Nota(Guid empresaId, Guid notaId, CancellationToken ct)
    {
        var n = await _notas.GetByIdAsync(notaId, ct);
        return n != null && n.EmpresaId == empresaId ? n : null;
    }

    public async Task<EmitirNotaFiscalDto?> Handle(PrepararNotaQuery r, CancellationToken ct)
    {
        if (await Nota(r.EmpresaId, r.NotaId, ct) is not { } n) return null;
        return new EmitirNotaFiscalDto(
            n.Tipo, n.Finalidade, n.TipoOperacao,
            Destinatario(n),
            n.Itens.OrderBy(i => i.NumeroItem).Select(i => new ItemNotaDto(
                i.CodigoProduto, i.Descricao, i.Ncm, i.Cest, i.Cfop, i.UnidadeComercial, i.Quantidade, i.ValorUnitario,
                i.ValorDesconto, i.CodigoEan,
                new ImpostosItemDto(
                    i.OrigemMercadoria, i.CstIcms, i.AliquotaIcms, null,
                    AplicarSt: (i.ValorIcmsSt ?? 0) > 0, MvaIcmsSt: null, AliquotaInternaIcmsSt: i.AliquotaIcmsSt,
                    i.CstPis, i.AliquotaPis, i.CstCofins, i.AliquotaCofins,
                    i.CsosnIcms, i.AliquotaIpi, i.CstIpi, i.AliquotaFcp, i.AliquotaInternaUfDestino,
                    i.AliquotaIbsUf, i.AliquotaIbsMun, i.AliquotaCbs, i.CstIbsCbs, i.ClassTribIbsCbs))).ToList(),
            new TransporteDto(n.ModalidadeFrete, n.TransportadoraCpfCnpj, n.TransportadoraRazaoSocial, n.TotalFrete, n.TotalSeguro),
            new PagamentoDto(n.FormaPagemento, n.ValorPagamento),
            n.InformacoesAdicionais,
            IdempotencyKey: null);
    }

    public async Task<CadastroSugeridoDto?> Handle(CadastroSugeridoQuery r, CancellationToken ct)
    {
        if (await Nota(r.EmpresaId, r.NotaId, ct) is not { } n) return null;

        var doc = CnpjValidator.ApenasDigitos(n.DestinatarioCpfCnpj);
        var destinatarioNovo = doc.Length is 11 or 14 && await _clientes.GetByCpfCnpjAsync(r.EmpresaId, doc, ct) == null;

        var itens = n.Itens.OrderBy(i => i.NumeroItem).ToList();
        var existentes = await _consultas.CodigosProdutosExistentesAsync(r.EmpresaId, itens.Select(i => i.CodigoProduto), ct);
        var novos = itens.Where(i => !existentes.Contains(i.CodigoProduto))
            .GroupBy(i => i.CodigoProduto).Select(g => g.First())
            .Select(i => new ItemSugeridoDto(i.NumeroItem, i.CodigoProduto, i.Descricao, i.Ncm, i.Cest, i.Cfop,
                i.UnidadeComercial, i.ValorUnitario, (int)i.OrigemMercadoria))
            .ToList();

        var divergencias = new List<DivergenciaProdutoDto>();
        if (n.Situacao == SituacaoNota.Autorizada)
        {
            foreach (var item in itens.Where(i => existentes.Contains(i.CodigoProduto)))
            {
                var p = await _produtos.GetByCodigoAsync(r.EmpresaId, item.CodigoProduto, ct);
                if (p == null) continue;
                void Comparar(string campo, string? cadastro, string? nota)
                {
                    var a = cadastro?.Trim() ?? ""; var b = nota?.Trim() ?? "";
                    if (b.Length > 0 && !string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                        divergencias.Add(new DivergenciaProdutoDto(p.Id, p.Codigo, campo, a, b));
                }
                Comparar("ncm", p.Ncm, item.Ncm);
                Comparar("cest", p.Cest, item.Cest);
                Comparar("cfop", p.CfopPadrao, item.Cfop);
            }
        }

        return new CadastroSugeridoDto(n.Id, n.Numero, destinatarioNovo, destinatarioNovo ? Destinatario(n) : null, novos, divergencias);
    }

    public async Task<ClienteExistenteDto?> Handle(DestinatarioExistenteQuery r, CancellationToken ct)
    {
        var doc = CnpjValidator.ApenasDigitos(r.Documento);
        if (doc.Length is not (11 or 14)) return null;
        var c = await _clientes.GetByCpfCnpjAsync(r.EmpresaId, doc, ct);
        return c == null || c.EmpresaId != r.EmpresaId ? null : new ClienteExistenteDto(c.Id, c.RazaoSocial);
    }

    /// <summary>
    /// A6: combinação CFOP + CST/CSOSN usada em ≥ 80% das últimas notas autorizadas (por destinatário e/ou produto).
    /// </summary>
    public async Task<PadroesPreenchimentoDto> Handle(GetPadroesPreenchimentoQuery r, CancellationToken ct)
    {
        var doc = CnpjValidator.ApenasDigitos(r.DestinatarioCpfCnpj);
        var notas = await _consultas.NotasAsync(r.EmpresaId, DateTime.UtcNow.AddYears(-1), SituacaoNota.Autorizada, 300, ct);
        if (doc.Length > 0) notas = notas.Where(n => CnpjValidator.ApenasDigitos(n.DestinatarioCpfCnpj) == doc).ToList();
        notas = notas.Take(NotasAnalisadasPadroes).ToList();

        var itens = notas.SelectMany(n => n.Itens)
            .Where(i => string.IsNullOrWhiteSpace(r.ProdutoCodigo) || i.CodigoProduto == r.ProdutoCodigo)
            .ToList();

        var padroes = itens
            .GroupBy(i => string.IsNullOrWhiteSpace(r.ProdutoCodigo) ? null : i.CodigoProduto)
            .SelectMany(g =>
            {
                var total = g.Count();
                return g.GroupBy(i => (i.Cfop, Cst: i.Csosn?.ToString() ?? i.CstIcms.ToString("00")))
                    .Select(c => new PadraoItemDto(g.Key, c.Key.Cfop, c.Key.Cst, null, c.Count(), total))
                    .Where(p => p.Total >= 3 && (double)p.Frequencia / p.Total >= LimiarPadrao);
            })
            .OrderByDescending(p => p.Frequencia)
            .ToList();

        return new PadroesPreenchimentoDto(padroes, notas.Count);
    }

    private static DestinatarioDto Destinatario(NotaFiscal n) => new(
        n.DestinatarioCpfCnpj, n.DestinatarioRazaoSocial, n.DestinatarioEmail, n.DestinatarioTipoPessoa,
        n.DestinatarioLogradouro, n.DestinatarioNumero, n.DestinatarioComplemento, n.DestinatarioBairro,
        n.DestinatarioCidade, n.DestinatarioUf, n.DestinatarioCep, n.DestinatarioCodigoMunicipio,
        n.DestinatarioInscricaoEstadual);
}
