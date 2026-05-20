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
        if (result.Falha != null)
        {
            // 401 para credencial inválida; 402 (Payment Required) para trial expirado;
            // 403 para escritório suspenso. Permite UI tratar cada caso diferentemente.
            var status = result.Falha.Codigo switch
            {
                "TrialExpirado" => 402,
                "EscritorioSuspenso" => 403,
                _ => 401
            };
            return StatusCode(status, new { message = result.Falha.Motivo, codigo = result.Falha.Codigo, assinatura = result.Falha.Assinatura });
        }
        return Ok(result.Sucesso);
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
