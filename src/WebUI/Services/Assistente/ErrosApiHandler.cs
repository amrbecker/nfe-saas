namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P4a: registra no IContextoAssistente os últimos erros HTTP (status ≥ 400)
// devolvidos pela API (rota, status, codigo, message) para o contexto da Ori.
public class ErrosApiHandler : DelegatingHandler
{
    private readonly IContextoAssistente _contexto;
    public ErrosApiHandler(IContextoAssistente contexto) => _contexto = contexto;
}
