using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Queries;

// === GET NOTAS ===
public record GetNotasQuery(Guid EmpresaId, int Pagina = 1, int TamanhoPagina = 20) : IRequest<GetNotasResult>;
public record GetNotasResult(IEnumerable<NotaFiscalResumoDto> Notas, int Total, int Pagina, int TamanhoPagina);

public class GetNotasQueryHandler : IRequestHandler<GetNotasQuery, GetNotasResult>
{
    private readonly INotaFiscalRepository _repo;

    public GetNotasQueryHandler(INotaFiscalRepository repo) => _repo = repo;

    public async Task<GetNotasResult> Handle(GetNotasQuery request, CancellationToken cancellationToken)
    {
        var notas = await _repo.GetByEmpresaAsync(request.EmpresaId, request.Pagina, request.TamanhoPagina, cancellationToken);
        var total = await _repo.CountByEmpresaAsync(request.EmpresaId, cancellationToken);

        var dtos = notas.Select(n => new NotaFiscalResumoDto(
            n.Id, n.Tipo, n.Serie, n.Numero, n.ChaveAcesso,
            n.Situacao, n.DestinatarioRazaoSocial, n.TotalNota,
            n.DataEmissao, n.DataAutorizacao));

        return new GetNotasResult(dtos, total, request.Pagina, request.TamanhoPagina);
    }
}

// === GET NOTA DETALHE ===
public record GetNotaDetalheQuery(Guid NotaFiscalId, Guid EmpresaId) : IRequest<NotaFiscalDetalheDto?>;

public class GetNotaDetalheQueryHandler : IRequestHandler<GetNotaDetalheQuery, NotaFiscalDetalheDto?>
{
    private readonly INotaFiscalRepository _repo;

    public GetNotaDetalheQueryHandler(INotaFiscalRepository repo) => _repo = repo;

    public async Task<NotaFiscalDetalheDto?> Handle(GetNotaDetalheQuery request, CancellationToken cancellationToken)
    {
        var nota = await _repo.GetByIdAsync(request.NotaFiscalId, cancellationToken);
        if (nota == null || nota.EmpresaId != request.EmpresaId) return null;

        var dest = new DestinatarioDto(
            nota.DestinatarioCpfCnpj, nota.DestinatarioRazaoSocial, nota.DestinatarioEmail,
            nota.DestinatarioTipoPessoa, nota.DestinatarioLogradouro, nota.DestinatarioNumero,
            nota.DestinatarioComplemento, nota.DestinatarioBairro, nota.DestinatarioCidade, nota.DestinatarioUf,
            nota.DestinatarioCep, nota.DestinatarioCodigoMunicipio, nota.DestinatarioInscricaoEstadual);

        var itens = nota.Itens.Select(i => new ItemNotaResumoDto(
            i.NumeroItem, i.CodigoProduto, i.Descricao, i.Ncm, i.Cfop,
            i.UnidadeComercial, i.Quantidade, i.ValorUnitario,
            i.ValorDesconto, i.ValorTotal, i.ValorIcms, i.ValorPis, i.ValorCofins)).ToList();

        return new NotaFiscalDetalheDto(
            nota.Id, nota.Tipo, nota.Serie, nota.Numero, nota.ChaveAcesso, nota.Protocolo,
            nota.Situacao, nota.Ambiente, nota.Finalidade, nota.TipoOperacao,
            dest, itens, nota.TotalProdutos, nota.TotalDesconto, nota.TotalIcms,
            nota.TotalIcmsSt, nota.TotalPis, nota.TotalCofins, nota.TotalFrete,
            nota.TotalNota, nota.DataEmissao, nota.DataAutorizacao,
            nota.MotivoRejeicao, nota.InformacoesAdicionais,
            nota.DataDescarteAutorizado, nota.DentroPeriodoRetencao, nota.EmailEnviadoEm);
    }
}

// === DASHBOARD ===
public record GetDashboardQuery(Guid EmpresaId, int Ano, int Mes) : IRequest<DashboardDto>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly INotaFiscalRepository _repo;

    public GetDashboardQueryHandler(INotaFiscalRepository repo) => _repo = repo;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var totalMes = await _repo.GetTotalEmitidoMesAsync(request.EmpresaId, request.Ano, request.Mes, cancellationToken);
        var contagemPorSituacao = await _repo.GetContagemPorSituacaoAsync(request.EmpresaId, cancellationToken);

        var inicio = new DateTime(request.Ano, request.Mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1).AddDays(-1);
        var notasMes = await _repo.GetByPeriodoAsync(request.EmpresaId, inicio, fim, cancellationToken);

        var faturamentoDiario = notasMes
            .Where(n => n.Situacao == NfeSaas.Domain.Enums.SituacaoNota.Autorizada)
            .GroupBy(n => n.DataEmissao.Date)
            .Select(g => new FaturamentoDiarioDto(g.Key, g.Sum(n => n.TotalNota), g.Count()))
            .OrderBy(f => f.Data)
            .ToList();

        contagemPorSituacao.TryGetValue(NfeSaas.Domain.Enums.SituacaoNota.Autorizada, out var autorizadas);
        contagemPorSituacao.TryGetValue(NfeSaas.Domain.Enums.SituacaoNota.Cancelada, out var canceladas);
        int pendentes = contagemPorSituacao
            .Where(k => k.Key is NfeSaas.Domain.Enums.SituacaoNota.Rascunho or NfeSaas.Domain.Enums.SituacaoNota.Enviada)
            .Sum(k => k.Value);
        int total = contagemPorSituacao.Values.Sum();

        return new DashboardDto(totalMes, total, autorizadas, canceladas, pendentes, faturamentoDiario);
    }
}
