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

    private static Empresa EmpresaComCertificadoValido()
    {
        var empresa = Empresa.Criar(Guid.NewGuid(), "Empresa Teste LTDA", "Empresa Teste", "12345678000195",
            "ISENTO", "Rua A", "100", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11999999999", "teste@empresa.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        empresa.AtualizarCertificado(new byte[] { 1, 2, 3 }, "senha", DateTime.UtcNow.AddYears(1), "12345678000195");
        return empresa;
    }

    private static EmitirNotaFiscalDto DtoComUmItem() => new(
        TipoNota.NFe, FinalidadeNota.Normal, TipoOperacao.Saida,
        new DestinatarioDto("11222333000181", "Cliente Teste", "c@x.com", TipoPessoa.PessoaJuridica,
            "Rua B", "1", null, "Centro", "Cidade", "SP", "01310100", "3550308", null),
        new List<ItemNotaDto>
        {
            new("PROD1", "Produto 1", "84713012", null, "5102", "UN", 1, 100m, 0m, null,
                new ImpostosItemDto(OrigemMercadoria.Nacional, CstIcms.Tributada, 12m, null, false, null, null,
                    CstPisCofins.TributadaAliquotaBasica, 0.65m, CstPisCofins.TributadaAliquotaBasica, 3m,
                    CsosnIcms: CsosnIcms.TributadaSemPermissaoCredito))
        },
        new TransporteDto(ModalidadeFrete.SemFrete, null, null, 0, 0),
        new PagamentoDto("01", 100m),
        null, null);

    private void SetupImpostosEXmlParaSucesso()
    {
        _impostoService.Setup(s => s.CalcularIcms(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal?>()))
                        .Returns(new ImpostoResultado(100m, 12m, 12m));
        _impostoService.Setup(s => s.CalcularPis(It.IsAny<decimal>(), It.IsAny<decimal>()))
                        .Returns(new ImpostoResultado(100m, 0.65m, 0.65m));
        _impostoService.Setup(s => s.CalcularCofins(It.IsAny<decimal>(), It.IsAny<decimal>()))
                        .Returns(new ImpostoResultado(100m, 3m, 3m));

        _xmlService.Setup(x => x.GerarXmlNFe(It.IsAny<NotaFiscal>(), It.IsAny<Empresa>())).Returns("<xml/>");
        _xmlService.Setup(x => x.AssinarXml(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns("<xml-assinado/>");
        IEnumerable<string> semErros = Array.Empty<string>();
        _xmlService.Setup(x => x.ValidarXml(It.IsAny<string>(), out semErros)).Returns(true);
    }

    [Fact]
    public async Task Handle_SefazIndisponivelMesmoAposContingenciaSvc_MarcaPendenteRetransmissaoEmVezDeRejeitar()
    {
        var empresa = EmpresaComCertificadoValido();
        _empresaRepo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _notaRepo.Setup(r => r.GetBySerieNumeroAsync(empresa.Id, TipoNota.NFe, empresa.SerieNFe, 1, empresa.AmbienteSefaz, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((NotaFiscal?)null);
        SetupImpostosEXmlParaSucesso();

        // CodigoRetorno -1 é o sentinel exclusivo de SefazService.EnviarNFeAsync para "primária E
        // contingência SVC inacessíveis" — nunca retornado por uma rejeição de negócio real.
        _sefaz.Setup(s => s.EnviarNFeAsync(It.IsAny<NotaFiscal>(), It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SefazResultado(false, null, null, null,
                  "SEFAZ indisponível. Nota salva em contingência. Retransmitir quando o serviço retornar.", -1));

        var result = await Handler().Handle(
            new EmitirNFeCommand(empresa.Id, Guid.NewGuid(), DtoComUmItem()), CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        _notaRepo.Verify(r => r.UpdateAsync(
            It.Is<NotaFiscal>(n => n.Situacao == SituacaoNota.PendenteRetransmissao), It.IsAny<CancellationToken>()),
            Times.Once);
        _notaRepo.Verify(r => r.UpdateAsync(
            It.Is<NotaFiscal>(n => n.Situacao == SituacaoNota.Rejeitada), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RejeicaoDeNegocioReal_ContinuaMarcandoRejeitada()
    {
        var empresa = EmpresaComCertificadoValido();
        _empresaRepo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _notaRepo.Setup(r => r.GetBySerieNumeroAsync(empresa.Id, TipoNota.NFe, empresa.SerieNFe, 1, empresa.AmbienteSefaz, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((NotaFiscal?)null);
        SetupImpostosEXmlParaSucesso();

        // cStat de rejeição real da SEFAZ (ex.: 225 - falha de schema) — nunca -1/-2.
        _sefaz.Setup(s => s.EnviarNFeAsync(It.IsAny<NotaFiscal>(), It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SefazResultado(false, null, null, null, "[225] Falha no schema XML.", 225));

        var result = await Handler().Handle(
            new EmitirNFeCommand(empresa.Id, Guid.NewGuid(), DtoComUmItem()), CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        _notaRepo.Verify(r => r.UpdateAsync(
            It.Is<NotaFiscal>(n => n.Situacao == SituacaoNota.Rejeitada), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
