using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.Auth;
using NfeSaas.Application.DTOs;

namespace NfeSaas.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : BaseApiController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await Mediator.Send(new LoginCommand(dto.Email, dto.Senha));
        if (result == null) return Unauthorized(new { message = "Email ou senha inválidos." });
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await Mediator.Send(new RefreshTokenCommand(dto.RefreshToken));
        if (result == null) return Unauthorized();
        return Ok(result);
    }

    [Authorize]
    [HttpPost("selecionar-empresa")]
    public async Task<IActionResult> SelecionarEmpresa([FromBody] SelecionarEmpresaDto dto)
    {
        var token = await Mediator.Send(new SelecionarEmpresaCommand(UserId, dto.EmpresaId));
        if (token == null) return BadRequest(new { message = "Empresa inválida ou não pertence ao seu escritório." });
        return Ok(new { accessToken = token });
    }
}
