using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NfeSaas.Application.Commands.EnviarNFePorEmail;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class EnviarNFePorEmailHandlerTests
{
    private readonly Mock<INotaFiscalRepository> _notaRepo = new();
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<IDanfeService> _danfe = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private EnviarNFePorEmailCommandHandler Handler() => new(
        _notaRepo.Object, _empresaRepo.Object, _danfe.Object, _email.Object, _audit.Object,
        _uow.Object, NullLogger<EnviarNFePorEmailCommandHandler>.Instance);

    private static NotaFiscal CriarNotaAutorizada(string? emailDestinatario = "cliente@exemplo.com")
    {
        var n = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        n.SetDestinatario("12345678000195", "Cliente Teste", emailDestinatario, TipoPessoa.PessoaJuridica,
            "Rua X", "1", "Bairro", "Cidade", "SP", "01310100", "3550308", null);
        n.MarcarEnviada("<x/>");
        n.Autorizar("CHAVE", "PROT", "<r/>");
        return n;
    }

    private static Empresa CriarEmpresa(Guid id) =>
        Empresa.Criar(id, "X", "X", "12345678000195",
            "111111111111", "R", "1", "B", "C", "SP", "01310100", "3550308",
            "11999999999", "x@x.com", RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);

    [Fact]
    public async Task Enviar_NotaNaoExiste_RetornaErro()
    {
        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null),
            CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        result.MensagemErro.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Enviar_DeOutraEmpresa_AcessoNegado()
    {
        var nota = CriarNotaAutorizada();
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, Guid.NewGuid(), Guid.NewGuid(), null),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("Acesso");
    }

    [Fact]
    public async Task Enviar_NotaNaoAutorizada_RetornaErro()
    {
        var nota = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, nota.EmpresaId, Guid.NewGuid(), null),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("autorizadas");
    }

    [Fact]
    public async Task Enviar_SemEmailDestinatarioENenhumOverride_RetornaErro()
    {
        var nota = CriarNotaAutorizada(emailDestinatario: null);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, nota.EmpresaId, Guid.NewGuid(), null),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("e-mail");
    }

    [Fact]
    public async Task Enviar_EmailInvalido_RetornaErro()
    {
        var nota = CriarNotaAutorizada();
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, nota.EmpresaId, Guid.NewGuid(), "nao-e-email"),
            CancellationToken.None);

        result.MensagemErro.Should().Contain("inválido");
    }

    [Fact]
    public async Task Enviar_GeracaoDanfeLancaExcecao_RetornaErroControladoSemPropagar()
    {
        var nota = CriarNotaAutorizada();
        var empresa = CriarEmpresa(nota.EmpresaId);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _danfe.Setup(d => d.GerarDanfePdfAsync(nota, empresa, It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("falha simulada na geração do PDF"));

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, nota.EmpresaId, Guid.NewGuid(), null),
            CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        result.MensagemErro.Should().Contain("DANFE");
        _email.Verify(e => e.EnviarNFeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Enviar_ServicoDeEmailFalha_RetornaErroENaoRegistraEnvio()
    {
        var nota = CriarNotaAutorizada();
        var empresa = CriarEmpresa(nota.EmpresaId);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _danfe.Setup(d => d.GerarDanfePdfAsync(nota, empresa, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new byte[] { 1 });
        _email.Setup(e => e.EnviarNFeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, nota.EmpresaId, Guid.NewGuid(), null),
            CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        nota.EmailEnviadoEm.Should().BeNull();
        _notaRepo.Verify(r => r.UpdateAsync(It.IsAny<NotaFiscal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Enviar_Sucesso_RegistraEnvioEAuditoria()
    {
        var nota = CriarNotaAutorizada();
        var empresa = CriarEmpresa(nota.EmpresaId);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _danfe.Setup(d => d.GerarDanfePdfAsync(nota, empresa, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new byte[] { 1 });
        _email.Setup(e => e.EnviarNFeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, nota.EmpresaId, Guid.NewGuid(), null),
            CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        nota.EmailEnviadoEm.Should().NotBeNull();
        _notaRepo.Verify(r => r.UpdateAsync(nota, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.RegistrarAsync(empresa.Id, "NFe.EmailEnviado", It.IsAny<Guid?>(),
            nota.ChaveAcesso, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Enviar_EmailEntregueMasPersistenciaFalha_RetornaSucessoComAvisoSemDizerQueFalhouEnviar()
    {
        // Cenário crítico: o e-mail JÁ foi entregue de verdade ao destinatário quando isso
        // acontece — reportar Sucesso=false incentivaria reenvio duplicado pro cliente.
        var nota = CriarNotaAutorizada();
        var empresa = CriarEmpresa(nota.EmpresaId);
        _notaRepo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);
        _empresaRepo.Setup(r => r.GetByIdAsync(nota.EmpresaId, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        _danfe.Setup(d => d.GerarDanfePdfAsync(nota, empresa, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new byte[] { 1 });
        _email.Setup(e => e.EnviarNFeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha simulada ao salvar"));

        var result = await Handler().Handle(
            new EnviarNFePorEmailCommand(nota.Id, nota.EmpresaId, Guid.NewGuid(), null),
            CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        result.MensagemErro.Should().Contain("não reenvie");
    }
}
