using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.Interfaces;

namespace NfeSaas.Infrastructure.Services;

// Implementação de IEmailService via Resend (https://resend.com). Chamada pelo
// EnviarNFePorEmailCommandHandler quando o usuário aciona o envio manual na tela da nota.
public class ResendEmailService : IEmailService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<ResendEmailService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> EnviarNFeAsync(string destinatario, string chaveAcesso, byte[] xmlBytes, byte[] danfeBytes, CancellationToken ct = default)
    {
        var apiKey = _config["Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Resend:ApiKey não configurado — email da NF-e {Chave} não enviado para {Destinatario}.",
                chaveAcesso, destinatario);
            return false;
        }

        var fromEmail = _config["Resend:FromEmail"];
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogError("Resend:FromEmail não configurado — email da NF-e {Chave} não enviado.", chaveAcesso);
            return false;
        }

        var payload = new
        {
            from = fromEmail,
            to = new[] { destinatario },
            subject = $"NF-e {chaveAcesso} — documento fiscal",
            html = $"<p>Segue em anexo o XML e o DANFE da NF-e <strong>{chaveAcesso}</strong>.</p>",
            attachments = new object[]
            {
                new { filename = $"NFe{chaveAcesso}.xml", content = Convert.ToBase64String(xmlBytes) },
                new { filename = $"DANFE-{chaveAcesso}.pdf", content = Convert.ToBase64String(danfeBytes) }
            }
        };

        using var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.resend.com/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var response = await client.PostAsJsonAsync("emails", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Falha ao enviar email da NF-e {Chave} via Resend: {Status} {Body}",
                    chaveAcesso, response.StatusCode, body);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email da NF-e {Chave} via Resend.", chaveAcesso);
            return false;
        }
    }
}
