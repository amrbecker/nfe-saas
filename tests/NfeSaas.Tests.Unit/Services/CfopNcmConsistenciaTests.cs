using FluentAssertions;
using NfeSaas.Domain.Services;

namespace NfeSaas.Tests.Unit.Services;

public class CfopNcmConsistenciaTests
{
    // ============================================================
    // Cenários CONSISTENTES (não deve gerar warning)
    // ============================================================

    [Theory]
    [InlineData("5102", false)]   // venda normal de NCM sem ST → ok
    [InlineData("6102", false)]   // venda interestadual normal → ok
    [InlineData("1102", false)]   // compra normal → ok
    [InlineData("2102", false)]   // compra interestadual → ok
    public void Verificar_CfopNormalComNcmSemST_RetornaConsistente(string cfop, bool exigeCest)
    {
        var r = CfopNcmConsistencia.Verificar(cfop, exigeCest);

        r.Consistente.Should().BeTrue();
        r.Mensagem.Should().BeNull();
    }

    [Theory]
    [InlineData("5403")]
    [InlineData("5405")]
    [InlineData("6403")]
    [InlineData("6405")]
    public void Verificar_CfopSTComNcmExigeCestComCestPreenchido_RetornaConsistente(string cfop)
    {
        var r = CfopNcmConsistencia.Verificar(cfop, ncmExigeCest: true, cest: "2807000");

        r.Consistente.Should().BeTrue();
        r.Mensagem.Should().BeNull();
    }

    // ============================================================
    // Cenários INCONSISTENTES (devem gerar warning)
    // ============================================================

    [Fact]
    public void Verificar_CfopSTComNcmSemSTSemCest_RetornaWarning()
    {
        var r = CfopNcmConsistencia.Verificar("5403", ncmExigeCest: false);

        r.Consistente.Should().BeFalse();
        r.Mensagem.Should().Contain("Substituição Tributária");
        r.Mensagem.Should().Contain("CEST");
    }

    [Fact]
    public void Verificar_CfopNormalComNcmExigeCest_RetornaWarning()
    {
        var r = CfopNcmConsistencia.Verificar("5102", ncmExigeCest: true);

        r.Consistente.Should().BeFalse();
        r.Mensagem.Should().Contain("Substituição Tributária");
        r.Mensagem.Should().Contain("exceção");
    }

    [Fact]
    public void Verificar_CfopSTComNcmExigeCestMasSemCestPreenchido_RetornaWarning()
    {
        var r = CfopNcmConsistencia.Verificar("5405", ncmExigeCest: true, cest: null);

        r.Consistente.Should().BeFalse();
        r.Mensagem.Should().Contain("CEST");
        r.Mensagem.Should().Contain("Preencha");
    }

    [Fact]
    public void Verificar_CfopSTComCestComMascara_AceitaQualquerFormato()
    {
        // CEST informado com pontuação ("28.070.00") deve ser aceito após normalização.
        var r = CfopNcmConsistencia.Verificar("5403", ncmExigeCest: true, cest: "28.070.00");

        r.Consistente.Should().BeTrue();
    }

    [Theory]
    [InlineData("28070")]      // 5 dígitos
    [InlineData("12345678")]   // 8 dígitos
    [InlineData("")]
    [InlineData(null)]
    public void Verificar_CestComFormatoInvalido_ConsideraComoSemCest(string? cest)
    {
        // CEST malformado deve ser tratado como ausente.
        var r = CfopNcmConsistencia.Verificar("5403", ncmExigeCest: true, cest: cest);

        r.Consistente.Should().BeFalse();
        r.Mensagem.Should().Contain("CEST");
    }

    // ============================================================
    // EhCfopSubstituicaoTributaria
    // ============================================================

    [Theory]
    [InlineData("5401", true)]
    [InlineData("5403", true)]
    [InlineData("5405", true)]
    [InlineData("6403", true)]
    [InlineData("6405", true)]
    [InlineData("1403", true)]
    [InlineData("2403", true)]
    [InlineData("5102", false)]
    [InlineData("6102", false)]
    [InlineData("1102", false)]
    [InlineData("5910", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EhCfopSubstituicaoTributaria_Classifica(string? cfop, bool esperado)
    {
        CfopNcmConsistencia.EhCfopSubstituicaoTributaria(cfop).Should().Be(esperado);
    }

    // ============================================================
    // Edge: CFOP/NCM vazios
    // ============================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Verificar_CfopAusente_NaoFazClassificacaoDeST(string? cfop)
    {
        // CFOP ausente: cfopST=false. NCM sem CEST + CFOP não-ST = ok.
        var r = CfopNcmConsistencia.Verificar(cfop, ncmExigeCest: false);
        r.Consistente.Should().BeTrue();
    }
}
