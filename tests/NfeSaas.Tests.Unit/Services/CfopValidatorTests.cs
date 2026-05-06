using NfeSaas.Domain.Services;

namespace NfeSaas.Tests.Unit.Services;

public class CfopValidatorTests
{
    [Theory]
    [InlineData("5102")]
    [InlineData("6102")]
    [InlineData("1102")]
    [InlineData("7102")]
    public void Existe_CfopCadastrado_RetornaTrue(string cfop)
    {
        CfopValidator.Existe(cfop).Should().BeTrue();
    }

    [Theory]
    [InlineData("9999")]
    [InlineData("0001")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("51")]
    public void Existe_CfopNaoCadastrado_RetornaFalse(string? cfop)
    {
        CfopValidator.Existe(cfop).Should().BeFalse();
    }

    [Theory]
    [InlineData("5102", false)]  // saída intraestadual
    [InlineData("5101", false)]  // saída intraestadual produção
    [InlineData("5403", false)]  // saída intraestadual ST
    public void ValidarParaSaida_CfopIntraestadual_SaidaNaoInterestadual_RetornaTrue(string cfop, bool interestadual)
    {
        CfopValidator.ValidarParaSaida(cfop, interestadual).Should().BeTrue();
    }

    [Theory]
    [InlineData("6102", true)]   // saída interestadual
    [InlineData("6101", true)]
    public void ValidarParaSaida_CfopInterestadual_SaidaInterestadual_RetornaTrue(string cfop, bool interestadual)
    {
        CfopValidator.ValidarParaSaida(cfop, interestadual).Should().BeTrue();
    }

    [Theory]
    [InlineData("5102", true)]  // intraestadual mas operação interestadual
    [InlineData("6102", false)] // interestadual mas operação intraestadual
    [InlineData("1102", false)] // CFOP de entrada em operação de saída
    public void ValidarParaSaida_CfopErrado_RetornaFalse(string cfop, bool interestadual)
    {
        CfopValidator.ValidarParaSaida(cfop, interestadual).Should().BeFalse();
    }

    [Theory]
    [InlineData("1102", false)]  // entrada intraestadual
    [InlineData("2102", true)]   // entrada interestadual
    public void ValidarParaEntrada_CfopCorreto_RetornaTrue(string cfop, bool interestadual)
    {
        CfopValidator.ValidarParaEntrada(cfop, interestadual).Should().BeTrue();
    }

    [Theory]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    public void EhSaida_PrefixosSaida_RetornaTrue(string prefixo)
    {
        CfopValidator.EhSaida($"{prefixo}102").Should().BeTrue();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public void EhSaida_PrefixosEntrada_RetornaFalse(string prefixo)
    {
        CfopValidator.EhSaida($"{prefixo}102").Should().BeFalse();
    }

    [Theory]
    [InlineData("6102")]
    [InlineData("2102")]
    [InlineData("7101")]
    [InlineData("3101")]
    public void EhInterestadual_CfopInterestadual_RetornaTrue(string cfop)
    {
        CfopValidator.EhInterestadual(cfop).Should().BeTrue();
    }

    [Theory]
    [InlineData("5102")]
    [InlineData("1102")]
    public void EhInterestadual_CfopIntraestadual_RetornaFalse(string cfop)
    {
        CfopValidator.EhInterestadual(cfop).Should().BeFalse();
    }

    [Fact]
    public void ObterDescricao_CfopExistente_RetornaDescricao()
    {
        var desc = CfopValidator.ObterDescricao("5102");
        desc.Should().NotBeNullOrWhiteSpace();
        desc.Should().Contain("mercadoria");
    }

    [Fact]
    public void ObterDescricao_CfopInexistente_RetornaNull()
    {
        CfopValidator.ObterDescricao("9999").Should().BeNull();
    }
}
