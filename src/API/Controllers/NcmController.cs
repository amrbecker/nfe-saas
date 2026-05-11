using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NfeSaas.API.Workers;
using NfeSaas.Application.Queries.NcmQueries;
using NfeSaas.Application.Services;

namespace NfeSaas.API.Controllers;

/// <summary>
/// Endpoints para consulta da tabela oficial NCM (Nomenclatura Comum do Mercosul).
/// Tabela global (não filtrada por empresa) — serve a todos os tenants.
/// </summary>
[Authorize]
[Route("api/ncm")]
public class NcmController : BaseApiController
{
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termo, [FromQuery] int limite = 10)
    {
        if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
            return Ok(Array.Empty<NcmDto>());

        var result = await Mediator.Send(new BuscarNcmQuery(termo, limite));
        return Ok(result);
    }

    [HttpGet("{codigo}")]
    public async Task<IActionResult> Validar(string codigo)
    {
        var result = await Mediator.Send(new ValidarNcmQuery(codigo));
        return result.Existe ? Ok(result.Ncm) : NotFound(new { message = result.MensagemErro });
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> Status()
    {
        var status = await Mediator.Send(new GetNcmStatusQuery());
        return Ok(status);
    }

    /// <summary>
    /// Dispara manualmente a atualização da tabela NCM a partir da fonte configurada
    /// (Portal Único Siscomex ou arquivo local). Disponível apenas para administradores.
    ///
    /// Aceita body opcional com <c>{ "sourceUrl": "...", "localFilePath": "...", "versaoOverride": "..." }</c>
    /// para sobrescrever a configuração do appsettings.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("atualizar")]
    public async Task<IActionResult> Atualizar(
        [FromBody] AtualizarNcmRequest? request,
        [FromServices] INcmUpdater updater,
        [FromServices] IOptions<NcmUpdateWorkerOptions> opts,
        CancellationToken ct)
    {
        var url = request?.SourceUrl ?? opts.Value.UpdateSourceUrl;
        var path = request?.LocalFilePath ?? opts.Value.LocalFilePath;
        var versao = request?.VersaoOverride;

        var result = await updater.AtualizarAsync(new NcmUpdateOptions(url, path, versao), ct);
        return result.Sucesso ? Ok(result) : BadRequest(new { message = result.MensagemErro, result });
    }
}

public record AtualizarNcmRequest(string? SourceUrl, string? LocalFilePath, string? VersaoOverride);
