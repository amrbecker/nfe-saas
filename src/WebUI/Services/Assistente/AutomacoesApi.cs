namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P5.
public interface IAutomacoesApi { }

public class AutomacoesApi : IAutomacoesApi
{
    private readonly ApiClient _api;
    public AutomacoesApi(ApiClient api) => _api = api;
}
