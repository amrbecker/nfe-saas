using NfeSaas.Domain.Common;

namespace NfeSaas.Domain.Entities;

public class Usuario : BaseEntity
{
    public Guid EscritorioId { get; private set; }
    public Escritorio Escritorio { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public string Role { get; private set; } = "User";
    public bool Ativo { get; private set; } = true;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    protected Usuario() { }

    public static Usuario Criar(Guid escritorioId, string nome, string email, string senhaHash, string role = "User")
    {
        return new Usuario
        {
            EscritorioId = escritorioId,
            Nome = nome,
            Email = email,
            SenhaHash = senhaHash,
            Role = role
        };
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
        SetUpdated();
    }

    public void AlterarSenha(string senhaHash)
    {
        SenhaHash = senhaHash;
        SetUpdated();
    }

    public bool RefreshTokenValido(string token) =>
        RefreshToken == token && RefreshTokenExpiry.HasValue && RefreshTokenExpiry.Value > DateTime.UtcNow;
}
