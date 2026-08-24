using FluentAssertions;
using NfeSaas.Domain.Entities;

namespace NfeSaas.Tests.Unit.Domain;

public class CnaeTests
{
    [Fact]
    public void Criar_CodigoComMascara_NormalizaApenasDigitos()
    {
        var cnae = Cnae.Criar("6920-6/01", "Atividades de contabilidade");

        cnae.Codigo.Should().Be("6920601");
    }

    [Theory]
    [InlineData("123456")]    // 6 dígitos
    [InlineData("12345678")]  // 8 dígitos
    [InlineData("")]
    [InlineData("abcdefg")]
    public void Criar_CodigoComMenosOuMaisQueSeteDigitos_LancaArgumentException(string codigo)
    {
        var act = () => Cnae.Criar(codigo, "Descrição válida");
        act.Should().Throw<ArgumentException>().WithMessage("*7 dígitos*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_DescricaoVazia_LancaArgumentException(string? descricao)
    {
        var act = () => Cnae.Criar("6920601", descricao!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Criar_PreencheDivisaoAutomaticamenteQuandoOmitida()
    {
        var cnae = Cnae.Criar("6920601", "Atividades de contabilidade");

        cnae.Divisao.Should().Be("69");
        cnae.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Criar_ComSecaoEDivisaoExplicitas_UsaValoresInformados()
    {
        var cnae = Cnae.Criar("6920601", "Atividades de contabilidade", secao: "M", divisao: "69");

        cnae.Secao.Should().Be("M");
        cnae.Divisao.Should().Be("69");
    }
}
