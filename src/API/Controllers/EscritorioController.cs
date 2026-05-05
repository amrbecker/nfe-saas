using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.EscritorioCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Queries;

namespace NfeSaas.API.Controllers;

[Route("api/escritorio")]
[ApiController]
public class EscritorioController : BaseApiController
{
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] CreateEscritorioDto dto)
    {
        var result = await Mediator.Send(new CreateEscritorioCommand(dto));
        if (result == null) return Conflict(new { message = "CNPJ já cadastrado." });
        return Ok(result);
    }

    [Authorize]
    [HttpGet("empresas")]
    public async Task<IActionResult> GetEmpresas()
    {
        var empresas = await Mediator.Send(new GetEmpresasQuery(EscritorioId));
        return Ok(empresas);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("empresas")]
    public async Task<IActionResult> CriarEmpresa([FromBody] CreateEmpresaDto dto)
    {
        var result = await Mediator.Send(new CreateEmpresaCommand(EscritorioId, dto));
        if (result == null) return BadRequest(new { message = "Escritório não encontrado ou dados inválidos." });
        return Ok(result);
    }
}
