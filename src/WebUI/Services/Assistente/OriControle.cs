namespace NfeSaas.WebUI.Services.Assistente;

// STUB — implementado pelo pacote P4a.
public class OriControle : IOriControle
{
    public EstadoOri Estado { get; private set; } = EstadoOri.Cafe;
    public bool PainelAberto { get; private set; }
    public PedidoOri? PedidoAtual { get; private set; }
    public event Action? OnChange;
    public void DefinirEstado(EstadoOri estado) { Estado = estado; OnChange?.Invoke(); }
    public void AbrirPainel(PedidoOri? pedido = null) { PedidoAtual = pedido; PainelAberto = true; OnChange?.Invoke(); }
    public void FecharPainel() { PainelAberto = false; OnChange?.Invoke(); }
    public bool MostrarDica(DicaOri dica) => false;
    public void Notificar(string evento) { }
}
