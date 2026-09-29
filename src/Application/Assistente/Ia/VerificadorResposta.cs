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
        // Citação sem o prefixo ("[sistema/cadastro-produtos]") conta quando o id existe na base.
        foreach (Match m in RegexCitacaoSemPrefixo().Matches(resposta))
        {
            var artigo = utilizaveis.FirstOrDefault(a => string.Equals(a.Id, m.Groups["id"].Value, StringComparison.OrdinalIgnoreCase));
            if (artigo != null && !citados.Contains(artigo)) citados.Add(artigo);
        }
        foreach (Match m in RegexCitacao().Matches(resposta))
        {
            // O modelo às vezes junta várias fontes num colchete só: [fonte: a; b].
            foreach (var id in m.Groups["id"].Value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var artigo = utilizaveis.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
                if (artigo == null) citacoesInvalidas.Add(id);
                else if (!citados.Contains(artigo)) citados.Add(artigo);
            }
        }

        var permitidos = string.Join("\n", textosPermitidos.Concat(citados.SelectMany(a => new[] { a.Titulo, a.ResumoCurto, a.Corpo })));
        var tokensPermitidos = new HashSet<string>(ConstantesDoDominio, StringComparer.Ordinal);
        foreach (Match m in RegexNumero().Matches(permitidos))
        {
            var t = m.Value.TrimEnd('%', '.', ',');
            tokensPermitidos.Add(t);
            tokensPermitidos.Add(SoDigitos(t));
            foreach (var parte in t.Split('/', StringSplitOptions.RemoveEmptyEntries)) tokensPermitidos.Add(parte);
        }

        var semFundamento = new List<string>();
        var respostaSemCitacoes = RegexCitacao().Replace(resposta, "");
        respostaSemCitacoes = RegexCitacaoSemPrefixo().Replace(respostaSemCitacoes, "");
        respostaSemCitacoes = RegexMarcador().Replace(respostaSemCitacoes, "");
        foreach (Match m in RegexNumero().Matches(respostaSemCitacoes))
        {
            var bruto = m.Value.TrimEnd('%', '.', ',', '/');
            if (Fundamentado(bruto, tokensPermitidos)) continue;
            // "55/65", "301/302": cada lado é um número próprio. Data dd/mm/aaaa vale se a fonte a traz em ISO (data_hoje).
            if (RegexData().Match(bruto) is { Success: true } d
                && permitidos.Contains($"{d.Groups["a"].Value}-{d.Groups["m"].Value}-{d.Groups["d"].Value}")) continue;
            if (bruto.Contains('/') && !RegexData().IsMatch(bruto)
                && bruto.Split('/', StringSplitOptions.RemoveEmptyEntries).All(parte => Fundamentado(parte, tokensPermitidos))) continue;
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

    /// <summary>Números que identificam o próprio documento (modelo 55 = NF-e, 65 = NFC-e): não precisam de artigo.</summary>
    private static readonly string[] ConstantesDoDominio = { "55", "65" };

    [GeneratedRegex(@"\[(?<id>[a-z0-9-]+/[a-z0-9-]+)\]", RegexOptions.IgnoreCase)]
    private static partial Regex RegexCitacaoSemPrefixo();

    private static bool Fundamentado(string numero, HashSet<string> permitidos)
    {
        var digitos = SoDigitos(numero);
        return digitos.Length < 2 // numeração de listas, "5 ou 6"
            || permitidos.Contains(numero) || permitidos.Contains(digitos);
    }

    private static string SoDigitos(string s) => RegexNaoDigito().Replace(s, "");

    [GeneratedRegex(@"^(?<d>\d{2})/(?<m>\d{2})/(?<a>\d{4})$")]
    private static partial Regex RegexData();

    [GeneratedRegex(@"\[fonte:\s*(?<id>[^\]]+)\]", RegexOptions.IgnoreCase)]
    public static partial Regex RegexCitacao();

    [GeneratedRegex(@"\[[A-Z_]+_\d+\]")]
    private static partial Regex RegexMarcador();

    [GeneratedRegex(@"\d[\d.,/]*%?")]
    private static partial Regex RegexNumero();

    [GeneratedRegex(@"\D")]
    private static partial Regex RegexNaoDigito();
}
