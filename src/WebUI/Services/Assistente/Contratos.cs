using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

// Contratos client-side da Ori. Donos: IContextoAssistente e IOriControle → pacote P4a;
// páginas (P5) implementam IContextoTela e chamam IOriControle. Ver docs/assistente/CAPTURA_CONTEXTO.md.

/// <summary>
/// Implementado por páginas-chave (EmitirNFe, NotaDetalhe, Produtos, Clientes, Empresa, Certificado).
/// A página se registra em OnInitialized (<see cref="IContextoAssistente.RegistrarTela"/>) e se remove no Dispose.
/// Deve devolver SIGNIFICADO (operação, etapa, erros), nunca valores de documento/nome/endereço.
/// </summary>
public interface IContextoTela
{
    ContextoTelaParcial ObterContexto();
}

public record ContextoTelaParcial(
    string Tela,
    OperacaoDto? Op = null,
    List<ErroCampoDto>? Erros = null,
    Guid? NotaId = null,
    DestinatarioAtributosDto? Dest = null);

/// <summary>Coleta o contexto da sessão (só em memória) e monta o <see cref="ContextoTelaDto"/> quando a Ori é chamada.</summary>
public interface IContextoAssistente
{
    void RegistrarTela(IContextoTela tela);
    void RemoverTela(IContextoTela tela);
    /// <summary>Ação semântica para o rastro (buffer circular), ex.: "adicionou item 2 (manual)".</summary>
    void RegistrarAcao(string descricao);
    /// <summary>Chamado pelo listener JS de foco (atributo data-ajuda). Valor já filtrado pela lista de bloqueio.</summary>
    void RegistrarFoco(string campo, string? rotulo, string? valor, int? item = null);
    void RegistrarErroApi(ErroApiDto erro);
    ContextoTelaDto Capturar();
    event Action? OnFocoAlterado;
    FocoDto? FocoAtual { get; }
}

public enum EstadoOri { Cafe, Cochilando, Atenta, Pensando, Falando, Preocupada, Comemorando, Dormindo, Silenciada }

/// <summary>Pedido para abrir o painel já numa hipótese (ex.: botão "Explicar rejeição" na NotaDetalhe).</summary>
public record PedidoOri(string Hipotese, Guid? NotaId = null, string? TextoInicial = null);

/// <summary>Dica silenciosa (balão de pensamento). <see cref="Chave"/> identifica a dica para "dispensar para sempre".</summary>
public record DicaOri(string Chave, string Texto, int Prioridade, PedidoOri? AoMostrar = null);

/// <summary>Controle da Ori na UI (estado da animação, painel, balões). Dono: P4a.</summary>
public interface IOriControle
{
    EstadoOri Estado { get; }
    bool PainelAberto { get; }
    PedidoOri? PedidoAtual { get; }
    event Action? OnChange;
    void DefinirEstado(EstadoOri estado);
    void AbrirPainel(PedidoOri? pedido = null);
    void FecharPainel();
    /// <summary>Exibe um balão respeitando as regras anti-intrusão (MASCOTE_UX.md §5). Retorna false se foi suprimido.</summary>
    bool MostrarDica(DicaOri dica);
    /// <summary>Evento de negócio para reações da Ori (ex.: "nota_autorizada_primeira", "nota_rejeitada").</summary>
    void Notificar(string evento);
}
