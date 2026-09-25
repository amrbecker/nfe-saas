namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P6: acumula EventoProdutoDto (navegação, emissões) e envia em lote.
public interface ITelemetriaService { }

public class TelemetriaService : ITelemetriaService
{
    private readonly ICsApi _cs;
    public TelemetriaService(ICsApi cs) => _cs = cs;
}
