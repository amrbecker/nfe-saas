using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

public interface IPreferenciasOriApi
{
    Task<StatusAssistenteDto?> StatusAsync();
    Task<PreferenciasOriDto?> SalvarAsync(PreferenciasOriDto dto);
    Task IgnorarAsync(string chave, bool definitivo);
}

public class PreferenciasOriApi : IPreferenciasOriApi
{
    private readonly ApiClient _api;
    public PreferenciasOriApi(ApiClient api) => _api = api;

    public async Task<StatusAssistenteDto?> StatusAsync()
    {
        try { return await _api.GetAsync<StatusAssistenteDto>("api/assistente/status"); }
        catch (HttpRequestException) { return null; }
    }

    public Task<PreferenciasOriDto?> SalvarAsync(PreferenciasOriDto dto) =>
        _api.PutAsync<PreferenciasOriDto, PreferenciasOriDto>("api/assistente/preferencias", dto);

    public async Task IgnorarAsync(string chave, bool definitivo)
    {
        try { await _api.PostRawAsync("api/assistente/sugestoes/ignorar", new SugestaoIgnoradaDto(chave, definitivo)); }
        catch (HttpRequestException) { /* não bloqueia a UI */ }
    }
}
