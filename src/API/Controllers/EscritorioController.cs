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
    // Auto-cadastro público — qualquer pessoa pode criar um escritório
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] CreateEscritorioDto dto)
    {
        var result = await Mediator.Send(new CreateEscritorioCommand(dto));
        if (result == null) return Conflict(new { message = "CNPJ já cadastrado." });
        return Ok(result);
    }

    // === EMPRESAS ===

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
        if (result == null) return BadRequest(new { message = "Dados inválidos ou escritório não encontrado." });
        return Ok(result);
    }

    // === USUÁRIOS ===

    [Authorize(Roles = "Admin")]
    [HttpGet("usuarios")]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await Mediator.Send(new GetUsuariosQuery(EscritorioId));
        return Ok(usuarios);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("usuarios")]
    public async Task<IActionResult> CriarUsuario([FromBody] CreateUsuarioDto dto)
    {
        var result = await Mediator.Send(new CreateUsuarioCommand(EscritorioId, dto));
        if (result == null) return Conflict(new { message = "E-mail já cadastrado." });
        return Ok(result);
    }
}
