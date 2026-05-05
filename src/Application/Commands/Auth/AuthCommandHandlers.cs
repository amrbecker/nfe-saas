using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NfeSaas.Application.Commands.Auth;

public record LoginCommand(string Email, string Senha) : IRequest<LoginResultDto?>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto?>
{
    private readonly IUsuarioRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;

    public LoginCommandHandler(IUsuarioRepository repo, ITokenService tokenService, IUnitOfWork uow)
    {
        _repo = repo;
        _tokenService = tokenService;
        _uow = uow;
    }

    public async Task<LoginResultDto?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _repo.GetByEmailAsync(request.Email, cancellationToken);
        if (usuario == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash)) return null;

        var accessToken = _tokenService.GerarAccessToken(usuario.Id, usuario.Email, usuario.Role, usuario.EmpresaId);
        var refreshToken = _tokenService.GerarRefreshToken();
        usuario.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await _repo.UpdateAsync(usuario, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new LoginResultDto(accessToken, refreshToken, usuario.Nome, usuario.Email, usuario.Role);
    }
}

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultDto?>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResultDto?>
{
    private readonly IUsuarioRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;

    public RefreshTokenCommandHandler(IUsuarioRepository repo, ITokenService tokenService, IUnitOfWork uow)
    {
        _repo = repo;
        _tokenService = tokenService;
        _uow = uow;
    }

    public async Task<LoginResultDto?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find user by refresh token (simplified - production should use index)
        // This would need a dedicated query in the real implementation
        return null; // placeholder
    }
}
