namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P4b.
public interface IOriApi { }

public class OriApi : IOriApi
{
    private readonly ApiClient _api;
    public OriApi(ApiClient api) => _api = api;
}
