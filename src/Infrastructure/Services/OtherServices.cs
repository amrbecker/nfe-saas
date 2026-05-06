using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GerarAccessToken(Guid usuarioId, string email, string role, Guid escritorioId, Guid? empresaId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("escritorio_id", escritorioId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (empresaId.HasValue)
            claimsList.Add(new Claim("empresa_id", empresaId.Value.ToString()));

        var claims = claimsList.ToArray();

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GerarRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public Guid? ObterUsuarioIdDoToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
        catch { return null; }
    }
}

public class CertificadoService : ICertificadoService
{
    // ICP-Brasil OIDs for NF-e certificates
    // 2.16.76.1.3.3 = CNPJ (PJ/e-CNPJ)
    // 2.16.76.1.3.7 = CNPJ in NF-e A1 certs
    // Policy OIDs for A1 (software) certificates
    private static readonly string[] _icpBrasilPolicyOids =
    [
        "2.16.76.1.2.1",  // A1 Pessoa Física
        "2.16.76.1.2.2",  // A2 Pessoa Física
        "2.16.76.1.2.3",  // A3 Pessoa Física
        "2.16.76.1.2.4",  // A4 Pessoa Física
        "2.16.76.1.2.101", // A1 Pessoa Jurídica
        "2.16.76.1.2.102", // A2 Pessoa Jurídica
        "2.16.76.1.2.103", // A3 Pessoa Jurídica
        "2.16.76.1.2.104", // A4 Pessoa Jurídica
    ];

    public CertificadoInfo ValidarCertificado(byte[] bytes, string senha)
    {
        try
        {
            var cert = new X509Certificate2(bytes, senha, X509KeyStorageFlags.Exportable);

            var subject = cert.Subject;
            var cnpj = ExtrairCnpjDoCertificado(cert);
            var validade = cert.NotAfter.ToUniversalTime();
            var isIcpBrasil = ValidarIcpBrasil(cert);

            if (!isIcpBrasil)
                return new CertificadoInfo(false, cnpj,
                    cert.GetNameInfo(X509NameType.SimpleName, false),
                    validade, "Certificado não é ICP-Brasil (A1/A3 e-CNPJ)");

            if (validade <= DateTime.UtcNow)
                return new CertificadoInfo(false, cnpj,
                    cert.GetNameInfo(X509NameType.SimpleName, false),
                    validade, "Certificado expirado");

            return new CertificadoInfo(
                Valido: true,
                Cnpj: cnpj,
                NomeTitular: cert.GetNameInfo(X509NameType.SimpleName, false),
                Validade: validade,
                MensagemErro: null);
        }
        catch (Exception ex)
        {
            // ex.Message is in English (.NET runtime); expose only a generic PT-BR message
            _ = ex;
            return new CertificadoInfo(false, null, null, DateTime.MinValue,
                "Não foi possível ler o certificado. Verifique se o arquivo é válido e se a senha está correta.");
        }
    }

    public byte[] ExportarPublicKey(byte[] bytes, string senha)
    {
        var cert = new X509Certificate2(bytes, senha);
        return cert.Export(X509ContentType.Cert);
    }

    private static bool ValidarIcpBrasil(X509Certificate2 cert)
    {
        // Check certificate policies extension for ICP-Brasil OIDs
        foreach (var ext in cert.Extensions)
        {
            if (ext.Oid?.Value == "2.5.29.32") // Certificate Policies
            {
                var raw = ext.RawData;
                var rawStr = Convert.ToBase64String(raw);
                // Check if any ICP-Brasil policy OID is present in the raw extension data
                foreach (var oid in _icpBrasilPolicyOids)
                {
                    if (rawStr.Contains(oid) || ext.Format(false).Contains(oid))
                        return true;
                }
            }
        }

        // Fallback: check issuer for known ICP-Brasil CAs
        var issuer = cert.Issuer;
        return issuer.Contains("ICP-Brasil", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("AC SOLUTI", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("AC CERTISIGN", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("AC VALID", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("AC SERASA", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("Autoridade Certificadora", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("2.16.76.", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtrairCnpjDoCertificado(X509Certificate2 cert)
    {
        // Try OID 2.16.76.1.3.3 (CNPJ in SAN/Subject)
        foreach (var ext in cert.Extensions)
        {
            if (ext.Oid?.Value == "2.5.29.17") // Subject Alternative Name
            {
                var sanText = ext.Format(false);
                var match = System.Text.RegularExpressions.Regex.Match(
                    sanText, @"2\.16\.76\.1\.3\.3[^=]*=(\d{14})");
                if (match.Success) return match.Groups[1].Value;

                // OtherName format
                match = System.Text.RegularExpressions.Regex.Match(sanText, @"\b(\d{14})\b");
                if (match.Success) return match.Groups[1].Value;
            }
        }

        // Fallback: parse from Subject
        var parts = cert.Subject.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Contains("CNPJ:") || trimmed.StartsWith("2.16.76.1.3.3"))
            {
                var value = trimmed.Split(':').LastOrDefault()?.Trim();
                var digits = new string(value?.Where(char.IsDigit).ToArray() ?? []);
                if (digits.Length == 14) return digits;
            }

            // CN may contain CNPJ directly: "CN=EMPRESA:12345678000195"
            var cnMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\b(\d{14})\b");
            if (cnMatch.Success) return cnMatch.Groups[1].Value;
        }

        return null;
    }
}

public class ImpostoCalculoService : IImpostoCalculoService
{
    public ImpostoResultado CalcularIcms(decimal valorProduto, decimal aliquota, decimal? percentualReducao = null)
    {
        var baseCalculo = valorProduto;
        if (percentualReducao.HasValue && percentualReducao.Value > 0)
            baseCalculo = valorProduto * (1 - percentualReducao.Value / 100);

        baseCalculo = Math.Round(baseCalculo, 2);
        var valor = Math.Round(baseCalculo * (aliquota / 100), 2);
        return new ImpostoResultado(baseCalculo, aliquota, valor);
    }

    public ImpostoResultado CalcularPis(decimal valorProduto, decimal aliquota)
    {
        var baseCalculo = Math.Round(valorProduto, 2);
        var valor = Math.Round(baseCalculo * (aliquota / 100), 2);
        return new ImpostoResultado(baseCalculo, aliquota, valor);
    }

    public ImpostoResultado CalcularCofins(decimal valorProduto, decimal aliquota)
    {
        var baseCalculo = Math.Round(valorProduto, 2);
        var valor = Math.Round(baseCalculo * (aliquota / 100), 2);
        return new ImpostoResultado(baseCalculo, aliquota, valor);
    }

    public ImpostoResultado CalcularIcmsSt(decimal valorProduto, decimal mva, decimal aliquotaInterna, decimal aliquotaInterestadual)
    {
        // Base ICMS-ST = (Valor Produto + IPI + Frete + Outras) * (1 + MVA/100)
        var baseCalculo = Math.Round(valorProduto * (1 + mva / 100), 2);
        var icmsInterestadual = Math.Round(valorProduto * (aliquotaInterestadual / 100), 2);
        var icmsInterno = Math.Round(baseCalculo * (aliquotaInterna / 100), 2);
        var valorSt = Math.Max(0, Math.Round(icmsInterno - icmsInterestadual, 2));
        return new ImpostoResultado(baseCalculo, aliquotaInterna, valorSt);
    }
}

public class CepValidationService : ICepValidationService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<CepValidationService> _logger;

    public CepValidationService(IHttpClientFactory httpFactory, ILogger<CepValidationService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public bool FormatoValido(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return false;
        var digits = new string(cep.Where(char.IsDigit).ToArray());
        return digits.Length == 8 && digits != "00000000";
    }

    public async Task<CepInfo?> ConsultarAsync(string cep, CancellationToken ct = default)
    {
        var digits = new string(cep.Where(char.IsDigit).ToArray());
        if (digits.Length != 8) return null;

        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetFromJsonAsync<ViaCepResponse>(
                $"https://viacep.com.br/ws/{digits}/json/", ct);

            if (response == null || response.Erro == true) return null;

            return new CepInfo(
                Cep: response.Cep?.Replace("-", "") ?? digits,
                Logradouro: response.Logradouro ?? "",
                Bairro: response.Bairro ?? "",
                Cidade: response.Localidade ?? "",
                Uf: response.Uf ?? "",
                CodigoMunicipio: response.Ibge ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar ViaCEP para CEP {Cep}", digits);
            return null;
        }
    }

    private record ViaCepResponse(
        string? Cep, string? Logradouro, string? Bairro,
        string? Localidade, string? Uf, string? Ibge, bool? Erro);
}

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditRepo;
    private readonly IUnitOfWork _uow;

    public AuditService(IAuditLogRepository auditRepo, IUnitOfWork uow)
    {
        _auditRepo = auditRepo;
        _uow = uow;
    }

    public async Task RegistrarAsync(Guid empresaId, string acao, Guid? usuarioId = null,
        string? chaveNfe = null, string? detalhes = null, string? ipOrigem = null,
        CancellationToken ct = default)
    {
        var log = AuditLog.Criar(empresaId, acao, usuarioId, chaveNfe, detalhes, ipOrigem);
        await _auditRepo.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
