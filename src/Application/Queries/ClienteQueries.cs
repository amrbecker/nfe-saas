using MediatR;
using NfeSaas.Application.Commands.ClienteCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Queries.ClienteQueries;

public record GetClientesQuery(Guid EmpresaId, bool ApenasAtivos = false) : IRequest<List<ClienteResumoDto>>;
public record GetClienteQuery(Guid EmpresaId, Guid ClienteId) : IRequest<ClienteDetalheDto?>;

public class GetClientesQueryHandler : IRequestHandler<GetClientesQuery, List<ClienteResumoDto>>
{
    private readonly IClienteRepository _repo;
    public GetClientesQueryHandler(IClienteRepository repo) => _repo = repo;

    public async Task<List<ClienteResumoDto>> Handle(GetClientesQuery request, CancellationToken ct)
    {
        var clientes = await _repo.GetByEmpresaAsync(request.EmpresaId, request.ApenasAtivos, ct);
        return clientes.Select(c => new ClienteResumoDto(
            c.Id, (int)c.TipoPessoa, c.CpfCnpj, c.RazaoSocial, c.NomeFantasia, c.Uf, c.Ativo)).ToList();
    }
}

public class GetClienteQueryHandler : IRequestHandler<GetClienteQuery, ClienteDetalheDto?>
{
    private readonly IClienteRepository _repo;
    public GetClienteQueryHandler(IClienteRepository repo) => _repo = repo;

    public async Task<ClienteDetalheDto?> Handle(GetClienteQuery request, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(request.ClienteId, ct);
        if (c == null || c.EmpresaId != request.EmpresaId) return null;
        return new ClienteDetalheDto(c.Id, (int)c.TipoPessoa, c.CpfCnpj, c.RazaoSocial, c.NomeFantasia,
            c.Email, c.Telefone, c.Logradouro, c.Numero, c.Complemento, c.Bairro, c.Cidade,
            c.Uf, c.Cep, c.CodigoMunicipio, c.InscricaoEstadual, (int)c.IndicadorIe, c.Ativo);
    }
}
