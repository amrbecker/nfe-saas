using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NfeSaas.WebUI.Services;

public record EnderecoCep(
    string Cep,
    string Logradouro,
    string Complemento,
    string Bairro,
    string Cidade,
    string Uf,
    string CodigoMunicipio);

public enum ViaCepStatus { Sucesso, CepInvalido, NaoEncontrado, FalhaRede }

public record ViaCepResultado(ViaCepStatus Status, EnderecoCep? Endereco, string? MensagemErro)
{
    public bool Sucesso => Status == ViaCepStatus.Sucesso && Endereco != null;

    public static ViaCepResultado Ok(EnderecoCep e) => new(ViaCepStatus.Sucesso, e, null);
    public static ViaCepResultado Invalido() => new(ViaCepStatus.CepInvalido, null, "CEP deve ter 8 dígitos.");
    public static ViaCepResultado NaoEncontrado() => new(ViaCepStatus.NaoEncontrado, null, "CEP não encontrado.");
    public static ViaCepResultado Falha(string msg) => new(ViaCepStatus.FalhaRede, null, msg);
}

public interface IViaCepService
{
    Task<ViaCepResultado> ConsultarAsync(string? cep, CancellationToken ct = default);
    string ApenasDigitos(string? cep);
    string Formatar(string? cep);
}

public class ViaCepService : IViaCepService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(6) };
    private static readonly ConcurrentDictionary<string, ViaCepResultado> _cache = new();
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public string ApenasDigitos(string? cep) =>
        string.IsNullOrEmpty(cep) ? "" : new string(cep.Where(char.IsDigit).ToArray());

    public string Formatar(string? cep)
    {
        var d = ApenasDigitos(cep);
        return d.Length == 8 ? $"{d[..5]}-{d[5..]}" : d;
    }

    public async Task<ViaCepResultado> ConsultarAsync(string? cep, CancellationToken ct = default)
    {
        var d = ApenasDigitos(cep);
        if (d.Length != 8 || d == "00000000")
            return ViaCepResultado.Invalido();

        if (_cache.TryGetValue(d, out var cached))
            return cached;

        try
        {
            var resp = await _http.GetAsync($"https://viacep.com.br/ws/{d}/json/", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return resp.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? ViaCepResultado.Invalido()
                    : ViaCepResultado.Falha("Não foi possível consultar o CEP no momento.");
            }

            var via = await resp.Content.ReadFromJsonAsync<ViaCepResponse>(_json, ct);
            if (via == null || via.Erro == true)
            {
                var nf = ViaCepResultado.NaoEncontrado();
                _cache.TryAdd(d, nf);
                return nf;
            }

            var ok = ViaCepResultado.Ok(new EnderecoCep(
                Cep: d,
                Logradouro: via.Logradouro ?? "",
                Complemento: via.Complemento ?? "",
                Bairro: via.Bairro ?? "",
                Cidade: via.Localidade ?? "",
                Uf: via.Uf ?? "",
                CodigoMunicipio: via.Ibge ?? ""));
            _cache.TryAdd(d, ok);
            return ok;
        }
        catch (TaskCanceledException)
        {
            return ViaCepResultado.Falha("Tempo esgotado ao consultar o CEP.");
        }
        catch (Exception)
        {
            return ViaCepResultado.Falha("Erro de rede ao consultar o CEP.");
        }
    }

    private sealed class ViaCepResponse
    {
        [JsonPropertyName("cep")]         public string? Cep { get; set; }
        [JsonPropertyName("logradouro")]  public string? Logradouro { get; set; }
        [JsonPropertyName("complemento")] public string? Complemento { get; set; }
        [JsonPropertyName("bairro")]      public string? Bairro { get; set; }
        [JsonPropertyName("localidade")]  public string? Localidade { get; set; }
        [JsonPropertyName("uf")]          public string? Uf { get; set; }
        [JsonPropertyName("ibge")]        public string? Ibge { get; set; }
        [JsonPropertyName("erro")]        public bool? Erro { get; set; }
    }
}
