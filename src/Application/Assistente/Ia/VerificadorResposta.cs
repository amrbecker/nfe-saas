using System.Text.RegularExpressions;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Application.Assistente.Ia;

public record ResultadoVerificacao(
    bool Aprovada,
    IReadOnlyList<string> ItensNaoFundamentados,
    IReadOnlyList<string> CitacoesInvalidas,
    IReadOnlyList<ArtigoKb> ArtigosCitados,
    NivelFonte Selo);

/// <summary>
/// Guarda-corpo determinístico (CONTEXTO_AGENTE.md §3.3): todo número/código/data da resposta precisa estar nos
/// artigos citados, nos resultados de ferramentas ou no que o próprio usuário informou; toda citação precisa existir.
/// O selo é calculado aqui, nunca pelo modelo.
/// </summary>
public static partial class VerificadorResposta
{
    public const string RespostaSemFonte =
        "Não encontrei base oficial para responder com segurança. Você pode confirmar no Portal Nacional da NF-e " +
        "(www.nfe.fazenda.gov.br) ou com a SEFAZ da sua UF. Registrei a dúvida para a nossa equipe.";

    public static ResultadoVerificacao Verificar(
        string resposta,
        IBaseConhecimento baseConhecimento,
        IEnumerable<string> textosPermitidos)
    {
        var utilizaveis = baseConhecimento.Utilizaveis();
        var citados = new List<ArtigoKb>();
        var citacoesInvalidas = new List<string>();
        foreach (Match m in RegexCitacao().Matches(resposta))
        {
            var id = m.Groups["id"].Value.Trim();
            var artigo = utilizaveis.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
            if (artigo == null) citacoesInvalidas.Add(id);
            else if (!citados.Contains(artigo)) citados.Add(artigo);
        }

        var permitidos = string.Join("\n", textosPermitidos.Concat(citados.SelectMany(a => new[] { a.Titulo, a.ResumoCurto, a.Corpo })));
        var tokensPermitidos = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in RegexNumero().Matches(permitidos))
        {
            var t = m.Value.TrimEnd('%', '.', ',');
            tokensPermitidos.Add(t);
            tokensPermitidos.Add(SoDigitos(t));
        }

        var semFundamento = new List<string>();
        var respostaSemCitacoes = RegexCitacao().Replace(resposta, "");
        respostaSemCitacoes = RegexMarcador().Replace(respostaSemCitacoes, "");
        foreach (Match m in RegexNumero().Matches(respostaSemCitacoes))
        {
            var bruto = m.Value.TrimEnd('%', '.', ',');
            var digitos = SoDigitos(bruto);
            if (digitos.Length < 2) continue; // numeração de listas, "5 ou 6"
            if (tokensPermitidos.Contains(bruto) || tokensPermitidos.Contains(digitos))
                continue;
            if (!semFundamento.Contains(bruto)) semFundamento.Add(bruto);
        }

        return new ResultadoVerificacao(
            semFundamento.Count == 0 && citacoesInvalidas.Count == 0,
            semFundamento, citacoesInvalidas, citados, CalcularSelo(citados));
    }

    /// <summary>Nível mais fraco entre os artigos fiscais citados; só artigos de sistema → GuiaDoSistema; nada → N4.</summary>
    public static NivelFonte CalcularSelo(IReadOnlyCollection<ArtigoKb> citados)
    {
        var fiscais = citados.Where(a => a.NivelFonte != NivelFonte.GuiaDoSistema).ToList();
        if (fiscais.Count > 0) return fiscais.Max(a => a.NivelFonte);
        return citados.Count > 0 ? NivelFonte.GuiaDoSistema : NivelFonte.N4SemFonte;
    }

    public static string InstrucaoCorrecao(ResultadoVerificacao r)
    {
        var partes = new List<string>();
        if (r.ItensNaoFundamentados.Count > 0)
            partes.Add($"remova ou fundamente com os artigos fornecidos: {string.Join(", ", r.ItensNaoFundamentados)}");
        if (r.CitacoesInvalidas.Count > 0)
            partes.Add($"estas citações não existem na base e devem ser removidas: {string.Join(", ", r.CitacoesInvalidas)}");
        return "Revise a resposta anterior: " + string.Join("; ", partes) + ". Responda de novo, completa.";
    }

    private static string SoDigitos(string s) => RegexNaoDigito().Replace(s, "");

    [GeneratedRegex(@"\[fonte:\s*(?<id>[^\]]+)\]", RegexOptions.IgnoreCase)]
    public static partial Regex RegexCitacao();

    [GeneratedRegex(@"\[[A-Z_]+_\d+\]")]
    private static partial Regex RegexMarcador();

    [GeneratedRegex(@"\d[\d.,/]*%?")]
    private static partial Regex RegexNumero();

    [GeneratedRegex(@"\D")]
    private static partial Regex RegexNaoDigito();
}
