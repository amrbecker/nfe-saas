using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class NotaFiscalTests
{
    private static NotaFiscal CriarNota() =>
        NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);

    [Fact]
    public void Criar_DeveComecarComSituacaoRascunho()
    {
        var nota = CriarNota();
        nota.Situacao.Should().Be(SituacaoNota.Rascunho);
        nota.ChaveAcesso.Should().BeNull();
        nota.Protocolo.Should().BeNull();
    }

    [Fact]
    public void MarcarEnviada_DeveMudarSituacaoEGuardarXml()
    {
        var nota = CriarNota();
        nota.MarcarEnviada("<xml>envio</xml>");

        nota.Situacao.Should().Be(SituacaoNota.Enviada);
        nota.XmlEnvio.Should().Be("<xml>envio</xml>");
    }

    [Fact]
    public void Autorizar_DeveMudarSituacaoEGuardarChaveEProtocolo()
    {
        var nota = CriarNota();
        nota.MarcarEnviada("<xml/>");
        nota.Autorizar("CHAVE123", "PROTO456", "<xml>retorno</xml>");

        nota.Situacao.Should().Be(SituacaoNota.Autorizada);
        nota.ChaveAcesso.Should().Be("CHAVE123");
        nota.Protocolo.Should().Be("PROTO456");
        nota.DataAutorizacao.Should().NotBeNull();
    }

    [Fact]
    public void Rejeitar_DeveMudarSituacaoEGuardarMotivo()
    {
        var nota = CriarNota();
        nota.MarcarEnviada("<xml/>");
        nota.Rejeitar("Erro 999 - CNPJ inválido");

        nota.Situacao.Should().Be(SituacaoNota.Rejeitada);
        nota.MotivoRejeicao.Should().Be("Erro 999 - CNPJ inválido");
    }

    [Fact]
    public void Cancelar_DeveMudarSituacaoEGuardarXml()
    {
        var nota = CriarNota();
        nota.Autorizar("CHAVE", "PROTO", "<xml/>");
        nota.Cancelar("<xml>cancelamento</xml>");

        nota.Situacao.Should().Be(SituacaoNota.Cancelada);
        nota.XmlCancelamento.Should().Be("<xml>cancelamento</xml>");
    }

    [Fact]
    public void AdicionarItem_DeveRecalcularTotaisCorretamente()
    {
        var nota = CriarNota();
        var item = ItemNotaFiscal.Criar(nota.Id, 1, "PROD001", "Produto Teste",
            "12345678", "5102", "UN", quantidade: 2m, valorUnitario: 100m, desconto: 10m);

        nota.AdicionarItem(item);

        nota.TotalProdutos.Should().Be(200m); // 2 * 100
        nota.TotalDesconto.Should().Be(10m);
        nota.TotalNota.Should().Be(190m); // 200 - 10
    }

    [Fact]
    public void AdicionarMultiplosItens_TotalNota_DeveAcumularCorretamente()
    {
        var nota = CriarNota();
        var item1 = ItemNotaFiscal.Criar(nota.Id, 1, "P1", "Item 1", "12345678", "5102", "UN", 1m, 50m, 0m);
        var item2 = ItemNotaFiscal.Criar(nota.Id, 2, "P2", "Item 2", "12345678", "5102", "UN", 3m, 20m, 5m);

        nota.AdicionarItem(item1);
        nota.AdicionarItem(item2);

        nota.TotalProdutos.Should().Be(110m); // 50 + 60
        nota.TotalDesconto.Should().Be(5m);
        nota.TotalNota.Should().Be(105m);
    }

    [Fact]
    public void SetTransporte_ComFrete_DeveIncluirFreteNoTotal()
    {
        var nota = CriarNota();
        var item = ItemNotaFiscal.Criar(nota.Id, 1, "P1", "Item", "12345678", "5102", "UN", 1m, 100m, 0m);
        nota.AdicionarItem(item);
        nota.SetTransporte(ModalidadeFrete.ContratacaoRemetente, frete: 25m);

        nota.TotalFrete.Should().Be(25m);
        nota.TotalNota.Should().Be(125m); // 100 + 25
    }

    [Fact]
    public void Criar_DeveAtribuirEmpresaIdCorretamente()
    {
        var empresaId = Guid.NewGuid();
        var nota = NotaFiscal.Criar(empresaId, TipoNota.NFCe, 1, 42,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Producao);

        nota.EmpresaId.Should().Be(empresaId);
        nota.Tipo.Should().Be(TipoNota.NFCe);
        nota.Numero.Should().Be(42);
    }
}
