using FluentAssertions;
using NfeSaas.Domain.Entities;

namespace NfeSaas.Tests.Unit.Domain;

public class NcmTests
{
    [Fact]
    public void Criar_CodigoComMascara_NormalizaApenasDigitos()
    {
        var ncm = Ncm.Criar("8517.12.31", "Telefones celulares", "2024-12");

        ncm.Codigo.Should().Be("85171231");
        ncm.CategoriaCapitulo.Should().Be("85");
        ncm.Posicao.Should().Be("8517");
    }

    [Theory]
    [InlineData("1234567")]    // 7 dígitos
    [InlineData("123456789")]  // 9 dígitos
    [InlineData("")]
    [InlineData("abcdefgh")]
    public void Criar_CodigoComMenosOuMaisQueOitoDigitos_LancaArgumentException(string codigo)
    {
        var act = () => Ncm.Criar(codigo, "Descrição válida", "2024-12");
        act.Should().Throw<ArgumentException>().WithMessage("*8 dígitos*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_DescricaoVazia_LancaArgumentException(string? descricao)
    {
        var act = () => Ncm.Criar("85171231", descricao!, "2024-12");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Criar_PreencheCapituloEPosicaoAutomaticamenteQuandoOmitidos()
    {
        var ncm = Ncm.Criar("22011000", "Águas minerais", "2024-12");

        ncm.CategoriaCapitulo.Should().Be("22");
        ncm.Posicao.Should().Be("2201");
    }

    [Fact]
    public void Atualizar_AlteraDescricaoEVersaoEMarcaUpdatedEm()
    {
        var ncm = Ncm.Criar("85171231", "Telefones", "2024-11");
        var antes = ncm.AtualizadoEm;
        Thread.Sleep(10);

        ncm.Atualizar("Telefones celulares (smartphones)", "2024-12", aliquotaIpi: 5m, exigeCest: true);

        ncm.Descricao.Should().Be("Telefones celulares (smartphones)");
        ncm.VersaoTabela.Should().Be("2024-12");
        ncm.AliquotaIpiPadrao.Should().Be(5m);
        ncm.ExigeCest.Should().BeTrue();
        ncm.AtualizadoEm.Should().BeAfter(antes);
    }

    [Fact]
    public void Desativar_ChangesAtivoToFalse()
    {
        var ncm = Ncm.Criar("85171231", "Telefones", "2024-12");
        ncm.Ativo.Should().BeTrue();

        ncm.Desativar();
        ncm.Ativo.Should().BeFalse();

        ncm.Ativar();
        ncm.Ativo.Should().BeTrue();
    }
}
