using NfeSaas.Application.Assistente;

namespace NfeSaas.Infrastructure.Assistente;

// STUB — implementado pelo pacote P1 (base de conhecimento). Lê docs/assistente/kb/**/*.md embutidos.
public class BaseConhecimento : IBaseConhecimento
{
    public IReadOnlyList<ArtigoKb> Todos() => Array.Empty<ArtigoKb>();
    public IReadOnlyList<ArtigoKb> Utilizaveis() => Array.Empty<ArtigoKb>();
    public ArtigoKb? Obter(string id) => null;
    public IReadOnlyList<ArtigoKb> Rotear(CriterioRoteamentoKb criterio, int maximo = 3) => Array.Empty<ArtigoKb>();
    public string? ExtrairCodigoRejeicao(string? motivoRejeicao) => null;
}
