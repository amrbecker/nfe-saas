using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.Auth;

public record LoginCommand(string Email, string Senha) : IRequest<LoginResultDto?>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;

    public LoginCommandHandler(IUsuarioRepository usuarioRepo, IEmpresaRepository empresaRepo, ITokenService tokenService, IUnitOfWork uow)
    {
        _usuarioRepo = usuarioRepo;
        _empresaRepo = empresaRepo;
        _tokenService = tokenService;
        _uow = uow;
    }

    public async Task<LoginResultDto?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (usuario == null || !usuario.Ativo) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash)) return null;

        var empresas = await _empresaRepo.GetByEscritorioAsync(usuario.EscritorioId, cancellationToken);
        var empresaDtos = empresas.Select(e => new EmpresaResumoDto(e.Id, e.RazaoSocial, e.NomeFantasia, e.Cnpj)).ToList();

        var accessToken = _tokenService.GerarAccessToken(usuario.Id, usuario.Email, usuario.Role, usuario.EscritorioId);
        var refreshToken = _tokenService.GerarRefreshToken();
        usuario.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await _usuarioRepo.UpdateAsync(usuario, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new LoginResultDto(accessToken, refreshToken, usuario.Nome, usuario.Email, usuario.Role, usuario.EscritorioId, empresaDtos);
    }
}

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultDto?>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResultDto?>
{
    public Task<LoginResultDto?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        => Task.FromResult<LoginResultDto?>(null); // placeholder
}

public record SelecionarEmpresaCommand(Guid UsuarioId, Guid EmpresaId) : IRequest<string?>;

public class SelecionarEmpresaCommandHandler : IRequestHandler<SelecionarEmpresaCommand, string?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly ITokenService _tokenService;

    public SelecionarEmpresaCommandHandler(IUsuarioRepository usuarioRepo, IEmpresaRepository empresaRepo, ITokenService tokenService)
    {
        _usuarioRepo = usuarioRepo;
        _empresaRepo = empresaRepo;
        _tokenService = tokenService;
    }

    public async Task<string?> Handle(SelecionarEmpresaCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.GetByIdAsync(request.UsuarioId, cancellationToken);
        if (usuario == null || !usuario.Ativo) return null;

        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, cancellationToken);
        if (empresa == null || empresa.EscritorioId != usuario.EscritorioId) return null;

        return _tokenService.GerarAccessToken(usuario.Id, usuario.Email, usuario.Role, usuario.EscritorioId, empresa.Id);
    }
}
