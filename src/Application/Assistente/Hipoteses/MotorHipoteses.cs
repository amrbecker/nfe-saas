using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.Application.Assistente.Hipoteses;

public record HipoteseOri(string Tipo, string Texto, int Prioridade, ArtigoResumoDto? Artigo);

/// <summary>
/// Hipóteses da dúvida sem LLM (CAPTURA_CONTEXTO.md §4): regras sobre o contexto da tela, instantâneas e de custo zero.
/// Até 3 hipóteses + "Outra dúvida…".
/// </summary>
public static class MotorHipoteses
{
    public const int Maximo = 3;

    public static List<HipoteseOri> Gerar(ContextoTelaDto c, IReadOnlyList<ArtigoResumoDto> mapa)
    {
        var lista = new List<HipoteseOri>();
        ArtigoResumoDto? PorCampo(string? campo) => campo == null ? null
            : mapa.Where(a => a.Campos.Contains(campo, StringComparer.OrdinalIgnoreCase)).OrderBy(a => a.Categoria == "sistema" ? 1 : 0).FirstOrDefault();
        ArtigoResumoDto? PorTela(string tela) =>
            mapa.Where(a => a.Telas.Contains(tela, StringComparer.OrdinalIgnoreCase)).OrderBy(a => a.Categoria == "sistema" ? 0 : 1).FirstOrDefault();

        if (c.NotaId != null && string.Equals(c.Op?.Etapa, "Rejeitada", StringComparison.OrdinalIgnoreCase))
            lista.Add(new("rejeicao", "Por que esta nota foi rejeitada?", 100, null));

        if (c.Api?.Any(e => e.Status >= 500) == true)
            lista.Add(new("erro_sistema", "Parece um erro do sistema. Quer que eu registre?", 90, null));

        if (c.Erros is { Count: > 0 } erros)
        {
            var campo = erros[0].Campo;
            lista.Add(new("campo", $"O que está errado em {Nome(campo, c.Foco)}?", 80, PorCampo(campo)));
        }

        if (c.Foco is { } foco && PorCampo(foco.Campo) is { } artigoFoco && lista.All(h => h.Artigo?.Id != artigoFoco.Id))
            lista.Add(new("campo", $"Como preencher {foco.Rotulo ?? foco.Campo}?", 70, artigoFoco));

        if (c.Rastro?.Count(r => r.Contains("tentou emitir", StringComparison.OrdinalIgnoreCase)) >= 2)
            lista.Add(new("emissao_falhando", "Não consegue emitir? Vamos ver juntos.", 60, null));

        if (c.Tela == "certificado" && c.Api is { Count: > 0 })
            lista.Add(new("certificado", "Problema com o certificado?", 50, PorTela("certificado")));

        if (PorTela(c.Tela) is { } artigoTela && lista.All(h => h.Artigo?.Id != artigoTela.Id))
            lista.Add(new("tela", $"Como usar esta tela? — {artigoTela.Titulo}", 40, artigoTela));

        var escolhidas = lista.OrderByDescending(h => h.Prioridade).Take(Maximo).ToList();
        escolhidas.Add(new("livre", "Outra dúvida…", 0, null));
        return escolhidas;
    }

    private static string Nome(string campo, FocoDto? foco) =>
        foco?.Campo == campo && !string.IsNullOrWhiteSpace(foco.Rotulo) ? foco.Rotulo! : campo.Replace('_', ' ').ToUpperInvariant();
}

/// <summary>
/// Markdown mínimo e seguro para as respostas da Ori: escapa TODO o HTML primeiro e só então aplica títulos, negrito,
/// itálico, código, listas (aninhadas pela indentação; numeração continua mesmo com linha em branco entre os itens) e
/// parágrafos. Citações [fonte: id] são removidas do texto (aparecem como lista à parte).
/// </summary>
public static partial class MarkdownSeguro
{
    public static string ParaHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";
        var texto = RegexCitacao().Replace(markdown, "").Replace("\r\n", "\n").Replace("\t", "    ").Trim();
        var sb = new StringBuilder();
        var listas = new Stack<(int Indent, string Tipo)>();
        var paragrafo = new List<string>();

        void FecharParagrafo()
        {
            if (paragrafo.Count == 0) return;
            sb.Append("<p>").Append(string.Join("<br>", paragrafo)).Append("</p>");
            paragrafo.Clear();
        }
        void FecharLista() { var l = listas.Pop(); sb.Append($"</li></{l.Tipo}>"); }
        void FecharListas() { while (listas.Count > 0) FecharLista(); }

        foreach (var bruta in texto.Split('\n'))
        {
            var linha = bruta.TrimEnd();
            var indent = linha.Length - linha.TrimStart().Length;
            if (linha.Length == 0) { FecharParagrafo(); continue; }
            if (RegexSeparador().IsMatch(linha)) { FecharParagrafo(); FecharListas(); continue; }

            var mTitulo = RegexTitulo().Match(linha);
            if (mTitulo.Success)
            {
                FecharParagrafo();
                FecharListas();
                sb.Append("<h4>").Append(Inline(mTitulo.Groups["t"].Value)).Append("</h4>");
                continue;
            }

            var mItem = RegexItem().Match(linha);
            var mNum = RegexNumerado().Match(linha);
            if (mItem.Success || mNum.Success)
            {
                FecharParagrafo();
                var tipo = mItem.Success ? "ul" : "ol";
                while (listas.Count > 0 && listas.Peek().Indent > indent) FecharLista();
                if (listas.Count > 0 && listas.Peek().Indent == indent)
                {
                    if (listas.Peek().Tipo == tipo) sb.Append("</li>");
                    else FecharLista();
                }
                if (listas.Count == 0 || listas.Peek().Indent < indent)
                {
                    var inicio = mNum.Success && int.TryParse(mNum.Groups["n"].Value, out var n) && n != 1 ? $" start=\"{n}\"" : "";
                    sb.Append($"<{tipo}{inicio}>");
                    listas.Push((indent, tipo));
                }
                sb.Append("<li>").Append(Inline((mItem.Success ? mItem : mNum).Groups["t"].Value));
                continue;
            }

            // Texto indentado logo abaixo de um item continua dentro dele.
            if (listas.Count > 0 && indent > 0)
            {
                sb.Append("<br>").Append(Inline(linha.Trim()));
                continue;
            }
            FecharListas();
            paragrafo.Add(Inline(linha.Trim()));
        }
        FecharParagrafo();
        FecharListas();
        return sb.ToString();
    }

    private static string Inline(string texto)
    {
        var s = WebUtility.HtmlEncode(texto);
        s = RegexNegrito().Replace(s, "<strong>$1</strong>");
        s = RegexItalico().Replace(s, "<em>$1</em>");
        s = RegexCodigo().Replace(s, "<code>$1</code>");
        return s;
    }

    [GeneratedRegex(@"\s*\[fonte:\s*[^\]]+\]", RegexOptions.IgnoreCase)]
    private static partial Regex RegexCitacao();

    [GeneratedRegex(@"^\s*[-*•]\s+(?<t>.+)$")]
    private static partial Regex RegexItem();

    [GeneratedRegex(@"^\s*(?<n>\d+)[.)]\s+(?<t>.+)$")]
    private static partial Regex RegexNumerado();

    [GeneratedRegex(@"^\s*#{1,6}\s+(?<t>.+)$")]
    private static partial Regex RegexTitulo();

    [GeneratedRegex(@"^\s*([-*_])(\s*\1){2,}\s*$")]
    private static partial Regex RegexSeparador();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex RegexNegrito();

    [GeneratedRegex(@"(?<![*\w])\*(?!\s)(.+?)(?<!\s)\*(?![*\w])")]
    private static partial Regex RegexItalico();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex RegexCodigo();
}
