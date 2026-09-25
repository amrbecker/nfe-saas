namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P6.
public interface ICsApi { }

public class CsApi : ICsApi
{
    private readonly ApiClient _api;
    public CsApi(ApiClient api) => _api = api;
}
