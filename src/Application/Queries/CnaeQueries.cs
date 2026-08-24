using MediatR;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Queries.CnaeQueries;

public record CnaeDto(string Codigo, string Descricao, string? Secao, string? Divisao);

// === BUSCAR (autocomplete) ===
public record BuscarCnaeQuery(string Termo, int Limite = 10) : IRequest<IReadOnlyList<CnaeDto>>;

public class BuscarCnaeQueryHandler : IRequestHandler<BuscarCnaeQuery, IReadOnlyList<CnaeDto>>
{
    private readonly ICnaeRepository _repo;
    public BuscarCnaeQueryHandler(ICnaeRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CnaeDto>> Handle(BuscarCnaeQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Termo)) return Array.Empty<CnaeDto>();
        var limite = Math.Clamp(request.Limite, 1, 50);

        var cnaes = await _repo.BuscarAsync(request.Termo, limite, ct);
        return cnaes
            .Select(c => new CnaeDto(c.Codigo, c.Descricao, c.Secao, c.Divisao))
            .ToList();
    }
}

// === VALIDAR ===
public record ValidarCnaeQuery(string Codigo) : IRequest<ValidarCnaeResult>;
public record ValidarCnaeResult(bool Existe, CnaeDto? Cnae, string? MensagemErro);

public class ValidarCnaeQueryHandler : IRequestHandler<ValidarCnaeQuery, ValidarCnaeResult>
{
    private readonly ICnaeRepository _repo;
    public ValidarCnaeQueryHandler(ICnaeRepository repo) => _repo = repo;

    public async Task<ValidarCnaeResult> Handle(ValidarCnaeQuery request, CancellationToken ct)
    {
        var digitos = new string(request.Codigo.Where(char.IsDigit).ToArray());
        if (digitos.Length != 7)
            return new ValidarCnaeResult(false, null, "CNAE deve ter 7 dígitos.");

        var cnae = await _repo.GetByCodigoAsync(digitos, ct);
        if (cnae == null)
            return new ValidarCnaeResult(false, null, $"CNAE {digitos} não existe na tabela oficial.");

        return new ValidarCnaeResult(true,
            new CnaeDto(cnae.Codigo, cnae.Descricao, cnae.Secao, cnae.Divisao),
            null);
    }
}
