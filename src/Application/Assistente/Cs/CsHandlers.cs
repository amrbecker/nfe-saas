using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Cs;

// ------------------------------------------------------------------ Telemetria

public record RegistrarEventosCommand(Guid EscritorioId, Guid? EmpresaId, Guid UsuarioId, LoteEventosDto Lote) : IRequest<int>;

public class RegistrarEventosHandler : IRequestHandler<RegistrarEventosCommand, int>
{
    public const int MaximoPorLote = 100;

    public static readonly IReadOnlySet<string> TiposPermitidos = new HashSet<string>
    {
        "tela_aberta", "emissao_iniciada", "emissao_autorizada", "emissao_rejeitada", "ori_aberta", "ori_hipotese",
        "ori_pergunta", "dica_mostrada", "dica_dispensada", "sugestao_aceita", "sugestao_recusada", "lembrete_criado"
    };

    // Chaves que costumam carregar dado pessoal: descartadas do DadosJson.
    private static readonly string[] ChavesProibidas = { "cpf", "cnpj", "email", "nome", "telefone", "endereco", "logradouro", "razao", "documento", "ie" };

    private readonly IEventoProdutoRepository _eventos;
    private readonly IUnitOfWork _uow;
    public RegistrarEventosHandler(IEventoProdutoRepository eventos, IUnitOfWork uow) { _eventos = eventos; _uow = uow; }

    public async Task<int> Handle(RegistrarEventosCommand r, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var validos = r.Lote.Eventos.Take(MaximoPorLote)
            .Where(e => TiposPermitidos.Contains(e.Tipo))
            .Select(e => EventoProduto.Criar(r.EscritorioId, r.EmpresaId, r.UsuarioId, e.Tipo,
                e.Tela is { Length: <= 120 } t ? t : null,
                LimparDados(e.DadosJson),
                e.OcorridoEm > agora.AddDays(-1) && e.OcorridoEm <= agora.AddMinutes(5) ? e.OcorridoEm : agora))
            .ToList();
        if (validos.Count == 0) return 0;
        await _eventos.AddRangeAsync(validos, ct);
        await _uow.SaveChangesAsync(ct);
        return validos.Count;
    }

    public static string? LimparDados(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 2000) return null;
        try
        {
            if (JsonNode.Parse(json) is not JsonObject obj) return null;
            foreach (var chave in obj.Select(kv => kv.Key).ToList())
                if (ChavesProibidas.Any(p => chave.Contains(p, StringComparison.OrdinalIgnoreCase))) obj.Remove(chave);
            return obj.ToJsonString();
        }
        catch (JsonException) { return null; }
    }
}

// ------------------------------------------------------------------ Alertas

public record ListarAlertasQuery(Guid EscritorioId, Guid? EmpresaId) : IRequest<List<AlertaCsDto>>;
public record MarcarAlertaCommand(Guid EscritorioId, Guid AlertaId, bool Dispensar) : IRequest<bool>;

public class AlertasHandlers : IRequestHandler<ListarAlertasQuery, List<AlertaCsDto>>, IRequestHandler<MarcarAlertaCommand, bool>
{
    private readonly IAlertaCsRepository _alertas;
    private readonly IUnitOfWork _uow;
    public AlertasHandlers(IAlertaCsRepository alertas, IUnitOfWork uow) { _alertas = alertas; _uow = uow; }

    public async Task<List<AlertaCsDto>> Handle(ListarAlertasQuery r, CancellationToken ct) =>
        (await _alertas.ListarAtivosAsync(r.EscritorioId, r.EmpresaId, incluirInternos: false, ct))
            .Select(a => new AlertaCsDto(a.Id, a.Tipo, a.Mensagem, a.LinkAcao, a.CreatedAt, a.VistoEm != null, a.EmpresaId))
            .ToList();

    public async Task<bool> Handle(MarcarAlertaCommand r, CancellationToken ct)
    {
        var a = await _alertas.GetByIdAsync(r.AlertaId, ct);
        if (a == null || a.EscritorioId != r.EscritorioId || a.Interno) return false;
        if (r.Dispensar) a.Dispensar(); else a.MarcarVisto();
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

// ------------------------------------------------------------------ Lembretes (A7)

public record ListarLembretesQuery(Guid EmpresaId) : IRequest<List<LembreteDto>>;
public record CriarLembreteCommand(Guid EmpresaId, Guid UsuarioId, CriarLembreteDto Dto) : IRequest<(LembreteDto? Lembrete, string? Erro)>;
public record AtualizarLembreteCommand(Guid EmpresaId, Guid Id, CriarLembreteDto Dto, bool? Ativo) : IRequest<(LembreteDto? Lembrete, string? Erro)>;
public record ExcluirLembreteCommand(Guid EmpresaId, Guid Id) : IRequest<bool>;

public class LembretesHandlers :
    IRequestHandler<ListarLembretesQuery, List<LembreteDto>>,
    IRequestHandler<CriarLembreteCommand, (LembreteDto? Lembrete, string? Erro)>,
    IRequestHandler<AtualizarLembreteCommand, (LembreteDto? Lembrete, string? Erro)>,
    IRequestHandler<ExcluirLembreteCommand, bool>
{
    private readonly ILembreteEmissaoRepository _lembretes;
    private readonly INotaFiscalRepository _notas;
    private readonly IUnitOfWork _uow;

    public LembretesHandlers(ILembreteEmissaoRepository lembretes, INotaFiscalRepository notas, IUnitOfWork uow)
    {
        _lembretes = lembretes;
        _notas = notas;
        _uow = uow;
    }

    public async Task<List<LembreteDto>> Handle(ListarLembretesQuery r, CancellationToken ct) =>
        (await _lembretes.ListarPorEmpresaAsync(r.EmpresaId, ct)).Select(Map).ToList();

    public async Task<(LembreteDto? Lembrete, string? Erro)> Handle(CriarLembreteCommand r, CancellationToken ct)
    {
        var nota = await _notas.GetByIdAsync(r.Dto.NotaModeloId, ct);
        if (nota == null || nota.EmpresaId != r.EmpresaId) return (null, "Nota modelo não encontrada nesta empresa.");
        if (nota.Situacao != SituacaoNota.Autorizada) return (null, "Use uma nota autorizada como modelo.");
        try
        {
            var l = LembreteEmissao.Criar(r.EmpresaId, r.UsuarioId, nota.Id, r.Dto.Descricao, r.Dto.Periodicidade, r.Dto.Dia, DateTime.UtcNow);
            await _lembretes.AddAsync(l, ct);
            await _uow.SaveChangesAsync(ct);
            return (Map(l), null);
        }
        catch (ArgumentOutOfRangeException ex) { return (null, ex.Message.Split(" (Parameter")[0]); }
    }

    public async Task<(LembreteDto? Lembrete, string? Erro)> Handle(AtualizarLembreteCommand r, CancellationToken ct)
    {
        var l = await _lembretes.GetByIdAsync(r.Id, ct);
        if (l == null || l.EmpresaId != r.EmpresaId || l.IsDeleted) return (null, null);
        try
        {
            l.Atualizar(r.Dto.Descricao, r.Dto.Periodicidade, r.Dto.Dia, DateTime.UtcNow);
            if (r.Ativo == true) l.Ativar(DateTime.UtcNow);
            if (r.Ativo == false) l.Desativar();
            await _uow.SaveChangesAsync(ct);
            return (Map(l), null);
        }
        catch (ArgumentOutOfRangeException ex) { return (null, ex.Message.Split(" (Parameter")[0]); }
    }

    public async Task<bool> Handle(ExcluirLembreteCommand r, CancellationToken ct)
    {
        var l = await _lembretes.GetByIdAsync(r.Id, ct);
        if (l == null || l.EmpresaId != r.EmpresaId) return false;
        l.Delete();
        await _uow.SaveChangesAsync(ct);
        return true;
    }

    private static LembreteDto Map(LembreteEmissao l) =>
        new(l.Id, l.NotaModeloId, l.Descricao, l.Periodicidade, l.Dia, l.ProximaEm, l.Ativo);
}

// ------------------------------------------------------------------ Saúde das contas (interno)

public record SaudeEscritorioDto(Guid EscritorioId, string Nome, string Plano, string StatusAssinatura, int Score,
    List<string> Pontos, int Empresas, int NotasAutorizadas30d, int NotasRejeitadas30d, decimal CustoIaMesUsd,
    List<string> AlertasInternos);

public record SaudeContasQuery : IRequest<List<SaudeEscritorioDto>>;

/// <summary>
/// Score 0–100 por escritório. Pesos: sem nota autorizada em 30 dias −30; taxa de rejeição > 20% −20; empresa sem
/// configuração concluída −15; certificado vencido ou vencendo em ≤ 15 dias −20; trial ≤ 7 dias, expirado ou suspenso −15.
/// </summary>
public class SaudeContasHandler : IRequestHandler<SaudeContasQuery, List<SaudeEscritorioDto>>
{
    private readonly IConsultasAssistenteRepository _consultas;
    private readonly IUsoAssistenteRepository _usos;
    private readonly IAlertaCsRepository _alertas;

    public SaudeContasHandler(IConsultasAssistenteRepository consultas, IUsoAssistenteRepository usos, IAlertaCsRepository alertas)
    {
        _consultas = consultas;
        _usos = usos;
        _alertas = alertas;
    }

    public async Task<List<SaudeEscritorioDto>> Handle(SaudeContasQuery r, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var hoje = DateOnly.FromDateTime(agora);
        var empresas = (await _consultas.EmpresasAtivasAsync(ct)).GroupBy(e => e.EscritorioId).ToDictionary(g => g.Key, g => g.ToList());
        var internos = (await _alertas.ListarInternosAsync(agora.AddDays(-30), ct)).GroupBy(a => a.EscritorioId).ToDictionary(g => g.Key, g => g.ToList());
        var resultado = new List<SaudeEscritorioDto>();

        foreach (var esc in await _consultas.EscritoriosAtivosAsync(ct))
        {
            var lista = empresas.GetValueOrDefault(esc.Id) ?? new();
            var autorizadas = 0; var rejeitadas = 0;
            foreach (var e in lista)
            {
                var notas = await _consultas.NotasAsync(e.EmpresaId, agora.AddDays(-30), null, 2000, ct);
                autorizadas += notas.Count(n => n.Situacao == SituacaoNota.Autorizada);
                rejeitadas += notas.Count(n => n.Situacao == SituacaoNota.Rejeitada);
            }

            var pontos = new List<string>();
            var score = 100;
            if (autorizadas == 0) { score -= 30; pontos.Add("sem nota autorizada em 30 dias"); }
            if (autorizadas + rejeitadas > 0 && (double)rejeitadas / (autorizadas + rejeitadas) > 0.2) { score -= 20; pontos.Add("taxa de rejeição acima de 20%"); }
            if (lista.Any(e => !e.ConfiguracaoConcluida)) { score -= 15; pontos.Add("empresa com configuração pendente"); }
            if (lista.Any(e => e.CertificadoValidade is { } v && (v - agora).TotalDays <= 15)) { score -= 20; pontos.Add("certificado vencido ou vencendo"); }
            var status = esc.CalcularStatusAssinatura(agora);
            if (status is StatusAssinaturaEscritorio.TrialExpirado or StatusAssinaturaEscritorio.Suspenso
                || (status == StatusAssinaturaEscritorio.TrialAtivo && esc.DiasRestantesTrial(agora) <= 7))
            { score -= 15; pontos.Add("assinatura em risco"); }

            var custo = await _usos.SomarCustoEscritorioAsync(esc.Id, new DateOnly(hoje.Year, hoje.Month, 1), hoje, ct);
            resultado.Add(new SaudeEscritorioDto(esc.Id, string.IsNullOrWhiteSpace(esc.NomeFantasia) ? esc.RazaoSocial : esc.NomeFantasia,
                esc.Plano.ToString(), status.ToString(), Math.Max(0, score), pontos, lista.Count, autorizadas, rejeitadas, custo,
                internos.GetValueOrDefault(esc.Id)?.Select(a => a.Mensagem).ToList() ?? new()));
        }
        return resultado.OrderBy(s => s.Score).ToList();
    }
}
