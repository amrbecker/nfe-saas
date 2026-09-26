using System.Globalization;
using NfeSaas.Application.Assistente;
using NfeSaas.Domain.Enums;
using YamlDotNet.RepresentationModel;

namespace NfeSaas.Infrastructure.Assistente;

/// <summary>
/// Converte um artigo Markdown com front matter YAML (docs/assistente/kb/_TEMPLATE.md) em <see cref="ArtigoKb"/>.
/// Tolerante a valores ausentes e aos placeholders do template ("AAAA-MM-DD", "nome"); erros viram exceção
/// com mensagem clara — quem chama decide ignorar o artigo.
/// </summary>
public static class BaseConhecimentoParser
{
    public static ArtigoKb Parse(string caminhoLogico, string texto)
    {
        texto = texto.Replace("\r\n", "\n").TrimStart('﻿');
        if (!texto.StartsWith("---\n"))
            throw new FormatException($"{caminhoLogico}: front matter ausente (o arquivo deve começar com '---').");
        var fim = texto.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (fim < 0)
            throw new FormatException($"{caminhoLogico}: front matter sem '---' de fechamento.");

        var yaml = texto[4..fim];
        var corpoInicio = texto.IndexOf('\n', fim + 4);
        var corpo = corpoInicio < 0 ? "" : texto[(corpoInicio + 1)..].Trim();

        var stream = new YamlStream();
        using (var reader = new StringReader(yaml)) stream.Load(reader);
        if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode raiz)
            throw new FormatException($"{caminhoLogico}: front matter vazio ou inválido.");

        var idPadrao = IdDoCaminho(caminhoLogico);
        var nivel = ParseNivel(Escalar(raiz, "nivel_fonte")) ?? NivelFonte.N4SemFonte;

        return new ArtigoKb(
            Id: Escalar(raiz, "id") ?? idPadrao,
            Titulo: Escalar(raiz, "titulo") ?? "",
            Categoria: Escalar(raiz, "categoria") ?? "",
            NivelFonte: nivel,
            CodigosRejeicao: Lista(raiz, "codigos_rejeicao"),
            CamposRelacionados: Lista(raiz, "campos_relacionados"),
            Telas: Lista(raiz, "telas"),
            Fontes: Fontes(raiz),
            VigenciaInicio: Data(Escalar(raiz, "vigencia_inicio")),
            VigenciaFim: Data(Escalar(raiz, "vigencia_fim")),
            VerificadoEm: Data(Escalar(raiz, "verificado_em")),
            RevisarAte: Data(Escalar(raiz, "revisar_ate")),
            Curador: Nulo(Escalar(raiz, "curador")) is { } c && c != "nome" ? c : null,
            Status: (Escalar(raiz, "status") ?? "rascunho").Trim().ToLowerInvariant(),
            ResumoCurto: (Escalar(raiz, "resumo_curto") ?? "").Trim(),
            Corpo: corpo);
    }

    /// <summary>"kb/rejeicoes/ncm.md" → "rejeicoes/ncm". Aceita separadores "/" e "\".</summary>
    public static string IdDoCaminho(string caminhoLogico)
    {
        var c = caminhoLogico.Replace('\\', '/');
        if (c.StartsWith("kb/", StringComparison.OrdinalIgnoreCase)) c = c[3..];
        return c.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? c[..^3] : c;
    }

    public static NivelFonte? ParseNivel(string? valor) => valor?.Trim().ToLowerInvariant() switch
    {
        "n1" => NivelFonte.N1NormaOficial,
        "n2" => NivelFonte.N2OrientacaoOficial,
        "n3" => NivelFonte.N3ReferenciaTecnica,
        "n4" => NivelFonte.N4SemFonte,
        "sistema" => NivelFonte.GuiaDoSistema,
        _ => null
    };

    private static string? Escalar(YamlMappingNode no, string chave) =>
        no.Children.TryGetValue(new YamlScalarNode(chave), out var v) && v is YamlScalarNode s ? Nulo(s.Value) : null;

    private static string? Nulo(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Trim() is "null" or "~" ? null : v.Trim();

    private static IReadOnlyList<string> Lista(YamlMappingNode no, string chave)
    {
        if (!no.Children.TryGetValue(new YamlScalarNode(chave), out var v)) return Array.Empty<string>();
        return v switch
        {
            YamlSequenceNode seq => seq.Children.OfType<YamlScalarNode>()
                .Select(s => Nulo(s.Value)).Where(s => s != null).Select(s => s!).ToList(),
            YamlScalarNode s when Nulo(s.Value) is { } unico => new[] { unico },
            _ => Array.Empty<string>()
        };
    }

    private static IReadOnlyList<FonteArtigo> Fontes(YamlMappingNode raiz)
    {
        if (!raiz.Children.TryGetValue(new YamlScalarNode("fontes"), out var v) || v is not YamlSequenceNode seq)
            return Array.Empty<FonteArtigo>();
        return seq.Children.OfType<YamlMappingNode>()
            .Select(f => new FonteArtigo(
                Escalar(f, "documento") ?? "",
                Escalar(f, "url"),
                ParseNivel(Escalar(f, "nivel")) ?? NivelFonte.N4SemFonte))
            .Where(f => f.Documento.Length > 0)
            .ToList();
    }

    private static DateOnly? Data(string? v) =>
        v != null && DateOnly.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
