using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.Auth;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class LoginHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepo = new();
    private readonly Mock<IEmpresaRepository> _empresaRepo = new();
    private readonly Mock<IEscritorioRepository> _escritorioRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private LoginCommandHandler Handler() => new(
        _usuarioRepo.Object, _empresaRepo.Object, _escritorioRepo.Object,
        _tokenService.Object, _uow.Object);

    private const string SenhaPlana = "Senha@123";
    private static string HashSenha() => BCrypt.Net.BCrypt.HashPassword(SenhaPlana);

    private static Usuario UsuarioDe(Guid escritorioId, string email = "user@x.com", bool ativo = true)
    {
        var u = Usuario.Criar(escritorioId, "User", email, HashSenha());
        if (!ativo) u.Desativar();
        return u;
    }

    [Fact]
    public async Task EmailNaoEncontrado_RetornaCredenciaisInvalidas()
    {
        _usuarioRepo.Setup(r => r.GetByEmailAsync("nao@existe.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await Handler().Handle(new LoginCommand("nao@existe.com", "x"), CancellationToken.None);

        result.Sucesso.Should().BeNull();
        result.Falha.Should().NotBeNull();
        result.Falha!.Codigo.Should().Be("CredenciaisInvalidas");
    }

    [Fact]
    public async Task SenhaErrada_RetornaCredenciaisInvalidas()
    {
        var usuario = UsuarioDe(Guid.NewGuid());
        _usuarioRepo.Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var result = await Handler().Handle(new LoginCommand(usuario.Email, "errada"), CancellationToken.None);

        result.Falha!.Codigo.Should().Be("CredenciaisInvalidas");
    }

    [Fact]
    public async Task UsuarioInativo_RetornaCredenciaisInvalidas()
    {
        // Política atual: usuário inativo gera mesma falha de credencial inválida (não revela existência).
        var usuario = UsuarioDe(Guid.NewGuid(), ativo: false);
        _usuarioRepo.Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var result = await Handler().Handle(new LoginCommand(usuario.Email, SenhaPlana), CancellationToken.None);

        result.Falha!.Codigo.Should().Be("CredenciaisInvalidas");
    }

    [Fact]
    public async Task EscritorioSuspenso_RetornaCodigoEscritorioSuspenso()
    {
        var escritorio = Escritorio.Criar("Esc", "Esc", "11222333000181", "e@x.com", null, PlanoSaas.Basico);
        escritorio.Suspender();
        var usuario = UsuarioDe(escritorio.Id);

        _usuarioRepo.Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escritorio);

        var result = await Handler().Handle(new LoginCommand(usuario.Email, SenhaPlana), CancellationToken.None);

        result.Sucesso.Should().BeNull();
        result.Falha!.Codigo.Should().Be("EscritorioSuspenso");
        result.Falha.Assinatura.Should().NotBeNull();
        result.Falha.Assinatura!.Status.Should().Be("Suspenso");
    }

    [Fact]
    public async Task TrialExpiradoSemPlanoPago_RetornaCodigoTrialExpirado()
    {
        var escritorio = Escritorio.Criar("Esc", "Esc", "11222333000181", "e@x.com", null, PlanoSaas.Basico);
        // Simula trial expirado mexendo no relógio via reflection — não há setter no domínio.
        // Alternativa: usar a sobrecarga de CalcularStatusAssinatura(referencia), mas o handler
        // chama sem referência. Por isso ajustamos TrialFimEm via propriedade privada.
        typeof(Escritorio).GetProperty(nameof(Escritorio.TrialFimEm))!
            .SetValue(escritorio, DateTime.UtcNow.AddDays(-1));
        var usuario = UsuarioDe(escritorio.Id);

        _usuarioRepo.Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escritorio);

        var result = await Handler().Handle(new LoginCommand(usuario.Email, SenhaPlana), CancellationToken.None);

        result.Sucesso.Should().BeNull();
        result.Falha!.Codigo.Should().Be("TrialExpirado");
        result.Falha.Assinatura!.Status.Should().Be("TrialExpirado");
    }

    [Fact]
    public async Task TrialAtivo_RetornaSucessoComAssinaturaPreenchida()
    {
        var escritorio = Escritorio.Criar("Esc", "Esc", "11222333000181", "e@x.com", null, PlanoSaas.Profissional);
        var usuario = UsuarioDe(escritorio.Id);

        _usuarioRepo.Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escritorio);
        _empresaRepo.Setup(r => r.GetByEscritorioAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Empresa>());
        _tokenService.Setup(t => t.GerarAccessToken(
                usuario.Id, usuario.Email, usuario.Role, escritorio.Id, null))
            .Returns("access-token");
        _tokenService.Setup(t => t.GerarRefreshToken()).Returns("refresh-token");

        var result = await Handler().Handle(new LoginCommand(usuario.Email, SenhaPlana), CancellationToken.None);

        result.Falha.Should().BeNull();
        result.Sucesso.Should().NotBeNull();
        result.Sucesso!.AccessToken.Should().Be("access-token");
        result.Sucesso.Assinatura.Status.Should().Be("TrialAtivo");
        result.Sucesso.Assinatura.Plano.Should().Be("Profissional");
        result.Sucesso.Assinatura.DiasRestantesTrial.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EscritorioComPlanoPagoEFora_DoTrial_RetornaSucessoComStatusPago()
    {
        var escritorio = Escritorio.Criar("Esc", "Esc", "11222333000181", "e@x.com", null, PlanoSaas.Enterprise);
        escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(60));
        var usuario = UsuarioDe(escritorio.Id);

        _usuarioRepo.Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _escritorioRepo.Setup(r => r.GetByIdAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(escritorio);
        _empresaRepo.Setup(r => r.GetByEscritorioAsync(escritorio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Empresa>());
        _tokenService.Setup(t => t.GerarAccessToken(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null))
            .Returns("access-token");
        _tokenService.Setup(t => t.GerarRefreshToken()).Returns("refresh-token");

        var result = await Handler().Handle(new LoginCommand(usuario.Email, SenhaPlana), CancellationToken.None);

        result.Sucesso!.Assinatura.Status.Should().Be("Pago");
        result.Sucesso.Assinatura.PlanoAtivoAteEm.Should().NotBeNull();
    }
}
