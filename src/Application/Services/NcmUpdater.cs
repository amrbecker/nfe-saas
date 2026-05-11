using System.Net.Http;
using Microsoft.Extensions.Logging;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Services;

public record NcmUpdateOptions(string? SourceUrl, string? LocalFilePath = null, string? VersaoOverride = null);

public record NcmUpdateResult(
    bool Sucesso,
    string? VersaoAnterior,
    string? VersaoNova,
    int TotalProcessados,
    int TotalInseridosOuAtualizados,
    TimeSpan Duracao,
    string? MensagemErro = null)
{
    public static NcmUpdateResult Falha(string mensagem) =>
        new(false, null, null, 0, 0, TimeSpan.Zero, mensagem);
}

public interface INcmUpdater
{
    /// <summary>
    /// Baixa a tabela NCM oficial, parseia e faz upsert. Idempotente — pode ser
    /// chamado várias vezes; só altera o banco se houver mudanças.
    /// </summary>
    Task<NcmUpdateResult> AtualizarAsync(NcmUpdateOptions opts, CancellationToken ct = default);
}

public class NcmUpdater : INcmUpdater
{
    private readonly INcmRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<NcmUpdater> _logger;

    // Garante que duas chamadas concorrentes não pisem uma na outra (web trigger + worker).
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public NcmUpdater(
        INcmRepository repo,
        IUnitOfWork uow,
        IHttpClientFactory httpFactory,
        ILogger<NcmUpdater> logger)
    {
        _repo = repo;
        _uow = uow;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<NcmUpdateResult> AtualizarAsync(NcmUpdateOptions opts, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(opts.SourceUrl) && string.IsNullOrWhiteSpace(opts.LocalFilePath))
            return NcmUpdateResult.Falha("Nenhuma URL ou arquivo local configurado para atualização NCM.");

        if (!await _gate.WaitAsync(TimeSpan.FromSeconds(2), ct))
            return NcmUpdateResult.Falha("Outra atualização NCM já está em andamento.");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var versaoAnterior = await _repo.GetVersaoTabelaAtualAsync(ct);

            // 1) Baixar o JSON (HTTP ou disco)
            string json;
            try
            {
                json = await ObterJsonAsync(opts, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao baixar tabela NCM de {Source}", opts.SourceUrl ?? opts.LocalFilePath);
                return NcmUpdateResult.Falha($"Falha ao obter JSON: {ex.Message}");
            }

            // 2) Parsear
            NcmParseResult parsed;
            try
            {
                parsed = PortalUnicoNcmParser.Parse(json, opts.VersaoOverride);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao parsear JSON NCM");
                return NcmUpdateResult.Falha($"Falha ao parsear JSON: {ex.Message}");
            }

            // 3) Curto-circuito se já está na mesma versão (evita escrita desnecessária).
            if (!string.IsNullOrEmpty(versaoAnterior) && versaoAnterior == parsed.Versao)
            {
                _logger.LogInformation(
                    "Tabela NCM já está na versão {Versao} ({Total} NCMs). Nada a fazer.",
                    parsed.Versao, parsed.Ncms.Count);
                return new NcmUpdateResult(
                    Sucesso: true,
                    VersaoAnterior: versaoAnterior,
                    VersaoNova: parsed.Versao,
                    TotalProcessados: parsed.TotalProcessados,
                    TotalInseridosOuAtualizados: 0,
                    Duracao: stopwatch.Elapsed);
            }

            // 4) Upsert
            await _repo.UpsertManyAsync(parsed.Ncms, parsed.Versao, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Atualização NCM concluída. Versão {Anterior} → {Nova}. {Total} NCMs vigentes processados em {Ms}ms.",
                versaoAnterior ?? "(vazio)", parsed.Versao, parsed.Ncms.Count, stopwatch.ElapsedMilliseconds);

            return new NcmUpdateResult(
                Sucesso: true,
                VersaoAnterior: versaoAnterior,
                VersaoNova: parsed.Versao,
                TotalProcessados: parsed.TotalProcessados,
                TotalInseridosOuAtualizados: parsed.Ncms.Count,
                Duracao: stopwatch.Elapsed);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> ObterJsonAsync(NcmUpdateOptions opts, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(opts.LocalFilePath))
        {
            _logger.LogInformation("Lendo tabela NCM do arquivo local {Path}", opts.LocalFilePath);
            return await File.ReadAllTextAsync(opts.LocalFilePath, ct);
        }

        _logger.LogInformation("Baixando tabela NCM de {Url}", opts.SourceUrl);
        var client = _httpFactory.CreateClient("ncm-updater");
        client.Timeout = TimeSpan.FromMinutes(2);
        using var resp = await client.GetAsync(opts.SourceUrl, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }
}
