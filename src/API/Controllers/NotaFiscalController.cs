using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Commands.CancelarNFe;
using NfeSaas.Application.Commands.EmitirNFe;
using NfeSaas.Application.Commands.EnviarNFePorEmail;
using NfeSaas.Application.Commands.EventosFiscaisCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Application.Queries;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.API.Controllers;

[Authorize]
[Route("api/notas-fiscais")]
public class NotaFiscalController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetNotas([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
    {
        var result = await Mediator.Send(new GetNotasQuery(EmpresaId, pagina, tamanhoPagina));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetNota(Guid id)
    {
        var result = await Mediator.Send(new GetNotaDetalheQuery(id, EmpresaId));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("emitir")]
    public async Task<IActionResult> Emitir([FromBody] EmitirNotaFiscalDto dto)
    {
        var result = await Mediator.Send(new EmitirNFeCommand(EmpresaId, UserId, dto));
        if (!result.Sucesso)
            return BadRequest(new { message = result.MensagemErro });

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarRequest req)
    {
        var result = await Mediator.Send(new CancelarNFeCommand(id, EmpresaId, req.Justificativa));
        if (!result.Sucesso)
            return BadRequest(new { message = result.MensagemErro });
        return Ok(new { message = "Nota cancelada com sucesso." });
    }

    [HttpPost("{id:guid}/enviar-email")]
    public async Task<IActionResult> EnviarEmail(Guid id, [FromBody] EnviarEmailRequest req)
    {
        var result = await Mediator.Send(new EnviarNFePorEmailCommand(id, EmpresaId, UserId, req.EmailDestino));
        if (!result.Sucesso)
            return BadRequest(new { message = result.MensagemErro });
        return Ok(new { message = result.MensagemErro ?? "E-mail enviado com sucesso." });
    }

    [HttpGet("{id:guid}/danfe")]
    public async Task<IActionResult> DownloadDanfe(Guid id,
        [FromServices] INotaFiscalRepository notaRepo,
        [FromServices] IEmpresaRepository empresaRepo,
        [FromServices] IDanfeService danfeService)
    {
        var nota = await notaRepo.GetByIdAsync(id);
        if (nota == null || nota.EmpresaId != EmpresaId) return NotFound();

        var empresa = await empresaRepo.GetByIdAsync(EmpresaId);
        if (empresa == null) return NotFound();

        var pdf = nota.Tipo == NfeSaas.Domain.Enums.TipoNota.NFCe
            ? await danfeService.GerarDanfeNFCePdfAsync(nota, empresa)
            : await danfeService.GerarDanfePdfAsync(nota, empresa);

        return File(pdf, "application/pdf", $"DANFE_{nota.Numero:D9}.pdf");
    }

    [HttpGet("{id:guid}/xml")]
    public async Task<IActionResult> DownloadXml(Guid id,
        [FromServices] INotaFiscalRepository notaRepo)
    {
        var nota = await notaRepo.GetByIdAsync(id);
        if (nota == null || nota.EmpresaId != EmpresaId) return NotFound();
        if (string.IsNullOrEmpty(nota.XmlEnvio)) return NotFound();

        var bytes = System.Text.Encoding.UTF8.GetBytes(nota.XmlRetorno ?? nota.XmlEnvio);
        return File(bytes, "application/xml", $"NFe_{nota.ChaveAcesso ?? nota.Numero.ToString()}.xml");
    }

    [HttpPost("{id:guid}/cce")]
    public async Task<IActionResult> EmitirCce(Guid id, [FromBody] EmitirCceDto dto)
    {
        var result = await Mediator.Send(new EmitirCartaCorrecaoCommand(EmpresaId, UserId, id, dto.Correcao));
        if (result.Evento == null) return BadRequest(new { message = result.Erro });
        if (result.Erro != null) return BadRequest(new { message = result.Erro, evento = result.Evento });
        return Ok(result.Evento);
    }

    [HttpPost("{id:guid}/manifestar")]
    public async Task<IActionResult> Manifestar(Guid id, [FromBody] ManifestarDto dto,
        [FromServices] INotaFiscalRepository notaRepo)
    {
        var nota = await notaRepo.GetByIdAsync(id);
        if (nota == null || nota.EmpresaId != EmpresaId) return NotFound();
        if (string.IsNullOrEmpty(nota.ChaveAcesso))
            return BadRequest(new { message = "Nota não possui chave de acesso." });

        var result = await Mediator.Send(new ManifestarDestinatarioCommand(EmpresaId, UserId, nota.ChaveAcesso, dto));
        if (result.Evento == null) return BadRequest(new { message = result.Erro });
        if (result.Erro != null) return BadRequest(new { message = result.Erro, evento = result.Evento });
        return Ok(result.Evento);
    }

    [HttpGet("{id:guid}/eventos")]
    public async Task<IActionResult> GetEventos(Guid id, [FromServices] INotaFiscalRepository notaRepo)
    {
        var nota = await notaRepo.GetByIdAsync(id);
        if (nota == null || nota.EmpresaId != EmpresaId) return NotFound();
        if (string.IsNullOrEmpty(nota.ChaveAcesso)) return Ok(new List<EventoFiscalResumoDto>());

        var eventos = await Mediator.Send(new GetEventosPorChaveQuery(EmpresaId, nota.ChaveAcesso));
        return Ok(eventos);
    }

    [HttpGet("elegiveis-descarte")]
    public async Task<IActionResult> GetElegiveisDescarte([FromServices] INotaFiscalRepository notaRepo)
    {
        var notas = await notaRepo.GetElegiveisDescarteAsync(EmpresaId);
        var resultado = notas.Select(n => new
        {
            n.Id,
            n.Tipo,
            n.Serie,
            n.Numero,
            n.ChaveAcesso,
            n.Situacao,
            n.DataEmissao,
            n.DataAutorizacao,
            n.DataCancelamento,
            DataDescarteAutorizado = n.DataDescarteAutorizado
        });
        return Ok(resultado);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? ano, [FromQuery] int? mes)
    {
        // Default = ano/mês atual em UTC (container roda em UTC; UI deve passar ano/mes explícitos se quiser fuso local).
        var agora = DateTime.UtcNow;
        var result = await Mediator.Send(new GetDashboardQuery(EmpresaId, ano ?? agora.Year, mes ?? agora.Month));
        return Ok(result);
    }
}

public record CancelarRequest(string Justificativa);
public record EnviarEmailRequest(string? EmailDestino);
