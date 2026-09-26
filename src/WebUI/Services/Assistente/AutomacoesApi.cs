using NfeSaas.Application.Assistente.Automacoes;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

public interface IAutomacoesApi
{
    Task<EmitirNotaFiscalDto?> PrepararNotaAsync(Guid notaId);
    Task<CadastroSugeridoDto?> CadastroSugeridoAsync(Guid notaId);
    Task<ClienteExistenteDto?> DestinatarioExistenteAsync(string documento);
    Task<PadroesPreenchimentoDto?> PadroesAsync(string? destinatario, string? produto);
    Task<PrimeirosPassosDto?> PrimeirosPassosAsync();
}

/// <summary>Automações da Ori (AUTOMACOES.md): só leem e preparam; quem salva e emite é o usuário.</summary>
public class AutomacoesApi : IAutomacoesApi
{
    private readonly ApiClient _api;
    public AutomacoesApi(ApiClient api) => _api = api;

    public Task<EmitirNotaFiscalDto?> PrepararNotaAsync(Guid notaId) =>
        Seguro(() => _api.GetAsync<EmitirNotaFiscalDto>($"api/assistente/automacoes/preparar-nota/{notaId}"));

    public Task<CadastroSugeridoDto?> CadastroSugeridoAsync(Guid notaId) =>
        Seguro(() => _api.GetAsync<CadastroSugeridoDto>($"api/assistente/automacoes/cadastro-sugerido/{notaId}"));

    public Task<ClienteExistenteDto?> DestinatarioExistenteAsync(string documento) =>
        Seguro(() => _api.GetAsync<ClienteExistenteDto>($"api/assistente/automacoes/destinatario-existente?doc={Uri.EscapeDataString(documento)}"));

    public Task<PadroesPreenchimentoDto?> PadroesAsync(string? destinatario, string? produto) =>
        Seguro(() => _api.GetAsync<PadroesPreenchimentoDto>(
            $"api/assistente/automacoes/padroes?destinatario={Uri.EscapeDataString(destinatario ?? "")}&produto={Uri.EscapeDataString(produto ?? "")}"));

    public Task<PrimeirosPassosDto?> PrimeirosPassosAsync() =>
        Seguro(() => _api.GetAsync<PrimeirosPassosDto>("api/assistente/automacoes/primeiros-passos"));

    private static async Task<T?> Seguro<T>(Func<Task<T?>> chamada) where T : class
    {
        try { return await chamada(); }
        catch (HttpRequestException) { return null; }
    }
}
