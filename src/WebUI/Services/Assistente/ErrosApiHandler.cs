using System.Text.Json;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.WebUI.Services.Assistente;

/// <summary>
/// Registra no contexto da Ori os últimos erros HTTP (≥ 400) devolvidos pela API — rota sem querystring, status,
/// codigo e message. O corpo continua disponível para quem chamou (fica em buffer). Chamadas da própria Ori são ignoradas.
/// </summary>
public class ErrosApiHandler : DelegatingHandler
{
    private readonly IContextoAssistente _contexto;
    public ErrosApiHandler(IContextoAssistente contexto) => _contexto = contexto;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var resposta = await base.SendAsync(request, ct);
        var caminho = request.RequestUri?.AbsolutePath ?? "";
        if ((int)resposta.StatusCode < 400 || caminho.StartsWith("/api/assistente") || caminho.StartsWith("/api/eventos"))
            return resposta;

        string? codigo = null, mensagem = null;
        try
        {
            await resposta.Content.LoadIntoBufferAsync();
            var corpo = await resposta.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(corpo) && corpo.TrimStart().StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(corpo);
                if (doc.RootElement.TryGetProperty("codigo", out var c) && c.ValueKind == JsonValueKind.String) codigo = c.GetString();
                if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String) mensagem = m.GetString();
                else if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String) mensagem = t.GetString();
            }
        }
        catch (Exception) { /* corpo não-JSON: registra só o status */ }

        _contexto.RegistrarErroApi(new ErroApiDto($"{request.Method} {caminho}", (int)resposta.StatusCode, codigo,
            mensagem is { Length: > 300 } ? mensagem[..300] : mensagem, 0));
        return resposta;
    }
}
