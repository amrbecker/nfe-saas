using NfeSaas.Domain.Services;

namespace NfeSaas.Tests.Unit.Services;

public class CnpjValidatorTests
{
    // === CNPJ válidos conhecidos ===
    [Theory]
    [InlineData("11.222.333/0001-81")]   // formatado
    [InlineData("11222333000181")]        // apenas dígitos
    [InlineData("00.000.000/0001-91")]   // seed do projeto
    [InlineData("99.999.999/0001-91")]   // seed do projeto (escritório)
    public void Validar_CnpjValido_RetornaTrue(string cnpj)
    {
        CnpjValidator.Validar(cnpj).Should().BeTrue();
    }

    [Theory]
    [InlineData("11.111.111/1111-11")]  // todos iguais
    [InlineData("00000000000000")]       // zeros
    [InlineData("12345678000199")]       // dígitos verificadores errados
    [InlineData("1234567800019")]        // tamanho errado (13 dígitos)
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validar_CnpjInvalido_RetornaFalse(string? cnpj)
    {
        CnpjValidator.Validar(cnpj).Should().BeFalse();
    }

    [Fact]
    public void Validar_CnpjComMascara_MesmoResultadoSemMascara()
    {
        var comMascara = "00.000.000/0001-91";
        var semMascara = "00000000000191";
        CnpjValidator.Validar(comMascara).Should().Be(CnpjValidator.Validar(semMascara));
    }

    // === CPF ===
    [Theory]
    [InlineData("529.982.247-25")]   // CPF válido formatado
    [InlineData("52998224725")]      // CPF válido sem formatação
    public void ValidarCpf_CpfValido_RetornaTrue(string cpf)
    {
        CnpjValidator.ValidarCpf(cpf).Should().BeTrue();
    }

    [Theory]
    [InlineData("111.111.111-11")]   // todos iguais
    [InlineData("000.000.000-00")]   // todos zeros
    [InlineData("")]
    [InlineData(null)]
    public void ValidarCpf_CpfInvalido_RetornaFalse(string? cpf)
    {
        CnpjValidator.ValidarCpf(cpf).Should().BeFalse();
    }

    [Fact]
    public void ApenasDigitos_RemovePontuacao()
    {
        CnpjValidator.ApenasDigitos("11.222.333/0001-81").Should().Be("11222333000181");
    }

    [Fact]
    public void ApenasDigitos_NullRetornaVazio()
    {
        CnpjValidator.ApenasDigitos(null).Should().Be("");
    }

    [Fact]
    public void FormatarCnpj_14Digitos_RetornaFormatado()
    {
        CnpjValidator.FormatarCnpj("11222333000181").Should().Be("11.222.333/0001-81");
    }

    [Fact]
    public void FormatarCnpj_MenosDe14Digitos_RetornaSemFormatar()
    {
        CnpjValidator.FormatarCnpj("123").Should().Be("123");
    }
}
