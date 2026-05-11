using MediatR;
using NfeSaas.Application.Commands.ProdutoCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Queries.ProdutoQueries;

public record GetProdutosQuery(Guid EmpresaId, bool ApenasAtivos = false) : IRequest<List<ProdutoResumoDto>>;
public record GetProdutoQuery(Guid EmpresaId, Guid ProdutoId) : IRequest<ProdutoDetalheDto?>;

public class GetProdutosQueryHandler : IRequestHandler<GetProdutosQuery, List<ProdutoResumoDto>>
{
    private readonly IProdutoRepository _repo;
    public GetProdutosQueryHandler(IProdutoRepository repo) => _repo = repo;

    public async Task<List<ProdutoResumoDto>> Handle(GetProdutosQuery request, CancellationToken ct)
    {
        var produtos = await _repo.GetByEmpresaAsync(request.EmpresaId, request.ApenasAtivos, ct);
        return produtos.Select(p => new ProdutoResumoDto(
            p.Id, p.Codigo, p.Descricao, p.Ncm, p.UnidadeComercial, p.ValorUnitarioPadrao, p.Ativo)).ToList();
    }
}

public class GetProdutoQueryHandler : IRequestHandler<GetProdutoQuery, ProdutoDetalheDto?>
{
    private readonly IProdutoRepository _repo;
    public GetProdutoQueryHandler(IProdutoRepository repo) => _repo = repo;

    public async Task<ProdutoDetalheDto?> Handle(GetProdutoQuery request, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(request.ProdutoId, ct);
        if (p == null || p.EmpresaId != request.EmpresaId) return null;
        return new ProdutoDetalheDto(p.Id, p.Codigo, p.Descricao, p.Ncm, p.Cest, p.CfopPadrao,
            p.UnidadeComercial, (int)p.OrigemMercadoria, p.ValorUnitarioPadrao,
            p.CodigoEan, p.CodigoAnp, p.Ativo);
    }
}
