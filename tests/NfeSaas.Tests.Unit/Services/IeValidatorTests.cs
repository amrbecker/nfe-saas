using NfeSaas.Domain.Services;

namespace NfeSaas.Tests.Unit.Services;

public class IeValidatorTests
{
    [Theory]
    [InlineData("ISENTO", "SP")]
    [InlineData("isento", "RJ")]
    [InlineData("Isento", "MG")]
    public void Validar_Isento_SempreValido(string ie, string uf)
    {
        IeValidator.Validar(ie, uf).Should().BeTrue();
    }

    [Theory]
    [InlineData("123456789012", "SP")]    // 12 dígitos SP
    [InlineData("110042490114", "SP")]    // 12 dígitos SP
    [InlineData("1234567890", "RS")]      // 10 dígitos RS
    [InlineData("123456789", "CE")]       // 9 dígitos CE
    [InlineData("123456789", "SC")]       // 9 dígitos SC
    [InlineData("12345678", "RJ")]        // 8 dígitos RJ
    [InlineData("123456789", "ES")]       // 9 dígitos ES
    [InlineData("12345678901", "MT")]     // 11 dígitos MT (correto)
    public void Validar_IeFormatoCorreto_RetornaTrue(string ie, string uf)
    {
        // Just test that the format is accepted
        IeValidator.Validar(ie, uf).Should().BeTrue();
    }

    [Theory]
    [InlineData("123", "SP")]      // muito curto para SP
    [InlineData("abc", "RJ")]      // letras inválidas
    [InlineData("", "MG")]
    [InlineData(null, "SP")]
    [InlineData("12345", null)]
    [InlineData("12345", "")]
    public void Validar_IeInvalida_RetornaFalse(string? ie, string? uf)
    {
        IeValidator.Validar(ie, uf).Should().BeFalse();
    }

    [Theory]
    [InlineData("SP")]
    [InlineData("RJ")]
    [InlineData("MG")]
    [InlineData("RS")]
    [InlineData("DF")]
    [InlineData("sp")] // lowercase
    public void UfValida_UfExistente_RetornaTrue(string uf)
    {
        IeValidator.UfValida(uf).Should().BeTrue();
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("BR")]
    public void UfValida_UfInexistente_RetornaFalse(string? uf)
    {
        IeValidator.UfValida(uf).Should().BeFalse();
    }

    [Fact]
    public void UfsValidas_Retorna27Estados()
    {
        IeValidator.UfsValidas().Count.Should().Be(27);
    }
}
