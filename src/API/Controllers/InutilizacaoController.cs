using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.EventosFiscaisCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.API.Controllers;

[Authorize]
[Route("api/inutilizacoes")]
public class InutilizacaoController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IEmpresaRepository empresaRepo)
    {
        var empresa = await empresaRepo.GetByIdAsync(EmpresaId);
        if (empresa == null) return NotFound();
        var inutilizacoes = await Mediator.Send(new GetInutilizacoesQuery(EmpresaId, empresa.AmbienteSefaz));
        return Ok(inutilizacoes);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] InutilizarDto dto)
    {
        var result = await Mediator.Send(new InutilizarNumeracaoCommand(EmpresaId, UserId, dto));
        if (result.Evento == null) return BadRequest(new { message = result.Erro });
        if (result.Erro != null) return BadRequest(new { message = result.Erro, evento = result.Evento });
        return Ok(result.Evento);
    }
}
