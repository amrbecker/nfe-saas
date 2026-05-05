using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Queries;

public record GetEmpresasQuery(Guid EscritorioId) : IRequest<List<EmpresaResumoDto>>;

public class GetEmpresasQueryHandler : IRequestHandler<GetEmpresasQuery, List<EmpresaResumoDto>>
{
    private readonly IEmpresaRepository _repo;

    public GetEmpresasQueryHandler(IEmpresaRepository repo) => _repo = repo;

    public async Task<List<EmpresaResumoDto>> Handle(GetEmpresasQuery request, CancellationToken cancellationToken)
    {
        var empresas = await _repo.GetByEscritorioAsync(request.EscritorioId, cancellationToken);
        return empresas.Select(e => new EmpresaResumoDto(e.Id, e.RazaoSocial, e.NomeFantasia, e.Cnpj)).ToList();
    }
}
