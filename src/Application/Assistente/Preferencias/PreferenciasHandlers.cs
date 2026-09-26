using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Preferencias;

public record GetStatusAssistenteQuery(Guid UsuarioId, Guid EscritorioId) : IRequest<StatusAssistenteDto>;

public record SalvarPreferenciasOriCommand(Guid UsuarioId, PreferenciasOriDto Dto) : IRequest<PreferenciasOriDto>;

public record GetPreferenciasOriQuery(Guid UsuarioId) : IRequest<PreferenciasOriDto>;

public record IgnorarSugestaoCommand(Guid UsuarioId, Guid? EmpresaId, SugestaoIgnoradaDto Dto) : IRequest<bool>;

public class PreferenciasHandlers :
    IRequestHandler<GetStatusAssistenteQuery, StatusAssistenteDto>,
    IRequestHandler<GetPreferenciasOriQuery, PreferenciasOriDto>,
    IRequestHandler<SalvarPreferenciasOriCommand, PreferenciasOriDto>,
    IRequestHandler<IgnorarSugestaoCommand, bool>
{
    private readonly IPreferenciaAssistenteRepository _preferencias;
    private readonly ISugestaoDispensadaRepository _sugestoes;
    private readonly IAssistenteIA _ia;
    private readonly ICotaAssistente _cota;
    private readonly IUnitOfWork _uow;
    private readonly IOptions<AssistenteOptions> _opcoes;
    private readonly ILogger<PreferenciasHandlers> _logger;

    public PreferenciasHandlers(IPreferenciaAssistenteRepository preferencias, ISugestaoDispensadaRepository sugestoes,
        IAssistenteIA ia, ICotaAssistente cota, IUnitOfWork uow, IOptions<AssistenteOptions> opcoes, ILogger<PreferenciasHandlers> logger)
    {
        _preferencias = preferencias;
        _sugestoes = sugestoes;
        _ia = ia;
        _cota = cota;
        _uow = uow;
        _opcoes = opcoes;
        _logger = logger;
    }

    public async Task<StatusAssistenteDto> Handle(GetStatusAssistenteQuery r, CancellationToken ct)
    {
        var habilitado = _opcoes.Value.HabilitadoPara(r.EscritorioId);
        var pref = await _preferencias.GetByUsuarioAsync(r.UsuarioId, ct);
        var bloqueadas = habilitado ? await _sugestoes.GetChavesBloqueadasAsync(r.UsuarioId, DateTime.UtcNow, ct) : new List<string>();
        StatusCotaDto cota;
        try { cota = await _cota.ObterAsync(r.UsuarioId, r.EscritorioId, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Não foi possível obter a cota da Ori.");
            cota = new StatusCotaDto(true, 0, 0, 0, 0, null);
        }
        return new StatusAssistenteDto(habilitado, habilitado && _ia.EstaHabilitado(RotaIa.Conversa),
            pref?.DicasSilenciadas ?? false, bloqueadas, cota);
    }

    public async Task<PreferenciasOriDto> Handle(GetPreferenciasOriQuery r, CancellationToken ct) =>
        new((await _preferencias.GetByUsuarioAsync(r.UsuarioId, ct))?.DicasSilenciadas ?? false);

    public async Task<PreferenciasOriDto> Handle(SalvarPreferenciasOriCommand r, CancellationToken ct)
    {
        var pref = await _preferencias.GetByUsuarioAsync(r.UsuarioId, ct);
        if (pref == null)
        {
            pref = PreferenciaAssistente.Criar(r.UsuarioId);
            await _preferencias.AddAsync(pref, ct);
        }
        pref.DefinirDicasSilenciadas(r.Dto.DicasSilenciadas);
        await _uow.SaveChangesAsync(ct);
        return new PreferenciasOriDto(pref.DicasSilenciadas);
    }

    public async Task<bool> Handle(IgnorarSugestaoCommand r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Dto.Chave) || r.Dto.Chave.Length > 200) return false;
        var s = await _sugestoes.GetAsync(r.UsuarioId, r.Dto.Chave.Trim(), ct);
        if (s == null)
        {
            s = SugestaoDispensada.Criar(r.UsuarioId, r.EmpresaId, r.Dto.Chave);
            await _sugestoes.AddAsync(s, ct);
        }
        if (r.Dto.Definitivo) s.Dispensar(); else s.RegistrarIgnorada(DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>
/// Regras anti-intrusão dos balões da Ori (MASCOTE_UX.md §5), puras e testáveis; usadas pela WebUI.
/// </summary>
public class RegrasDicaOri
{
    public static readonly TimeSpan IntervaloMinimo = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan SilencioDigitacao = TimeSpan.FromSeconds(4);
    public const int MaximoPorSessao = 3;

    private readonly Func<DateTime> _agora;
    private DateTime? _ultimaDica;
    private int _mostradasNaSessao;

    public RegrasDicaOri(Func<DateTime>? agora = null) => _agora = agora ?? (() => DateTime.UtcNow);

    public int MostradasNaSessao => _mostradasNaSessao;

    /// <summary>Decide se uma dica pode aparecer agora; se puder, registra a exibição.</summary>
    public bool PodeMostrar(string chave, IReadOnlyCollection<string> chavesBloqueadas, bool dicasSilenciadas,
        DateTime? ultimaTecla, bool modalAberto, bool painelAberto)
    {
        var agora = _agora();
        if (dicasSilenciadas || painelAberto || modalAberto) return false;
        if (chavesBloqueadas.Contains(chave)) return false;
        if (_mostradasNaSessao >= MaximoPorSessao) return false;
        if (_ultimaDica is { } u && agora - u < IntervaloMinimo) return false;
        if (ultimaTecla is { } t && agora - t < SilencioDigitacao) return false;
        _ultimaDica = agora;
        _mostradasNaSessao++;
        return true;
    }
}
