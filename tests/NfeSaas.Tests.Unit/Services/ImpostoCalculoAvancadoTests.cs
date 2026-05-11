using FluentAssertions;
using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Tests.Unit.Services;

public class ImpostoCalculoAvancadoTests
{
    private readonly ImpostoCalculoService _service = new();

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(200, 5, 10)]
    [InlineData(1234.56, 12, 148.15)]
    [InlineData(0, 10, 0)]
    public void CalcularIpi_DeveRetornarValorCorreto(decimal valor, decimal aliquota, decimal esperado)
    {
        var r = _service.CalcularIpi(valor, aliquota);
        r.BaseCalculo.Should().Be(Math.Round(valor, 2));
        r.Aliquota.Should().Be(aliquota);
        r.Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData(100, 0, 0)]
    [InlineData(100, 2, 2)]
    [InlineData(500, 2, 10)]
    [InlineData(1000.50, 4, 40.02)]
    public void CalcularFcp_DeveRetornarValorCorreto(decimal baseIcms, decimal aliquota, decimal esperado)
    {
        var r = _service.CalcularFcp(baseIcms, aliquota);
        r.Valor.Should().Be(esperado);
    }

    [Fact]
    public void CalcularDifal_OperacaoInterestadual_CalcuaValorParaDestino()
    {
        // SP→RJ: alíquota interestadual 12%, alíquota interna RJ 20%
        var r = _service.CalcularDifal(valorProduto: 1000m, aliquotaInternaUfDestino: 20m, aliquotaInterestadual: 12m);

        r.BaseCalculo.Should().Be(1000m);
        r.AliquotaInterna.Should().Be(20m);
        r.AliquotaInterestadual.Should().Be(12m);
        r.ValorUfDestino.Should().Be(80m);     // 1000 * (20-12)% = 80
        r.ValorUfRemetente.Should().Be(0m);    // partilha 100% destino desde 2019
    }

    [Fact]
    public void CalcularDifal_AliquotaInternaIgualInterestadual_RetornaZero()
    {
        var r = _service.CalcularDifal(valorProduto: 1000m, aliquotaInternaUfDestino: 12m, aliquotaInterestadual: 12m);
        r.ValorUfDestino.Should().Be(0m);
    }

    [Fact]
    public void CalcularDifal_AliquotaInternaMenorInterestadual_RetornaZeroSemNegativo()
    {
        // Caso atípico mas o cálculo deve clamp em 0, não negativo
        var r = _service.CalcularDifal(valorProduto: 1000m, aliquotaInternaUfDestino: 10m, aliquotaInterestadual: 12m);
        r.ValorUfDestino.Should().Be(0m);
    }

    [Fact]
    public void CalcularDifal_ValorPequeno_ArredondaCorretamente()
    {
        var r = _service.CalcularDifal(valorProduto: 13.33m, aliquotaInternaUfDestino: 18m, aliquotaInterestadual: 12m);
        // (18-12)% de 13.33 = 0.7998 → arredonda 0.80
        r.ValorUfDestino.Should().Be(0.80m);
    }
}
