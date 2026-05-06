using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using NfeSaas.Application.DTOs;

namespace NfeSaas.WebUI.Services;

public static class ApiHelper
{
    public static async Task<string> ExtrairMensagemErro(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg) && msg.GetString() is { } m)
                return m;
            if (doc.RootElement.TryGetProperty("title", out var title) && title.GetString() is { } t)
                return t;
            return body;
        }
        catch
        {
            return $"Erro {(int)response.StatusCode}: {response.ReasonPhrase}";
        }
    }
}

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

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        await SetAuthHeader();
        var response = await _http.PutAsJsonAsync(url, data);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task<TResponse?> PatchAsync<TResponse>(string url)
    {
        await SetAuthHeader();
        var response = await _http.PatchAsync(url, null);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        await SetAuthHeader();
        return await _http.DeleteAsync(url);
    }

    public async Task<HttpResponseMessage> PostMultipartAsync(string url, MultipartFormDataContent form)
    {
        await SetAuthHeader();
        return await _http.PostAsync(url, form);
    }
}

// === AUTH SERVICE ===
public interface IAuthService
{
    Task<LoginResultDto?> LoginAsync(string email, string senha);
    Task<bool> SelecionarEmpresaAsync(Guid empresaId);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> HasEmpresaSelecionadaAsync();
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

    public async Task<LoginResultDto?> LoginAsync(string email, string senha)
    {
        var result = await _api.PostAsync<LoginDto, LoginResultDto>(
            "api/auth/login", new LoginDto(email, senha));

        if (result == null) return null;

        await _storage.SetItemAsStringAsync("access_token", result.AccessToken);
        await _storage.SetItemAsStringAsync("refresh_token", result.RefreshToken);
        await _storage.SetItemAsStringAsync("user_name", result.NomeUsuario);
        await _storage.SetItemAsStringAsync("user_role", result.Role);
        await _storage.SetItemAsStringAsync("escritorio_id", result.EscritorioId.ToString());

        // Store empresas list for selection
        var empresasJson = System.Text.Json.JsonSerializer.Serialize(result.Empresas);
        await _storage.SetItemAsStringAsync("empresas", empresasJson);

        // Auto-select if only one empresa
        if (result.Empresas.Count == 1)
            await SelecionarEmpresaAsync(result.Empresas[0].Id);

        return result;
    }

    public async Task<bool> SelecionarEmpresaAsync(Guid empresaId)
    {
        var response = await _api.PostRawAsync("api/auth/selecionar-empresa", new { empresaId });
        if (!response.IsSuccessStatusCode) return false;

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var token = json?.RootElement.GetProperty("accessToken").GetString();
        if (token == null) return false;

        await _storage.SetItemAsStringAsync("access_token", token);
        await _storage.SetItemAsStringAsync("empresa_id", empresaId.ToString());
        return true;
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveItemAsync("access_token");
        await _storage.RemoveItemAsync("refresh_token");
        await _storage.RemoveItemAsync("user_name");
        await _storage.RemoveItemAsync("user_role");
        await _storage.RemoveItemAsync("escritorio_id");
        await _storage.RemoveItemAsync("empresa_id");
        await _storage.RemoveItemAsync("empresas");
    }

    public async Task<string?> GetTokenAsync() =>
        await _storage.GetItemAsStringAsync("access_token");

    public async Task<bool> IsAuthenticatedAsync() =>
        !string.IsNullOrEmpty(await _storage.GetItemAsStringAsync("access_token"));

    public async Task<bool> HasEmpresaSelecionadaAsync() =>
        !string.IsNullOrEmpty(await _storage.GetItemAsStringAsync("empresa_id"));
}

// === ESCRITÓRIO SERVICE ===
public interface IEscritorioService
{
    Task<List<EmpresaResumoDto>?> GetEmpresasAsync();
    Task<EmpresaResumoDto?> CriarEmpresaAsync(CreateEmpresaDto dto);
    Task<(EmpresaResumoDto? Empresa, string? Erro)> CriarEmpresaComResultadoAsync(CreateEmpresaDto dto);
    Task<List<UsuarioResumoDto>?> GetUsuariosAsync();
    Task<UsuarioResumoDto?> CriarUsuarioAsync(CreateUsuarioDto dto);
    Task<UsuarioResumoDto?> AtualizarUsuarioAsync(Guid id, UpdateUsuarioDto dto);
    Task<UsuarioResumoDto?> ToggleAtivoUsuarioAsync(Guid id);
    Task<bool> ExcluirUsuarioAsync(Guid id);
    Task<EscritorioDto?> RegistrarAsync(CreateEscritorioDto dto);
}

public class EscritorioService : IEscritorioService
{
    private readonly ApiClient _api;
    private readonly ILocalStorageService _storage;

    public EscritorioService(ApiClient api, ILocalStorageService storage)
    {
        _api = api;
        _storage = storage;
    }

    public async Task<List<EmpresaResumoDto>?> GetEmpresasAsync()
    {
        var cached = await _storage.GetItemAsStringAsync("empresas");
        if (!string.IsNullOrEmpty(cached))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<EmpresaResumoDto>>(cached,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch { }
        }
        return await _api.GetAsync<List<EmpresaResumoDto>>("api/escritorio/empresas");
    }

    public async Task<EmpresaResumoDto?> CriarEmpresaAsync(CreateEmpresaDto dto)
    {
        var result = await _api.PostAsync<CreateEmpresaDto, EmpresaResumoDto>("api/escritorio/empresas", dto);
        if (result != null)
            await _storage.RemoveItemAsync("empresas");
        return result;
    }

    public async Task<(EmpresaResumoDto? Empresa, string? Erro)> CriarEmpresaComResultadoAsync(CreateEmpresaDto dto)
    {
        var response = await _api.PostRawAsync("api/escritorio/empresas", dto);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<EmpresaResumoDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (result != null) await _storage.RemoveItemAsync("empresas");
            return (result, null);
        }
        var erro = await ApiHelper.ExtrairMensagemErro(response);
        return (null, erro);
    }

    public async Task<List<UsuarioResumoDto>?> GetUsuariosAsync() =>
        await _api.GetAsync<List<UsuarioResumoDto>>("api/escritorio/usuarios");

    public async Task<UsuarioResumoDto?> CriarUsuarioAsync(CreateUsuarioDto dto) =>
        await _api.PostAsync<CreateUsuarioDto, UsuarioResumoDto>("api/escritorio/usuarios", dto);

    public async Task<UsuarioResumoDto?> AtualizarUsuarioAsync(Guid id, UpdateUsuarioDto dto) =>
        await _api.PutAsync<UpdateUsuarioDto, UsuarioResumoDto>($"api/escritorio/usuarios/{id}", dto);

    public async Task<UsuarioResumoDto?> ToggleAtivoUsuarioAsync(Guid id) =>
        await _api.PatchAsync<UsuarioResumoDto>($"api/escritorio/usuarios/{id}/toggle-ativo");

    public async Task<bool> ExcluirUsuarioAsync(Guid id)
    {
        var response = await _api.DeleteAsync($"api/escritorio/usuarios/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<EscritorioDto?> RegistrarAsync(CreateEscritorioDto dto) =>
        await _api.PostAsync<CreateEscritorioDto, EscritorioDto>("api/escritorio/registrar", dto);
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
    Task<EmpresaDetalheDto?> GetEmpresaAsync();
    Task<CertificadoStatusDto?> GetCertificadoStatusAsync();
}

public class EmpresaService : IEmpresaService
{
    private readonly ApiClient _api;

    public EmpresaService(ApiClient api) => _api = api;

    public async Task<EmpresaDetalheDto?> GetEmpresaAsync() =>
        await _api.GetAsync<EmpresaDetalheDto>("api/empresa");

    public async Task<CertificadoStatusDto?> GetCertificadoStatusAsync() =>
        await _api.GetAsync<CertificadoStatusDto>("api/empresa/certificado/status");
}

public record EmitirNFeResult(bool Sucesso, Guid? NotaFiscalId, string? ChaveAcesso, string? Protocolo, string? MensagemErro);
public record GetNotasResult(IEnumerable<NotaFiscalResumoDto> Notas, int Total, int Pagina, int TamanhoPagina);
