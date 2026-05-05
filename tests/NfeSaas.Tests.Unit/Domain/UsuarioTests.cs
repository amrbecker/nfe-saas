using FluentAssertions;
using NfeSaas.Domain.Entities;

namespace NfeSaas.Tests.Unit.Domain;

public class UsuarioTests
{
    private static Usuario CriarUsuario() =>
        Usuario.Criar(Guid.NewGuid(), "João Silva", "joao@teste.com",
            BCrypt.Net.BCrypt.HashPassword("Senha@123"));

    [Fact]
    public void Criar_DeveIniciarAtivo()
    {
        var usuario = CriarUsuario();
        usuario.Ativo.Should().BeTrue();
        usuario.Role.Should().Be("User");
        usuario.RefreshToken.Should().BeNull();
    }

    [Fact]
    public void Criar_ComRole_DeveAtribuirRoleCorreto()
    {
        var usuario = Usuario.Criar(Guid.NewGuid(), "Admin", "admin@teste.com",
            "hash", "Admin");
        usuario.Role.Should().Be("Admin");
    }

    [Fact]
    public void RefreshTokenValido_QuandoTokenCorretoENaoExpirado_RetornaTrue()
    {
        var usuario = CriarUsuario();
        usuario.SetRefreshToken("meu-token-secreto", DateTime.UtcNow.AddDays(7));

        usuario.RefreshTokenValido("meu-token-secreto").Should().BeTrue();
    }

    [Fact]
    public void RefreshTokenValido_QuandoTokenErrado_RetornaFalse()
    {
        var usuario = CriarUsuario();
        usuario.SetRefreshToken("token-correto", DateTime.UtcNow.AddDays(7));

        usuario.RefreshTokenValido("token-errado").Should().BeFalse();
    }

    [Fact]
    public void RefreshTokenValido_QuandoExpirado_RetornaFalse()
    {
        var usuario = CriarUsuario();
        usuario.SetRefreshToken("meu-token", DateTime.UtcNow.AddSeconds(-1)); // expirou

        usuario.RefreshTokenValido("meu-token").Should().BeFalse();
    }

    [Fact]
    public void RefreshTokenValido_QuandoSemToken_RetornaFalse()
    {
        var usuario = CriarUsuario();
        usuario.RefreshTokenValido("qualquer-token").Should().BeFalse();
    }

    [Fact]
    public void SetRefreshToken_DeveAtualizarToken()
    {
        var usuario = CriarUsuario();
        var expiry = DateTime.UtcNow.AddDays(7);
        usuario.SetRefreshToken("novo-token", expiry);

        usuario.RefreshToken.Should().Be("novo-token");
        usuario.RefreshTokenExpiry.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
    }
}
