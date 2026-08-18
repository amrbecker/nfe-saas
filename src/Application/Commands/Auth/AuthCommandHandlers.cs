using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.Auth;

public record LoginCommand(string Email, string Senha) : IRequest<LoginCommandResult>;

// Discriminated result: Sucesso = LoginResultDto, Falha = LoginFailureDto.
// Codigos de falha: "CredenciaisInvalidas", "TrialExpirado", "EscritorioSuspenso", "UsuarioInativo".
public record LoginCommandResult(LoginResultDto? Sucesso, LoginFailureDto? Falha);

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginCommandResult>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;

    public LoginCommandHandler(
        IUsuarioRepository usuarioRepo,
        IEmpresaRepository empresaRepo,
        IEscritorioRepository escritorioRepo,
        ITokenService tokenService,
        IUnitOfWork uow)
    {
        _usuarioRepo = usuarioRepo;
        _empresaRepo = empresaRepo;
        _escritorioRepo = escritorioRepo;
        _tokenService = tokenService;
        _uow = uow;
    }

    public async Task<LoginCommandResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (usuario == null || !usuario.Ativo)
            return new(null, new LoginFailureDto("E-mail ou senha inválidos.", "CredenciaisInvalidas", null));

        if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
            return new(null, new LoginFailureDto("E-mail ou senha inválidos.", "CredenciaisInvalidas", null));

        var escritorio = await _escritorioRepo.GetByIdAsync(usuario.EscritorioId, cancellationToken);
        if (escritorio == null)
            return new(null, new LoginFailureDto("Escritório não encontrado.", "EscritorioInvalido", null));

        var status = escritorio.CalcularStatusAssinatura();
        var assinatura = MapAssinatura(escritorio);

        if (status == StatusAssinaturaEscritorio.Suspenso)
            return new(null, new LoginFailureDto(
                "Escritório suspenso. Entre em contato com o suporte.",
                "EscritorioSuspenso", assinatura));

        if (status == StatusAssinaturaEscritorio.TrialExpirado)
            return new(null, new LoginFailureDto(
                "Seu período de avaliação de 30 dias expirou. Ative seu plano para continuar usando o NfeSaas.",
                "TrialExpirado", assinatura));

        var empresas = await _empresaRepo.GetByEscritorioAsync(usuario.EscritorioId, cancellationToken);
        var empresaDtos = empresas.Select(e => new EmpresaResumoDto(e.Id, e.RazaoSocial, e.NomeFantasia, e.Cnpj)).ToList();

        var accessToken = _tokenService.GerarAccessToken(usuario.Id, usuario.Email, usuario.Role, usuario.EscritorioId);
        var refreshToken = _tokenService.GerarRefreshToken();
        usuario.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await _usuarioRepo.UpdateAsync(usuario, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new(new LoginResultDto(
            accessToken, refreshToken, usuario.Nome, usuario.Email, usuario.Role,
            usuario.EscritorioId, empresaDtos, assinatura), null);
    }

    internal static AssinaturaDto MapAssinatura(Escritorio escritorio) => new(
        escritorio.Plano.ToString(),
        escritorio.CalcularStatusAssinatura().ToString(),
        escritorio.DiasRestantesTrial(),
        escritorio.TrialFimEm,
        escritorio.PlanoAtivoAteEm);
}

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultDto?>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResultDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;

    public RefreshTokenCommandHandler(
        IUsuarioRepository usuarioRepo,
        IEmpresaRepository empresaRepo,
        IEscritorioRepository escritorioRepo,
        ITokenService tokenService,
        IUnitOfWork uow)
    {
        _usuarioRepo = usuarioRepo;
        _empresaRepo = empresaRepo;
        _escritorioRepo = escritorioRepo;
        _tokenService = tokenService;
        _uow = uow;
    }

    public async Task<LoginResultDto?> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return null;

        var usuario = await _usuarioRepo.GetByRefreshTokenAsync(request.RefreshToken, ct);
        if (usuario == null || !usuario.Ativo || !usuario.RefreshTokenValido(request.RefreshToken))
            return null;

        var escritorio = await _escritorioRepo.GetByIdAsync(usuario.EscritorioId, ct);
        if (escritorio == null) return null;

        // Trial expirado/suspenso desde o último login: força passar pelo /login de novo para
        // que a UI receba o código de falha (TrialExpirado/EscritorioSuspenso) e a mensagem certa.
        var status = escritorio.CalcularStatusAssinatura();
        if (status is StatusAssinaturaEscritorio.Suspenso or StatusAssinaturaEscritorio.TrialExpirado)
            return null;

        var empresas = await _empresaRepo.GetByEscritorioAsync(usuario.EscritorioId, ct);
        var empresaDtos = empresas.Select(e => new EmpresaResumoDto(e.Id, e.RazaoSocial, e.NomeFantasia, e.Cnpj)).ToList();

        // Rotaciona o refresh token a cada uso (mitiga replay de um token vazado).
        var accessToken = _tokenService.GerarAccessToken(usuario.Id, usuario.Email, usuario.Role, usuario.EscritorioId);
        var novoRefreshToken = _tokenService.GerarRefreshToken();
        usuario.SetRefreshToken(novoRefreshToken, DateTime.UtcNow.AddDays(7));

        await _usuarioRepo.UpdateAsync(usuario, ct);
        await _uow.SaveChangesAsync(ct);

        // Igual ao login: token sem empresa_id — cliente precisa chamar selecionar-empresa de novo.
        return new LoginResultDto(accessToken, novoRefreshToken, usuario.Nome, usuario.Email, usuario.Role,
            usuario.EscritorioId, empresaDtos, LoginCommandHandler.MapAssinatura(escritorio));
    }
}

public record SelecionarEmpresaCommand(Guid UsuarioId, Guid EmpresaId) : IRequest<string?>;

public class SelecionarEmpresaCommandHandler : IRequestHandler<SelecionarEmpresaCommand, string?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly ITokenService _tokenService;

    public SelecionarEmpresaCommandHandler(
        IUsuarioRepository usuarioRepo,
        IEmpresaRepository empresaRepo,
        IEscritorioRepository escritorioRepo,
        ITokenService tokenService)
    {
        _usuarioRepo = usuarioRepo;
        _empresaRepo = empresaRepo;
        _escritorioRepo = escritorioRepo;
        _tokenService = tokenService;
    }

    public async Task<string?> Handle(SelecionarEmpresaCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.GetByIdAsync(request.UsuarioId, cancellationToken);
        if (usuario == null || !usuario.Ativo) return null;

        var escritorio = await _escritorioRepo.GetByIdAsync(usuario.EscritorioId, cancellationToken);
        if (escritorio == null || !escritorio.PodeAcessar()) return null;

        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, cancellationToken);
        if (empresa == null || empresa.EscritorioId != usuario.EscritorioId) return null;

        return _tokenService.GerarAccessToken(usuario.Id, usuario.Email, usuario.Role, usuario.EscritorioId, empresa.Id);
    }
}
