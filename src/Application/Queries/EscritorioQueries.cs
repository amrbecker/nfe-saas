using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Queries;

public record GetEmpresasQuery(Guid EscritorioId) : IRequest<List<EmpresaResumoDto>>;

public class GetEmpresasQueryHandler : IRequestHandler<GetEmpresasQuery, List<EmpresaResumoDto>>
{
    private readonly IEmpresaRepository _repo;
    private readonly IConfiguracaoEmpresaRepository _configRepo;

    public GetEmpresasQueryHandler(IEmpresaRepository repo, IConfiguracaoEmpresaRepository configRepo)
    {
        _repo = repo;
        _configRepo = configRepo;
    }

    public async Task<List<EmpresaResumoDto>> Handle(GetEmpresasQuery request, CancellationToken cancellationToken)
    {
        var empresas = await _repo.GetByEscritorioAsync(request.EscritorioId, cancellationToken);
        var configuradas = await _configRepo.GetEmpresaIdsConfiguradosAsync(empresas.Select(e => e.Id), cancellationToken);
        return empresas.Select(e => new EmpresaResumoDto(
            e.Id, e.RazaoSocial, e.NomeFantasia, e.Cnpj, configuradas.Contains(e.Id))).ToList();
    }
}

public record GetUsuariosQuery(Guid EscritorioId) : IRequest<List<UsuarioResumoDto>>;

public class GetUsuariosQueryHandler : IRequestHandler<GetUsuariosQuery, List<UsuarioResumoDto>>
{
    private readonly IUsuarioRepository _repo;

    public GetUsuariosQueryHandler(IUsuarioRepository repo) => _repo = repo;

    public async Task<List<UsuarioResumoDto>> Handle(GetUsuariosQuery request, CancellationToken cancellationToken)
    {
        var usuarios = await _repo.GetByEscritorioAsync(request.EscritorioId, cancellationToken);
        return usuarios.Select(u => new UsuarioResumoDto(u.Id, u.Nome, u.Email, u.Role, u.Ativo)).ToList();
    }
}
