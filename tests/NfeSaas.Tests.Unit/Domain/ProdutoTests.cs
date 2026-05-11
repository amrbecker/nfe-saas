using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class ProdutoTests
{
    private static Produto CriarProduto() =>
        Produto.Criar(
            Guid.NewGuid(), "PROD001", "Produto Teste", "12345678",
            "5102", "UN", OrigemMercadoria.Nacional, valorUnitarioPadrao: 10.50m);

    [Fact]
    public void Criar_DevePersistirCamposObrigatorios()
    {
        var empresaId = Guid.NewGuid();
        var p = Produto.Criar(empresaId, "P01", "Descrição", "12345678",
            "5102", "UN", OrigemMercadoria.Nacional, 25m);

        p.EmpresaId.Should().Be(empresaId);
        p.Codigo.Should().Be("P01");
        p.Descricao.Should().Be("Descrição");
        p.Ncm.Should().Be("12345678");
        p.CfopPadrao.Should().Be("5102");
        p.UnidadeComercial.Should().Be("UN");
        p.OrigemMercadoria.Should().Be(OrigemMercadoria.Nacional);
        p.ValorUnitarioPadrao.Should().Be(25m);
        p.Cest.Should().BeNull();
        p.CodigoEan.Should().BeNull();
        p.CodigoAnp.Should().BeNull();
        p.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Criar_ComCamposOpcionais_DevePersistir()
    {
        var p = Produto.Criar(Guid.NewGuid(), "C01", "Combustível", "27101259",
            "5101", "L", OrigemMercadoria.EstrangeiraImportacaoDireta, 5.99m,
            cest: "0100100", codigoEan: "7891234567895", codigoAnp: "320101001");

        p.Cest.Should().Be("0100100");
        p.CodigoEan.Should().Be("7891234567895");
        p.CodigoAnp.Should().Be("320101001");
        p.OrigemMercadoria.Should().Be(OrigemMercadoria.EstrangeiraImportacaoDireta);
    }

    [Fact]
    public void Atualizar_DeveModificarTodosOsCampos()
    {
        var p = CriarProduto();
        var antes = p.UpdatedAt;

        p.Atualizar("NEW01", "Nova Descrição", "87654321",
            "5405", "KG", OrigemMercadoria.NacionalConteudoImportacaoSuperior40, 99.99m,
            cest: "1234567", codigoEan: null, codigoAnp: null);

        p.Codigo.Should().Be("NEW01");
        p.Descricao.Should().Be("Nova Descrição");
        p.Ncm.Should().Be("87654321");
        p.CfopPadrao.Should().Be("5405");
        p.UnidadeComercial.Should().Be("KG");
        p.OrigemMercadoria.Should().Be(OrigemMercadoria.NacionalConteudoImportacaoSuperior40);
        p.ValorUnitarioPadrao.Should().Be(99.99m);
        p.Cest.Should().Be("1234567");
        p.CodigoEan.Should().BeNull();
        p.UpdatedAt.Should().NotBe(antes);
    }

    [Fact]
    public void Desativar_DeveMudarStatus()
    {
        var p = CriarProduto();
        p.Desativar();
        p.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Ativar_AposDesativar_DeveRestaurarStatus()
    {
        var p = CriarProduto();
        p.Desativar();
        p.Ativar();
        p.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Delete_NovoProduto_DeveMarcarIsDeleted()
    {
        var p = CriarProduto();
        p.Delete();
        p.IsDeleted.Should().BeTrue();
    }

    [Theory]
    [InlineData(OrigemMercadoria.Nacional, 0)]
    [InlineData(OrigemMercadoria.EstrangeiraImportacaoDireta, 1)]
    [InlineData(OrigemMercadoria.EstrangeiraAdquiridaMercadoInterno, 2)]
    [InlineData(OrigemMercadoria.NacionalConteudoImportacaoSuperior40, 3)]
    [InlineData(OrigemMercadoria.NacionalProcessosBasicos, 4)]
    [InlineData(OrigemMercadoria.NacionalConteudoImportacaoInferior40, 5)]
    [InlineData(OrigemMercadoria.EstrangeiraImportacaoDiretaSemSimilar, 6)]
    [InlineData(OrigemMercadoria.EstrangeiraAdquiridaMercadoInternoSemSimilar, 7)]
    [InlineData(OrigemMercadoria.NacionalConteudoImportacaoSuperior70, 8)]
    public void OrigemMercadoria_TemValoresSefazCorretos(OrigemMercadoria origem, int valorEsperado)
    {
        ((int)origem).Should().Be(valorEsperado);
    }
}
