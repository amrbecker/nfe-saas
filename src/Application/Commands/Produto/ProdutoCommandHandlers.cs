using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Commands.ProdutoCommands;

public record CreateProdutoCommand(Guid EmpresaId, CreateProdutoDto Dto) : IRequest<ProdutoResult>;
public record UpdateProdutoCommand(Guid EmpresaId, Guid ProdutoId, UpdateProdutoDto Dto) : IRequest<ProdutoResult>;
public record ToggleAtivoProdutoCommand(Guid EmpresaId, Guid ProdutoId) : IRequest<ProdutoResult>;
public record DeleteProdutoCommand(Guid EmpresaId, Guid ProdutoId) : IRequest<bool>;

public record ProdutoResult(ProdutoDetalheDto? Produto, string? Erro);

public class CreateProdutoCommandHandler : IRequestHandler<CreateProdutoCommand, ProdutoResult>
{
    private readonly IProdutoRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateProdutoCommandHandler(IProdutoRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ProdutoResult> Handle(CreateProdutoCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var erro = ValidarProduto(dto.Codigo, dto.Descricao, dto.Ncm, dto.CfopPadrao,
            dto.UnidadeComercial, dto.ValorUnitarioPadrao, dto.Cest, dto.CodigoEan, dto.CodigoAnp);
        if (erro != null) return new ProdutoResult(null, erro);

        var existente = await _repo.GetByCodigoAsync(request.EmpresaId, dto.Codigo, ct);
        if (existente != null) return new ProdutoResult(null, $"Já existe produto com o código '{dto.Codigo}' nesta empresa.");

        var produto = Produto.Criar(
            request.EmpresaId, dto.Codigo, dto.Descricao, dto.Ncm,
            dto.CfopPadrao, dto.UnidadeComercial, (OrigemMercadoria)dto.OrigemMercadoria,
            dto.ValorUnitarioPadrao,
            string.IsNullOrWhiteSpace(dto.Cest) ? null : dto.Cest,
            string.IsNullOrWhiteSpace(dto.CodigoEan) ? null : dto.CodigoEan,
            string.IsNullOrWhiteSpace(dto.CodigoAnp) ? null : dto.CodigoAnp);

        await _repo.AddAsync(produto, ct);
        await _uow.SaveChangesAsync(ct);

        return new ProdutoResult(MapDetalhe(produto), null);
    }

    internal static string? ValidarProduto(
        string codigo, string descricao, string ncm, string cfop,
        string unidade, decimal valorUnitario,
        string? cest, string? gtin, string? anp)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return "Código é obrigatório.";
        if (string.IsNullOrWhiteSpace(descricao)) return "Descrição é obrigatória.";
        if (!NcmValidator.Validar(ncm)) return "NCM deve ter 8 dígitos.";
        if (!CfopValidator.Existe(cfop)) return $"CFOP '{cfop}' inválido.";
        if (string.IsNullOrWhiteSpace(unidade)) return "Unidade comercial é obrigatória.";
        if (valorUnitario < 0) return "Valor unitário não pode ser negativo.";
        if (!string.IsNullOrWhiteSpace(cest) && cest.Where(char.IsDigit).Count() != 7)
            return "CEST deve ter 7 dígitos.";
        if (!string.IsNullOrWhiteSpace(gtin) && !GtinValidator.Validar(gtin))
            return "Código de barras (GTIN) inválido.";
        if (!string.IsNullOrWhiteSpace(anp) && anp.Where(char.IsDigit).Count() != 9)
            return "Código ANP deve ter 9 dígitos.";
        return null;
    }

    internal static ProdutoDetalheDto MapDetalhe(Produto p) => new(
        p.Id, p.Codigo, p.Descricao, p.Ncm, p.Cest, p.CfopPadrao, p.UnidadeComercial,
        (int)p.OrigemMercadoria, p.ValorUnitarioPadrao, p.CodigoEan, p.CodigoAnp, p.Ativo);
}

public class UpdateProdutoCommandHandler : IRequestHandler<UpdateProdutoCommand, ProdutoResult>
{
    private readonly IProdutoRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateProdutoCommandHandler(IProdutoRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ProdutoResult> Handle(UpdateProdutoCommand request, CancellationToken ct)
    {
        var produto = await _repo.GetByIdAsync(request.ProdutoId, ct);
        if (produto == null || produto.EmpresaId != request.EmpresaId)
            return new ProdutoResult(null, "Produto não encontrado.");

        var dto = request.Dto;
        var erro = CreateProdutoCommandHandler.ValidarProduto(
            dto.Codigo, dto.Descricao, dto.Ncm, dto.CfopPadrao,
            dto.UnidadeComercial, dto.ValorUnitarioPadrao,
            dto.Cest, dto.CodigoEan, dto.CodigoAnp);
        if (erro != null) return new ProdutoResult(null, erro);

        // Verifica conflito de código
        if (produto.Codigo != dto.Codigo)
        {
            var existente = await _repo.GetByCodigoAsync(request.EmpresaId, dto.Codigo, ct);
            if (existente != null) return new ProdutoResult(null, $"Já existe outro produto com o código '{dto.Codigo}'.");
        }

        produto.Atualizar(
            dto.Codigo, dto.Descricao, dto.Ncm,
            dto.CfopPadrao, dto.UnidadeComercial, (OrigemMercadoria)dto.OrigemMercadoria,
            dto.ValorUnitarioPadrao,
            string.IsNullOrWhiteSpace(dto.Cest) ? null : dto.Cest,
            string.IsNullOrWhiteSpace(dto.CodigoEan) ? null : dto.CodigoEan,
            string.IsNullOrWhiteSpace(dto.CodigoAnp) ? null : dto.CodigoAnp);

        await _repo.UpdateAsync(produto, ct);
        await _uow.SaveChangesAsync(ct);

        return new ProdutoResult(CreateProdutoCommandHandler.MapDetalhe(produto), null);
    }
}

public class ToggleAtivoProdutoCommandHandler : IRequestHandler<ToggleAtivoProdutoCommand, ProdutoResult>
{
    private readonly IProdutoRepository _repo;
    private readonly IUnitOfWork _uow;
    public ToggleAtivoProdutoCommandHandler(IProdutoRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ProdutoResult> Handle(ToggleAtivoProdutoCommand request, CancellationToken ct)
    {
        var produto = await _repo.GetByIdAsync(request.ProdutoId, ct);
        if (produto == null || produto.EmpresaId != request.EmpresaId)
            return new ProdutoResult(null, "Produto não encontrado.");
        if (produto.Ativo) produto.Desativar(); else produto.Ativar();
        await _repo.UpdateAsync(produto, ct);
        await _uow.SaveChangesAsync(ct);
        return new ProdutoResult(CreateProdutoCommandHandler.MapDetalhe(produto), null);
    }
}

public class DeleteProdutoCommandHandler : IRequestHandler<DeleteProdutoCommand, bool>
{
    private readonly IProdutoRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteProdutoCommandHandler(IProdutoRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteProdutoCommand request, CancellationToken ct)
    {
        var produto = await _repo.GetByIdAsync(request.ProdutoId, ct);
        if (produto == null || produto.EmpresaId != request.EmpresaId) return false;
        produto.Delete();
        await _repo.UpdateAsync(produto, ct);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
