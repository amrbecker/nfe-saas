using FluentAssertions;
using NfeSaas.Application.Services;

namespace NfeSaas.Tests.Unit.Application;

public class PortalUnicoNcmParserTests
{
    // Mini fixture inspirado no formato real do Portal Único Siscomex.
    private const string JsonMinimo = """
    {
      "Data_Ultima_Atualizacao_NCM": "Vigente em 11/05/2026",
      "Ato": "Resolução Gecex nº 812/2025",
      "Nomenclaturas": [
        { "Codigo": "01",         "Descricao": "Animais vivos.",            "Data_Fim": "31/12/9999" },
        { "Codigo": "01.01",      "Descricao": "Cavalos, asininos e muares, vivos.", "Data_Fim": "31/12/9999" },
        { "Codigo": "0101.2",     "Descricao": "- Cavalos:",                "Data_Fim": "31/12/9999" },
        { "Codigo": "0101.21.00", "Descricao": "-- Reprodutores de raça pura", "Data_Fim": "31/12/9999" },
        { "Codigo": "0101.29.00", "Descricao": "-- Outros",                 "Data_Fim": "31/12/9999" },
        { "Codigo": "0101.30.00", "Descricao": "- Asininos",                "Data_Fim": "31/12/9999" },
        { "Codigo": "9999.99.99", "Descricao": "Obsoleto",                  "Data_Fim": "01/01/2024" }
      ]
    }
    """;

    [Fact]
    public void Parse_RetornaApenasNcmsFinaisVigentes()
    {
        var result = PortalUnicoNcmParser.Parse(JsonMinimo);

        result.Ncms.Should().HaveCount(3);
        result.Ncms.Select(n => n.Codigo).Should().BeEquivalentTo(["01012100", "01012900", "01013000"]);
    }

    [Fact]
    public void Parse_DescartaNcmsNaoVigentes()
    {
        var result = PortalUnicoNcmParser.Parse(JsonMinimo);
        result.DescartadosNaoVigentes.Should().Be(1);
        result.Ncms.Should().NotContain(n => n.Codigo == "99999999");
    }

    [Fact]
    public void Parse_EnriqueceDescricaoComPaiMaisEspecifico()
    {
        var result = PortalUnicoNcmParser.Parse(JsonMinimo);

        // 0101.21.00 → subposição "Cavalos:" → "Cavalos — Reprodutores de raça pura"
        var ncm = result.Ncms.Single(n => n.Codigo == "01012100");
        ncm.Descricao.Should().Be("Cavalos — Reprodutores de raça pura");
    }

    [Fact]
    public void Parse_RemovePrefixosHierarquicos()
    {
        var result = PortalUnicoNcmParser.Parse(JsonMinimo);
        result.Ncms.Should().OnlyContain(n => !n.Descricao.StartsWith("-"));
    }

    [Fact]
    public void Parse_PreservaVersaoDoAto()
    {
        var result = PortalUnicoNcmParser.Parse(JsonMinimo);
        result.Versao.Should().Be("Resolução Gecex nº 812/2025");
        result.Ato.Should().Be("Resolução Gecex nº 812/2025");
        result.DataPublicacao.Should().Be("Vigente em 11/05/2026");
    }

    [Fact]
    public void Parse_AceitaVersaoOverride()
    {
        var result = PortalUnicoNcmParser.Parse(JsonMinimo, versaoOverride: "2026-05");
        result.Versao.Should().Be("2026-05");
        result.Ncms.Should().OnlyContain(n => n.VersaoTabela == "2026-05");
    }

    [Fact]
    public void Parse_PopulaCapituloEPosicao()
    {
        var result = PortalUnicoNcmParser.Parse(JsonMinimo);

        var ncm = result.Ncms.Single(n => n.Codigo == "01012100");
        ncm.CategoriaCapitulo.Should().Be("01");
        ncm.Posicao.Should().Be("0101");
    }

    [Fact]
    public void Parse_DataFimVazioOuAusenteContaComoVigente()
    {
        var json = """
        {
          "Nomenclaturas": [
            { "Codigo": "1234.56.78", "Descricao": "Teste sem Data_Fim" }
          ]
        }
        """;
        var result = PortalUnicoNcmParser.Parse(json);
        result.Ncms.Should().HaveCount(1);
        result.Ncms[0].Codigo.Should().Be("12345678");
    }

    [Fact]
    public void Parse_DescricaoMuitoLonga_TruncaA500Chars()
    {
        var longa = new string('A', 600);
        var json = $$"""
        {
          "Nomenclaturas": [
            { "Codigo": "1234.56.78", "Descricao": "{{longa}}", "Data_Fim": "31/12/9999" }
          ]
        }
        """;
        var result = PortalUnicoNcmParser.Parse(json);
        result.Ncms.Should().HaveCount(1);
        result.Ncms[0].Descricao.Length.Should().Be(500);
    }

    [Fact]
    public void Parse_JsonVazioOuInvalido_LancaInvalidOperationException()
    {
        Action act = () => PortalUnicoNcmParser.Parse("null");
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("- Cavalos:", "Cavalos")]           // remove prefixo "-" e ":" final (categoria visual)
    [InlineData("--Reprodutores", "Reprodutores")]
    [InlineData("   espaços extras   ", "espaços extras")]
    [InlineData("  -- multi   spaces  ", "multi spaces")]
    public void Limpar_TrataPrefixoseEspacos(string? entrada, string esperado)
    {
        PortalUnicoNcmParser.Limpar(entrada).Should().Be(esperado);
    }
}
