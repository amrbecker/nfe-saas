using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P4a.
public class ContextoAssistente : IContextoAssistente
{
    public event Action? OnFocoAlterado;
    public FocoDto? FocoAtual => null;
    public void RegistrarTela(IContextoTela tela) { }
    public void RemoverTela(IContextoTela tela) { }
    public void RegistrarAcao(string descricao) { }
    public void RegistrarFoco(string campo, string? rotulo, string? valor, int? item = null) => OnFocoAlterado?.Invoke();
    public void RegistrarErroApi(ErroApiDto erro) { }
    public ContextoTelaDto Capturar() => new("desconhecida");
}
