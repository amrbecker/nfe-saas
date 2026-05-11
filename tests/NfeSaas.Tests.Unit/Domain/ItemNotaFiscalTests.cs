using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class ItemNotaFiscalTests
{
    private static ItemNotaFiscal Criar(decimal qtd = 2m, decimal valor = 100m, decimal desc = 0m) =>
        ItemNotaFiscal.Criar(Guid.NewGuid(), 1, "PROD01", "Item",
            "12345678", "5102", "UN", qtd, valor, desc);

    [Fact]
    public void Criar_DeveCalcularValorTotalCorretamente()
    {
        var item = Criar(qtd: 3m, valor: 25m, desc: 5m);
        item.ValorTotal.Should().Be(70m); // 3*25 - 5
        item.Quantidade.Should().Be(3m);
        item.ValorUnitario.Should().Be(25m);
        item.ValorDesconto.Should().Be(5m);
    }

    [Fact]
    public void SetIcms_DeveCalcularValorEZerarCsosn()
    {
        var item = Criar();
        item.SetIcms(OrigemMercadoria.Nacional, CstIcms.Tributada, 200m, 18m);

        item.OrigemMercadoria.Should().Be(OrigemMercadoria.Nacional);
        item.CstIcms.Should().Be(CstIcms.Tributada);
        item.CsosnIcms.Should().BeNull();
        item.BaseCalculoIcms.Should().Be(200m);
        item.AliquotaIcms.Should().Be(18m);
        item.ValorIcms.Should().Be(36m); // 200 * 18%
    }

    [Theory]
    [InlineData(CsosnIcms.TributadaSemPermissaoCredito, 0)]   // 102 — sem cálculo
    [InlineData(CsosnIcms.IsencaoIcmsFaixaReceitaBruta, 0)]   // 103 — isenção
    [InlineData(CsosnIcms.Imune, 0)]                          // 300 — imune
    [InlineData(CsosnIcms.NaoTributada, 0)]                   // 400 — não tributada
    [InlineData(CsosnIcms.IcmsCobradoAnteriormentePorSt, 0)]  // 500 — ST anterior
    public void SetIcmsSimples_NaoCalcula_QuandoCsosnZeraIcms(CsosnIcms csosn, decimal valorEsperado)
    {
        var item = Criar();
        item.SetIcmsSimples(OrigemMercadoria.Nacional, csosn, 200m, 18m);

        item.CsosnIcms.Should().Be(csosn);
        item.ValorIcms.Should().Be(valorEsperado);
    }

    [Fact]
    public void SetIcmsSimples_Outros900_CalculaIcms()
    {
        var item = Criar();
        item.SetIcmsSimples(OrigemMercadoria.Nacional, CsosnIcms.Outros, 200m, 18m);

        item.CsosnIcms.Should().Be(CsosnIcms.Outros);
        item.ValorIcms.Should().Be(36m); // CSOSN 900 calcula como CST normal
    }

    [Fact]
    public void SetIpi_DeveCalcularValor()
    {
        var item = Criar();
        item.SetIpi("50", baseCalculo: 200m, aliquota: 10m);

        item.CstIpi.Should().Be("50");
        item.BaseCalculoIpi.Should().Be(200m);
        item.AliquotaIpi.Should().Be(10m);
        item.ValorIpi.Should().Be(20m);
    }

    [Fact]
    public void SetFcp_DeveCalcularValor()
    {
        var item = Criar();
        item.SetFcp(baseCalculo: 200m, aliquota: 2m);

        item.BaseCalculoFcp.Should().Be(200m);
        item.AliquotaFcp.Should().Be(2m);
        item.ValorFcp.Should().Be(4m);
    }

    [Fact]
    public void SetDifal_DeveCalcularPartilha100PorcentoDestino()
    {
        var item = Criar();
        // Operação interestadual SP→RJ: alíquota interna RJ=20%, interestadual=12%
        item.SetDifal(baseCalculo: 1000m, aliquotaInternaUfDestino: 20m, aliquotaInterestadual: 12m);

        item.BaseCalculoDifal.Should().Be(1000m);
        item.AliquotaInternaUfDestino.Should().Be(20m);
        item.AliquotaInterestadual.Should().Be(12m);
        // Diferença = 8% → 1000 * 8% = 80
        item.ValorIcmsUfDestino.Should().Be(80m);
        item.ValorIcmsUfRemetente.Should().Be(0m);  // 100% destino desde 2019
    }

    [Fact]
    public void SetDifal_QuandoAliquotaInternaIgualOuMenorInterestadual_ResultaZero()
    {
        var item = Criar();
        item.SetDifal(baseCalculo: 1000m, aliquotaInternaUfDestino: 12m, aliquotaInterestadual: 12m);
        item.ValorIcmsUfDestino.Should().Be(0m);
    }

    [Fact]
    public void SetIcms_AposSetIcmsSimples_DeveLimparCsosn()
    {
        var item = Criar();
        item.SetIcmsSimples(OrigemMercadoria.Nacional, CsosnIcms.TributadaSemPermissaoCredito, 100m, 0m);
        item.CsosnIcms.Should().NotBeNull();

        item.SetIcms(OrigemMercadoria.Nacional, CstIcms.Tributada, 100m, 18m);
        item.CsosnIcms.Should().BeNull();
    }

    [Fact]
    public void SetPis_DeveCalcularValor()
    {
        var item = Criar();
        item.SetPis(CstPisCofins.TributadaAliquotaBasica, 200m, 1.65m);
        item.ValorPis.Should().Be(3.30m);
    }

    [Fact]
    public void SetCofins_DeveCalcularValor()
    {
        var item = Criar();
        item.SetCofins(CstPisCofins.TributadaAliquotaBasica, 200m, 7.6m);
        item.ValorCofins.Should().Be(15.20m);
    }

    [Fact]
    public void SetCest_DevePersistir()
    {
        var item = Criar();
        item.SetCest("0100100");
        item.Cest.Should().Be("0100100");
    }

    [Fact]
    public void SetCodigoEan_DevePersistir()
    {
        var item = Criar();
        item.SetCodigoEan("7891234567895");
        item.CodigoEan.Should().Be("7891234567895");
    }

    [Fact]
    public void SetIcmsSt_DeveCalcularValor()
    {
        var item = Criar();
        item.SetIcmsSt(baseCalculo: 250m, aliquota: 18m);
        item.BaseCalculoIcmsSt.Should().Be(250m);
        item.AliquotaIcmsSt.Should().Be(18m);
        item.ValorIcmsSt.Should().Be(45m);
    }

    [Theory]
    [InlineData(CsosnIcms.TributadaComPermissaoCredito, 101)]
    [InlineData(CsosnIcms.TributadaSemPermissaoCredito, 102)]
    [InlineData(CsosnIcms.IsencaoIcmsFaixaReceitaBruta, 103)]
    [InlineData(CsosnIcms.TributadaComPermissaoCreditoSt, 201)]
    [InlineData(CsosnIcms.TributadaSemPermissaoCreditoSt, 202)]
    [InlineData(CsosnIcms.IsencaoIcmsFaixaReceitaBrutaSt, 203)]
    [InlineData(CsosnIcms.Imune, 300)]
    [InlineData(CsosnIcms.NaoTributada, 400)]
    [InlineData(CsosnIcms.IcmsCobradoAnteriormentePorSt, 500)]
    [InlineData(CsosnIcms.Outros, 900)]
    public void CsosnIcms_TemValoresSefazCorretos(CsosnIcms cs, int valorEsperado)
    {
        ((int)cs).Should().Be(valorEsperado);
    }
}
