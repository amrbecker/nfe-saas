using MediatR;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Queries.NcmQueries;

public record NcmDto(
    string Codigo,
    string Descricao,
    string? Capitulo,
    string? Posicao,
    decimal? AliquotaIpiPadrao,
    bool ExigeCest);

// === BUSCAR (autocomplete) ===
public record BuscarNcmQuery(string Termo, int Limite = 10) : IRequest<IReadOnlyList<NcmDto>>;

public class BuscarNcmQueryHandler : IRequestHandler<BuscarNcmQuery, IReadOnlyList<NcmDto>>
{
    private readonly INcmRepository _repo;
    public BuscarNcmQueryHandler(INcmRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<NcmDto>> Handle(BuscarNcmQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Termo)) return Array.Empty<NcmDto>();
        var limite = Math.Clamp(request.Limite, 1, 50);

        var ncms = await _repo.BuscarAsync(request.Termo, limite, ct);
        return ncms
            .Select(n => new NcmDto(n.Codigo, n.Descricao, n.CategoriaCapitulo, n.Posicao, n.AliquotaIpiPadrao, n.ExigeCest))
            .ToList();
    }
}

// === VALIDAR ===
public record ValidarNcmQuery(string Codigo) : IRequest<ValidarNcmResult>;
public record ValidarNcmResult(bool Existe, NcmDto? Ncm, string? MensagemErro);

public class ValidarNcmQueryHandler : IRequestHandler<ValidarNcmQuery, ValidarNcmResult>
{
    private readonly INcmRepository _repo;
    public ValidarNcmQueryHandler(INcmRepository repo) => _repo = repo;

    public async Task<ValidarNcmResult> Handle(ValidarNcmQuery request, CancellationToken ct)
    {
        var digitos = new string(request.Codigo.Where(char.IsDigit).ToArray());
        if (digitos.Length != 8)
            return new ValidarNcmResult(false, null, "NCM deve ter 8 dígitos.");

        var ncm = await _repo.GetByCodigoAsync(digitos, ct);
        if (ncm == null)
            return new ValidarNcmResult(false, null, $"NCM {digitos} não existe na tabela oficial.");

        return new ValidarNcmResult(true,
            new NcmDto(ncm.Codigo, ncm.Descricao, ncm.CategoriaCapitulo, ncm.Posicao, ncm.AliquotaIpiPadrao, ncm.ExigeCest),
            null);
    }
}

// === STATUS DA TABELA ===
public record GetNcmStatusQuery : IRequest<NcmStatusDto>;
public record NcmStatusDto(int TotalAtivos, string? VersaoTabela);

public class GetNcmStatusQueryHandler : IRequestHandler<GetNcmStatusQuery, NcmStatusDto>
{
    private readonly INcmRepository _repo;
    public GetNcmStatusQueryHandler(INcmRepository repo) => _repo = repo;

    public async Task<NcmStatusDto> Handle(GetNcmStatusQuery request, CancellationToken ct)
    {
        var total = await _repo.CountAsync(ct);
        var versao = await _repo.GetVersaoTabelaAtualAsync(ct);
        return new NcmStatusDto(total, versao);
    }
}
