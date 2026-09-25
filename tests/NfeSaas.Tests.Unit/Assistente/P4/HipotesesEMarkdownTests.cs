using FluentAssertions;
using NfeSaas.Application.Assistente.Hipoteses;
using NfeSaas.Application.Assistente.Preferencias;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Assistente.P4;

public class HipotesesEMarkdownTests
{
    private static ArtigoResumoDto Artigo(string id, string categoria = "preenchimento", string[]? campos = null, string[]? telas = null) =>
        new(id, $"Título {id}", categoria, NivelFonte.N1NormaOficial, "publicado", "resumo",
            new(), (campos ?? Array.Empty<string>()).ToList(), (telas ?? Array.Empty<string>()).ToList(), null);

    [Fact]
    public void Nota_rejeitada_vem_primeiro_e_sempre_ha_outra_duvida()
    {
        var c = new ContextoTelaDto("nota-detalhe", new OperacaoDto("VisualizandoNota", "Rejeitada"), NotaId: Guid.NewGuid());
        var h = MotorHipoteses.Gerar(c, new List<ArtigoResumoDto>());

        h[0].Tipo.Should().Be("rejeicao");
        h[^1].Tipo.Should().Be("livre");
    }

    [Fact]
    public void Campo_com_foco_sugere_o_artigo_do_campo_e_respeita_o_maximo()
    {
        var cfop = Artigo("preenchimento/cfop", campos: new[] { "cfop" });
        var tela = Artigo("sistema/emitir-nfe", "sistema", telas: new[] { "emitir-nfe" });
        var c = new ContextoTelaDto("emitir-nfe", Foco: new FocoDto("cfop", "CFOP", "5102"),
            Api: new() { new ErroApiDto("POST /api/notas", 500, null, "erro", 3) },
            Erros: new() { new ErroCampoDto("ncm", "inválido") },
            Rastro: new() { "tentou emitir", "tentou emitir" });

        var h = MotorHipoteses.Gerar(c, new[] { cfop, tela });

        h.Should().HaveCount(MotorHipoteses.Maximo + 1);
        h.Take(3).Select(x => x.Tipo).Should().Equal("erro_sistema", "campo", "campo");
        h.Should().Contain(x => x.Artigo != null && x.Artigo.Id == "preenchimento/cfop");
    }

    [Fact]
    public void Tela_sem_nada_especial_sugere_o_guia_da_tela()
    {
        var guia = Artigo("sistema/certificado-upload", "sistema", telas: new[] { "certificado" });
        var h = MotorHipoteses.Gerar(new ContextoTelaDto("certificado"), new[] { guia });
        h[0].Artigo!.Id.Should().Be("sistema/certificado-upload");
    }

    [Fact]
    public void Markdown_escapa_html_antes_de_formatar()
    {
        var html = MarkdownSeguro.ParaHtml("**O que fazer** <script>alert(1)</script> use `6102` [fonte: preenchimento/cfop]\n\n- passo *um*\n- passo dois\n\n1. primeiro");

        html.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
        html.Should().Contain("<strong>O que fazer</strong>").And.Contain("<code>6102</code>");
        html.Should().Contain("<ul><li>passo <em>um</em></li><li>passo dois</li></ul>").And.Contain("<ol><li>primeiro</li></ol>");
        html.Should().NotContain("fonte:");
    }

    [Fact]
    public void Regras_de_dica_limitam_frequencia_e_respeitam_contexto()
    {
        var agora = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        var r = new RegrasDicaOri(() => agora);
        var nada = Array.Empty<string>();

        r.PodeMostrar("dica:a", nada, false, null, false, false).Should().BeTrue();
        r.PodeMostrar("dica:b", nada, false, null, false, false).Should().BeFalse("intervalo mínimo de 10 minutos");

        agora = agora.AddMinutes(11);
        r.PodeMostrar("dica:b", nada, false, agora.AddSeconds(-2), false, false).Should().BeFalse("usuário digitando");
        r.PodeMostrar("dica:b", new[] { "dica:b" }, false, null, false, false).Should().BeFalse("dispensada");
        r.PodeMostrar("dica:b", nada, false, null, true, false).Should().BeFalse("modal aberto");
        r.PodeMostrar("dica:b", nada, true, null, false, false).Should().BeFalse("dicas silenciadas");
        r.PodeMostrar("dica:b", nada, false, null, false, false).Should().BeTrue();

        agora = agora.AddMinutes(11);
        r.PodeMostrar("dica:c", nada, false, null, false, false).Should().BeTrue();
        agora = agora.AddMinutes(11);
        r.PodeMostrar("dica:d", nada, false, null, false, false).Should().BeFalse("máximo de 3 por sessão");
    }
}
