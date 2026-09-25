namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P4a.
public interface IPreferenciasOriApi { }

public class PreferenciasOriApi : IPreferenciasOriApi
{
    private readonly ApiClient _api;
    public PreferenciasOriApi(ApiClient api) => _api = api;
}
