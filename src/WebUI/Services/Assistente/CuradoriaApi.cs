namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P7.
public interface ICuradoriaApi { }

public class CuradoriaApi : ICuradoriaApi
{
    private readonly ApiClient _api;
    public CuradoriaApi(ApiClient api) => _api = api;
}
