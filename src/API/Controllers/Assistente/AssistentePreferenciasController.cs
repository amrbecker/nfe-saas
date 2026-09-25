using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Assistente.Preferencias;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.API.Controllers;

/// <summary>Status da Ori para a WebUI, preferências do usuário e dicas/sugestões ignoradas.</summary>
[Authorize]
[Route("api/assistente")]
public class AssistentePreferenciasController : BaseApiController
{
    [HttpGet("status")]
    public async Task<ActionResult<StatusAssistenteDto>> Status(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetStatusAssistenteQuery(UserId, EscritorioId), ct));

    [HttpGet("preferencias")]
    public async Task<ActionResult<PreferenciasOriDto>> Preferencias(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPreferenciasOriQuery(UserId), ct));

    [HttpPut("preferencias")]
    public async Task<ActionResult<PreferenciasOriDto>> Salvar([FromBody] PreferenciasOriDto dto, CancellationToken ct) =>
        Ok(await Mediator.Send(new SalvarPreferenciasOriCommand(UserId, dto), ct));

    [HttpPost("sugestoes/ignorar")]
    public async Task<IActionResult> Ignorar([FromBody] SugestaoIgnoradaDto dto, CancellationToken ct)
    {
        Guid? empresa = HasEmpresaSelecionada ? EmpresaId : null;
        return await Mediator.Send(new IgnorarSugestaoCommand(UserId, empresa, dto), ct) ? NoContent() : BadRequest();
    }
}
