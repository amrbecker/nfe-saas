using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Automacoes;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.API.Controllers;

/// <summary>Automações da Ori: só preparam dados; nada é emitido ou salvo por aqui.</summary>
[Authorize]
[Route("api/assistente/automacoes")]
public class AssistenteAutomacoesController : BaseApiController
{
    [HttpGet("preparar-nota/{notaId:guid}")]
    public async Task<ActionResult<EmitirNotaFiscalDto>> PrepararNota(Guid notaId, CancellationToken ct) =>
        await Mediator.Send(new PrepararNotaQuery(EmpresaId, notaId), ct) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("cadastro-sugerido/{notaId:guid}")]
    public async Task<ActionResult<CadastroSugeridoDto>> CadastroSugerido(Guid notaId, CancellationToken ct) =>
        await Mediator.Send(new CadastroSugeridoQuery(EmpresaId, notaId), ct) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("destinatario-existente")]
    public async Task<ActionResult<ClienteExistenteDto>> DestinatarioExistente([FromQuery] string doc, CancellationToken ct) =>
        await Mediator.Send(new DestinatarioExistenteQuery(EmpresaId, doc), ct) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("primeiros-passos")]
    public async Task<ActionResult<PrimeirosPassosDto>> PrimeirosPassos(CancellationToken ct) =>
        await Mediator.Send(new PrimeirosPassosQuery(EmpresaId), ct) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("padroes")]
    public async Task<ActionResult<PadroesPreenchimentoDto>> Padroes([FromQuery] string? destinatario, [FromQuery] string? produto,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPadroesPreenchimentoQuery(EmpresaId, destinatario, produto), ct));
}
