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
            if (!string.IsNullOrWhiteSpace(body) && body.Length < 300)
                return body;
        }
        catch { }

        return (int)response.StatusCode switch
        {
            400 => "Requisição inválida. Verifique os dados informados.",
            401 => "Não autenticado. Faça login novamente.",
            403 => "Acesso negado. Você não tem permissão para esta operação.",
            404 => "Recurso não encontrado.",
            409 => "Conflito: o recurso já existe ou está em uso.",
            422 => "Dados inválidos. Verifique os campos e tente novamente.",
            500 => "Erro interno no servidor. Tente novamente ou contate o suporte.",
            502 or 503 => "Serviço temporariamente indisponível. Tente novamente em instantes.",
            _ => $"Erro {(int)response.StatusCode}. Tente novamente ou contate o suporte."
        };
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

    public async Task<HttpResponseMessage> PutRawAsync<T>(string url, T data)
    {
        await SetAuthHeader();
        return await _http.PutAsJsonAsync(url, data);
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
    Task<LoginAttemptResult> LoginAsync(string email, string senha);
    Task<bool> SelecionarEmpresaAsync(Guid empresaId);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> HasEmpresaSelecionadaAsync();
}

// Resultado do login: Sucesso ou falha detalhada (TrialExpirado, EscritorioSuspenso, Credencial).
public record LoginAttemptResult(LoginResultDto? Sucesso, string? Codigo, string? Mensagem, AssinaturaDto? Assinatura);

public class AuthService : IAuthService
{
    private readonly ApiClient _api;
    private readonly ILocalStorageService _storage;

    public AuthService(ApiClient api, ILocalStorageService storage)
    {
        _api = api;
        _storage = storage;
    }

    public async Task<LoginAttemptResult> LoginAsync(string email, string senha)
    {
        var resp = await _api.PostRawAsync("api/auth/login", new LoginDto(email, senha));

        if (resp.IsSuccessStatusCode)
        {
            var result = await resp.Content.ReadFromJsonAsync<LoginResultDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (result == null)
                return new(null, "RespostaInvalida", "Servidor retornou resposta inválida.", null);

            await _storage.SetItemAsStringAsync("access_token", result.AccessToken);
            await _storage.SetItemAsStringAsync("refresh_token", result.RefreshToken);
            await _storage.SetItemAsStringAsync("user_name", result.NomeUsuario);
            await _storage.SetItemAsStringAsync("user_role", result.Role);
            await _storage.SetItemAsStringAsync("escritorio_id", result.EscritorioId.ToString());

            var empresasJson = System.Text.Json.JsonSerializer.Serialize(result.Empresas);
            await _storage.SetItemAsStringAsync("empresas", empresasJson);
            await _storage.SetItemAsStringAsync("assinatura", System.Text.Json.JsonSerializer.Serialize(result.Assinatura));

            if (result.Empresas.Count == 1)
                await SelecionarEmpresaAsync(result.Empresas[0].Id);

            return new(result, null, null, result.Assinatura);
        }

        // Falha estruturada: 401 (credencial), 402 (trial expirado), 403 (suspenso)
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var codigo = doc.RootElement.TryGetProperty("codigo", out var c) ? c.GetString() : null;
            var mensagem = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
            AssinaturaDto? assinatura = null;
            if (doc.RootElement.TryGetProperty("assinatura", out var a) && a.ValueKind == JsonValueKind.Object)
            {
                assinatura = a.Deserialize<AssinaturaDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            return new(null, codigo, mensagem, assinatura);
        }
        catch
        {
            return new(null, "Erro", await ApiHelper.ExtrairMensagemErro(resp), null);
        }
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
        await _storage.RemoveItemAsync("assinatura");
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
    Task<(EmpresaResumoDto? Empresa, string? Erro)> CadastrarComoEmpresaAsync(CadastrarEscritorioComoEmpresaDto dto);
    Task<(bool Sucesso, string? Erro)> AtivarPlanoAsync(AtivarPlanoPagoDto dto);
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

    public async Task<(EmpresaResumoDto? Empresa, string? Erro)> CadastrarComoEmpresaAsync(CadastrarEscritorioComoEmpresaDto dto)
    {
        var resp = await _api.PostRawAsync("api/escritorio/cadastrar-como-empresa", dto);
        if (resp.IsSuccessStatusCode)
        {
            var empresa = await resp.Content.ReadFromJsonAsync<EmpresaResumoDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (empresa != null) await _storage.RemoveItemAsync("empresas");
            return (empresa, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(resp));
    }

    public async Task<(bool Sucesso, string? Erro)> AtivarPlanoAsync(AtivarPlanoPagoDto dto)
    {
        var resp = await _api.PostRawAsync("api/escritorio/ativar-plano", dto);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ApiHelper.ExtrairMensagemErro(resp));
    }
}

// === NOTA FISCAL SERVICE ===
public interface INotaFiscalService
{
    Task<GetNotasResult?> GetNotasAsync(int pagina = 1, int tamanhoPagina = 20);
    Task<NotaFiscalDetalheDto?> GetNotaAsync(Guid id);
    Task<EmitirNFeResult?> EmitirAsync(EmitirNotaFiscalDto dto);
    Task<bool> CancelarAsync(Guid id, string justificativa);
    Task<byte[]?> GetDanfePdfAsync(Guid id);
    Task<(bool Sucesso, string? Erro)> EnviarEmailAsync(Guid id, string? emailDestino);
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

    public async Task<EmitirNFeResult?> EmitirAsync(EmitirNotaFiscalDto dto)
    {
        var response = await _api.PostRawAsync("api/notas-fiscais/emitir", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<EmitirNFeResult>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        var erro = await ApiHelper.ExtrairMensagemErro(response);
        return new EmitirNFeResult(false, null, null, null, erro);
    }

    public async Task<bool> CancelarAsync(Guid id, string justificativa)
    {
        var response = await _api.PostRawAsync($"api/notas-fiscais/{id}/cancelar", new { justificativa });
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> GetDanfePdfAsync(Guid id) =>
        await _api.GetBytesAsync($"api/notas-fiscais/{id}/danfe");

    public async Task<(bool Sucesso, string? Erro)> EnviarEmailAsync(Guid id, string? emailDestino)
    {
        var response = await _api.PostRawAsync($"api/notas-fiscais/{id}/enviar-email", new { emailDestino });
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiHelper.ExtrairMensagemErro(response));
    }

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
    Task<(bool Sucesso, string? Erro)> AtualizarEmpresaAsync(UpdateEmpresaDto dto);
}

public class EmpresaService : IEmpresaService
{
    private readonly ApiClient _api;

    public EmpresaService(ApiClient api) => _api = api;

    public async Task<EmpresaDetalheDto?> GetEmpresaAsync() =>
        await _api.GetAsync<EmpresaDetalheDto>("api/empresa");

    public async Task<CertificadoStatusDto?> GetCertificadoStatusAsync() =>
        await _api.GetAsync<CertificadoStatusDto>("api/empresa/certificado/status");

    public async Task<(bool Sucesso, string? Erro)> AtualizarEmpresaAsync(UpdateEmpresaDto dto)
    {
        var resp = await _api.PutRawAsync("api/empresa", dto);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ApiHelper.ExtrairMensagemErro(resp));
    }
}

// === CONFIGURAÇÃO EMPRESA SERVICE ===
public interface IConfiguracaoEmpresaService
{
    Task<ConfiguracaoEmpresaDto?> GetAsync();
    Task<(ConfiguracaoEmpresaDto? Configuracao, string? Erro)> SalvarAsync(ConfiguracaoEmpresaDto dto);
}

public class ConfiguracaoEmpresaService : IConfiguracaoEmpresaService
{
    private readonly ApiClient _api;

    public ConfiguracaoEmpresaService(ApiClient api) => _api = api;

    public async Task<ConfiguracaoEmpresaDto?> GetAsync() =>
        await _api.GetAsync<ConfiguracaoEmpresaDto>("api/empresa/configuracao");

    public async Task<(ConfiguracaoEmpresaDto? Configuracao, string? Erro)> SalvarAsync(ConfiguracaoEmpresaDto dto)
    {
        var response = await _api.PostRawAsync("api/empresa/configuracao", dto);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ConfiguracaoEmpresaDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (result, null);
        }
        var erro = await ApiHelper.ExtrairMensagemErro(response);
        return (null, erro);
    }
}

public record EmitirNFeResult(bool Sucesso, Guid? NotaFiscalId, string? ChaveAcesso, string? Protocolo, string? MensagemErro);
public record GetNotasResult(IEnumerable<NotaFiscalResumoDto> Notas, int Total, int Pagina, int TamanhoPagina);

// === NCM SERVICE ===
public record NcmDto(string Codigo, string Descricao, string? Capitulo, string? Posicao,
    decimal? AliquotaIpiPadrao, bool ExigeCest);
public record NcmStatusDto(int TotalAtivos, string? VersaoTabela);

public interface INcmService
{
    Task<List<NcmDto>> BuscarAsync(string termo, int limite = 10);
    Task<NcmDto?> ValidarAsync(string codigo);
    Task<NcmStatusDto?> StatusAsync();
}

public class NcmService : INcmService
{
    private readonly ApiClient _api;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, List<NcmDto>> _cacheBusca = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, NcmDto?> _cacheValidar = new();

    public NcmService(ApiClient api) => _api = api;

    public async Task<List<NcmDto>> BuscarAsync(string termo, int limite = 10)
    {
        if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
            return new();

        var chave = $"{termo.Trim().ToLowerInvariant()}|{limite}";
        if (_cacheBusca.TryGetValue(chave, out var cached)) return cached;

        var result = await _api.GetAsync<List<NcmDto>>(
            $"api/ncm/buscar?termo={Uri.EscapeDataString(termo)}&limite={limite}") ?? new();

        _cacheBusca[chave] = result;
        return result;
    }

    public async Task<NcmDto?> ValidarAsync(string codigo)
    {
        var d = new string((codigo ?? "").Where(char.IsDigit).ToArray());
        if (d.Length != 8) return null;

        if (_cacheValidar.TryGetValue(d, out var cached)) return cached;

        var result = await _api.GetAsync<NcmDto>($"api/ncm/{d}");
        _cacheValidar[d] = result;
        return result;
    }

    public Task<NcmStatusDto?> StatusAsync() => _api.GetAsync<NcmStatusDto>("api/ncm/status");
}

// === EVENTOS FISCAIS SERVICE ===
public interface IEventoFiscalService
{
    Task<List<EventoFiscalResumoDto>?> ListarPorNotaAsync(Guid notaId);
    Task<(EventoFiscalResumoDto? Evento, string? Erro)> EmitirCceAsync(Guid notaId, EmitirCceDto dto);
    Task<(EventoFiscalResumoDto? Evento, string? Erro)> ManifestarAsync(Guid notaId, ManifestarDto dto);
    Task<List<EventoFiscalResumoDto>?> ListarInutilizacoesAsync();
    Task<(EventoFiscalResumoDto? Evento, string? Erro)> InutilizarAsync(InutilizarDto dto);
}

public class EventoFiscalService : IEventoFiscalService
{
    private readonly ApiClient _api;
    public EventoFiscalService(ApiClient api) => _api = api;

    public Task<List<EventoFiscalResumoDto>?> ListarPorNotaAsync(Guid notaId) =>
        _api.GetAsync<List<EventoFiscalResumoDto>>($"api/notas-fiscais/{notaId}/eventos");

    public async Task<(EventoFiscalResumoDto? Evento, string? Erro)> EmitirCceAsync(Guid notaId, EmitirCceDto dto)
    {
        var response = await _api.PostRawAsync($"api/notas-fiscais/{notaId}/cce", dto);
        if (response.IsSuccessStatusCode)
        {
            var ev = await response.Content.ReadFromJsonAsync<EventoFiscalResumoDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (ev, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(response));
    }

    public async Task<(EventoFiscalResumoDto? Evento, string? Erro)> ManifestarAsync(Guid notaId, ManifestarDto dto)
    {
        var response = await _api.PostRawAsync($"api/notas-fiscais/{notaId}/manifestar", dto);
        if (response.IsSuccessStatusCode)
        {
            var ev = await response.Content.ReadFromJsonAsync<EventoFiscalResumoDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (ev, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(response));
    }

    public Task<List<EventoFiscalResumoDto>?> ListarInutilizacoesAsync() =>
        _api.GetAsync<List<EventoFiscalResumoDto>>("api/inutilizacoes");

    public async Task<(EventoFiscalResumoDto? Evento, string? Erro)> InutilizarAsync(InutilizarDto dto)
    {
        var response = await _api.PostRawAsync("api/inutilizacoes", dto);
        if (response.IsSuccessStatusCode)
        {
            var ev = await response.Content.ReadFromJsonAsync<EventoFiscalResumoDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (ev, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(response));
    }
}

// === PRODUTO SERVICE ===
public interface IProdutoService
{
    Task<List<ProdutoResumoDto>?> ListarAsync(bool apenasAtivos = false);
    Task<ProdutoDetalheDto?> GetAsync(Guid id);
    Task<(ProdutoDetalheDto? Produto, string? Erro)> CriarAsync(CreateProdutoDto dto);
    Task<(ProdutoDetalheDto? Produto, string? Erro)> AtualizarAsync(Guid id, UpdateProdutoDto dto);
    Task<ProdutoDetalheDto?> ToggleAtivoAsync(Guid id);
    Task<bool> ExcluirAsync(Guid id);
}

public class ProdutoService : IProdutoService
{
    private readonly ApiClient _api;
    public ProdutoService(ApiClient api) => _api = api;

    public Task<List<ProdutoResumoDto>?> ListarAsync(bool apenasAtivos = false) =>
        _api.GetAsync<List<ProdutoResumoDto>>($"api/produtos?apenasAtivos={apenasAtivos}");

    public Task<ProdutoDetalheDto?> GetAsync(Guid id) =>
        _api.GetAsync<ProdutoDetalheDto>($"api/produtos/{id}");

    public async Task<(ProdutoDetalheDto? Produto, string? Erro)> CriarAsync(CreateProdutoDto dto)
    {
        var response = await _api.PostRawAsync("api/produtos", dto);
        if (response.IsSuccessStatusCode)
        {
            var produto = await response.Content.ReadFromJsonAsync<ProdutoDetalheDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (produto, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(response));
    }

    public async Task<(ProdutoDetalheDto? Produto, string? Erro)> AtualizarAsync(Guid id, UpdateProdutoDto dto)
    {
        var response = await _api.PutRawAsync($"api/produtos/{id}", dto);
        if (response.IsSuccessStatusCode)
        {
            var produto = await response.Content.ReadFromJsonAsync<ProdutoDetalheDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (produto, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(response));
    }

    public Task<ProdutoDetalheDto?> ToggleAtivoAsync(Guid id) =>
        _api.PatchAsync<ProdutoDetalheDto>($"api/produtos/{id}/toggle-ativo");

    public async Task<bool> ExcluirAsync(Guid id)
    {
        var resp = await _api.DeleteAsync($"api/produtos/{id}");
        return resp.IsSuccessStatusCode;
    }
}

// === CLIENTE SERVICE ===
public interface IClienteService
{
    Task<List<ClienteResumoDto>?> ListarAsync(bool apenasAtivos = false);
    Task<ClienteDetalheDto?> GetAsync(Guid id);
    Task<(ClienteDetalheDto? Cliente, string? Erro)> CriarAsync(CreateClienteDto dto);
    Task<(ClienteDetalheDto? Cliente, string? Erro)> AtualizarAsync(Guid id, UpdateClienteDto dto);
    Task<ClienteDetalheDto?> ToggleAtivoAsync(Guid id);
    Task<bool> ExcluirAsync(Guid id);
}

public class ClienteService : IClienteService
{
    private readonly ApiClient _api;
    public ClienteService(ApiClient api) => _api = api;

    public Task<List<ClienteResumoDto>?> ListarAsync(bool apenasAtivos = false) =>
        _api.GetAsync<List<ClienteResumoDto>>($"api/clientes?apenasAtivos={apenasAtivos}");

    public Task<ClienteDetalheDto?> GetAsync(Guid id) =>
        _api.GetAsync<ClienteDetalheDto>($"api/clientes/{id}");

    public async Task<(ClienteDetalheDto? Cliente, string? Erro)> CriarAsync(CreateClienteDto dto)
    {
        var response = await _api.PostRawAsync("api/clientes", dto);
        if (response.IsSuccessStatusCode)
        {
            var cliente = await response.Content.ReadFromJsonAsync<ClienteDetalheDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (cliente, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(response));
    }

    public async Task<(ClienteDetalheDto? Cliente, string? Erro)> AtualizarAsync(Guid id, UpdateClienteDto dto)
    {
        var response = await _api.PutRawAsync($"api/clientes/{id}", dto);
        if (response.IsSuccessStatusCode)
        {
            var cliente = await response.Content.ReadFromJsonAsync<ClienteDetalheDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (cliente, null);
        }
        return (null, await ApiHelper.ExtrairMensagemErro(response));
    }

    public Task<ClienteDetalheDto?> ToggleAtivoAsync(Guid id) =>
        _api.PatchAsync<ClienteDetalheDto>($"api/clientes/{id}/toggle-ativo");

    public async Task<bool> ExcluirAsync(Guid id)
    {
        var resp = await _api.DeleteAsync($"api/clientes/{id}");
        return resp.IsSuccessStatusCode;
    }
}
