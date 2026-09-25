using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Application.Assistente.Monitoramento;

/// <summary>Fontes oficiais monitoradas por padrão (MONITORAMENTO_FONTES.md §2). URLs verificadas em 2026-09-25.</summary>
public static class FontesPadrao
{
    public const string TermosDou = "Ajuste SINIEF;Convênio ICMS;Nota Fiscal Eletrônica;NF-e;NFC-e;IBS;CBS;cClassTrib;Comitê Gestor";

    public static IEnumerable<FonteMonitorada> Criar() => new[]
    {
        FonteMonitorada.Criar("Portal Nacional da NF-e — Notas Técnicas", NivelFonte.N1NormaOficial,
            "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY=", MecanismoMonitoramento.HashPagina, 24),
        FonteMonitorada.Criar("Portal Nacional da NF-e — página principal (informes)", NivelFonte.N1NormaOficial,
            "https://www.nfe.fazenda.gov.br/portal/principal.aspx", MecanismoMonitoramento.HashPagina, 24),
        FonteMonitorada.Criar("Portal DF-e SVRS — Notícias NF-e", NivelFonte.N2OrientacaoOficial,
            "https://dfe-portal.svrs.rs.gov.br/Nfe/Noticias", MecanismoMonitoramento.HashPagina, 24),
        FonteMonitorada.Criar("CONFAZ — Ajustes SINIEF", NivelFonte.N1NormaOficial,
            "https://www.confaz.fazenda.gov.br/legislacao/ajustes", MecanismoMonitoramento.HashPagina, 168),
        FonteMonitorada.Criar("CONFAZ — Convênios ICMS", NivelFonte.N1NormaOficial,
            "https://www.confaz.fazenda.gov.br/legislacao/convenios", MecanismoMonitoramento.HashPagina, 168),
        FonteMonitorada.Criar("Receita Federal — Reforma Tributária", NivelFonte.N2OrientacaoOficial,
            "https://www.gov.br/receitafederal/pt-br/assuntos/reforma-tributaria", MecanismoMonitoramento.HashPagina, 168),
        FonteMonitorada.Criar("Diário Oficial da União — Seção 1 (INLABS)", NivelFonte.N1NormaOficial,
            "https://inlabs.in.gov.br/", MecanismoMonitoramento.Inlabs, 24, TermosDou),
    };
}

public record ItemExtraido(string Titulo, string Hash);

/// <summary>Extrai itens relevantes (NTs, ajustes, informes) do HTML de uma fonte, sem dependências de parser.</summary>
public static partial class ExtratorPublicacoes
{
    public static List<ItemExtraido> Extrair(string html)
    {
        var semScripts = RegexBlocos().Replace(html, " ");
        var linhas = RegexTag().Replace(semScripts, "\n")
            .Split('\n')
            .Select(l => RegexEspacos().Replace(WebUtility.HtmlDecode(l), " ").Trim())
            .Where(l => l.Length is >= 20 and <= 300 && RegexRelevante().IsMatch(l));
        return linhas.Distinct().Select(l => new ItemExtraido(l, Hash(l))).ToList();
    }

    public static string HashConjunto(IEnumerable<ItemExtraido> itens) => Hash(string.Join("\n", itens.Select(i => i.Hash).OrderBy(h => h)));

    public static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..40];

    [GeneratedRegex(@"<(script|style|noscript)[^>]*>.*?</\1>|<!--.*?-->", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex RegexBlocos();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex RegexTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex RegexEspacos();

    [GeneratedRegex(@"nota t[eé]cnica|\bNT\s?\d{4}|informe|ajuste sinief|conv[eê]nio icms|reforma tribut|\bIBS\b|\bCBS\b|cclasstrib|schema|esquema|leiaute|\bMOC\b|implantad|rejei[cç]|conting[eê]ncia|NFC-?e|NF-?e", RegexOptions.IgnoreCase)]
    private static partial Regex RegexRelevante();
}

public record ClassificacaoPublicacao(bool Relevante, string? Resumo, DateTime? VigenciaInicio, List<string> ArtigosAfetados, bool ImpactoTecnico);

/// <summary>Classificação de uma publicação pública pela IA (rota FontesPublicas — dado 100% público).</summary>
public class ClassificadorPublicacoes
{
    private readonly IAssistenteIA _ia;
    private readonly IBaseConhecimento _base;

    public ClassificadorPublicacoes(IAssistenteIA ia, IBaseConhecimento baseConhecimento)
    {
        _ia = ia;
        _base = baseConhecimento;
    }

    public bool Disponivel => _ia.EstaHabilitado(RotaIa.FontesPublicas);

    public async Task<ClassificacaoPublicacao?> ClassificarAsync(PublicacaoDetectada p, CancellationToken ct)
    {
        var artigos = string.Join("\n", _base.Todos().Select(a => $"- {a.Id}: {a.Titulo}"));
        var mensagens = new[]
        {
            new MensagemIa(PapelIa.Sistema,
                "Você classifica publicações oficiais para a curadoria da base de conhecimento de um emissor de NF-e/NFC-e. " +
                "Responda SOMENTE um objeto JSON com: relevante (bool: afeta emissão, validação, leiaute ou tributação de NF-e/NFC-e?), " +
                "resumo (até 400 caracteres, em português, sem inventar números que não estejam no texto), vigencia_inicio (AAAA-MM-DD ou null), " +
                "artigos_afetados (lista de ids da base abaixo, pode ser vazia), impacto_tecnico (bool: exige mudança de software — novo campo, regra de validação, schema ou URL)."),
            new MensagemIa(PapelIa.Usuario,
                $"<artigos_da_base>\n{artigos}\n</artigos_da_base>\n<publicacao fonte=\"{p.Fonte?.Nome}\">\n{p.Titulo}\n{p.Url}\n</publicacao>")
        };
        var r = await _ia.CompletarAsync(RotaIa.FontesPublicas, mensagens, new OpcoesIa(600, Raciocinio: false), ct);
        return Interpretar(r.Texto, _base.Todos().Select(a => a.Id).ToHashSet());
    }

    public static ClassificacaoPublicacao? Interpretar(string texto, IReadOnlySet<string> idsValidos)
    {
        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');
        if (inicio < 0 || fim <= inicio) return null;
        try
        {
            using var doc = JsonDocument.Parse(texto[inicio..(fim + 1)]);
            var r = doc.RootElement;
            bool B(string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.True;
            string? S(string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var artigos = r.TryGetProperty("artigos_afetados", out var a) && a.ValueKind == JsonValueKind.Array
                ? a.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(idsValidos.Contains).ToList()
                : new List<string>();
            DateTime? vigencia = DateTime.TryParseExact(S("vigencia_inicio"), "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var d) ? d : null;
            var resumo = S("resumo");
            return new ClassificacaoPublicacao(B("relevante"), resumo is { Length: > 400 } ? resumo[..400] : resumo, vigencia, artigos, B("impacto_tecnico"));
        }
        catch (JsonException) { return null; }
    }
}
