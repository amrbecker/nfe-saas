using MediatR;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Curadoria;

// Fila de curadoria do escritório parceiro (PESQUISA_REFINAMENTO.md §4): nada aqui expõe escritório, empresa,
// usuário ou dado de cliente — só publicações públicas, artigos da base e lacunas agregadas e sanitizadas.

public record PublicacaoCuradoriaDto(Guid Id, string Fonte, NivelFonte NivelFonte, string Titulo, string? Url, DateTime DetectadaEm,
    bool? Relevante, string? Resumo, DateTime? VigenciaInicio, List<string> ArtigosAfetados, bool ImpactoTecnico,
    StatusPublicacao Status, string? NotaCurador);

public record AtualizarPublicacaoDto(StatusPublicacao Status, string? Nota);

public record ArtigoCuradoriaDto(string Id, string Titulo, string Categoria, string Status, DateOnly? VerificadoEm,
    DateOnly? RevisarAte, bool Vencido, string? Curador);

public record LacunaAgrupadaDto(string Tema, int Quantidade, List<string> Exemplos);

public record FonteCuradoriaDto(Guid Id, string Nome, NivelFonte Nivel, string Url, MecanismoMonitoramento Mecanismo, bool Ativa,
    DateTime? UltimaLeituraEm, DateTime? UltimaMudancaEm, int FalhasSeguidas, bool Cega);

public record ListarPublicacoesQuery(StatusPublicacao? Status, int Pagina) : IRequest<List<PublicacaoCuradoriaDto>>;
public record AtualizarPublicacaoCommand(Guid Id, AtualizarPublicacaoDto Dto) : IRequest<bool>;
public record ArtigosPendentesQuery : IRequest<List<ArtigoCuradoriaDto>>;
public record LacunasQuery(int Dias) : IRequest<List<LacunaAgrupadaDto>>;
public record FontesQuery : IRequest<List<FonteCuradoriaDto>>;

public class CuradoriaHandlers :
    IRequestHandler<ListarPublicacoesQuery, List<PublicacaoCuradoriaDto>>,
    IRequestHandler<AtualizarPublicacaoCommand, bool>,
    IRequestHandler<ArtigosPendentesQuery, List<ArtigoCuradoriaDto>>,
    IRequestHandler<LacunasQuery, List<LacunaAgrupadaDto>>,
    IRequestHandler<FontesQuery, List<FonteCuradoriaDto>>
{
    private readonly IPublicacaoDetectadaRepository _publicacoes;
    private readonly IFonteMonitoradaRepository _fontes;
    private readonly ISinalProdutoRepository _sinais;
    private readonly IBaseConhecimento _base;
    private readonly IUnitOfWork _uow;

    public CuradoriaHandlers(IPublicacaoDetectadaRepository publicacoes, IFonteMonitoradaRepository fontes,
        ISinalProdutoRepository sinais, IBaseConhecimento baseConhecimento, IUnitOfWork uow)
    {
        _publicacoes = publicacoes;
        _fontes = fontes;
        _sinais = sinais;
        _base = baseConhecimento;
        _uow = uow;
    }

    public async Task<List<PublicacaoCuradoriaDto>> Handle(ListarPublicacoesQuery r, CancellationToken ct) =>
        (await _publicacoes.ListarAsync(r.Status, Math.Max(1, r.Pagina), 50, ct))
            .Where(p => r.Status != null || p.Status != StatusPublicacao.Descartada)
            .Select(p => new PublicacaoCuradoriaDto(p.Id, p.Fonte.Nome, p.Fonte.Nivel, p.Titulo, p.Url, p.DetectadaEm, p.Relevante,
                p.Resumo, p.VigenciaInicio,
                (p.ArtigosAfetados ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                p.ImpactoTecnico, p.Status, p.NotaCurador))
            .ToList();

    public async Task<bool> Handle(AtualizarPublicacaoCommand r, CancellationToken ct)
    {
        var p = await _publicacoes.GetByIdAsync(r.Id, ct);
        if (p == null) return false;
        p.AtualizarStatus(r.Dto.Status, r.Dto.Nota);
        await _uow.SaveChangesAsync(ct);
        return true;
    }

    public Task<List<ArtigoCuradoriaDto>> Handle(ArtigosPendentesQuery r, CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var lista = _base.Todos()
            .Select(a => new ArtigoCuradoriaDto(a.Id, a.Titulo, a.Categoria, a.Status, a.VerificadoEm, a.RevisarAte,
                a.RevisarAte is { } d && d < hoje, a.Curador))
            .Where(a => a.Vencido || a.Status is "rascunho" or "revisado" or "em_revisao")
            .OrderByDescending(a => a.Vencido).ThenBy(a => a.Status).ThenBy(a => a.Id)
            .ToList();
        return Task.FromResult(lista);
    }

    public async Task<List<LacunaAgrupadaDto>> Handle(LacunasQuery r, CancellationToken ct)
    {
        var sinais = await _sinais.ListarAsync(TipoSinalProduto.LacunaConhecimento, DateTime.UtcNow.AddDays(-Math.Clamp(r.Dias, 1, 180)), 2000, ct);
        return sinais
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Tema) ? "sem tema" : s.Tema!)
            .Select(g => new LacunaAgrupadaDto(g.Key, g.Count(), g.Select(s => s.ResumoSanitizado).Distinct().Take(3).ToList()))
            .OrderByDescending(l => l.Quantidade)
            .ToList();
    }

    public async Task<List<FonteCuradoriaDto>> Handle(FontesQuery r, CancellationToken ct) =>
        (await _fontes.ListarAsync(apenasAtivas: false, ct))
            .Select(f => new FonteCuradoriaDto(f.Id, f.Nome, f.Nivel, f.Url, f.Mecanismo, f.Ativa, f.UltimaLeituraEm,
                f.UltimaMudancaEm, f.FalhasSeguidas, f.Cega))
            .ToList();
}
