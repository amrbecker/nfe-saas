using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.ProdutoCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Queries.ProdutoQueries;

namespace NfeSaas.API.Controllers;

[Authorize]
[Route("api/produtos")]
public class ProdutoController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool apenasAtivos = false)
    {
        var produtos = await Mediator.Send(new GetProdutosQuery(EmpresaId, apenasAtivos));
        return Ok(produtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var produto = await Mediator.Send(new GetProdutoQuery(EmpresaId, id));
        if (produto == null) return NotFound();
        return Ok(produto);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CreateProdutoDto dto)
    {
        var result = await Mediator.Send(new CreateProdutoCommand(EmpresaId, dto));
        if (result.Produto == null) return BadRequest(new { message = result.Erro });
        return Ok(result.Produto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] UpdateProdutoDto dto)
    {
        var result = await Mediator.Send(new UpdateProdutoCommand(EmpresaId, id, dto));
        if (result.Produto == null) return BadRequest(new { message = result.Erro });
        return Ok(result.Produto);
    }

    [HttpPatch("{id:guid}/toggle-ativo")]
    public async Task<IActionResult> ToggleAtivo(Guid id)
    {
        var result = await Mediator.Send(new ToggleAtivoProdutoCommand(EmpresaId, id));
        if (result.Produto == null) return NotFound();
        return Ok(result.Produto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var ok = await Mediator.Send(new DeleteProdutoCommand(EmpresaId, id));
        if (!ok) return NotFound();
        return NoContent();
    }
}
