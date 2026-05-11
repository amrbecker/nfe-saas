using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.ClienteCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Queries.ClienteQueries;

namespace NfeSaas.API.Controllers;

[Authorize]
[Route("api/clientes")]
public class ClienteController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool apenasAtivos = false)
    {
        var clientes = await Mediator.Send(new GetClientesQuery(EmpresaId, apenasAtivos));
        return Ok(clientes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var cliente = await Mediator.Send(new GetClienteQuery(EmpresaId, id));
        if (cliente == null) return NotFound();
        return Ok(cliente);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CreateClienteDto dto)
    {
        var result = await Mediator.Send(new CreateClienteCommand(EmpresaId, dto));
        if (result.Cliente == null) return BadRequest(new { message = result.Erro });
        return Ok(result.Cliente);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] UpdateClienteDto dto)
    {
        var result = await Mediator.Send(new UpdateClienteCommand(EmpresaId, id, dto));
        if (result.Cliente == null) return BadRequest(new { message = result.Erro });
        return Ok(result.Cliente);
    }

    [HttpPatch("{id:guid}/toggle-ativo")]
    public async Task<IActionResult> ToggleAtivo(Guid id)
    {
        var result = await Mediator.Send(new ToggleAtivoClienteCommand(EmpresaId, id));
        if (result.Cliente == null) return NotFound();
        return Ok(result.Cliente);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var ok = await Mediator.Send(new DeleteClienteCommand(EmpresaId, id));
        if (!ok) return NotFound();
        return NoContent();
    }
}
