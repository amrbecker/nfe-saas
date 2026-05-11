using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NfeSaas.Application.Commands.EventosFiscaisCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class EventosFiscaisHandlerTests
{
    private readonly Mock<INotaFiscalRepository> _notaRepo = new();
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<IEventoFiscalRepository> _eventoRepo = new();
    private readonly Mock<ISefazService> _sefaz = new();
    private readonly Mock<IXmlNFeService> _xml = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAuditService> _audit = new();

    private const string ChaveValida = "35260512345678000195550010000000011000000019";

    private static Empresa EmpresaComCertificado()
    {
        var e = Empresa.Criar(Guid.NewGuid(), "X LTDA", "X", "12345678000195",
            "111111111111", "Rua A", "1", "Centro", "Cidade", "SP",
            "01310100", "3550308", "11999999999", "x@x.com",
            RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);
        e.AtualizarCertificado(new byte[] { 1, 2, 3 }, "senha",
            DateTime.UtcNow.AddYears(1), "12345678000195");
        return e;
    }

    private static NotaFiscal NotaAutorizadaComChave(Guid empresaId, string chave = ChaveValida)
    {
        var n = NotaFiscal.Criar(empresaId, TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        n.MarcarEnviada("<x/>");
        n.Autorizar(chave, "PROT", "<r/>");
        return n;
    }

    // ============================================================
    // CC-e
    // ============================================================
    private EmitirCartaCorrecaoHandler CceHandler() => new(
        _notaRepo.Object, _empresaRepo.Object, _eventoRepo.Object,
        _sefaz.Object, _xml.Object, _uow.Object, _audit.Object,
        NullLogger<EmitirCartaCorrecaoHandler>.Instance);

    [Fact]
    public async Task Cce_CorrecaoMenorQue15Chars_RetornaErro()
    {
        var result = await CceHandler().Handle(
            new EmitirCartaCorrecaoCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "curto"),
            CancellationToken.None);

        result.Evento.Should().BeNull();
        result.Erro.Should().Contain("15 caracteres");
    }

    [Fact]
    public async Task Cce_CorrecaoMaiorQue1000_RetornaErro()
    {
        var texto = new string('x', 1001);
        var result = await CceHandler().Handle(
            new EmitirCartaCorrecaoCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), texto),
            CancellationToken.None);

        result.Erro.Should().Contain("1000");
    }

    [Fact]
    public async Task Cce_NotaNaoExiste_RetornaErro()
    {
        _notaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((NotaFiscal?)null);

        var result = await CceHandler().Handle(
            new EmitirCartaCorrecaoCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Correção válida com mais de 15 caracteres."),
            CancellationToken.None);

        result.Erro.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Cce_NotaNaoAutorizada_RetornaErro()
    {
        var empresaId = Guid.NewGuid();
        var nota = NotaFiscal.Criar(empresaId, TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        _notaRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(nota);

        var result = await CceHandler().Handle(
            new EmitirCartaCorrecaoCommand(empresaId, Guid.NewGuid(), nota.Id,
                "Correção válida com mais de 15 caracteres."),
            CancellationToken.None);

        result.Erro.Should().Contain("autorizadas");
    }

    [Fact]
    public async Task Cce_Limite20Atingido_RetornaErro()
    {
        var empresaId = Guid.NewGuid();
        var nota = NotaAutorizadaComChave(empresaId);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _eventoRepo.Setup(r => r.CountCcePorChaveAsync(empresaId, nota.ChaveAcesso!, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(20);

        var result = await CceHandler().Handle(
            new EmitirCartaCorrecaoCommand(empresaId, Guid.NewGuid(), nota.Id,
                "Correção válida com mais de 15 caracteres."),
            CancellationToken.None);

        result.Erro.Should().Contain("Limite de 20");
    }

    [Fact]
    public async Task Cce_SemCertificado_RetornaErro()
    {
        var empresaId = Guid.NewGuid();
        var nota = NotaAutorizadaComChave(empresaId);
        var empresa = Empresa.Criar(empresaId, "X", "X", "12345678000195",
            "111111111111", "R", "1", "B", "C", "SP", "01310100", "3550308",
            "11999999999", "x@x.com", RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);
        // Sem certificado configurado

        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(empresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _eventoRepo.Setup(r => r.CountCcePorChaveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(0);

        var result = await CceHandler().Handle(
            new EmitirCartaCorrecaoCommand(empresaId, Guid.NewGuid(), nota.Id,
                "Correção válida com mais de 15 caracteres."),
            CancellationToken.None);

        result.Erro.Should().Contain("Certificado");
    }

    // ============================================================
    // Inutilização
    // ============================================================
    private InutilizarNumeracaoHandler InutHandler() => new(
        _empresaRepo.Object, _eventoRepo.Object, _notaRepo.Object,
        _sefaz.Object, _xml.Object, _uow.Object, _audit.Object,
        NullLogger<InutilizarNumeracaoHandler>.Instance);

    [Fact]
    public async Task Inut_JustificativaCurta_RetornaErro()
    {
        var dto = new InutilizarDto(2026, 55, 1, 1, 5, "curta");
        var result = await InutHandler().Handle(
            new InutilizarNumeracaoCommand(Guid.NewGuid(), Guid.NewGuid(), dto),
            CancellationToken.None);
        result.Erro.Should().Contain("15 caracteres");
    }

    [Fact]
    public async Task Inut_NumeroFinalMenorQueInicial_RetornaErro()
    {
        var dto = new InutilizarDto(2026, 55, 1, 10, 5, "Justificativa válida com mais de 15 chars.");
        var result = await InutHandler().Handle(
            new InutilizarNumeracaoCommand(Guid.NewGuid(), Guid.NewGuid(), dto),
            CancellationToken.None);
        result.Erro.Should().Contain("inválidos");
    }

    [Fact]
    public async Task Inut_AnoForaDoIntervalo_RetornaErro()
    {
        var dto = new InutilizarDto(1999, 55, 1, 1, 5, "Justificativa válida com mais de 15 chars.");
        var result = await InutHandler().Handle(
            new InutilizarNumeracaoCommand(Guid.NewGuid(), Guid.NewGuid(), dto),
            CancellationToken.None);
        result.Erro.Should().Contain("Ano");
    }

    [Fact]
    public async Task Inut_TipoNotaInvalido_RetornaErro()
    {
        var dto = new InutilizarDto(2026, 99, 1, 1, 5, "Justificativa válida com mais de 15 chars.");
        var result = await InutHandler().Handle(
            new InutilizarNumeracaoCommand(Guid.NewGuid(), Guid.NewGuid(), dto),
            CancellationToken.None);
        result.Erro.Should().Contain("Tipo de nota");
    }

    [Fact]
    public async Task Inut_NumeroJaUsadoEmNota_RetornaErro()
    {
        var empresa = EmpresaComCertificado();  // empresa.Id é o Guid efetivo
        _empresaRepo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var notaExistente = NotaAutorizadaComChave(empresa.Id);
        _notaRepo.Setup(r => r.GetBySerieNumeroAsync(empresa.Id, TipoNota.NFe, 1, 100,
                It.IsAny<AmbienteSefaz>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notaExistente);

        var dto = new InutilizarDto(2026, 55, 1, 100, 100, "Justificativa válida com mais de 15 chars.");
        var result = await InutHandler().Handle(
            new InutilizarNumeracaoCommand(empresa.Id, Guid.NewGuid(), dto),
            CancellationToken.None);

        result.Erro.Should().Contain("já foi usado");
    }

    // ============================================================
    // Manifestação
    // ============================================================
    private ManifestarDestinatarioHandler ManifHandler() => new(
        _empresaRepo.Object, _eventoRepo.Object,
        _sefaz.Object, _xml.Object, _uow.Object, _audit.Object,
        NullLogger<ManifestarDestinatarioHandler>.Instance);

    [Fact]
    public async Task Manif_ChaveTamanhoInvalido_RetornaErro()
    {
        var dto = new ManifestarDto(210210, null);
        var result = await ManifHandler().Handle(
            new ManifestarDestinatarioCommand(Guid.NewGuid(), Guid.NewGuid(), "chave-curta", dto),
            CancellationToken.None);
        result.Erro.Should().Contain("44 dígitos");
    }

    [Fact]
    public async Task Manif_TipoInvalido_RetornaErro()
    {
        var dto = new ManifestarDto(999999, null);
        var result = await ManifHandler().Handle(
            new ManifestarDestinatarioCommand(Guid.NewGuid(), Guid.NewGuid(), ChaveValida, dto),
            CancellationToken.None);
        result.Erro.Should().Contain("inválido");
    }

    [Fact]
    public async Task Manif_OperacaoNaoRealizadaSemJustificativa_RetornaErro()
    {
        var dto = new ManifestarDto(210240, null);
        var result = await ManifHandler().Handle(
            new ManifestarDestinatarioCommand(Guid.NewGuid(), Guid.NewGuid(), ChaveValida, dto),
            CancellationToken.None);
        result.Erro.Should().Contain("Justificativa");
    }

    [Fact]
    public async Task Manif_TipoEventoNaoEhManifestacao_RetornaErro()
    {
        // 110110 (CC-e) não é manifestação válida pra esse handler
        var dto = new ManifestarDto(110110, null);
        var result = await ManifHandler().Handle(
            new ManifestarDestinatarioCommand(Guid.NewGuid(), Guid.NewGuid(), ChaveValida, dto),
            CancellationToken.None);
        result.Erro.Should().Contain("inválido");
    }
}
