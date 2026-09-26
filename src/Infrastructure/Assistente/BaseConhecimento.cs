using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NfeSaas.Application.Assistente;

namespace NfeSaas.Infrastructure.Assistente;

/// <summary>
/// Base de conhecimento da Ori: artigos de docs/assistente/kb/**.md embutidos no assembly (LogicalName "kb/...").
/// Carregada uma vez; roteamento determinístico, sem LLM (PESQUISA_REFINAMENTO.md §2.4, E2/E3).
/// </summary>
public partial class BaseConhecimento : IBaseConhecimento
{
    private static readonly HashSet<string> StatusPublicaveis = new() { "publicado", "em_revisao" };
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "o", "as", "os", "de", "da", "do", "das", "dos", "e", "em", "no", "na", "nos", "nas", "um", "uma",
        "para", "por", "com", "que", "qual", "quais", "como", "se", "ao", "aos", "minha", "meu", "nota", "nfe",
        "é", "e", "eu", "ou", "mais", "sobre", "isso", "esta", "este", "essa", "esse", "porque", "pq", "foi",
        "posso", "pode", "preciso", "quero", "fazer", "faco", "depois", "antes", "cliente", "clientes", "empresa", "notas",
        "tenho", "tem", "ter", "sim", "nao", "ele", "ela", "meu", "minha", "dele", "dela", "agora", "hoje", "ainda"
    };

    private readonly Lazy<IReadOnlyList<ArtigoKb>> _artigos;
    private readonly IOptions<AssistenteOptions> _opcoes;

    public BaseConhecimento(IOptions<AssistenteOptions> opcoes, ILogger<BaseConhecimento> logger)
        : this(opcoes, () => CarregarEmbutidos(logger)) { }

    /// <summary>Construtor para testes: artigos fornecidos diretamente.</summary>
    public BaseConhecimento(IOptions<AssistenteOptions> opcoes, Func<IReadOnlyList<ArtigoKb>> fonte)
    {
        _opcoes = opcoes;
        _artigos = new Lazy<IReadOnlyList<ArtigoKb>>(fonte);
    }

    public IReadOnlyList<ArtigoKb> Todos() => _artigos.Value;

    public IReadOnlyList<ArtigoKb> Utilizaveis()
    {
        var incluirRascunhos = _opcoes.Value.IncluirRascunhosKb;
        return _artigos.Value.Where(a => StatusPublicaveis.Contains(a.Status)
            || (incluirRascunhos && a.Status is "rascunho" or "revisado")).ToList();
    }

    public ArtigoKb? Obter(string id) =>
        _artigos.Value.FirstOrDefault(a => string.Equals(a.Id, id?.Trim('/'), StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ArtigoKb> Rotear(CriterioRoteamentoKb criterio, int maximo = 3)
    {
        var candidatos = Utilizaveis();
        var escolhidos = new List<ArtigoKb>();
        void Adicionar(IEnumerable<ArtigoKb> artigos)
        {
            foreach (var a in artigos)
            {
                if (escolhidos.Count >= maximo) return;
                if (!escolhidos.Contains(a)) escolhidos.Add(a);
            }
        }

        if (criterio.CodigosRejeicao is { Count: > 0 } codigos)
            Adicionar(candidatos.Where(a => a.CodigosRejeicao.Any(c => codigos.Contains(c.TrimStart('0')) || codigos.Contains(c))));
        if (!string.IsNullOrWhiteSpace(criterio.Campo))
            Adicionar(candidatos.Where(a => a.CamposRelacionados.Contains(criterio.Campo, StringComparer.OrdinalIgnoreCase))
                .OrderBy(a => a.Categoria == "sistema" ? 1 : 0));
        if (!string.IsNullOrWhiteSpace(criterio.Termo))
        {
            var termos = Tokenizar(criterio.Termo);
            if (termos.Count > 0)
            {
                Adicionar(candidatos
                    .Select(a => (a, pontos: Pontuar(a, termos)))
                    .Where(x => x.pontos > 0)
                    .OrderByDescending(x => x.pontos)
                    .Select(x => x.a));
            }
        }
        if (!string.IsNullOrWhiteSpace(criterio.Tela))
            Adicionar(candidatos.Where(a => a.Telas.Contains(criterio.Tela, StringComparer.OrdinalIgnoreCase))
                .OrderBy(a => a.Categoria == "sistema" ? 0 : 1));

        return escolhidos;
    }

    public string? ExtrairCodigoRejeicao(string? motivoRejeicao)
    {
        if (string.IsNullOrWhiteSpace(motivoRejeicao)) return null;
        // Formato gravado pelo SefazService: "[778] Rejeição: Informado NCM inexistente".
        var m = RegexCodigo().Match(motivoRejeicao);
        return m.Success ? m.Groups["c"].Value : null;
    }

    [GeneratedRegex(@"^\s*\[(?<c>\d{3})\]|(?:rejei[cç][aã]o|cstat)\s*:?\s*(?<c>\d{3})\b|^\s*(?<c>\d{3})\s*[-–:]", RegexOptions.IgnoreCase)]
    private static partial Regex RegexCodigo();

    // ------------------------------------------------------------------ roteamento por termos

    private static int Pontuar(ArtigoKb a, IReadOnlyCollection<string> termos)
    {
        var titulo = Normalizar(a.Titulo);
        var resumo = Normalizar(a.ResumoCurto);
        var corpo = Normalizar(a.Corpo);
        var pontos = 0;
        foreach (var termo in termos)
        {
            // Radical simples: "cancelar" e "cancelamento" batem pelo prefixo de 6 letras.
            var t = termo.Length > 6 ? termo[..6] : termo;
            if (titulo.Contains(t)) pontos += 5;
            if (resumo.Contains(t)) pontos += 3;
            if (a.CamposRelacionados.Any(c => c.Equals(termo, StringComparison.OrdinalIgnoreCase))) pontos += 4;
            if (corpo.Contains(t)) pontos += 1;
        }
        return pontos;
    }

    internal static IReadOnlyCollection<string> Tokenizar(string texto) =>
        Regex.Split(Normalizar(texto), @"[^a-z0-9_]+")
            .Where(t => t.Length >= 3 && !StopWords.Contains(t) && !(t.Contains('_') && t.Any(char.IsDigit)))
            .Distinct()
            .ToList();

    internal static string Normalizar(string texto)
    {
        var decomposto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var ch in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // ------------------------------------------------------------------ carga

    private static IReadOnlyList<ArtigoKb> CarregarEmbutidos(ILogger logger)
    {
        var assembly = typeof(BaseConhecimento).Assembly;
        var artigos = new List<ArtigoKb>();
        foreach (var nome in assembly.GetManifestResourceNames()
                     .Where(n => n.Replace('\\', '/').StartsWith("kb/", StringComparison.OrdinalIgnoreCase)
                              && n.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(nome)!;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                artigos.Add(BaseConhecimentoParser.Parse(nome, reader.ReadToEnd()));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Artigo da base de conhecimento ignorado: {Recurso}", nome);
            }
        }
        logger.LogInformation("Base de conhecimento da Ori carregada: {Total} artigos.", artigos.Count);
        return artigos;
    }

    /// <summary>Todos os recursos "kb/*.md" embutidos, com o caminho lógico — para validação em testes.</summary>
    public static IEnumerable<(string Caminho, string Texto)> RecursosEmbutidos()
    {
        var assembly = typeof(BaseConhecimento).Assembly;
        foreach (var nome in assembly.GetManifestResourceNames()
                     .Where(n => n.Replace('\\', '/').StartsWith("kb/", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = assembly.GetManifestResourceStream(nome)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            yield return (nome.Replace('\\', '/'), reader.ReadToEnd());
        }
    }
}
