using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.EmpresaCommands;

public record AjustarNumeracaoEmpresaCommand(Guid EmpresaId, AjustarNumeracaoDto Dto) : IRequest<AjustarNumeracaoResult>;

public record AjustarNumeracaoResult(bool Sucesso, string? Erro);

public class AjustarNumeracaoEmpresaCommandHandler : IRequestHandler<AjustarNumeracaoEmpresaCommand, AjustarNumeracaoResult>
{
    private readonly IEmpresaRepository _repo;
    private readonly IUnitOfWork _uow;

    public AjustarNumeracaoEmpresaCommandHandler(IEmpresaRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<AjustarNumeracaoResult> Handle(AjustarNumeracaoEmpresaCommand request, CancellationToken ct)
    {
        var empresa = await _repo.GetByIdAsync(request.EmpresaId, ct);
        if (empresa == null) return new AjustarNumeracaoResult(false, "Empresa não encontrada.");

        var dto = request.Dto;

        try
        {
            if (dto.UltimoNumeronFe.HasValue)
                empresa.AjustarUltimoNumero(TipoNota.NFe, dto.UltimoNumeronFe.Value);

            if (dto.UltimoNumeronFCe.HasValue)
                empresa.AjustarUltimoNumero(TipoNota.NFCe, dto.UltimoNumeronFCe.Value);
        }
        catch (InvalidOperationException ex)
        {
            return new AjustarNumeracaoResult(false, ex.Message);
        }

        await _repo.UpdateAsync(empresa, ct);
        await _uow.SaveChangesAsync(ct);

        return new AjustarNumeracaoResult(true, null);
    }
}
