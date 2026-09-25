using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Cs;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.API.Controllers;

/// <summary>Telemetria de produto (sem valores de campos nem dados pessoais).</summary>
[Authorize]
[Route("api/eventos")]
public class EventosController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] LoteEventosDto lote, CancellationToken ct)
    {
        if (lote.Eventos is null || lote.Eventos.Count == 0) return Accepted();
        Guid? empresa = HasEmpresaSelecionada ? EmpresaId : null;
        await Mediator.Send(new RegistrarEventosCommand(EscritorioId, empresa, UserId, lote), ct);
        return Accepted();
    }
}

/// <summary>Alertas de Customer Success do escritório/empresa do token.</summary>
[Authorize]
[Route("api/assistente/alertas")]
public class AlertasCsController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<AlertaCsDto>>> Listar(CancellationToken ct) =>
        Ok(await Mediator.Send(new ListarAlertasQuery(EscritorioId, HasEmpresaSelecionada ? EmpresaId : null), ct));

    [HttpPost("{id:guid}/visto")]
    public async Task<IActionResult> Visto(Guid id, CancellationToken ct) =>
        await Mediator.Send(new MarcarAlertaCommand(EscritorioId, id, Dispensar: false), ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/dispensar")]
    public async Task<IActionResult> Dispensar(Guid id, CancellationToken ct) =>
        await Mediator.Send(new MarcarAlertaCommand(EscritorioId, id, Dispensar: true), ct) ? NoContent() : NotFound();
}

/// <summary>Lembretes de nota recorrente (A7): no dia, a Ori avisa e o formulário abre preparado.</summary>
[Authorize]
[Route("api/assistente/lembretes")]
public class LembretesController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<LembreteDto>>> Listar(CancellationToken ct) =>
        Ok(await Mediator.Send(new ListarLembretesQuery(EmpresaId), ct));

    [HttpPost]
    public async Task<ActionResult<LembreteDto>> Criar([FromBody] CriarLembreteDto dto, CancellationToken ct)
    {
        var (l, erro) = await Mediator.Send(new CriarLembreteCommand(EmpresaId, UserId, dto), ct);
        return l == null ? BadRequest(new { message = erro }) : Ok(l);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LembreteDto>> Atualizar(Guid id, [FromBody] CriarLembreteDto dto, [FromQuery] bool? ativo, CancellationToken ct)
    {
        var (l, erro) = await Mediator.Send(new AtualizarLembreteCommand(EmpresaId, id, dto, ativo), ct);
        if (l != null) return Ok(l);
        return erro == null ? NotFound() : BadRequest(new { message = erro });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct) =>
        await Mediator.Send(new ExcluirLembreteCommand(EmpresaId, id), ct) ? NoContent() : NotFound();
}

/// <summary>Saúde das contas — equipe NFeFlow.</summary>
[Authorize(Roles = PapeisAssistente.Plataforma)]
[Route("api/interno/saude")]
public class InternoSaudeController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<SaudeEscritorioDto>>> Listar(CancellationToken ct) =>
        Ok(await Mediator.Send(new SaudeContasQuery(), ct));
}
