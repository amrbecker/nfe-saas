using NfeSaas.Application.Assistente.Chamados;
using NfeSaas.Application.Assistente.Curadoria;
using NfeSaas.Domain.Enums;

namespace NfeSaas.WebUI.Services.Assistente;

public interface ICuradoriaApi
{
    Task<List<PublicacaoCuradoriaDto>> PublicacoesAsync(StatusPublicacao? status);
    Task<bool> AtualizarPublicacaoAsync(Guid id, AtualizarPublicacaoDto dto);
    Task<List<ArtigoCuradoriaDto>> ArtigosPendentesAsync();
    Task<List<LacunaAgrupadaDto>> LacunasAsync(int dias);
    Task<List<FonteCuradoriaDto>> FontesAsync();
    Task<List<ChamadoInternoDto>> ChamadosAsync(StatusChamado? status);
    Task<bool> AtualizarChamadoAsync(Guid id, AtualizarChamadoDto dto);
}

public class CuradoriaApi : ICuradoriaApi
{
    private readonly ApiClient _api;
    public CuradoriaApi(ApiClient api) => _api = api;

    public async Task<List<PublicacaoCuradoriaDto>> PublicacoesAsync(StatusPublicacao? status) =>
        await _api.GetAsync<List<PublicacaoCuradoriaDto>>($"api/curadoria/publicacoes{(status is { } s ? $"?status={(int)s}" : "")}") ?? new();

    public async Task<bool> AtualizarPublicacaoAsync(Guid id, AtualizarPublicacaoDto dto) =>
        (await _api.PutRawAsync($"api/curadoria/publicacoes/{id}", dto)).IsSuccessStatusCode;

    public async Task<List<ArtigoCuradoriaDto>> ArtigosPendentesAsync() =>
        await _api.GetAsync<List<ArtigoCuradoriaDto>>("api/curadoria/artigos-vencidos") ?? new();

    public async Task<List<LacunaAgrupadaDto>> LacunasAsync(int dias) =>
        await _api.GetAsync<List<LacunaAgrupadaDto>>($"api/curadoria/lacunas?dias={dias}") ?? new();

    public async Task<List<FonteCuradoriaDto>> FontesAsync() =>
        await _api.GetAsync<List<FonteCuradoriaDto>>("api/curadoria/fontes") ?? new();

    public async Task<List<ChamadoInternoDto>> ChamadosAsync(StatusChamado? status) =>
        await _api.GetAsync<List<ChamadoInternoDto>>($"api/interno/chamados{(status is { } s ? $"?status={(int)s}" : "")}") ?? new();

    public async Task<bool> AtualizarChamadoAsync(Guid id, AtualizarChamadoDto dto) =>
        (await _api.PutRawAsync($"api/interno/chamados/{id}", dto)).IsSuccessStatusCode;
}
