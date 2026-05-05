using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using NfeSaas.Application.DTOs;

namespace NfeSaas.WebUI.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;

    public ApiClient(HttpClient http, ILocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    public async Task SetAuthHeader()
    {
        var token = await _storage.GetItemAsStringAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        await SetAuthHeader();
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        await SetAuthHeader();
        var response = await _http.PostAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task<HttpResponseMessage> PostRawAsync<T>(string url, T data)
    {
        await SetAuthHeader();
        return await _http.PostAsJsonAsync(url, data);
    }

    public async Task<byte[]?> GetBytesAsync(string url)
    {
        await SetAuthHeader();
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }
}

// === AUTH SERVICE ===
public interface IAuthService
{
    Task<bool> LoginAsync(string email, string senha);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
}

public class AuthService : IAuthService
{
    private readonly ApiClient _api;
    private readonly ILocalStorageService _storage;

    public AuthService(ApiClient api, ILocalStorageService storage)
    {
        _api = api;
        _storage = storage;
    }

    public async Task<bool> LoginAsync(string email, string senha)
    {
        var result = await _api.PostAsync<LoginDto, LoginResultDto>(
            "api/auth/login", new LoginDto(email, senha));

        if (result == null) return false;

        await _storage.SetItemAsStringAsync("access_token", result.AccessToken);
        await _storage.SetItemAsStringAsync("refresh_token", result.RefreshToken);
        await _storage.SetItemAsStringAsync("user_name", result.NomeUsuario);
        await _storage.SetItemAsStringAsync("user_role", result.Role);
        return true;
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveItemAsync("access_token");
        await _storage.RemoveItemAsync("refresh_token");
        await _storage.RemoveItemAsync("user_name");
        await _storage.RemoveItemAsync("user_role");
    }

    public async Task<string?> GetTokenAsync() =>
        await _storage.GetItemAsStringAsync("access_token");
}

// === NOTA FISCAL SERVICE ===
public interface INotaFiscalService
{
    Task<GetNotasResult?> GetNotasAsync(int pagina = 1, int tamanhoPagina = 20);
    Task<NotaFiscalDetalheDto?> GetNotaAsync(Guid id);
    Task<EmitirNFeResult?> EmitirAsync(EmitirNotaFiscalDto dto);
    Task<bool> CancelarAsync(Guid id, string justificativa);
    Task<byte[]?> GetDanfePdfAsync(Guid id);
    Task<DashboardDto?> GetDashboardAsync(int? ano = null, int? mes = null);
}

public class NotaFiscalService : INotaFiscalService
{
    private readonly ApiClient _api;

    public NotaFiscalService(ApiClient api) => _api = api;

    public async Task<GetNotasResult?> GetNotasAsync(int pagina = 1, int tamanhoPagina = 20) =>
        await _api.GetAsync<GetNotasResult>($"api/notas-fiscais?pagina={pagina}&tamanhoPagina={tamanhoPagina}");

    public async Task<NotaFiscalDetalheDto?> GetNotaAsync(Guid id) =>
        await _api.GetAsync<NotaFiscalDetalheDto>($"api/notas-fiscais/{id}");

    public async Task<EmitirNFeResult?> EmitirAsync(EmitirNotaFiscalDto dto) =>
        await _api.PostAsync<EmitirNotaFiscalDto, EmitirNFeResult>("api/notas-fiscais/emitir", dto);

    public async Task<bool> CancelarAsync(Guid id, string justificativa)
    {
        var response = await _api.PostRawAsync($"api/notas-fiscais/{id}/cancelar", new { justificativa });
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> GetDanfePdfAsync(Guid id) =>
        await _api.GetBytesAsync($"api/notas-fiscais/{id}/danfe");

    public async Task<DashboardDto?> GetDashboardAsync(int? ano = null, int? mes = null)
    {
        var url = "api/notas-fiscais/dashboard";
        if (ano.HasValue) url += $"?ano={ano}&mes={mes}";
        return await _api.GetAsync<DashboardDto>(url);
    }
}

// === EMPRESA SERVICE ===
public interface IEmpresaService
{
    Task<object?> GetEmpresaAsync();
    Task<object?> GetCertificadoStatusAsync();
}

public class EmpresaService : IEmpresaService
{
    private readonly ApiClient _api;

    public EmpresaService(ApiClient api) => _api = api;

    public async Task<object?> GetEmpresaAsync() =>
        await _api.GetAsync<object>("api/empresa");

    public async Task<object?> GetCertificadoStatusAsync() =>
        await _api.GetAsync<object>("api/empresa/certificado/status");
}

public record EmitirNFeResult(bool Sucesso, Guid? NotaFiscalId, string? ChaveAcesso, string? Protocolo, string? MensagemErro);
public record GetNotasResult(IEnumerable<NotaFiscalResumoDto> Notas, int Total, int Pagina, int TamanhoPagina);
