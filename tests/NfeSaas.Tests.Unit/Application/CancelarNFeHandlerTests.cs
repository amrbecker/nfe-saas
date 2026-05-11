using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NfeSaas.Application.Commands.CancelarNFe;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class CancelarNFeHandlerTests
{
    private readonly Mock<INotaFiscalRepository> _notaRepo = new();
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<ISefazService> _sefaz = new();
    private readonly Mock<IXmlNFeService> _xml = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CancelarNFeCommandHandler Handler() => new(
        _notaRepo.Object, _empresaRepo.Object, _sefaz.Object, _xml.Object, _uow.Object,
        NullLogger<CancelarNFeCommandHandler>.Instance);

    private static NotaFiscal CriarNotaAutorizada()
    {
        var n = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        n.MarcarEnviada("<x/>");
        n.Autorizar("CHAVE", "PROT", "<r/>");
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
    public async Task Cancelar_JustificativaCurta_RetornaErro()
    {
        var result = await Handler().Handle(
            new CancelarNFeCommand(Guid.NewGuid(), Guid.NewGuid(), "curto"),
            CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        result.MensagemErro.Should().Contain("15 caracteres");
    }

    [Fact]
    public async Task Cancelar_JustificativaMaiorQue255_RetornaErro()
    {
        var justif = new string('x', 256);
        var result = await Handler().Handle(
            new CancelarNFeCommand(Guid.NewGuid(), Guid.NewGuid(), justif),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("255");
    }

    [Fact]
    public async Task Cancelar_NotaNaoExiste_RetornaErro()
    {
        _notaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((NotaFiscal?)null);

        var result = await Handler().Handle(
            new CancelarNFeCommand(Guid.NewGuid(), Guid.NewGuid(),
                "Justificativa válida com mais de 15 chars"),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Cancelar_DeOutraEmpresa_AcessoNegado()
    {
        var nota = CriarNotaAutorizada();
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new CancelarNFeCommand(nota.Id, Guid.NewGuid() /* outra empresa */,
                "Justificativa válida com mais de 15 chars"),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("Acesso");
    }

    [Fact]
    public async Task Cancelar_NotaNaoAutorizada_RetornaErro()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new CancelarNFeCommand(nota.Id, nota.EmpresaId,
                "Justificativa válida com mais de 15 chars"),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("autorizadas");
    }

    [Fact]
    public async Task Cancelar_PrazoExpirado_RetornaErro()
    {
        var nota = CriarNotaAutorizada();
        // Força DataAutorizacao para mais de 24h atrás via reflection
        typeof(NotaFiscal).GetProperty("DataAutorizacao")!
            .SetValue(nota, DateTime.UtcNow.AddHours(-25));

        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new CancelarNFeCommand(nota.Id, nota.EmpresaId,
                "Justificativa válida com mais de 15 chars"),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("Prazo");
    }

    [Fact]
    public async Task Cancelar_SemCertificado_RetornaErro()
    {
        var nota = CriarNotaAutorizada();
        var empresa = Empresa.Criar(nota.EmpresaId, "X", "X", "12345678000195",
            "111111111111", "R", "1", "B", "C", "SP", "01310100", "3550308",
            "11999999999", "x@x.com", RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);
        // sem AtualizarCertificado

        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new CancelarNFeCommand(nota.Id, nota.EmpresaId,
                "Justificativa válida com mais de 15 chars"),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("Certificado");
    }
}
