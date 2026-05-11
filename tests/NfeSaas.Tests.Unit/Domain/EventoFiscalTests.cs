using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class EventoFiscalTests
{
    private const string ChaveValida = "35260512345678000195550010000000011000000019";
    private static readonly Guid EmpresaId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [Fact]
    public void CriarCce_DeveDefinirTipoESequencial()
    {
        var ev = EventoFiscal.CriarCce(EmpresaId, UsuarioId, AmbienteSefaz.Homologacao,
            ChaveValida, sequencial: 1, "Correção do CFOP do item 1.");

        ev.EmpresaId.Should().Be(EmpresaId);
        ev.UsuarioId.Should().Be(UsuarioId);
        ev.Tipo.Should().Be(TipoEventoFiscal.CartaCorrecao);
        ev.Ambiente.Should().Be(AmbienteSefaz.Homologacao);
        ev.ChaveAcesso.Should().Be(ChaveValida);
        ev.SequencialCce.Should().Be(1);
        ev.Justificativa.Should().Be("Correção do CFOP do item 1.");
        ev.Situacao.Should().Be(SituacaoEventoFiscal.Registrado);
        ev.AnoInutilizacao.Should().BeNull();
        ev.SerieInutilizacao.Should().BeNull();
    }

    [Fact]
    public void CriarInutilizacao_DeveDefinirRangeENumeros()
    {
        var ev = EventoFiscal.CriarInutilizacao(EmpresaId, UsuarioId, AmbienteSefaz.Producao,
            ano: 2026, TipoNota.NFe, serie: 1, numIni: 100, numFin: 105, "Quebra de sequência por descarte de notas.");

        ev.Tipo.Should().Be(TipoEventoFiscal.Inutilizacao);
        ev.AnoInutilizacao.Should().Be(2026);
        ev.TipoNotaInutilizacao.Should().Be(TipoNota.NFe);
        ev.SerieInutilizacao.Should().Be(1);
        ev.NumeroInicialInutilizacao.Should().Be(100);
        ev.NumeroFinalInutilizacao.Should().Be(105);
        ev.ChaveAcesso.Should().BeNull();
        ev.SequencialCce.Should().BeNull();
        ev.Ambiente.Should().Be(AmbienteSefaz.Producao);
    }

    [Fact]
    public void CriarManifestacao_Confirmacao_DeveDefinirTipoCorretamente()
    {
        var ev = EventoFiscal.CriarManifestacao(EmpresaId, UsuarioId, AmbienteSefaz.Homologacao,
            TipoEventoFiscal.ManifestacaoConfirmacao, ChaveValida, "");

        ev.Tipo.Should().Be(TipoEventoFiscal.ManifestacaoConfirmacao);
        ev.ChaveAcesso.Should().Be(ChaveValida);
        ev.Justificativa.Should().BeEmpty();
    }

    [Fact]
    public void CriarManifestacao_OperacaoNaoRealizada_AceitaJustificativa()
    {
        var ev = EventoFiscal.CriarManifestacao(EmpresaId, UsuarioId, AmbienteSefaz.Homologacao,
            TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada, ChaveValida,
            "Mercadoria não foi entregue conforme combinado.");

        ev.Tipo.Should().Be(TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada);
        ev.Justificativa.Should().Be("Mercadoria não foi entregue conforme combinado.");
    }

    [Fact]
    public void RegistrarEnvio_DeveArmazenarXml()
    {
        var ev = EventoFiscal.CriarCce(EmpresaId, UsuarioId, AmbienteSefaz.Homologacao,
            ChaveValida, 1, "correção válida");
        ev.RegistrarEnvio("<envEvento>...</envEvento>");

        ev.XmlEvento.Should().Be("<envEvento>...</envEvento>");
    }

    [Fact]
    public void Aceitar_DeveMudarSituacaoEArmazenarProtocolo()
    {
        var ev = EventoFiscal.CriarCce(EmpresaId, UsuarioId, AmbienteSefaz.Homologacao,
            ChaveValida, 1, "correção da Carta de Correção válida");
        ev.Aceitar("PROTO12345", "<retorno>OK</retorno>");

        ev.Situacao.Should().Be(SituacaoEventoFiscal.Aceito);
        ev.Protocolo.Should().Be("PROTO12345");
        ev.XmlRetorno.Should().Be("<retorno>OK</retorno>");
        ev.MotivoRejeicao.Should().BeNull();
        ev.DataRetorno.Should().NotBeNull();
    }

    [Fact]
    public void Rejeitar_DeveMudarSituacaoEArmazenarMotivo()
    {
        var ev = EventoFiscal.CriarCce(EmpresaId, UsuarioId, AmbienteSefaz.Homologacao,
            ChaveValida, 1, "correção válida com texto");
        ev.Rejeitar("Erro 999", "<retorno>NOK</retorno>");

        ev.Situacao.Should().Be(SituacaoEventoFiscal.Rejeitado);
        ev.MotivoRejeicao.Should().Be("Erro 999");
        ev.XmlRetorno.Should().Be("<retorno>NOK</retorno>");
        ev.Protocolo.Should().BeNull();
        ev.DataRetorno.Should().NotBeNull();
    }

    [Fact]
    public void Aceitar_AposRejeitar_DeveLimparMotivoRejeicao()
    {
        var ev = EventoFiscal.CriarCce(EmpresaId, UsuarioId, AmbienteSefaz.Homologacao,
            ChaveValida, 1, "correção da Carta de Correção válida");
        ev.Rejeitar("Erro temporário");
        ev.Aceitar("PROTO456", null);

        ev.MotivoRejeicao.Should().BeNull();
        ev.Situacao.Should().Be(SituacaoEventoFiscal.Aceito);
    }

    [Theory]
    [InlineData(TipoEventoFiscal.CartaCorrecao, 110110)]
    [InlineData(TipoEventoFiscal.Cancelamento, 110111)]
    [InlineData(TipoEventoFiscal.ManifestacaoConfirmacao, 210200)]
    [InlineData(TipoEventoFiscal.ManifestacaoCiencia, 210210)]
    [InlineData(TipoEventoFiscal.ManifestacaoDesconhecimento, 210220)]
    [InlineData(TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada, 210240)]
    public void TipoEventoFiscal_TemValoresSefazCorretos(TipoEventoFiscal tipo, int valorEsperado)
    {
        ((int)tipo).Should().Be(valorEsperado);
    }
}
