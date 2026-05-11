using FluentAssertions;
using NfeSaas.Domain.Services;

namespace NfeSaas.Tests.Unit.Services;

public class NcmValidatorTests
{
    [Theory]
    [InlineData("12345678")]
    [InlineData("87654321")]
    [InlineData("00000000")]
    [InlineData("99999999")]
    public void Validar_NcmCom8Digitos_RetornaTrue(string ncm)
    {
        NcmValidator.Validar(ncm).Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567")]   // 7 dígitos
    [InlineData("123456789")] // 9 dígitos
    [InlineData("1234")]
    [InlineData("")]
    [InlineData(null)]
    public void Validar_NcmComTamanhoErrado_RetornaFalse(string? ncm)
    {
        NcmValidator.Validar(ncm).Should().BeFalse();
    }

    [Fact]
    public void Validar_NcmComLetras_RetornaFalse()
    {
        NcmValidator.Validar("1234ABCD").Should().BeFalse();
    }

    [Fact]
    public void Validar_NcmComEspacos_RemoveNaContagem()
    {
        // ApenasDigitos remove tudo exceto dígitos — 8 dígitos válidos com pontos viram 8
        NcmValidator.Validar("1234.5678").Should().BeTrue();
    }

    [Fact]
    public void ApenasDigitos_RemoveNaoNumericos()
    {
        NcmValidator.ApenasDigitos("1234.5678").Should().Be("12345678");
        NcmValidator.ApenasDigitos("AB12CD34").Should().Be("1234");
        NcmValidator.ApenasDigitos(null).Should().Be("");
    }
}

public class CnaeValidatorTests
{
    [Theory]
    [InlineData("4751201")]
    [InlineData("0000001")]
    public void Validar_CnaeCom7Digitos_RetornaTrue(string cnae)
    {
        CnaeValidator.Validar(cnae).Should().BeTrue();
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("12345678")]
    [InlineData("")]
    [InlineData(null)]
    public void Validar_CnaeComTamanhoErrado_RetornaFalse(string? cnae)
    {
        CnaeValidator.Validar(cnae).Should().BeFalse();
    }

    [Fact]
    public void Validar_CnaeFormatadoComBarra_AceitaSeTiver7Digitos()
    {
        CnaeValidator.Validar("4751-2/01").Should().BeTrue();
    }
}

public class GtinValidatorTests
{
    // GTIN-13 (EAN-13) válidos com check digit correto
    [Theory]
    [InlineData("7891234567895")]  // EAN-13 padrão
    [InlineData("4006381333931")]  // exemplo conhecido
    [InlineData("5901234123457")]  // exemplo conhecido
    public void Validar_Gtin13_ComDigitoVerificadorCorreto_RetornaTrue(string gtin)
    {
        GtinValidator.Validar(gtin).Should().BeTrue();
    }

    [Theory]
    [InlineData("12345670")]        // GTIN-8
    [InlineData("12345678901231")]  // GTIN-14
    public void Validar_Gtin8e14_ComDigitoCorreto_RetornaTrue(string gtin)
    {
        // Apenas verifica que o validador aceita os tamanhos 8/14 quando dígito está correto
        GtinValidator.Validar(gtin).Should().BeTrue();
    }

    [Theory]
    [InlineData("7891234567890")]   // dígito verificador errado (deveria ser 5)
    [InlineData("1234567890123")]   // dígito verificador errado
    public void Validar_GtinComDigitoVerificadorErrado_RetornaFalse(string gtin)
    {
        GtinValidator.Validar(gtin).Should().BeFalse();
    }

    [Theory]
    [InlineData("1234567890")]      // 10 dígitos — tamanho inválido
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ABCDEFGH")]
    public void Validar_GtinComTamanhoOuFormatoErrado_RetornaFalse(string? gtin)
    {
        GtinValidator.Validar(gtin).Should().BeFalse();
    }

    [Fact]
    public void Validar_GtinComEspacos_AceitaQuandoDigitosBaterem()
    {
        // 7891234567895 com espaços ainda tem 13 dígitos válidos
        GtinValidator.Validar("789 1234 567895").Should().BeTrue();
    }
}
