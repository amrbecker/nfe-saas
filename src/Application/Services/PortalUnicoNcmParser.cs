using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NfeSaas.Domain.Entities;

namespace NfeSaas.Application.Services;

/// <summary>
/// Parser do JSON oficial de NCM publicado pelo Portal Único Siscomex
/// (https://portalunico.siscomex.gov.br).
///
/// Equivale ao script <c>scripts/load_ncm_oficial.py</c> mas executável dentro
/// do worker .NET — permite que o cron de atualização rode sem dependência Python.
/// </summary>
public static class PortalUnicoNcmParser
{
    private static readonly Regex PrefixRegex = new(@"^[\-\s°•·]+", RegexOptions.Compiled);
    private static readonly Regex CollapseWs = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Parseia o JSON e retorna a lista de NCMs finais (8 dígitos) vigentes,
    /// com a descrição enriquecida pelo contexto da posição/subposição pai.
    /// </summary>
    public static NcmParseResult Parse(string json, string? versaoOverride = null)
    {
        var doc = JsonSerializer.Deserialize<PortalUnicoDoc>(json, JsonOpts)
                  ?? throw new InvalidOperationException("JSON do Portal Único vazio ou inválido.");

        var versao = !string.IsNullOrWhiteSpace(versaoOverride)
            ? versaoOverride
            : (doc.Ato ?? doc.DataUltimaAtualizacao ?? "desconhecida");

        var todos = doc.Nomenclaturas ?? new List<PortalUnicoNomenclatura>();

        // Indexa por código (sem pontos) para lookup do pai.
        var indexado = new Dictionary<string, PortalUnicoNomenclatura>(todos.Count);
        foreach (var n in todos)
        {
            var cod = ApenasDigitos(n.Codigo);
            if (!string.IsNullOrEmpty(cod) && !indexado.ContainsKey(cod))
                indexado[cod] = n;
        }

        var finais = new List<Ncm>();
        var descartadosNaoVigentes = 0;

        foreach (var n in todos)
        {
            var cod = ApenasDigitos(n.Codigo);

            // 1) Só códigos de 8 dígitos.
            if (cod.Length != 8) continue;

            // 2) Só vigentes (Data_Fim = 31/12/9999 ou vazio/ausente).
            var dataFim = (n.DataFim ?? "").Trim();
            if (!string.IsNullOrEmpty(dataFim) && dataFim != "31/12/9999")
            {
                descartadosNaoVigentes++;
                continue;
            }

            // Hierarquia: tenta achar o pai mais específico (níveis 6, 5 ou 4 dígitos).
            // O Portal Único Siscomex usa 5 dígitos para categorias visuais ("0101.2" = Cavalos:).
            PortalUnicoNomenclatura? subpos = null;
            for (int len = 6; len >= 5 && subpos == null; len--)
                indexado.TryGetValue(cod[..len], out subpos);

            indexado.TryGetValue(cod[..4], out var pos);

            var descricao = MontarDescricao(n.Descricao, subpos?.Descricao, pos?.Descricao);
            if (string.IsNullOrWhiteSpace(descricao)) continue;
            if (descricao.Length > 500) descricao = descricao[..500];

            // Ncm.Criar valida os 8 dígitos e a descrição não-vazia.
            finais.Add(Ncm.Criar(cod, descricao, versao));
        }

        return new NcmParseResult(
            Versao: versao,
            DataPublicacao: doc.DataUltimaAtualizacao,
            Ato: doc.Ato,
            Ncms: finais,
            TotalProcessados: todos.Count,
            DescartadosNaoVigentes: descartadosNaoVigentes);
    }

    // ============================================================
    // Helpers
    // ============================================================
    internal static string ApenasDigitos(string? s) =>
        string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    internal static string Limpar(string? desc)
    {
        if (string.IsNullOrWhiteSpace(desc)) return "";
        var s = HtmlTagRegex.Replace(desc, "");
        s = PrefixRegex.Replace(s, "").Trim();
        s = CollapseWs.Replace(s, " ");
        // Subcategorias visuais do Portal Único terminam com ":" (ex.: "Cavalos:")
        // — atrapalha quando concatenado com a descrição do filho ("Cavalos: — Reprodutores").
        return s.TrimEnd(':', ' ');
    }

    internal static string MontarDescricao(string? proprio, string? subpos, string? pos)
    {
        var item = Limpar(proprio);
        var s = Limpar(subpos);
        var p = Limpar(pos);

        // Prioriza o pai mais específico (subposição). Para itens genéricos
        // ("Outros", "Outras") o contexto do pai é essencial para identificar o NCM.
        if (!string.IsNullOrEmpty(s) && s.Length > 3 && !string.Equals(s, item, StringComparison.OrdinalIgnoreCase))
            return $"{s} — {item}".TrimEnd(' ', '—');
        if (!string.IsNullOrEmpty(p) && p.Length > 3 && !string.Equals(p, item, StringComparison.OrdinalIgnoreCase))
            return $"{p} — {item}".TrimEnd(' ', '—');
        return item;
    }

    // ============================================================
    // Modelos do JSON
    // ============================================================
    private sealed class PortalUnicoDoc
    {
        [JsonPropertyName("Data_Ultima_Atualizacao_NCM")]
        public string? DataUltimaAtualizacao { get; set; }

        [JsonPropertyName("Ato")]
        public string? Ato { get; set; }

        [JsonPropertyName("Nomenclaturas")]
        public List<PortalUnicoNomenclatura>? Nomenclaturas { get; set; }
    }

    private sealed class PortalUnicoNomenclatura
    {
        [JsonPropertyName("Codigo")]    public string? Codigo { get; set; }
        [JsonPropertyName("Descricao")] public string? Descricao { get; set; }
        [JsonPropertyName("Data_Inicio")] public string? DataInicio { get; set; }
        [JsonPropertyName("Data_Fim")]    public string? DataFim { get; set; }
    }
}

/// <summary>
/// Resultado do parsing — contém NCMs prontos para upsert e metadados da publicação.
/// </summary>
public record NcmParseResult(
    string Versao,
    string? DataPublicacao,
    string? Ato,
    IReadOnlyList<Ncm> Ncms,
    int TotalProcessados,
    int DescartadosNaoVigentes);
