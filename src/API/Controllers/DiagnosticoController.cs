using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NfeSaas.API.Controllers;

// Endpoint temporário para validar a integração do Sentry em produção (deploy-producao.md,
// passo 6.3). Remover depois de confirmar o evento no dashboard do Sentry.
[ApiController]
[Route("api/diagnostico")]
[AllowAnonymous]
public class DiagnosticoController : ControllerBase
{
    [HttpGet("sentry-test")]
    public IActionResult SentryTest()
    {
        throw new InvalidOperationException("Teste deliberado de integração Sentry — pode ignorar.");
    }
}
