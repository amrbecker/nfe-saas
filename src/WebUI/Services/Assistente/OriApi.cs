using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

public interface IOriApi
{
    Task<List<ArtigoResumoDto>> MapaAsync();
    Task<ArtigoDetalheDto?> ArtigoAsync(string id);
    Task<(RespostaOriDto? Resposta, string? Erro)> ExplicarRejeicaoAsync(Guid notaId, ContextoTelaDto contexto);
    Task<(RespostaOriDto? Resposta, string? Erro)> PerguntarAsync(string pergunta, ContextoTelaDto contexto);
    Task<ConversaResumoDto?> CriarConversaAsync(string titulo);
    IAsyncEnumerable<EventoConversaDto> EnviarMensagemAsync(Guid conversaId, NovaMensagemDto mensagem, CancellationToken ct = default);
    Task AvaliarAsync(Guid interacaoId, int valor);
    Task<bool> CriarChamadoAsync(CriarChamadoDto dto);
    Task<bool> CriarSinalAsync(CriarSinalDto dto);
    Task<string?> CriarLembreteAsync(CriarLembreteDto dto);
}

public class OriApi : IOriApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ApiClient _api;
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private List<ArtigoResumoDto>? _mapa;

    public OriApi(ApiClient api, HttpClient http, ILocalStorageService storage)
    {
        _api = api;
        _http = http;
        _storage = storage;
    }

    public async Task<List<ArtigoResumoDto>> MapaAsync()
    {
        if (_mapa != null) return _mapa;
        try { _mapa = (await _api.GetAsync<MapaKbDto>("api/assistente/base/mapa"))?.Artigos; }
        catch (HttpRequestException) { }
        return _mapa ?? new List<ArtigoResumoDto>();
    }

    public async Task<ArtigoDetalheDto?> ArtigoAsync(string id)
    {
        try { return await _api.GetAsync<ArtigoDetalheDto>($"api/assistente/base/artigos/{id}"); }
        catch (HttpRequestException) { return null; }
    }

    public Task<(RespostaOriDto?, string?)> ExplicarRejeicaoAsync(Guid notaId, ContextoTelaDto contexto) =>
        PostarResposta($"api/assistente/explicar-rejeicao/{notaId}", new ExplicarRejeicaoDto(contexto));

    public Task<(RespostaOriDto?, string?)> PerguntarAsync(string pergunta, ContextoTelaDto contexto) =>
        PostarResposta("api/assistente/perguntar", new PerguntaOriDto(pergunta, contexto));

    private async Task<(RespostaOriDto?, string?)> PostarResposta<T>(string url, T corpo)
    {
        try
        {
            var r = await _api.PostRawAsync(url, corpo);
            if (r.IsSuccessStatusCode) return (await r.Content.ReadFromJsonAsync<RespostaOriDto>(Json), null);
            return (null, (int)r.StatusCode == 404 ? "Não encontrei isso para a empresa selecionada." : await ApiHelper.ExtrairMensagemErro(r));
        }
        catch (HttpRequestException) { return (null, "Sem conexão com o servidor. Tente de novo."); }
    }

    public async Task<ConversaResumoDto?> CriarConversaAsync(string titulo)
    {
        try { return await _api.PostAsync<object, ConversaResumoDto>("api/assistente/conversas", new { titulo }); }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>SSE: lê linhas "data: {json}" à medida que chegam (streaming habilitado no fetch do navegador).</summary>
    public async IAsyncEnumerable<EventoConversaDto> EnviarMensagemAsync(Guid conversaId, NovaMensagemDto mensagem,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"api/assistente/conversas/{conversaId}/mensagens")
        {
            Content = JsonContent.Create(mensagem, options: Json)
        };
        req.SetBrowserResponseStreamingEnabled(true);
        var token = await _storage.GetItemAsStringAsync("access_token");
        if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage? resp = null;
        string? erroConexao = null;
        try { resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct); }
        catch (HttpRequestException) { erroConexao = "Sem conexão com o servidor. Tente de novo."; }
        if (resp == null || !resp.IsSuccessStatusCode)
        {
            yield return new EventoConversaDto("erro", erroConexao ?? "Não consegui falar com a Ori agora.", null);
            yield break;
        }

        using (resp)
        {
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var leitor = new StreamReader(stream);
            while (!ct.IsCancellationRequested)
            {
                var linha = await leitor.ReadLineAsync(ct);
                if (linha == null) yield break;
                if (!linha.StartsWith("data: ")) continue;
                EventoConversaDto? evento = null;
                try { evento = JsonSerializer.Deserialize<EventoConversaDto>(linha[6..], Json); } catch (JsonException) { }
                if (evento != null) yield return evento;
                if (evento?.Tipo is "resposta" or "erro") yield break;
            }
        }
    }

    public async Task AvaliarAsync(Guid interacaoId, int valor)
    {
        try { await _api.PostRawAsync($"api/assistente/interacoes/{interacaoId}/avaliacao", new AvaliacaoDto(valor)); }
        catch (HttpRequestException) { }
    }

    public async Task<bool> CriarChamadoAsync(CriarChamadoDto dto)
    {
        try { return (await _api.PostRawAsync("api/assistente/chamados", dto)).IsSuccessStatusCode; }
        catch (HttpRequestException) { return false; }
    }

    public async Task<bool> CriarSinalAsync(CriarSinalDto dto)
    {
        try { return (await _api.PostRawAsync("api/assistente/sinais", dto)).IsSuccessStatusCode; }
        catch (HttpRequestException) { return false; }
    }

    public async Task<string?> CriarLembreteAsync(CriarLembreteDto dto)
    {
        try
        {
            var r = await _api.PostRawAsync("api/assistente/lembretes", dto);
            return r.IsSuccessStatusCode ? null : await ApiHelper.ExtrairMensagemErro(r);
        }
        catch (HttpRequestException) { return "Sem conexão com o servidor."; }
    }
}
