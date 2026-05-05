using FluentAssertions;
using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Tests.Unit.Services;

public class ImpostoCalculoServiceTests
{
    private readonly ImpostoCalculoService _service = new();

    // === ICMS ===

    [Fact]
    public void CalcularIcms_SemReducao_BaseIgualAoValorProduto()
    {
        var resultado = _service.CalcularIcms(valorProduto: 1000m, aliquota: 12m);

        resultado.BaseCalculo.Should().Be(1000m);
        resultado.Aliquota.Should().Be(12m);
        resultado.Valor.Should().Be(120m);
    }

    [Theory]
    [InlineData(1000, 12, 50, 500, 60)]  // base reduzida 50%, 12% sobre 500 = 60
    [InlineData(1000, 7,  33.33, 666.70, 46.67)] // redução 1/3
    public void CalcularIcms_ComReducao_ReduzBaseDeCalculo(
        decimal valor, decimal aliquota, decimal reducao,
        decimal baseEsperada, decimal valorEsperado)
    {
        var resultado = _service.CalcularIcms(valor, aliquota, reducao);

        resultado.BaseCalculo.Should().BeApproximately(baseEsperada, 0.02m);
        resultado.Valor.Should().BeApproximately(valorEsperado, 0.02m);
    }

    [Fact]
    public void CalcularIcms_AliquotaZero_ValorZero()
    {
        var resultado = _service.CalcularIcms(500m, 0m);
        resultado.Valor.Should().Be(0m);
    }

    // === PIS ===

    [Fact]
    public void CalcularPis_AliquotaPadrao_RetornaValorCorreto()
    {
        var resultado = _service.CalcularPis(valorProduto: 1000m, aliquota: 0.65m);

        resultado.BaseCalculo.Should().Be(1000m);
        resultado.Valor.Should().Be(6.50m);
    }

    [Theory]
    [InlineData(500, 0.65, 3.25)]
    [InlineData(1000, 1.65, 16.50)]
    [InlineData(250.50, 0.65, 1.63)]
    public void CalcularPis_DiversosValores(decimal base_, decimal aliquota, decimal esperado)
    {
        var resultado = _service.CalcularPis(base_, aliquota);
        resultado.Valor.Should().BeApproximately(esperado, 0.01m);
    }

    // === COFINS ===

    [Fact]
    public void CalcularCofins_AliquotaPadrao_RetornaValorCorreto()
    {
        var resultado = _service.CalcularCofins(valorProduto: 1000m, aliquota: 3m);

        resultado.BaseCalculo.Should().Be(1000m);
        resultado.Valor.Should().Be(30m);
    }

    // === ICMS-ST ===

    [Fact]
    public void CalcularIcmsSt_ComMva_RetornaValorDiferencial()
    {
        // MVA 40%, aliquota interna 12%, aliquota interestadual 7%
        // Base ST = 1000 * (1 + 40/100) = 1400
        // ICMS interno = 1400 * 12% = 168
        // ICMS interestadual = 1000 * 7% = 70
        // ST = 168 - 70 = 98
        var resultado = _service.CalcularIcmsSt(1000m, mva: 40m, aliquotaInterna: 12m, aliquotaInterestadual: 7m);

        resultado.BaseCalculo.Should().Be(1400m);
        resultado.Valor.Should().Be(98m);
    }

    [Fact]
    public void CalcularIcmsSt_QuandoIcmsInternoMenorQueInterestadual_RetornaZero()
    {
        // Caso onde aliquota interna < interestadual → ST não pode ser negativo
        var resultado = _service.CalcularIcmsSt(1000m, mva: 10m, aliquotaInterna: 5m, aliquotaInterestadual: 12m);

        resultado.Valor.Should().Be(0m);
    }

    [Fact]
    public void CalcularIcmsSt_SemMva_BaseSt_IgualAoValorOriginal()
    {
        var resultado = _service.CalcularIcmsSt(1000m, mva: 0m, aliquotaInterna: 12m, aliquotaInterestadual: 7m);

        resultado.BaseCalculo.Should().Be(1000m);
        resultado.Valor.Should().Be(50m); // 120 - 70
    }
}
