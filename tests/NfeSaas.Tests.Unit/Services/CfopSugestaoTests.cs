using FluentAssertions;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Services;

namespace NfeSaas.Tests.Unit.Services;

public class CfopSugestaoTests
{
    // ============================================================
    // Matriz Saída × Intra/Inter × Normal/Devolução
    // ============================================================

    [Fact]
    public void Sugerir_SaidaIntraestadualNormal_TopEhVendaMercadoria5102()
    {
        var sug = CfopValidator.Sugerir("SP", "SP", TipoOperacao.Saida, FinalidadeNota.Normal).ToList();

        sug.Should().NotBeEmpty();
        sug.First().Codigo.Should().Be("5102");
        sug.First().Saida.Should().BeTrue();
        sug.First().Interestadual.Should().BeFalse();
    }

    [Fact]
    public void Sugerir_SaidaInterestadualNormal_TopEhVendaMercadoria6102()
    {
        var sug = CfopValidator.Sugerir("SP", "MG", TipoOperacao.Saida, FinalidadeNota.Normal).ToList();

        sug.First().Codigo.Should().Be("6102");
        sug.First().Interestadual.Should().BeTrue();
    }

    [Fact]
    public void Sugerir_SaidaIntraestadualDevolucao_TopEhDevolucaoCompra5202()
    {
        var sug = CfopValidator.Sugerir("SP", "SP", TipoOperacao.Saida, FinalidadeNota.Devolucao).ToList();

        sug.First().Codigo.Should().Be("5202");
    }

    [Fact]
    public void Sugerir_SaidaInterestadualDevolucao_TopEhDevolucaoCompra6202()
    {
        var sug = CfopValidator.Sugerir("SP", "RJ", TipoOperacao.Saida, FinalidadeNota.Devolucao).ToList();

        sug.First().Codigo.Should().Be("6202");
    }

    [Fact]
    public void Sugerir_SaidaParaExterior_RetornaCfop7xxx()
    {
        var sug = CfopValidator.Sugerir("SP", "EX", TipoOperacao.Saida, FinalidadeNota.Normal, exterior: true).ToList();

        sug.Should().NotBeEmpty();
        sug.First().Codigo.Should().StartWith("7");
    }

    // ============================================================
    // Matriz Entrada × Intra/Inter × Normal
    // ============================================================

    [Fact]
    public void Sugerir_EntradaIntraestadualNormal_TopEhCompraComercializacao1102()
    {
        var sug = CfopValidator.Sugerir("SP", "SP", TipoOperacao.Entrada, FinalidadeNota.Normal).ToList();

        sug.First().Codigo.Should().Be("1102");
        sug.First().Saida.Should().BeFalse();
    }

    [Fact]
    public void Sugerir_EntradaInterestadualNormal_TopEhCompraComercializacao2102()
    {
        var sug = CfopValidator.Sugerir("SP", "MG", TipoOperacao.Entrada, FinalidadeNota.Normal).ToList();

        sug.First().Codigo.Should().Be("2102");
        sug.First().Interestadual.Should().BeTrue();
    }

    [Fact]
    public void Sugerir_EntradaDeImportacao_RetornaCfop3xxx()
    {
        var sug = CfopValidator.Sugerir("SP", "EX", TipoOperacao.Entrada, FinalidadeNota.Normal, exterior: true).ToList();

        sug.First().Codigo.Should().StartWith("3");
    }

    // ============================================================
    // Caso de borda: UF iguais mas com diferença de case
    // ============================================================

    [Theory]
    [InlineData("sp", "SP")]
    [InlineData("SP", "sp")]
    [InlineData("Sp", "sP")]
    public void Sugerir_UfsDiferenciamApenasNoCase_TrataComoIntraestadual(string uf1, string uf2)
    {
        var sug = CfopValidator.Sugerir(uf1, uf2, TipoOperacao.Saida).First();
        sug.Interestadual.Should().BeFalse();
        sug.Codigo[0].Should().Be('5');
    }

    [Fact]
    public void Sugerir_UfEmitenteVazio_AssumeIntraestadual()
    {
        // Sem UF de emitente conhecida, escolhe o caminho mais comum (intra).
        var sug = CfopValidator.Sugerir(null, "SP", TipoOperacao.Saida).First();
        sug.Codigo[0].Should().Be('5');
    }

    // ============================================================
    // Listar / ListarTodos
    // ============================================================

    [Fact]
    public void ListarTodos_RetornaTabelaCompletaCom50MaisCfops()
    {
        var todos = CfopValidator.ListarTodos();
        todos.Count.Should().BeGreaterThan(40);
        todos.Should().ContainKey("5102");
        todos.Should().ContainKey("6102");
        todos.Should().ContainKey("1102");
        todos.Should().ContainKey("2102");
    }

    [Fact]
    public void Listar_FiltraPorSentidoEAbrangencia()
    {
        var saidaIntra = CfopValidator.Listar(saida: true, interestadual: false).ToList();
        saidaIntra.Should().NotBeEmpty();
        saidaIntra.Should().OnlyContain(c => c.Saida && !c.Interestadual);
        saidaIntra.Should().OnlyContain(c => c.Codigo.StartsWith("5"));

        var entradaInter = CfopValidator.Listar(saida: false, interestadual: true).ToList();
        entradaInter.Should().OnlyContain(c => !c.Saida && c.Interestadual);
        entradaInter.Should().OnlyContain(c => c.Codigo.StartsWith("2"));
    }

    // ============================================================
    // Display format
    // ============================================================

    [Fact]
    public void CfopOpcao_DisplayFormata5102Como5Ponto102()
    {
        var op = new CfopOpcao("5102", "Venda de mercadoria adquirida ou recebida de terceiros", false, true);
        op.Display.Should().StartWith("5.102 — ");
    }

    // ============================================================
    // Sanidade: sugestão sempre retorna pelo menos 1 opção para cenários comuns
    // ============================================================

    [Theory]
    [InlineData("SP", "SP", TipoOperacao.Saida)]
    [InlineData("SP", "RJ", TipoOperacao.Saida)]
    [InlineData("MG", "MG", TipoOperacao.Entrada)]
    [InlineData("RS", "SC", TipoOperacao.Entrada)]
    public void Sugerir_CenariosComuns_RetornaPeloMenosUmaSugestao(string ufEmit, string ufDest, TipoOperacao op)
    {
        var sug = CfopValidator.Sugerir(ufEmit, ufDest, op).ToList();
        sug.Should().NotBeEmpty();
    }
}
