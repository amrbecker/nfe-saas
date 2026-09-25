using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Interfaces.Assistente;

// Repositórios do assistente Ori. Registrados automaticamente por varredura de namespace
// (Infrastructure/DependencyInjection.Assistente.cs): qualquer interface neste namespace com
// implementação em NfeSaas.Infrastructure.Repositories.Assistente vira Scoped.
// Entidades carregadas por estes repositórios são rastreadas pelo DbContext — basta IUnitOfWork.SaveChangesAsync.

public interface IEventoProdutoRepository
{
    Task AddRangeAsync(IEnumerable<EventoProduto> eventos, CancellationToken ct = default);
}

public interface IPreferenciaAssistenteRepository
{
    Task<PreferenciaAssistente?> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task AddAsync(PreferenciaAssistente preferencia, CancellationToken ct = default);
}

public interface ISugestaoDispensadaRepository
{
    Task<SugestaoDispensada?> GetAsync(Guid usuarioId, string chave, CancellationToken ct = default);
    /// <summary>Chaves bloqueadas (dispensadas ou suspensas) do usuário — para a UI filtrar dicas e sugestões.</summary>
    Task<List<string>> GetChavesBloqueadasAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct = default);
    Task AddAsync(SugestaoDispensada sugestao, CancellationToken ct = default);
}

public interface IUsoAssistenteRepository
{
    Task<UsoAssistente?> GetAsync(Guid usuarioId, DateOnly dia, CancellationToken ct = default);
    Task<int> SomarMensagensUsuarioAsync(Guid usuarioId, DateOnly desde, DateOnly ate, CancellationToken ct = default);
    Task<int> SomarMensagensEscritorioAsync(Guid escritorioId, DateOnly desde, DateOnly ate, CancellationToken ct = default);
    Task<decimal> SomarCustoEscritorioAsync(Guid escritorioId, DateOnly desde, DateOnly ate, CancellationToken ct = default);
    Task AddAsync(UsoAssistente uso, CancellationToken ct = default);
}

public interface IInteracaoAssistenteRepository
{
    Task<InteracaoAssistente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<InteracaoAssistente>> ListarDesdeAsync(DateTime desde, int limite, CancellationToken ct = default);
    Task AddAsync(InteracaoAssistente interacao, CancellationToken ct = default);
}

public interface IConversaRepository
{
    Task<Conversa?> GetByIdComMensagensAsync(Guid id, CancellationToken ct = default);
    Task<List<Conversa>> ListarPorUsuarioAsync(Guid usuarioId, Guid empresaId, int limite, CancellationToken ct = default);
    Task AddAsync(Conversa conversa, CancellationToken ct = default);
    /// <summary>Remove fisicamente conversas (e mensagens) sem atividade desde <paramref name="limite"/>. Retorna quantas.</summary>
    Task<int> ExpurgarAnterioresAsync(DateTime limite, CancellationToken ct = default);
}

public interface IChamadoRepository
{
    Task<Chamado?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Chamado>> ListarAsync(StatusChamado? status, int pagina, int tamanho, CancellationToken ct = default);
    Task AddAsync(Chamado chamado, CancellationToken ct = default);
}

public interface ISinalProdutoRepository
{
    Task<List<SinalProduto>> ListarAsync(TipoSinalProduto? tipo, DateTime desde, int limite, CancellationToken ct = default);
    Task AddAsync(SinalProduto sinal, CancellationToken ct = default);
}

public interface IAlertaCsRepository
{
    Task<bool> ExisteChaveAsync(string chave, CancellationToken ct = default);
    Task<AlertaCs?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Alertas não dispensados do escritório (e da empresa, quando informada, mais os de nível escritório).</summary>
    Task<List<AlertaCs>> ListarAtivosAsync(Guid escritorioId, Guid? empresaId, bool incluirInternos, CancellationToken ct = default);
    Task<List<AlertaCs>> ListarInternosAsync(DateTime desde, CancellationToken ct = default);
    Task<List<AlertaCs>> ListarPendentesDeEmailAsync(IEnumerable<TipoAlertaCs> tipos, CancellationToken ct = default);
    Task AddAsync(AlertaCs alerta, CancellationToken ct = default);
}

public interface ILembreteEmissaoRepository
{
    Task<LembreteEmissao?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<LembreteEmissao>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken ct = default);
    Task<List<LembreteEmissao>> ListarDevidosAsync(DateTime agoraUtc, CancellationToken ct = default);
    Task<bool> ExisteParaNotaModeloAsync(Guid empresaId, Guid notaModeloId, CancellationToken ct = default);
    Task AddAsync(LembreteEmissao lembrete, CancellationToken ct = default);
}

public interface IFonteMonitoradaRepository
{
    Task<List<FonteMonitorada>> ListarAsync(bool apenasAtivas, CancellationToken ct = default);
    Task<FonteMonitorada?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(FonteMonitorada fonte, CancellationToken ct = default);
}

public interface IPublicacaoDetectadaRepository
{
    Task<PublicacaoDetectada?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteHashAsync(Guid fonteId, string hash, CancellationToken ct = default);
    Task<List<PublicacaoDetectada>> ListarAsync(StatusPublicacao? status, int pagina, int tamanho, CancellationToken ct = default);
    Task<List<PublicacaoDetectada>> ListarNaoClassificadasAsync(int limite, CancellationToken ct = default);
    Task AddAsync(PublicacaoDetectada publicacao, CancellationToken ct = default);
}
