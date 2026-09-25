using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Curadoria;
using NfeSaas.Domain.Enums;

namespace NfeSaas.API.Controllers;

/// <summary>Fila de curadoria do escritório parceiro. Sem dados de clientes.</summary>
[Authorize(Roles = PapeisAssistente.Curador + "," + PapeisAssistente.Plataforma)]
[Route("api/curadoria")]
public class CuradoriaController : BaseApiController
{
    [HttpGet("publicacoes")]
    public async Task<ActionResult<List<PublicacaoCuradoriaDto>>> Publicacoes([FromQuery] StatusPublicacao? status, [FromQuery] int pagina = 1,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ListarPublicacoesQuery(status, pagina), ct));

    [HttpPut("publicacoes/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarPublicacaoDto dto, CancellationToken ct) =>
        await Mediator.Send(new AtualizarPublicacaoCommand(id, dto), ct) ? NoContent() : NotFound();

    [HttpGet("artigos-vencidos")]
    public async Task<ActionResult<List<ArtigoCuradoriaDto>>> Artigos(CancellationToken ct) =>
        Ok(await Mediator.Send(new ArtigosPendentesQuery(), ct));

    [HttpGet("lacunas")]
    public async Task<ActionResult<List<LacunaAgrupadaDto>>> Lacunas([FromQuery] int dias = 30, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new LacunasQuery(dias), ct));

    [HttpGet("fontes")]
    public async Task<ActionResult<List<FonteCuradoriaDto>>> Fontes(CancellationToken ct) =>
        Ok(await Mediator.Send(new FontesQuery(), ct));
}
