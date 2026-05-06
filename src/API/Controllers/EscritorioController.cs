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

    [Authorize(Roles = "Admin")]
    [HttpPut("usuarios/{id:guid}")]
    public async Task<IActionResult> AtualizarUsuario(Guid id, [FromBody] UpdateUsuarioDto dto)
    {
        var result = await Mediator.Send(new UpdateUsuarioCommand(EscritorioId, id, dto));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("usuarios/{id:guid}/toggle-ativo")]
    public async Task<IActionResult> ToggleAtivoUsuario(Guid id)
    {
        var result = await Mediator.Send(new ToggleAtivoUsuarioCommand(EscritorioId, id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("usuarios/{id:guid}")]
    public async Task<IActionResult> ExcluirUsuario(Guid id)
    {
        var ok = await Mediator.Send(new DeleteUsuarioCommand(EscritorioId, id));
        if (!ok) return NotFound();
        return NoContent();
    }
}
