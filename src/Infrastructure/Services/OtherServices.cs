using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NfeSaas.Application.Interfaces;

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
    public CertificadoInfo ValidarCertificado(byte[] bytes, string senha)
    {
        try
        {
            var cert = new X509Certificate2(bytes, senha, X509KeyStorageFlags.Exportable);

            // Extrair CNPJ do subject
            var subject = cert.Subject;
            var cnpj = ExtrairCnpjDoCertificado(subject);

            return new CertificadoInfo(
                Valido: cert.NotAfter > DateTime.UtcNow,
                Cnpj: cnpj,
                NomeTitular: cert.GetNameInfo(X509NameType.SimpleName, false),
                Validade: cert.NotAfter,
                MensagemErro: cert.NotAfter <= DateTime.UtcNow ? "Certificado expirado" : null);
        }
        catch (Exception ex)
        {
            return new CertificadoInfo(false, null, null, DateTime.MinValue, $"Erro ao ler certificado: {ex.Message}");
        }
    }

    public byte[] ExportarPublicKey(byte[] bytes, string senha)
    {
        var cert = new X509Certificate2(bytes, senha);
        return cert.Export(X509ContentType.Cert);
    }

    private static string? ExtrairCnpjDoCertificado(string subject)
    {
        // CNPJ está geralmente no OID 2.16.76.1.3.3 ou no subject como CNPJ:
        var parts = subject.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Contains("CNPJ:") || trimmed.StartsWith("2.16.76.1.3.3"))
            {
                var value = trimmed.Split(':').LastOrDefault()?.Trim();
                if (value?.Length == 14) return value;
            }
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
