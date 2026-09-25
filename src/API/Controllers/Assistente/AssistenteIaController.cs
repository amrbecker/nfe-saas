using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using NfeSaas.Application.Assistente.Ia;
using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.API.Controllers;

/// <summary>Respostas da Ori (explicar rejeição, pergunta livre, conversa com SSE, avaliação).</summary>
[Authorize]
[Route("api/assistente")]
public class AssistenteIaController : BaseApiController
{
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

    private EscopoOri Escopo => new(UserId, EscritorioId, EmpresaId, UserRole);

    [HttpPost("explicar-rejeicao/{notaId:guid}")]
    public async Task<ActionResult<RespostaOriDto>> ExplicarRejeicao(Guid notaId, [FromBody] ExplicarRejeicaoDto? dto, CancellationToken ct)
    {
        if (!HasEmpresaSelecionada) return BadRequest(new { message = "Selecione uma empresa." });
        var r = await Mediator.Send(new ExplicarRejeicaoCommand(Escopo, notaId, dto?.Contexto), ct);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpPost("perguntar")]
    public async Task<ActionResult<RespostaOriDto>> Perguntar([FromBody] PerguntaOriDto dto, CancellationToken ct)
    {
        if (!HasEmpresaSelecionada) return BadRequest(new { message = "Selecione uma empresa." });
        var r = await Mediator.Send(new PerguntarOriCommand(Escopo, dto.Pergunta, dto.Contexto), ct);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpGet("conversas")]
    public async Task<ActionResult<List<ConversaResumoDto>>> Conversas(CancellationToken ct)
    {
        if (!HasEmpresaSelecionada) return BadRequest(new { message = "Selecione uma empresa." });
        return Ok(await Mediator.Send(new ListarConversasQuery(Escopo), ct));
    }

    [HttpPost("conversas")]
    public async Task<ActionResult<ConversaResumoDto>> CriarConversa([FromBody] CriarConversaDto? dto, CancellationToken ct)
    {
        if (!HasEmpresaSelecionada) return BadRequest(new { message = "Selecione uma empresa." });
        var r = await Mediator.Send(new CriarConversaCommand(Escopo, dto?.Titulo), ct);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpGet("conversas/{id:guid}/mensagens")]
    public async Task<ActionResult<List<MensagemConversaDto>>> Mensagens(Guid id, CancellationToken ct)
    {
        if (!HasEmpresaSelecionada) return BadRequest(new { message = "Selecione uma empresa." });
        var r = await Mediator.Send(new ListarMensagensConversaQuery(Escopo, id), ct);
        return r == null ? NotFound() : Ok(r);
    }

    /// <summary>
    /// SSE: eventos "status"/"ferramenta" enquanto a Ori trabalha e "resposta" (ou "erro") no fim. Sem streaming de
    /// tokens de propósito: toda resposta passa pelo verificador de fontes antes de aparecer.
    /// </summary>
    [HttpPost("conversas/{id:guid}/mensagens")]
    public async Task EnviarMensagem(Guid id, [FromBody] NovaMensagemDto dto, CancellationToken ct)
    {
        if (!HasEmpresaSelecionada)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var canal = Channel.CreateUnbounded<EventoConversaDto>(new UnboundedChannelOptions { SingleReader = true });
        var progresso = new ProgressoCanal(canal.Writer);
        var escopo = Escopo;

        var tarefa = Task.Run(async () =>
        {
            try
            {
                var resposta = await Mediator.Send(new EnviarMensagemConversaCommand(escopo, id, dto, progresso), ct);
                canal.Writer.TryWrite(resposta == null
                    ? new EventoConversaDto("erro", "Conversa não encontrada ou Ori desabilitada.", null)
                    : new EventoConversaDto("resposta", null, resposta));
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                canal.Writer.TryWrite(new EventoConversaDto("erro", "Não consegui responder agora. Tente de novo em instantes.", null));
                throw;
            }
            finally
            {
                canal.Writer.TryComplete();
            }
        }, ct);

        await foreach (var evento in canal.Reader.ReadAllAsync(ct))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(evento, JsonWeb)}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        try { await tarefa; } catch { /* já sinalizado ao cliente e logado pelo pipeline */ }
    }

    [HttpPost("interacoes/{id:guid}/avaliacao")]
    public async Task<IActionResult> Avaliar(Guid id, [FromBody] AvaliacaoDto dto, CancellationToken ct)
    {
        if (!HasEmpresaSelecionada) return BadRequest(new { message = "Selecione uma empresa." });
        return await Mediator.Send(new AvaliarInteracaoCommand(Escopo, id, dto.Valor), ct) ? NoContent() : NotFound();
    }

    private sealed class ProgressoCanal : IProgress<EventoConversaDto>
    {
        private readonly ChannelWriter<EventoConversaDto> _writer;
        public ProgressoCanal(ChannelWriter<EventoConversaDto> writer) => _writer = writer;
        public void Report(EventoConversaDto value) => _writer.TryWrite(value);
    }
}

public record CriarConversaDto(string? Titulo);
