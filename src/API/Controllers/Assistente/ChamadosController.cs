using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Chamados;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Enums;

namespace NfeSaas.API.Controllers;

/// <summary>Chamados e sinais confirmados pelo usuário a partir da Ori.</summary>
[Authorize]
[Route("api/assistente")]
public class ChamadosController : BaseApiController
{
    [HttpPost("chamados")]
    public async Task<IActionResult> Criar([FromBody] CriarChamadoDto dto, CancellationToken ct)
    {
        if (!HasEmpresaSelecionada) return BadRequest(new { message = "Selecione uma empresa." });
        var id = await Mediator.Send(new CriarChamadoCommand(EscritorioId, EmpresaId, UserId, dto), ct);
        return Ok(new { id });
    }

    [HttpPost("sinais")]
    public async Task<IActionResult> Sinal([FromBody] CriarSinalDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Resumo)) return BadRequest(new { message = "Descreva o que aconteceu." });
        Guid? empresa = HasEmpresaSelecionada ? EmpresaId : null;
        var id = await Mediator.Send(new RegistrarSinalCommand(EscritorioId, empresa, UserId, dto.Tipo, dto.Resumo, dto.Tela, null), ct);
        return Ok(new { id });
    }
}

/// <summary>Triagem interna de chamados (equipe NFeFlow).</summary>
[Authorize(Roles = PapeisAssistente.Plataforma)]
[Route("api/interno/chamados")]
public class ChamadosInternoController : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<List<ChamadoInternoDto>>> Listar([FromQuery] StatusChamado? status, [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 30, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ListarChamadosQuery(status, pagina, tamanho), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarChamadoDto dto, CancellationToken ct) =>
        await Mediator.Send(new AtualizarChamadoCommand(id, dto), ct) ? NoContent() : NotFound();
}
