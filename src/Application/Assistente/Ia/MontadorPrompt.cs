using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NfeSaas.Application.Assistente.Ia;

/// <summary>Texto fixo do prompt de sistema (docs/assistente/prompt/sistema.txt, embutido na Infrastructure).</summary>
public interface IPromptSistema
{
    string Texto { get; }
}

/// <summary>
/// Monta as mensagens na ordem que maximiza o cache de prefixo (PESQUISA_REFINAMENTO.md §2.4, E3/E9):
/// 1) prompt fixo, byte-idêntico; 2) artigos roteados; 3) histórico em janela; 4) contexto + pergunta.
/// Nada de data, nome ou id no bloco fixo.
/// </summary>
public class MontadorPrompt
{
    public const int JanelaTurnos = 6;
    public const int LimiteCorpoArtigo = 6000;

    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IPromptSistema _prompt;
    public MontadorPrompt(IPromptSistema prompt) => _prompt = prompt;

    public IReadOnlyList<MensagemIa> Montar(
        IReadOnlyList<ArtigoKb> artigos,
        object contextoSanitizado,
        IReadOnlyList<MensagemIa> historico,
        string? resumoAnterior,
        string perguntaSanitizada)
    {
        var mensagens = new List<MensagemIa> { new(PapelIa.Sistema, _prompt.Texto) };

        mensagens.Add(new MensagemIa(PapelIa.Sistema, BlocoArtigos(artigos)));

        if (!string.IsNullOrWhiteSpace(resumoAnterior))
            mensagens.Add(new MensagemIa(PapelIa.Sistema, $"<resumo_conversa_anterior>\n{resumoAnterior}\n</resumo_conversa_anterior>"));

        // Janela: últimos N turnos (um turno = pergunta do usuário + resposta).
        var janela = historico.Count > JanelaTurnos * 2 ? historico.Skip(historico.Count - JanelaTurnos * 2) : historico;
        mensagens.AddRange(janela);

        var contextoJson = JsonSerializer.Serialize(contextoSanitizado, Json);
        mensagens.Add(new MensagemIa(PapelIa.Usuario,
            $"<contexto>\n{contextoJson}\n</contexto>\n\n<pergunta>\n{perguntaSanitizada}\n</pergunta>"));
        return mensagens;
    }

    public static string BlocoArtigos(IReadOnlyList<ArtigoKb> artigos)
    {
        if (artigos.Count == 0) return "<artigos>\n(nenhum artigo da base cobre este contexto)\n</artigos>";
        var sb = new StringBuilder("<artigos>\n");
        foreach (var a in artigos)
        {
            var nivel = a.NivelFonte switch
            {
                Domain.Enums.NivelFonte.N1NormaOficial => "N1",
                Domain.Enums.NivelFonte.N2OrientacaoOficial => "N2",
                Domain.Enums.NivelFonte.N3ReferenciaTecnica => "N3",
                Domain.Enums.NivelFonte.GuiaDoSistema => "sistema",
                _ => "N4"
            };
            var corpo = a.Corpo.Length > LimiteCorpoArtigo ? a.Corpo[..LimiteCorpoArtigo] + "\n(...)" : a.Corpo;
            sb.Append($"<artigo id=\"{a.Id}\" nivel=\"{nivel}\" status=\"{a.Status}\"");
            if (a.VigenciaInicio != null) sb.Append($" vigencia_inicio=\"{a.VigenciaInicio:yyyy-MM-dd}\"");
            if (a.VigenciaFim != null) sb.Append($" vigencia_fim=\"{a.VigenciaFim:yyyy-MM-dd}\"");
            if (a.VerificadoEm != null) sb.Append($" verificado_em=\"{a.VerificadoEm:yyyy-MM-dd}\"");
            sb.Append($">\n# {a.Titulo}\n{corpo}\n");
            if (a.Fontes.Count > 0)
                sb.Append("Fontes: ").Append(string.Join("; ", a.Fontes.Select(f => f.Documento))).Append('\n');
            sb.Append("</artigo>\n");
        }
        return sb.Append("</artigos>").ToString();
    }
}
