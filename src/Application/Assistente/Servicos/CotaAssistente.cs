using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Servicos;

/// <summary>
/// Cotas de mensagens ao modelo (PESQUISA_REFINAMENTO.md §2.5): limite diário por usuário e mensal por plano do
/// escritório. Existem contra abuso e para previsibilidade — respostas da base (sem modelo) não consomem cota.
/// </summary>
public class CotaAssistente : ICotaAssistente
{
    private readonly IUsoAssistenteRepository _usos;
    private readonly IEscritorioRepository _escritorios;
    private readonly IUnitOfWork _uow;
    private readonly IOptions<AssistenteOptions> _opcoes;
    private readonly ILogger<CotaAssistente> _logger;

    public CotaAssistente(IUsoAssistenteRepository usos, IEscritorioRepository escritorios, IUnitOfWork uow,
        IOptions<AssistenteOptions> opcoes, ILogger<CotaAssistente> logger)
    {
        _usos = usos;
        _escritorios = escritorios;
        _uow = uow;
        _opcoes = opcoes;
        _logger = logger;
    }

    public async Task<StatusCotaDto> ObterAsync(Guid usuarioId, Guid escritorioId, CancellationToken ct = default)
    {
        var o = _opcoes.Value;
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);

        var usoHoje = await _usos.GetAsync(usuarioId, hoje, ct);
        var usadasHoje = usoHoje?.Mensagens ?? 0;
        var usadasMes = await _usos.SomarMensagensUsuarioAsync(usuarioId, inicioMes, hoje, ct);

        var escritorio = await _escritorios.GetByIdAsync(escritorioId, ct);
        var limiteMes = (escritorio?.Plano ?? PlanoSaas.Basico) switch
        {
            PlanoSaas.Enterprise => o.LimiteMensalEnterprise,
            PlanoSaas.Profissional => o.LimiteMensalProfissional,
            _ => o.LimiteMensalBasico
        };

        string? motivo = null;
        if (usadasHoje >= o.LimiteDiarioPorUsuario) motivo = "Limite diário de perguntas atingido. Volto amanhã — os artigos continuam disponíveis.";
        else if (usadasMes >= limiteMes) motivo = "Limite mensal de perguntas do seu plano atingido. Os artigos continuam disponíveis.";

        return new StatusCotaDto(motivo == null, usadasHoje, o.LimiteDiarioPorUsuario, usadasMes, limiteMes, motivo);
    }

    public async Task RegistrarAsync(Guid usuarioId, Guid escritorioId, UsoIa uso, decimal custoUsd, CancellationToken ct = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var tentativa = 0; tentativa < 2; tentativa++)
        {
            try
            {
                var registro = await _usos.GetAsync(usuarioId, hoje, ct);
                if (registro == null)
                {
                    registro = UsoAssistente.Criar(usuarioId, escritorioId, hoje);
                    await _usos.AddAsync(registro, ct);
                }
                registro.Registrar(uso.TokensEntrada, uso.TokensCache, uso.TokensSaida, custoUsd);
                await _uow.SaveChangesAsync(ct);
                return;
            }
            catch (Exception ex) when (tentativa == 0 && ex is not OperationCanceledException)
            {
                // Corrida na criação do registro do dia (índice único usuário+dia): tenta de novo, agora atualizando.
                _logger.LogWarning(ex, "Conflito ao registrar uso da Ori; tentando novamente.");
            }
        }
    }
}
