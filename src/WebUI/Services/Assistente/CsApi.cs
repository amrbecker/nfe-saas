using NfeSaas.Application.Assistente.Cs;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

public interface ICsApi
{
    Task<List<AlertaCsDto>> AlertasAsync();
    Task MarcarAlertaAsync(Guid id, bool dispensar);
    Task<List<LembreteDto>> LembretesAsync();
    Task<string?> CriarLembreteAsync(CriarLembreteDto dto);
    Task<string?> AtualizarLembreteAsync(Guid id, CriarLembreteDto dto, bool? ativo);
    Task<bool> ExcluirLembreteAsync(Guid id);
    Task EnviarEventosAsync(LoteEventosDto lote);
    Task<List<SaudeEscritorioDto>> SaudeAsync();
}

public class CsApi : ICsApi
{
    private readonly ApiClient _api;
    public CsApi(ApiClient api) => _api = api;

    public async Task<List<AlertaCsDto>> AlertasAsync()
    {
        try { return await _api.GetAsync<List<AlertaCsDto>>("api/assistente/alertas") ?? new(); }
        catch (HttpRequestException) { return new(); }
    }

    public async Task MarcarAlertaAsync(Guid id, bool dispensar)
    {
        try { await _api.PostRawAsync($"api/assistente/alertas/{id}/{(dispensar ? "dispensar" : "visto")}", new { }); }
        catch (HttpRequestException) { }
    }

    public async Task<List<LembreteDto>> LembretesAsync()
    {
        try { return await _api.GetAsync<List<LembreteDto>>("api/assistente/lembretes") ?? new(); }
        catch (HttpRequestException) { return new(); }
    }

    public async Task<string?> CriarLembreteAsync(CriarLembreteDto dto)
    {
        var r = await _api.PostRawAsync("api/assistente/lembretes", dto);
        return r.IsSuccessStatusCode ? null : await ApiHelper.ExtrairMensagemErro(r);
    }

    public async Task<string?> AtualizarLembreteAsync(Guid id, CriarLembreteDto dto, bool? ativo)
    {
        var r = await _api.PutRawAsync($"api/assistente/lembretes/{id}{(ativo is { } a ? $"?ativo={a.ToString().ToLowerInvariant()}" : "")}", dto);
        return r.IsSuccessStatusCode ? null : await ApiHelper.ExtrairMensagemErro(r);
    }

    public async Task<bool> ExcluirLembreteAsync(Guid id) => (await _api.DeleteAsync($"api/assistente/lembretes/{id}")).IsSuccessStatusCode;

    public async Task EnviarEventosAsync(LoteEventosDto lote)
    {
        try { await _api.PostRawAsync("api/eventos", lote); }
        catch (HttpRequestException) { /* telemetria nunca atrapalha o uso */ }
    }

    public async Task<List<SaudeEscritorioDto>> SaudeAsync() =>
        await _api.GetAsync<List<SaudeEscritorioDto>>("api/interno/saude") ?? new();
}
