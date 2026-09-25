using Microsoft.JSInterop;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Preferencias;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

/// <summary>
/// Controle da Ori na tela (MASCOTE_UX.md §4–§5): estado da animação, painel, balões com regras anti-intrusão
/// e ponte com o ori.js (foco, atalho, atividade para o cochilo).
/// </summary>
public class OriControle : IOriControle, IAsyncDisposable
{
    private static readonly TimeSpan TempoCochilo = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan DuracaoBalao = TimeSpan.FromSeconds(12);

    private readonly IJSRuntime _js;
    private readonly IPreferenciasOriApi _prefs;
    private readonly IContextoAssistente _contexto;
    private readonly Microsoft.AspNetCore.Components.NavigationManager _nav;
    private readonly RegrasDicaOri _regras = new();
    private DotNetObjectReference<OriControle>? _ref;
    private Timer? _timerTransitorio, _timerOcio, _timerBalao;
    private bool _iniciado;

    public OriControle(IJSRuntime js, IPreferenciasOriApi prefs, IContextoAssistente contexto,
        Microsoft.AspNetCore.Components.NavigationManager nav)
    {
        _nav = nav;
        _js = js;
        _prefs = prefs;
        _contexto = contexto;
        _contexto.OnFocoAlterado += AoFocar;
    }

    public EstadoOri Estado { get; private set; } = EstadoOri.Cafe;
    public bool PainelAberto { get; private set; }
    public PedidoOri? PedidoAtual { get; private set; }
    public StatusAssistenteDto? Status { get; private set; }
    public DicaOri? DicaAtual { get; private set; }
    public bool PontoDica { get; private set; }
    public bool Habilitada => Status?.Habilitado == true;
    public event Action? OnChange;

    private EstadoOri EstadoBase =>
        Status?.IaHabilitada == true && Status.Cota.Permitido == false ? EstadoOri.Dormindo
        : Status?.DicasSilenciadas == true ? EstadoOri.Silenciada
        : EstadoOri.Cafe;

    public async Task IniciarAsync()
    {
        if (_iniciado) return;
        _iniciado = true;
        Status = await _prefs.StatusAsync();
        Estado = EstadoBase;
        if (!Habilitada) { Notificar(); return; }
        _ref = DotNetObjectReference.Create(this);
        try { await _js.InvokeVoidAsync("ori.iniciar", _ref, CamposOri.Sensiveis.ToArray()); } catch (JSException) { }
        ReiniciarOcio();
        Notificar();
    }

    public async Task AtualizarStatusAsync()
    {
        Status = await _prefs.StatusAsync() ?? Status;
        if (Estado is EstadoOri.Cafe or EstadoOri.Dormindo or EstadoOri.Silenciada) Estado = EstadoBase;
        Notificar();
    }

    public void DefinirEstado(EstadoOri estado)
    {
        _timerTransitorio?.Dispose();
        Estado = estado;
        var volta = estado switch
        {
            EstadoOri.Atenta => TimeSpan.FromSeconds(8),
            EstadoOri.Falando => TimeSpan.FromSeconds(2),
            EstadoOri.Preocupada => TimeSpan.FromSeconds(5),
            EstadoOri.Comemorando => TimeSpan.FromSeconds(3),
            _ => (TimeSpan?)null
        };
        if (volta is { } t)
            _timerTransitorio = new Timer(_ => { Estado = PainelAberto ? EstadoOri.Atenta : EstadoBase; Notificar(); }, null, t, Timeout.InfiniteTimeSpan);
        Notificar();
    }

    public void AbrirPainel(PedidoOri? pedido = null)
    {
        PedidoAtual = pedido;
        PainelAberto = true;
        PontoDica = false;
        FecharBalao();
        DefinirEstado(EstadoOri.Atenta);
    }

    public void FecharPainel()
    {
        PainelAberto = false;
        PedidoAtual = null;
        _timerTransitorio?.Dispose();
        Estado = EstadoBase;
        ReiniciarOcio();
        Notificar();
    }

    public bool MostrarDica(DicaOri dica)
    {
        if (!Habilitada || DicaAtual != null) return false;
        var inProcess = _js as IJSInProcessRuntime;
        var msTecla = inProcess?.Invoke<double>("ori.msDesdeUltimaTecla") ?? double.MaxValue;
        var modal = inProcess?.Invoke<bool>("ori.modalAberto") ?? false;
        var bloqueadas = Status?.ChavesBloqueadas ?? new List<string>();
        var ultimaTecla = msTecla > 1e8 ? (DateTime?)null : DateTime.UtcNow.AddMilliseconds(-msTecla);
        if (!_regras.PodeMostrar(dica.Chave, bloqueadas, Status?.DicasSilenciadas == true, ultimaTecla, modal, PainelAberto))
            return false;

        DicaAtual = dica;
        _timerBalao?.Dispose();
        _timerBalao = new Timer(_ => ExpirarBalao(), null, DuracaoBalao, Timeout.InfiniteTimeSpan);
        Notificar();
        return true;
    }

    /// <summary>"Mostrar" no balão.</summary>
    public void AbrirDica()
    {
        var dica = DicaAtual;
        FecharBalao();
        if (dica?.AoMostrar is { Hipotese: "rota", TextoInicial: { } rota } && rota.StartsWith('/'))
        {
            Notificar();
            _nav.NavigateTo(rota);
            return;
        }
        AbrirPainel(dica?.AoMostrar ?? new PedidoOri("livre", TextoInicial: dica?.Texto));
    }

    /// <summary>"✕" no balão: essa dica não volta.</summary>
    public async Task DispensarDicaAsync()
    {
        if (DicaAtual is not { } dica) return;
        FecharBalao();
        Status?.ChavesBloqueadas.Add(dica.Chave);
        await _prefs.IgnorarAsync(dica.Chave, definitivo: true);
        Notificar();
    }

    public async Task SilenciarDicasAsync(bool silenciar)
    {
        await _prefs.SalvarAsync(new PreferenciasOriDto(silenciar));
        await AtualizarStatusAsync();
    }

    public void Notificar(string evento)
    {
        switch (evento)
        {
            case "nota_rejeitada":
            case "erro_api":
                DefinirEstado(EstadoOri.Preocupada);
                break;
            case "nota_autorizada_primeira":
            case "marco_notas":
                DefinirEstado(EstadoOri.Comemorando);
                break;
        }
    }

    // ------------------------------------------------------------------ ponte JS

    [JSInvokable]
    public void OriFoco(string campo, string? rotulo, string? valor, int? item)
    {
        _contexto.RegistrarFoco(campo, rotulo, valor, item);
    }

    [JSInvokable]
    public void OriAtalho()
    {
        if (PainelAberto) FecharPainel(); else AbrirPainel();
    }

    [JSInvokable]
    public void OriEscape()
    {
        if (PainelAberto) FecharPainel();
    }

    [JSInvokable]
    public void OriAtividade()
    {
        if (Estado == EstadoOri.Cochilando) { Estado = EstadoBase; Notificar(); }
        ReiniciarOcio();
    }

    private void AoFocar()
    {
        var campo = _contexto.FocoAtual?.Campo;
        if (!PainelAberto && campo != null && CamposOri.Fiscais.Contains(campo) && Estado is EstadoOri.Cafe or EstadoOri.Cochilando)
            DefinirEstado(EstadoOri.Atenta);
    }

    private void ReiniciarOcio()
    {
        _timerOcio?.Dispose();
        _timerOcio = new Timer(_ =>
        {
            if (!PainelAberto && Estado == EstadoOri.Cafe) { Estado = EstadoOri.Cochilando; Notificar(); }
        }, null, TempoCochilo, Timeout.InfiniteTimeSpan);
    }

    private void ExpirarBalao()
    {
        if (DicaAtual is not { } dica) return;
        FecharBalao();
        PontoDica = true;
        Notificar();
        _ = _prefs.IgnorarAsync(dica.Chave, definitivo: false);
    }

    private void FecharBalao()
    {
        _timerBalao?.Dispose();
        DicaAtual = null;
    }

    private void Notificar() => OnChange?.Invoke();

    public async ValueTask DisposeAsync()
    {
        _contexto.OnFocoAlterado -= AoFocar;
        _timerTransitorio?.Dispose();
        _timerOcio?.Dispose();
        _timerBalao?.Dispose();
        if (_ref != null)
        {
            try { await _js.InvokeVoidAsync("ori.parar"); } catch (JSException) { } catch (JSDisconnectedException) { }
            _ref.Dispose();
        }
    }
}
