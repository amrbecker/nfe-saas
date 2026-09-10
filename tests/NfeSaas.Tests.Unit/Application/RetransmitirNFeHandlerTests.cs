using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NfeSaas.Application.Commands.RetransmitirNFe;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class RetransmitirNFeHandlerTests
{
    private readonly Mock<INotaFiscalRepository> _notaRepo = new();
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<ISefazService> _sefaz = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RetransmitirNFeCommandHandler Handler() => new(
        _notaRepo.Object, _empresaRepo.Object, _sefaz.Object, _auditService.Object, _uow.Object,
        NullLogger<RetransmitirNFeCommandHandler>.Instance);

    private static NotaFiscal CriarNotaPendente()
    {
        var n = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        n.MarcarEnviada("<xml-assinado/>");
        n.MarcarPendenteRetransmissao("SEFAZ indisponível. Nota salva em contingência. Retransmitir quando o serviço retornar.");
        return n;
    }

    private static Empresa EmpresaComCert()
    {
        var e = Empresa.Criar(Guid.NewGuid(), "X", "X", "12345678000195",
            "111111111111", "R", "1", "B", "C", "SP", "01310100", "3550308",
            "11999999999", "x@x.com", RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);
        e.AtualizarCertificado(new byte[] { 1, 2, 3 }, "senha", DateTime.UtcNow.AddYears(1), "12345678000195");
        return e;
    }

    [Fact]
    public async Task Retransmitir_NotaNaoExiste_RetornaErro()
    {
        _notaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((NotaFiscal?)null);

        var result = await Handler().Handle(
            new RetransmitirNFeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        result.MensagemErro.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Retransmitir_DeOutraEmpresa_AcessoNegado()
    {
        var nota = CriarNotaPendente();
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new RetransmitirNFeCommand(nota.Id, Guid.NewGuid() /* outra empresa */), CancellationToken.None);

        result.MensagemErro.Should().Contain("Acesso");
    }

    [Fact]
    public async Task Retransmitir_NotaNaoPendente_RetornaErro()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.MarcarEnviada("<xml/>");
        nota.Autorizar("CHAVE", "PROTO", "<r/>"); // já autorizada, não pendente

        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new RetransmitirNFeCommand(nota.Id, nota.EmpresaId), CancellationToken.None);

        result.MensagemErro.Should().Contain("pendente de retransmissão");
    }

    [Fact]
    public async Task Retransmitir_SefazAutoriza_MarcaAutorizadaEReenviaMesmoXml()
    {
        var nota = CriarNotaPendente();
        var empresa = EmpresaComCert();
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _sefaz.Setup(s => s.EnviarNFeAsync(nota, empresa, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SefazResultado(true, "CHAVE123", "PROTO456", "<retorno/>", null, 100));

        var result = await Handler().Handle(
            new RetransmitirNFeCommand(nota.Id, nota.EmpresaId), CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        nota.Situacao.Should().Be(SituacaoNota.Autorizada);
        nota.ChaveAcesso.Should().Be("CHAVE123");
        _sefaz.Verify(s => s.EnviarNFeAsync(
            It.Is<NotaFiscal>(n => n.XmlEnvio == "<xml-assinado/>"), empresa, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Retransmitir_SefazAindaIndisponivel_ContinuaPendente()
    {
        var nota = CriarNotaPendente();
        var empresa = EmpresaComCert();
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _sefaz.Setup(s => s.EnviarNFeAsync(nota, empresa, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SefazResultado(false, null, null, null, "SEFAZ indisponível.", -1));

        var result = await Handler().Handle(
            new RetransmitirNFeCommand(nota.Id, nota.EmpresaId), CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        nota.Situacao.Should().Be(SituacaoNota.PendenteRetransmissao);
    }

    [Fact]
    public async Task Retransmitir_SefazRejeitaPorMotivoDeNegocio_MarcaRejeitada()
    {
        var nota = CriarNotaPendente();
        var empresa = EmpresaComCert();
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _sefaz.Setup(s => s.EnviarNFeAsync(nota, empresa, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SefazResultado(false, null, null, null, "[204] Duplicidade de NF-e.", 204));

        var result = await Handler().Handle(
            new RetransmitirNFeCommand(nota.Id, nota.EmpresaId), CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        nota.Situacao.Should().Be(SituacaoNota.Rejeitada);
    }
}
