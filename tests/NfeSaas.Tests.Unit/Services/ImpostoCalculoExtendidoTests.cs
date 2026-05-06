using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Tests.Unit.Services;

public class ImpostoCalculoExtendidoTests
{
    private readonly ImpostoCalculoService _svc = new();

    // === ICMS arredondamento ===

    [Theory]
    [InlineData(100.005, 12, 12.00)]
    [InlineData(100.004, 12, 12.00)]
    [InlineData(333.33, 18, 60.00)]
    public void CalcularIcms_ArredondamentoCents(decimal valor, decimal aliquota, decimal esperado)
    {
        var r = _svc.CalcularIcms(valor, aliquota);
        r.Valor.Should().BeApproximately(esperado, 0.01m);
    }

    [Fact]
    public void CalcularIcms_ValorNegativo_BaseNegativa()
    {
        // Desconto maior que produto resulta em base negativa — sistema aceita mas valor é zero
        var r = _svc.CalcularIcms(-100m, 12m);
        r.BaseCalculo.Should().Be(-100m);
        r.Valor.Should().BeLessThanOrEqualTo(0m);
    }

    [Theory]
    [InlineData(1000, 12, 100, 0, 0)]    // redução 100% → base zero
    [InlineData(500, 7, 0, 500, 35)]     // redução 0% → base original
    public void CalcularIcms_CasosLimite(decimal valor, decimal aliquota, decimal reducao,
        decimal baseEsperada, decimal valorEsperado)
    {
        var r = _svc.CalcularIcms(valor, aliquota, reducao);
        r.BaseCalculo.Should().Be(baseEsperada);
        r.Valor.Should().BeApproximately(valorEsperado, 0.01m);
    }

    // === PIS / COFINS alíquotas ===

    [Theory]
    [InlineData(0.65)]   // Lucro Presumido PIS
    [InlineData(1.65)]   // Lucro Real PIS
    [InlineData(3.00)]   // Lucro Presumido COFINS
    [InlineData(7.60)]   // Lucro Real COFINS
    public void CalcularPis_AliquotasRegime_BaseIgualAoValorProduto(decimal aliquota)
    {
        var r = _svc.CalcularPis(1000m, aliquota);
        r.BaseCalculo.Should().Be(1000m);
        r.Valor.Should().Be(Math.Round(1000m * aliquota / 100, 2));
    }

    [Theory]
    [InlineData(0.65)]
    [InlineData(3.00)]
    [InlineData(7.60)]
    public void CalcularCofins_BaseIgualAoValorProduto(decimal aliquota)
    {
        var r = _svc.CalcularCofins(1000m, aliquota);
        r.BaseCalculo.Should().Be(1000m);
        r.Valor.Should().Be(Math.Round(1000m * aliquota / 100, 2));
    }

    // === ICMS-ST ===

    [Theory]
    // MVA 40%, interno 18%, interestadual 12%: base=1400, interno=252, interestadual=120, ST=132
    [InlineData(1000, 40, 18, 12, 1400, 132)]
    // MVA 60%, interno 12%, interestadual 7%: base=800, interno=96, interestadual=35, ST=61
    [InlineData(500, 60, 12, 7, 800, 61)]
    public void CalcularIcmsSt_ValoresComuns(decimal valor, decimal mva,
        decimal aliqInterna, decimal aliqInterest,
        decimal baseEsperada, decimal valorEsperado)
    {
        var r = _svc.CalcularIcmsSt(valor, mva, aliqInterna, aliqInterest);
        r.BaseCalculo.Should().BeApproximately(baseEsperada, 0.01m);
        r.Valor.Should().BeApproximately(valorEsperado, 0.01m);
    }

    [Fact]
    public void CalcularIcmsSt_AliquotaInternaMenorQueInterestadual_NuncaNegativo()
    {
        // ST cannot be negative (state cannot owe taxpayer)
        var r = _svc.CalcularIcmsSt(1000m, 0m, 5m, 12m);
        r.Valor.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void CalcularIcmsSt_MvaZero_BaseIgualOriginal()
    {
        var r = _svc.CalcularIcmsSt(1000m, 0m, 18m, 12m);
        r.BaseCalculo.Should().Be(1000m);
    }

    // === Consistência: PIS + COFINS nunca excedem valor produto ===

    [Theory]
    [InlineData(100, 0.65, 3)]
    [InlineData(1000, 1.65, 7.60)]
    [InlineData(50000, 0.65, 3)]
    public void PisMaisCofins_NuncaExcedemValorProduto(decimal valor, decimal aliqPis, decimal aliqCofins)
    {
        var pis = _svc.CalcularPis(valor, aliqPis);
        var cofins = _svc.CalcularCofins(valor, aliqCofins);
        (pis.Valor + cofins.Valor).Should().BeLessThan(valor);
    }

    // === Valor total com impostos ===

    [Fact]
    public void TotalComImpostos_SimplesNacional_NaoUsaPisCofins()
    {
        // Simples Nacional: ICMS dentro do DAS, PIS/COFINS zerado (CST 07)
        var pis = _svc.CalcularPis(1000m, 0m);
        var cofins = _svc.CalcularCofins(1000m, 0m);

        pis.Valor.Should().Be(0m);
        cofins.Valor.Should().Be(0m);
    }

    [Fact]
    public void CalcularIcms_AliquotaMaxima_NaoExcedeBase()
    {
        // ICMS máximo em SP é 25%
        var r = _svc.CalcularIcms(1000m, 25m);
        r.Valor.Should().Be(250m);
        r.Valor.Should().BeLessThanOrEqualTo(r.BaseCalculo);
    }
}
