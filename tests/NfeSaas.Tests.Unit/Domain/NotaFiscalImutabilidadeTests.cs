using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class NotaFiscalImutabilidadeTests
{
    private static NotaFiscal CriarAutorizada()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.MarcarEnviada("<xml/>");
        nota.Autorizar("CHAVE", "PROTO", "<retorno/>");
        return nota;
    }

    [Fact]
    public void SetDestinatario_NotaAutorizada_DeveLancar()
    {
        var nota = CriarAutorizada();
        Action a = () => nota.SetDestinatario("123", "X", null, TipoPessoa.PessoaFisica,
            "Rua", "1", "Bairro", "Cidade", "SP", "01000000", "3550308");
        a.Should().Throw<InvalidOperationException>().WithMessage("*alterar destinatário*");
    }

    [Fact]
    public void SetTransporte_NotaAutorizada_DeveLancar()
    {
        var nota = CriarAutorizada();
        Action a = () => nota.SetTransporte(ModalidadeFrete.SemFrete);
        a.Should().Throw<InvalidOperationException>().WithMessage("*alterar transporte*");
    }

    [Fact]
    public void SetPagamento_NotaAutorizada_DeveLancar()
    {
        var nota = CriarAutorizada();
        Action a = () => nota.SetPagamento("01", 100m);
        a.Should().Throw<InvalidOperationException>().WithMessage("*alterar pagamento*");
    }

    [Fact]
    public void SetInformacoesAdicionais_NotaAutorizada_DeveLancar()
    {
        var nota = CriarAutorizada();
        Action a = () => nota.SetInformacoesAdicionais("texto");
        a.Should().Throw<InvalidOperationException>().WithMessage("*informações adicionais*");
    }

    [Fact]
    public void AdicionarItem_NotaAutorizada_DeveLancar()
    {
        var nota = CriarAutorizada();
        var item = ItemNotaFiscal.Criar(nota.Id, 1, "P", "D", "12345678", "5102", "UN", 1m, 10m);
        Action a = () => nota.AdicionarItem(item);
        a.Should().Throw<InvalidOperationException>().WithMessage("*adicionar item*");
    }

    [Fact]
    public void SetXmlEnvio_NotaAutorizada_DeveLancar()
    {
        var nota = CriarAutorizada();
        Action a = () => nota.SetXmlEnvio("<novo/>");
        a.Should().Throw<InvalidOperationException>().WithMessage("*XML de envio*");
    }

    [Fact]
    public void MarcarContingencia_NotaAutorizada_DeveLancar()
    {
        var nota = CriarAutorizada();
        Action a = () => nota.MarcarContingencia(TipoEmissao.ContingenciaSvcAn);
        a.Should().Throw<InvalidOperationException>().WithMessage("*contingência*");
    }

    [Fact]
    public void Cancelar_NotaNaoAutorizada_DeveLancar()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        Action a = () => nota.Cancelar("<xml/>");
        a.Should().Throw<InvalidOperationException>()
            .WithMessage("*Apenas notas autorizadas*");
    }

    [Fact]
    public void Cancelar_NotaAutorizada_DefineDataCancelamento()
    {
        var nota = CriarAutorizada();
        var antes = DateTime.UtcNow;
        nota.Cancelar("<canc/>");

        nota.Situacao.Should().Be(SituacaoNota.Cancelada);
        nota.DataCancelamento.Should().NotBeNull();
        nota.DataCancelamento!.Value.Should().BeOnOrAfter(antes);
    }

    [Fact]
    public void Cancelar_NotaJaCancelada_DeveLancar()
    {
        var nota = CriarAutorizada();
        nota.Cancelar("<canc/>");
        Action a = () => nota.Cancelar("<canc2/>");
        a.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DataDescarteAutorizado_Autorizada_Igual5AnosAposAutorizacao()
    {
        var nota = CriarAutorizada();
        var esperado = nota.DataAutorizacao!.Value.AddYears(5);
        nota.DataDescarteAutorizado.Should().BeCloseTo(esperado, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DataDescarteAutorizado_Cancelada_Igual5AnosAposCancelamento()
    {
        var nota = CriarAutorizada();
        nota.Cancelar("<canc/>");
        var esperado = nota.DataCancelamento!.Value.AddYears(5);
        nota.DataDescarteAutorizado.Should().BeCloseTo(esperado, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DataDescarteAutorizado_Rascunho_RetornaNull()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.DataDescarteAutorizado.Should().BeNull();
        nota.DentroPeriodoRetencao.Should().BeFalse();
    }

    [Fact]
    public void DentroPeriodoRetencao_NotaRecemAutorizada_RetornaTrue()
    {
        var nota = CriarAutorizada();
        nota.DentroPeriodoRetencao.Should().BeTrue();
    }

    [Fact]
    public void Delete_NotaDentroRetencao_DeveLancar()
    {
        var nota = CriarAutorizada();
        Action a = () => nota.Delete();
        a.Should().Throw<InvalidOperationException>()
            .WithMessage("*retenção fiscal*");
        nota.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Delete_NotaRascunho_PermitidoSemErro()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.Delete();
        nota.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_NotaRejeitada_PermitidoSemErro()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.MarcarEnviada("<xml/>");
        nota.Rejeitar("erro");

        nota.Delete();
        nota.IsDeleted.Should().BeTrue();
    }
}
