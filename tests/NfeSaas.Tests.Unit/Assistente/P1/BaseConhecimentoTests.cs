using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NfeSaas.Application.Assistente;
using NfeSaas.Domain.Enums;
using NfeSaas.Infrastructure.Assistente;

namespace NfeSaas.Tests.Unit.Assistente.P1;

public class BaseConhecimentoTests
{
    private const string ArtigoValido = """
        ---
        id: rejeicoes/ncm
        titulo: "NCM inexistente"
        categoria: rejeicoes
        nivel_fonte: N1
        codigos_rejeicao: [778]
        campos_relacionados: [ncm, cest]
        telas: [emitir-nfe]
        fontes:
          - documento: "MOC 7.0, Anexo I"
            url: "https://www.nfe.fazenda.gov.br/"
            nivel: N1
        vigencia_inicio: null
        vigencia_fim: null
        verificado_em: 2026-09-01
        revisar_ate: 2026-12-01
        curador: "Fulana — CRC SP-000000/O"
        status: publicado
        resumo_curto: "O NCM do item não existe na tabela vigente."
        ---

        ## Resumo

        Corrija o NCM do produto.
        """;

    private static ArtigoKb Artigo(string id, string status = "publicado", string categoria = "preenchimento",
        NivelFonte nivel = NivelFonte.N1NormaOficial, string[]? codigos = null, string[]? campos = null,
        string[]? telas = null, string titulo = "Título", string resumo = "Resumo", string corpo = "") =>
        new(id, titulo, categoria, nivel, codigos ?? Array.Empty<string>(), campos ?? Array.Empty<string>(),
            telas ?? Array.Empty<string>(), Array.Empty<FonteArtigo>(), null, null, null, null, null,
            status, resumo, corpo);

    private static BaseConhecimento Base(bool rascunhos, params ArtigoKb[] artigos) =>
        new(Options.Create(new AssistenteOptions { IncluirRascunhosKb = rascunhos }), () => artigos);

    [Fact]
    public void Parser_le_front_matter_completo()
    {
        var a = BaseConhecimentoParser.Parse("kb/rejeicoes/ncm.md", ArtigoValido);

        a.Id.Should().Be("rejeicoes/ncm");
        a.NivelFonte.Should().Be(NivelFonte.N1NormaOficial);
        a.CodigosRejeicao.Should().Equal("778");
        a.CamposRelacionados.Should().Equal("ncm", "cest");
        a.Fontes.Should().ContainSingle().Which.Nivel.Should().Be(NivelFonte.N1NormaOficial);
        a.VerificadoEm.Should().Be(new DateOnly(2026, 9, 1));
        a.VigenciaInicio.Should().BeNull();
        a.Status.Should().Be("publicado");
        a.Corpo.Should().StartWith("## Resumo");
    }

    [Fact]
    public void Parser_trata_placeholders_do_template_como_nulos_e_nivel_sistema()
    {
        var texto = ArtigoValido
            .Replace("verificado_em: 2026-09-01", "verificado_em: AAAA-MM-DD")
            .Replace("curador: \"Fulana — CRC SP-000000/O\"", "curador: nome")
            .Replace("nivel_fonte: N1", "nivel_fonte: sistema");

        var a = BaseConhecimentoParser.Parse("kb/rejeicoes/ncm.md", texto);

        a.VerificadoEm.Should().BeNull();
        a.Curador.Should().BeNull();
        a.NivelFonte.Should().Be(NivelFonte.GuiaDoSistema);
    }

    [Theory]
    [InlineData("sem front matter")]
    [InlineData("---\nid: x\nsem fechamento")]
    public void Parser_rejeita_arquivo_malformado(string texto)
    {
        var acao = () => BaseConhecimentoParser.Parse("kb/x.md", texto);
        acao.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("[778] Rejeição: Informado NCM inexistente", "778")]
    [InlineData("Rejeição 539: Duplicidade de NF-e", "539")]
    [InlineData("Rejeicao: 225 Falha no Schema", "225")]
    [InlineData("cStat 204", "204")]
    [InlineData("610 - Total da NF difere", "610")]
    [InlineData("SEFAZ indisponível. Nota salva em contingência.", null)]
    [InlineData("Timeout na comunicação com SEFAZ.", null)]
    [InlineData(null, null)]
    public void Extrai_codigo_de_rejeicao(string? motivo, string? esperado) =>
        Base(false).ExtrairCodigoRejeicao(motivo).Should().Be(esperado);

    [Fact]
    public void Utilizaveis_exclui_rascunhos_por_padrao_e_inclui_quando_configurado()
    {
        var pub = Artigo("a/publicado");
        var rev = Artigo("a/em-revisao", status: "em_revisao");
        var ras = Artigo("a/rascunho", status: "rascunho");

        Base(false, pub, rev, ras).Utilizaveis().Should().BeEquivalentTo(new[] { pub, rev });
        Base(true, pub, rev, ras).Utilizaveis().Should().HaveCount(3);
    }

    [Fact]
    public void Roteamento_prioriza_codigo_depois_campo_depois_termo_e_respeita_maximo()
    {
        var porCodigo = Artigo("rejeicoes/ncm", codigos: new[] { "778" });
        var porCampo = Artigo("preenchimento/cfop", campos: new[] { "cfop" });
        var porTermo = Artigo("eventos/contingencia", titulo: "Contingência SVC", resumo: "Quando a SEFAZ está indisponível");
        var semRelacao = Artigo("outros/x", titulo: "Assunto diferente");
        var kb = Base(false, semRelacao, porTermo, porCampo, porCodigo);

        var r = kb.Rotear(new CriterioRoteamentoKb(new[] { "778" }, "cfop", null, "sefaz indisponivel contingencia"));

        r.Select(a => a.Id).Should().Equal("rejeicoes/ncm", "preenchimento/cfop", "eventos/contingencia");
        kb.Rotear(new CriterioRoteamentoKb(new[] { "778" }, "cfop"), maximo: 1).Should().ContainSingle();
    }

    [Fact]
    public void Roteamento_por_termo_ignora_acentos_e_caixa()
    {
        var a = Artigo("preenchimento/ibs", titulo: "IBS e CBS: cClassTrib na NF-e");
        Base(false, a).Rotear(new CriterioRoteamentoKb(Termo: "o que é CCLASSTRIB?")).Should().ContainSingle();
    }

    [Fact]
    public void Roteamento_nao_usa_rascunho_em_producao()
    {
        var ras = Artigo("rejeicoes/ncm", status: "rascunho", codigos: new[] { "778" });
        Base(false, ras).Rotear(new CriterioRoteamentoKb(new[] { "778" })).Should().BeEmpty();
    }

    [Fact]
    public void Validador_aponta_problemas_de_governanca()
    {
        var publicadoSemFonte = Artigo("rejeicoes/x", campos: new[] { "campo_inexistente" }, telas: new[] { "tela-x" });
        var erros = BaseConhecimentoValidador.Validar(new[] { ("kb/rejeicoes/y.md", publicadoSemFonte) });

        erros.Should().Contain(e => e.Contains("id deve ser igual ao caminho"));
        erros.Should().Contain(e => e.Contains("exige ao menos uma fonte"));
        erros.Should().Contain(e => e.Contains("campo não canônico 'campo_inexistente'"));
        erros.Should().Contain(e => e.Contains("tela não canônica 'tela-x'"));
    }

    [Fact]
    public void Validador_aceita_artigo_publicado_completo()
    {
        var a = BaseConhecimentoParser.Parse("kb/rejeicoes/ncm.md", ArtigoValido);
        BaseConhecimentoValidador.Validar(new[] { ("kb/rejeicoes/ncm.md", a) }).Should().BeEmpty();
    }

    /// <summary>Guarda de merge: todo artigo em docs/assistente/kb/ precisa parsear e passar no validador.</summary>
    [Fact]
    public void Base_embutida_e_valida()
    {
        var erros = new List<string>();
        var artigos = new List<(string, ArtigoKb)>();
        foreach (var (caminho, texto) in BaseConhecimento.RecursosEmbutidos())
        {
            try { artigos.Add((caminho, BaseConhecimentoParser.Parse(caminho, texto))); }
            catch (FormatException ex) { erros.Add(ex.Message); }
        }
        erros.AddRange(BaseConhecimentoValidador.Validar(artigos));

        erros.Should().BeEmpty();
    }

    [Fact]
    public void Base_embutida_carrega_os_artigos()
    {
        var kb = new BaseConhecimento(Options.Create(new AssistenteOptions { IncluirRascunhosKb = true }),
            NullLogger<BaseConhecimento>.Instance);
        kb.Todos().Should().HaveCount(BaseConhecimento.RecursosEmbutidos().Count());
    }
}
