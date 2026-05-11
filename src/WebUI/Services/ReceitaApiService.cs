using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NfeSaas.WebUI.Services;

public record EmpresaReceita(
    string Cnpj,
    string RazaoSocial,
    string NomeFantasia,
    string Cnae,
    string DescricaoCnae,
    string Email,
    string Telefone,
    string Logradouro,
    string Numero,
    string Complemento,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    string CodigoMunicipio,
    string SituacaoCadastral);

public enum ReceitaStatus { Sucesso, CnpjInvalido, NaoEncontrado, FalhaRede }

public record ReceitaResultado(ReceitaStatus Status, EmpresaReceita? Empresa, string? MensagemErro)
{
    public bool Sucesso => Status == ReceitaStatus.Sucesso && Empresa != null;

    public static ReceitaResultado Ok(EmpresaReceita e) => new(ReceitaStatus.Sucesso, e, null);
    public static ReceitaResultado Invalido() => new(ReceitaStatus.CnpjInvalido, null, "CNPJ deve ter 14 dígitos válidos.");
    public static ReceitaResultado NaoEncontrado() => new(ReceitaStatus.NaoEncontrado, null, "CNPJ não encontrado na Receita Federal.");
    public static ReceitaResultado Falha(string msg) => new(ReceitaStatus.FalhaRede, null, msg);
}

public interface IReceitaApiService
{
    Task<ReceitaResultado> ConsultarAsync(string? cnpj, CancellationToken ct = default);
    string ApenasDigitos(string? cnpj);
    string Formatar(string? cnpj);
}

/// <summary>
/// Consulta dados cadastrais de CNPJ na BrasilAPI (https://brasilapi.com.br).
/// Cache em memória da SPA (ConcurrentDictionary estático) com TTL configurável.
/// Padrão equivalente ao <see cref="ViaCepService"/>.
/// </summary>
public class ReceitaApiService : IReceitaApiService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan _ttl = TimeSpan.FromDays(30);

    public string ApenasDigitos(string? cnpj) =>
        string.IsNullOrEmpty(cnpj) ? "" : new string(cnpj.Where(char.IsDigit).ToArray());

    public string Formatar(string? cnpj)
    {
        var d = ApenasDigitos(cnpj);
        return d.Length == 14
            ? $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}"
            : d;
    }

    public async Task<ReceitaResultado> ConsultarAsync(string? cnpj, CancellationToken ct = default)
    {
        var d = ApenasDigitos(cnpj);
        if (d.Length != 14 || !CnpjVerificador.Valido(d))
            return ReceitaResultado.Invalido();

        if (_cache.TryGetValue(d, out var cached) && cached.Expira > DateTime.UtcNow)
            return cached.Resultado;

        try
        {
            var resp = await _http.GetAsync($"https://brasilapi.com.br/api/cnpj/v1/{d}", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var nf = ReceitaResultado.NaoEncontrado();
                _cache[d] = new CacheEntry(nf, DateTime.UtcNow.Add(_ttl));
                return nf;
            }
            if (!resp.IsSuccessStatusCode)
                return ReceitaResultado.Falha("BrasilAPI indisponível no momento. Preencha manualmente.");

            var br = await resp.Content.ReadFromJsonAsync<BrasilApiResponse>(_json, ct);
            if (br == null)
                return ReceitaResultado.Falha("Resposta inválida da BrasilAPI.");

            var cep = ApenasDigitos(br.Cep);
            var empresa = new EmpresaReceita(
                Cnpj: d,
                RazaoSocial: br.RazaoSocial ?? "",
                NomeFantasia: br.NomeFantasia ?? "",
                Cnae: br.CnaeFiscal?.ToString("D7") ?? "",
                DescricaoCnae: br.CnaeFiscalDescricao ?? "",
                Email: br.Email ?? "",
                Telefone: br.DddTelefone1 ?? "",
                Logradouro: $"{br.DescricaoTipoLogradouro} {br.Logradouro}".Trim(),
                Numero: br.Numero ?? "",
                Complemento: br.Complemento ?? "",
                Bairro: br.Bairro ?? "",
                Cidade: br.Municipio ?? "",
                Uf: br.Uf ?? "",
                Cep: cep,
                CodigoMunicipio: br.CodigoMunicipioIbge?.ToString() ?? "",
                SituacaoCadastral: br.DescricaoSituacaoCadastral ?? "");

            var ok = ReceitaResultado.Ok(empresa);
            _cache[d] = new CacheEntry(ok, DateTime.UtcNow.Add(_ttl));
            return ok;
        }
        catch (TaskCanceledException)
        {
            return ReceitaResultado.Falha("Tempo esgotado ao consultar a Receita.");
        }
        catch (Exception)
        {
            return ReceitaResultado.Falha("Erro de rede ao consultar a Receita.");
        }
    }

    private sealed record CacheEntry(ReceitaResultado Resultado, DateTime Expira);

    private sealed class BrasilApiResponse
    {
        [JsonPropertyName("cnpj")]                        public string? Cnpj { get; set; }
        [JsonPropertyName("razao_social")]                public string? RazaoSocial { get; set; }
        [JsonPropertyName("nome_fantasia")]               public string? NomeFantasia { get; set; }
        [JsonPropertyName("cnae_fiscal")]                 public int? CnaeFiscal { get; set; }
        [JsonPropertyName("cnae_fiscal_descricao")]       public string? CnaeFiscalDescricao { get; set; }
        [JsonPropertyName("email")]                       public string? Email { get; set; }
        [JsonPropertyName("ddd_telefone_1")]              public string? DddTelefone1 { get; set; }
        [JsonPropertyName("descricao_tipo_logradouro")]   public string? DescricaoTipoLogradouro { get; set; }
        [JsonPropertyName("logradouro")]                  public string? Logradouro { get; set; }
        [JsonPropertyName("numero")]                      public string? Numero { get; set; }
        [JsonPropertyName("complemento")]                 public string? Complemento { get; set; }
        [JsonPropertyName("bairro")]                      public string? Bairro { get; set; }
        [JsonPropertyName("municipio")]                   public string? Municipio { get; set; }
        [JsonPropertyName("uf")]                          public string? Uf { get; set; }
        [JsonPropertyName("cep")]                         public string? Cep { get; set; }
        [JsonPropertyName("codigo_municipio_ibge")]       public int? CodigoMunicipioIbge { get; set; }
        [JsonPropertyName("descricao_situacao_cadastral")] public string? DescricaoSituacaoCadastral { get; set; }
    }

    // Validação CNPJ duplicada localmente para evitar acoplar a WebUI ao assembly Domain.
    internal static class CnpjVerificador
    {
        public static bool Valido(string digitos)
        {
            if (digitos.Length != 14 || digitos.All(c => c == digitos[0])) return false;
            int[] m1 = [5,4,3,2,9,8,7,6,5,4,3,2];
            int[] m2 = [6,5,4,3,2,9,8,7,6,5,4,3,2];
            int d1 = Dv(digitos[..12], m1);
            int d2 = Dv(digitos[..12] + d1, m2);
            return digitos[12] - '0' == d1 && digitos[13] - '0' == d2;
        }

        private static int Dv(string s, int[] mult)
        {
            int sum = 0;
            for (int i = 0; i < s.Length; i++) sum += (s[i] - '0') * mult[i];
            int r = sum % 11;
            return r < 2 ? 0 : 11 - r;
        }
    }
}
