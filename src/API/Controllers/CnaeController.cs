using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Queries.CnaeQueries;

namespace NfeSaas.API.Controllers;

/// <summary>
/// Endpoints para consulta da tabela oficial CNAE (IBGE/CONCLA).
/// Tabela global (não filtrada por empresa) — serve a todos os tenants.
/// </summary>
[Authorize]
[Route("api/cnae")]
public class CnaeController : BaseApiController
{
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termo, [FromQuery] int limite = 10)
    {
        if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
            return Ok(Array.Empty<CnaeDto>());

        var result = await Mediator.Send(new BuscarCnaeQuery(termo, limite));
        return Ok(result);
    }

    [HttpGet("{codigo}")]
    public async Task<IActionResult> Validar(string codigo)
    {
        var result = await Mediator.Send(new ValidarCnaeQuery(codigo));
        return result.Existe ? Ok(result.Cnae) : NotFound(new { message = result.MensagemErro });
    }
}
