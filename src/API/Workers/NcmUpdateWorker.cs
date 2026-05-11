using NfeSaas.Application.Services;

namespace NfeSaas.API.Workers;

/// <summary>
/// Configuração lida da seção <c>Ncm</c> do appsettings (ou env vars com prefixo Ncm__).
/// </summary>
public class NcmUpdateWorkerOptions
{
    /// <summary>URL absoluta do JSON oficial. Vazio = worker desabilitado.</summary>
    public string? UpdateSourceUrl { get; set; }

    /// <summary>Intervalo entre execuções automáticas. Padrão: 7 dias.</summary>
    public int UpdateIntervalDays { get; set; } = 7;

    /// <summary>
    /// Se true, executa uma vez ao iniciar a API (útil em dev).
    /// Em prod recomenda-se false para não atrasar o startup.
    /// </summary>
    public bool UpdateOnStartup { get; set; } = false;

    /// <summary>
    /// Caminho alternativo: lê o JSON de um arquivo local em vez de baixar.
    /// Útil em ambientes sem acesso à internet ou para testes.
    /// </summary>
    public string? LocalFilePath { get; set; }
}

/// <summary>
/// Worker que atualiza a tabela NCM semanalmente a partir da fonte oficial.
///
/// Estratégia:
///   1. Se <c>UpdateOnStartup=true</c>, executa após pequeno delay (15s).
///   2. Em seguida, dorme pelo intervalo configurado e repete.
///   3. Falhas (rede, parsing) são logadas mas não derrubam o worker — só esperam a próxima janela.
///
/// O worker é seguro para múltiplas instâncias da API:
///   - <c>NcmUpdater</c> usa um <c>SemaphoreSlim</c> in-process
///   - O <c>UpsertManyAsync</c> é idempotente
///   - Curto-circuito por <c>VersaoTabela</c> evita escrita desnecessária
/// </summary>
public class NcmUpdateWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly NcmUpdateWorkerOptions _opts;
    private readonly ILogger<NcmUpdateWorker> _logger;

    public NcmUpdateWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<NcmUpdateWorker> logger)
    {
        _services = services;
        _logger = logger;
        _opts = config.GetSection("Ncm").Get<NcmUpdateWorkerOptions>() ?? new NcmUpdateWorkerOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_opts.UpdateSourceUrl) && string.IsNullOrWhiteSpace(_opts.LocalFilePath))
        {
            _logger.LogInformation(
                "NcmUpdateWorker desabilitado: configure Ncm__UpdateSourceUrl ou Ncm__LocalFilePath para ativar.");
            return;
        }

        // Delay inicial para não competir com o startup da API e do banco.
        var delayInicial = _opts.UpdateOnStartup ? TimeSpan.FromSeconds(15) : TimeSpan.FromDays(_opts.UpdateIntervalDays);
        _logger.LogInformation(
            "NcmUpdateWorker ativo. Primeira execução em {Delay}. Intervalo: {Dias} dias.",
            delayInicial, _opts.UpdateIntervalDays);

        try { await Task.Delay(delayInicial, stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecutarComProtecaoAsync(stoppingToken);

            try { await Task.Delay(TimeSpan.FromDays(_opts.UpdateIntervalDays), stoppingToken); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task ExecutarComProtecaoAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var updater = scope.ServiceProvider.GetRequiredService<INcmUpdater>();
            var result = await updater.AtualizarAsync(
                new NcmUpdateOptions(_opts.UpdateSourceUrl, _opts.LocalFilePath), ct);

            if (!result.Sucesso)
                _logger.LogWarning("Atualização NCM falhou: {Erro}", result.MensagemErro);
        }
        catch (Exception ex)
        {
            // Não propagar — o worker tem que sobreviver para a próxima janela.
            _logger.LogError(ex, "Exceção não tratada no NcmUpdateWorker. Próxima tentativa em {Dias} dias.",
                _opts.UpdateIntervalDays);
        }
    }
}
