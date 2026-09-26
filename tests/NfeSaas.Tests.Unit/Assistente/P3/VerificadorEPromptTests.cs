using FluentAssertions;
using Moq;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Ia;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Assistente.P3;

public class VerificadorEPromptTests
{
    internal static ArtigoKb Artigo(string id, NivelFonte nivel = NivelFonte.N1NormaOficial, string corpo = "", string resumo = "") =>
        new(id, $"Título {id}", nivel == NivelFonte.GuiaDoSistema ? "sistema" : "preenchimento", nivel,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<FonteArtigo>(),
            null, null, null, null, null, "publicado", resumo, corpo);

    internal static IBaseConhecimento Base(params ArtigoKb[] artigos)
    {
        var m = new Mock<IBaseConhecimento>();
        m.Setup(b => b.Utilizaveis()).Returns(artigos);
        return m.Object;
    }

    [Fact]
    public void Aprova_resposta_com_numeros_presentes_no_artigo_citado()
    {
        var cfop = Artigo("preenchimento/cfop", corpo: "Operações com outra UF usam CFOP do grupo 6, como 6102.");
        var r = VerificadorResposta.Verificar("**O que fazer** Use 6102. [fonte: preenchimento/cfop]", Base(cfop), Array.Empty<string>());

        r.Aprovada.Should().BeTrue();
        r.Selo.Should().Be(NivelFonte.N1NormaOficial);
        r.ArtigosCitados.Should().ContainSingle();
    }

    [Fact]
    public void Reprova_numero_inventado_e_citacao_inexistente()
    {
        var cfop = Artigo("preenchimento/cfop", corpo: "Use 6102.");
        var r = VerificadorResposta.Verificar("A alíquota é 12% e o prazo é de 30 dias. [fonte: nao/existe]", Base(cfop), Array.Empty<string>());

        r.Aprovada.Should().BeFalse();
        r.ItensNaoFundamentados.Should().Contain(new[] { "12", "30" });
        r.CitacoesInvalidas.Should().Contain("nao/existe");
        VerificadorResposta.InstrucaoCorrecao(r).Should().Contain("12").And.Contain("nao/existe");
    }

    [Fact]
    public void Aceita_numeros_vindos_da_pergunta_ou_das_ferramentas_e_formatos_equivalentes()
    {
        var r = VerificadorResposta.Verificar("A nota 1482 tem o NCM 6912.00.00 no item 2.", Base(),
            new[] { "{\"numero\":1482,\"ncm\":\"69120000\"}" });
        r.Aprovada.Should().BeTrue();
    }

    [Fact]
    public void Ignora_marcadores_e_numeracao_de_lista()
    {
        var r = VerificadorResposta.Verificar("1. Confira o [CPF_1].\n2. Salve.", Base(), Array.Empty<string>());
        r.Aprovada.Should().BeTrue();
    }

    [Fact]
    public void Selo_e_o_nivel_mais_fraco_entre_os_fiscais_e_guia_quando_so_sistema()
    {
        VerificadorResposta.CalcularSelo(new[] { Artigo("a"), Artigo("b", NivelFonte.N2OrientacaoOficial) })
            .Should().Be(NivelFonte.N2OrientacaoOficial);
        VerificadorResposta.CalcularSelo(new[] { Artigo("s", NivelFonte.GuiaDoSistema), Artigo("a") })
            .Should().Be(NivelFonte.N1NormaOficial);
        VerificadorResposta.CalcularSelo(new[] { Artigo("s", NivelFonte.GuiaDoSistema) })
            .Should().Be(NivelFonte.GuiaDoSistema);
        VerificadorResposta.CalcularSelo(Array.Empty<ArtigoKb>()).Should().Be(NivelFonte.N4SemFonte);
    }

    private sealed class PromptFixo : IPromptSistema { public string Texto => "PROMPT FIXO"; }

    [Fact]
    public void Prefixo_do_prompt_e_identico_entre_requisicoes_diferentes()
    {
        var montador = new MontadorPrompt(new PromptFixo());
        var a = montador.Montar(new[] { Artigo("x") }, new { data_hoje = "2026-09-25", tela = "emitir-nfe" },
            Array.Empty<MensagemIa>(), null, "pergunta A");
        var b = montador.Montar(Array.Empty<ArtigoKb>(), new { data_hoje = "2026-09-26", tela = "notas" },
            Array.Empty<MensagemIa>(), null, "pergunta B");

        a[0].Should().Be(b[0]);
        a[0].Papel.Should().Be(PapelIa.Sistema);
        a[0].Texto.Should().NotContain("2026");
        a[^1].Texto.Should().Contain("<contexto>").And.Contain("pergunta A");
    }

    [Fact]
    public void Historico_respeita_janela_de_turnos()
    {
        var montador = new MontadorPrompt(new PromptFixo());
        var historico = Enumerable.Range(1, 20)
            .Select(i => new MensagemIa(i % 2 == 1 ? PapelIa.Usuario : PapelIa.Assistente, $"m{i}")).ToList();

        var msgs = montador.Montar(Array.Empty<ArtigoKb>(), new { }, historico, "resumo antigo", "nova");

        msgs.Count(m => m.Papel != PapelIa.Sistema).Should().Be(MontadorPrompt.JanelaTurnos * 2 + 1);
        msgs.Should().Contain(m => m.Texto.Contains("resumo antigo"));
        msgs.Should().NotContain(m => m.Texto == "m1");
    }
}
