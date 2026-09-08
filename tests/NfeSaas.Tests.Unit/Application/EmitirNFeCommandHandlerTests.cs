using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NfeSaas.Application.Commands.EmitirNFe;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

/// <summary>
/// Idempotência de emissão: um retry do cliente com a mesma IdempotencyKey (ex.: após timeout de
/// rede na resposta) não pode reprocessar nada nem gerar uma segunda NF-e real na SEFAZ.
/// </summary>
public class EmitirNFeCommandHandlerTests
{
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<INotaFiscalRepository> _notaRepo = new();
    private readonly Mock<ISefazService> _sefaz = new();
    private readonly Mock<IXmlNFeService> _xmlService = new();
    private readonly Mock<IImpostoCalculoService> _impostoService = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private EmitirNFeCommandHandler Handler() => new(
        _empresaRepo.Object, _notaRepo.Object, _sefaz.Object, _xmlService.Object,
        _impostoService.Object, _auditService.Object, _uow.Object,
        Mock.Of<ILogger<EmitirNFeCommandHandler>>());

    private static EmitirNotaFiscalDto DtoComChave(string? idempotencyKey) => new(
        TipoNota.NFe, FinalidadeNota.Normal, TipoOperacao.Saida,
        new DestinatarioDto("12345678000195", "Dest", "d@x.com", TipoPessoa.PessoaJuridica,
            "Rua", "1", null, "Centro", "Cidade", "SP", "01310100", "3550308", null),
        new List<ItemNotaDto>(),
        new TransporteDto(ModalidadeFrete.SemFrete, null, null, 0, 0),
        new PagamentoDto("01", 0),
        null,
        idempotencyKey);

    [Fact]
    public async Task Handle_ComIdempotencyKeyDeNotaJaAutorizada_RetornaResultadoExistenteSemReprocessar()
    {
        var empresaId = Guid.NewGuid();
        var notaExistente = NotaFiscal.Criar(empresaId, TipoNota.NFe, 1, 42,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao, "chave-retry-1");
        notaExistente.Autorizar("35260512345678000195550010000000421000000019", "135260000123456", "<xml/>");

        _notaRepo.Setup(r => r.GetByIdempotencyKeyAsync(empresaId, "chave-retry-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(notaExistente);

        var result = await Handler().Handle(
            new EmitirNFeCommand(empresaId, Guid.NewGuid(), DtoComChave("chave-retry-1")), CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        result.NotaFiscalId.Should().Be(notaExistente.Id);
        result.ChaveAcesso.Should().Be(notaExistente.ChaveAcesso);
        result.Protocolo.Should().Be(notaExistente.Protocolo);

        // Nenhum efeito colateral de uma nova emissão — a proteção precisa curto-circuitar ANTES
        // de tocar empresa/SEFAZ/XML/persistência de uma nova nota.
        _empresaRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _sefaz.Verify(s => s.EnviarNFeAsync(It.IsAny<NotaFiscal>(), It.IsAny<Empresa>(), It.IsAny<CancellationToken>()), Times.Never);
        _notaRepo.Verify(r => r.AddAsync(It.IsAny<NotaFiscal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComIdempotencyKeyDeNotaRejeitada_RetornaFalhaExistenteSemReprocessar()
    {
        var empresaId = Guid.NewGuid();
        var notaExistente = NotaFiscal.Criar(empresaId, TipoNota.NFe, 1, 43,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao, "chave-retry-2");
        notaExistente.Rejeitar("Rejeição 999: erro de schema");

        _notaRepo.Setup(r => r.GetByIdempotencyKeyAsync(empresaId, "chave-retry-2", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(notaExistente);

        var result = await Handler().Handle(
            new EmitirNFeCommand(empresaId, Guid.NewGuid(), DtoComChave("chave-retry-2")), CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        result.NotaFiscalId.Should().Be(notaExistente.Id);
        result.MensagemErro.Should().Be("Rejeição 999: erro de schema");
        _sefaz.Verify(s => s.EnviarNFeAsync(It.IsAny<NotaFiscal>(), It.IsAny<Empresa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemIdempotencyKey_NaoConsultaRepositorioDeIdempotencia()
    {
        var empresaId = Guid.NewGuid();
        _empresaRepo.Setup(r => r.GetByIdAsync(empresaId, It.IsAny<CancellationToken>())).ReturnsAsync((Empresa?)null);

        await Handler().Handle(new EmitirNFeCommand(empresaId, Guid.NewGuid(), DtoComChave(null)), CancellationToken.None);

        _notaRepo.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComIdempotencyKeyInexistente_SeguePraFluxoNormal()
    {
        var empresaId = Guid.NewGuid();
        _notaRepo.Setup(r => r.GetByIdempotencyKeyAsync(empresaId, "chave-nova", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((NotaFiscal?)null);
        _empresaRepo.Setup(r => r.GetByIdAsync(empresaId, It.IsAny<CancellationToken>())).ReturnsAsync((Empresa?)null);

        var result = await Handler().Handle(
            new EmitirNFeCommand(empresaId, Guid.NewGuid(), DtoComChave("chave-nova")), CancellationToken.None);

        result.MensagemErro.Should().Be("Empresa não encontrada.");
        _empresaRepo.Verify(r => r.GetByIdAsync(empresaId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
