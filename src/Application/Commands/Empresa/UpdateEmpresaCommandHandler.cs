using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Commands.EmpresaCommands;

public record UpdateEmpresaCommand(Guid EmpresaId, UpdateEmpresaDto Dto) : IRequest<UpdateEmpresaResult>;

public record UpdateEmpresaResult(bool Sucesso, string? Erro);

public class UpdateEmpresaCommandHandler : IRequestHandler<UpdateEmpresaCommand, UpdateEmpresaResult>
{
    private readonly IEmpresaRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateEmpresaCommandHandler(IEmpresaRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<UpdateEmpresaResult> Handle(UpdateEmpresaCommand request, CancellationToken ct)
    {
        var empresa = await _repo.GetByIdAsync(request.EmpresaId, ct);
        if (empresa == null) return new UpdateEmpresaResult(false, "Empresa não encontrada.");

        var dto = request.Dto;

        if (string.IsNullOrWhiteSpace(dto.RazaoSocial))
            return new UpdateEmpresaResult(false, "Razão social é obrigatória.");
        if (!IeValidator.UfValida(dto.Uf))
            return new UpdateEmpresaResult(false, $"UF inválida: {dto.Uf}.");
        if (!IeValidator.Validar(dto.InscricaoEstadual, dto.Uf))
            return new UpdateEmpresaResult(false, $"Inscrição estadual inválida para UF {dto.Uf}.");
        if (dto.Cep.Where(char.IsDigit).Count() != 8)
            return new UpdateEmpresaResult(false, "CEP deve ter 8 dígitos.");
        if (dto.CodigoMunicipio.Where(char.IsDigit).Count() != 7)
            return new UpdateEmpresaResult(false, "Código IBGE do município deve ter 7 dígitos.");
        if (!string.IsNullOrWhiteSpace(dto.Cnae) && !CnaeValidator.Validar(dto.Cnae))
            return new UpdateEmpresaResult(false, "CNAE deve ter 7 dígitos.");
        if (!Enum.IsDefined(typeof(RegimeTributario), dto.RegimeTributario))
            return new UpdateEmpresaResult(false, "Regime tributário inválido.");
        if (!Enum.IsDefined(typeof(AmbienteSefaz), dto.AmbienteSefaz))
            return new UpdateEmpresaResult(false, "Ambiente SEFAZ inválido.");

        // CSC: preserva token existente se DTO veio vazio (UI não recebe o token por segurança).
        // Só sobrescreve se usuário explicitamente preencheu novo token, ou se limpou também o CscId.
        var cscIdFinal = string.IsNullOrWhiteSpace(dto.CscId) ? null : dto.CscId.Trim();
        var cscTokenFinal = !string.IsNullOrWhiteSpace(dto.CscToken)
            ? dto.CscToken.Trim()
            : (cscIdFinal == null ? null : empresa.CscToken);

        empresa.Atualizar(
            dto.RazaoSocial, dto.NomeFantasia, dto.InscricaoEstadual,
            dto.Logradouro, dto.Numero, dto.Bairro, dto.Cidade, dto.Uf,
            dto.Cep, dto.CodigoMunicipio, dto.Telefone, dto.Email,
            (RegimeTributario)dto.RegimeTributario, (AmbienteSefaz)dto.AmbienteSefaz,
            string.IsNullOrWhiteSpace(dto.Cnae) ? null : dto.Cnae.Trim(),
            cscIdFinal, cscTokenFinal);

        await _repo.UpdateAsync(empresa, ct);
        await _uow.SaveChangesAsync(ct);

        return new UpdateEmpresaResult(true, null);
    }
}
