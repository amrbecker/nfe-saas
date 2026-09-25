using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

public interface ITelemetriaService
{
    void Iniciar();
    void Registrar(string tipo, string? tela = null, object? dados = null);
}

/// <summary>
/// Telemetria de produto (PLANO_ACAO.md 0.1): eventos semânticos em fila, enviados em lote a cada 30 s ou 20 eventos.
/// Nunca envia valores de campos — só tipo, tela e metadados sem dado pessoal (o servidor filtra de novo).
/// </summary>
public class TelemetriaService : ITelemetriaService, IDisposable
{
    private const int TamanhoLote = 20;
    private readonly ICsApi _cs;
    private readonly NavigationManager _nav;
    private readonly List<EventoProdutoDto> _fila = new();
    private Timer? _timer;
    private bool _iniciado;

    public TelemetriaService(ICsApi cs, NavigationManager nav)
    {
        _cs = cs;
        _nav = nav;
    }

    public void Iniciar()
    {
        if (_iniciado) return;
        _iniciado = true;
        _nav.LocationChanged += AoNavegar;
        _timer = new Timer(_ => _ = EnviarAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        Registrar("tela_aberta", ContextoAssistente.TelaDaRota(_nav.Uri));
    }

    public void Registrar(string tipo, string? tela = null, object? dados = null)
    {
        _fila.Add(new EventoProdutoDto(tipo, tela ?? ContextoAssistente.TelaDaRota(_nav.Uri),
            dados == null ? null : JsonSerializer.Serialize(dados), DateTime.UtcNow));
        if (_fila.Count >= TamanhoLote) _ = EnviarAsync();
    }

    private void AoNavegar(object? sender, LocationChangedEventArgs e) => Registrar("tela_aberta", ContextoAssistente.TelaDaRota(e.Location));

    private async Task EnviarAsync()
    {
        if (_fila.Count == 0) return;
        var lote = _fila.ToList();
        _fila.Clear();
        await _cs.EnviarEventosAsync(new LoteEventosDto(lote));
    }

    public void Dispose()
    {
        _nav.LocationChanged -= AoNavegar;
        _timer?.Dispose();
    }
}
