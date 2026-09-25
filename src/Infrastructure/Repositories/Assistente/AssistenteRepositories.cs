using Microsoft.EntityFrameworkCore;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces.Assistente;
using NfeSaas.Infrastructure.Data;

namespace NfeSaas.Infrastructure.Repositories.Assistente;

public class EventoProdutoRepository : IEventoProdutoRepository
{
    private readonly NfeDbContext _ctx;
    public EventoProdutoRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task AddRangeAsync(IEnumerable<EventoProduto> eventos, CancellationToken ct = default) =>
        await _ctx.EventosProduto.AddRangeAsync(eventos, ct);
}

public class PreferenciaAssistenteRepository : IPreferenciaAssistenteRepository
{
    private readonly NfeDbContext _ctx;
    public PreferenciaAssistenteRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<PreferenciaAssistente?> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        _ctx.PreferenciasAssistente.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

    public async Task AddAsync(PreferenciaAssistente preferencia, CancellationToken ct = default) =>
        await _ctx.PreferenciasAssistente.AddAsync(preferencia, ct);
}

public class SugestaoDispensadaRepository : ISugestaoDispensadaRepository
{
    private readonly NfeDbContext _ctx;
    public SugestaoDispensadaRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<SugestaoDispensada?> GetAsync(Guid usuarioId, string chave, CancellationToken ct = default) =>
        _ctx.SugestoesDispensadas.FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.Chave == chave, ct);

    public Task<List<string>> GetChavesBloqueadasAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken ct = default) =>
        _ctx.SugestoesDispensadas
            .Where(s => s.UsuarioId == usuarioId && (s.DispensadaDefinitivamente || (s.SuspensaAte != null && s.SuspensaAte > agoraUtc)))
            .Select(s => s.Chave)
            .ToListAsync(ct);

    public async Task AddAsync(SugestaoDispensada sugestao, CancellationToken ct = default) =>
        await _ctx.SugestoesDispensadas.AddAsync(sugestao, ct);
}

public class UsoAssistenteRepository : IUsoAssistenteRepository
{
    private readonly NfeDbContext _ctx;
    public UsoAssistenteRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<UsoAssistente?> GetAsync(Guid usuarioId, DateOnly dia, CancellationToken ct = default) =>
        _ctx.UsosAssistente.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId && u.Dia == dia, ct);

    public Task<int> SomarMensagensUsuarioAsync(Guid usuarioId, DateOnly desde, DateOnly ate, CancellationToken ct = default) =>
        _ctx.UsosAssistente.Where(u => u.UsuarioId == usuarioId && u.Dia >= desde && u.Dia <= ate).SumAsync(u => u.Mensagens, ct);

    public Task<int> SomarMensagensEscritorioAsync(Guid escritorioId, DateOnly desde, DateOnly ate, CancellationToken ct = default) =>
        _ctx.UsosAssistente.Where(u => u.EscritorioId == escritorioId && u.Dia >= desde && u.Dia <= ate).SumAsync(u => u.Mensagens, ct);

    public Task<decimal> SomarCustoEscritorioAsync(Guid escritorioId, DateOnly desde, DateOnly ate, CancellationToken ct = default) =>
        _ctx.UsosAssistente.Where(u => u.EscritorioId == escritorioId && u.Dia >= desde && u.Dia <= ate).SumAsync(u => u.CustoEstimadoUsd, ct);

    public async Task AddAsync(UsoAssistente uso, CancellationToken ct = default) =>
        await _ctx.UsosAssistente.AddAsync(uso, ct);
}

public class InteracaoAssistenteRepository : IInteracaoAssistenteRepository
{
    private readonly NfeDbContext _ctx;
    public InteracaoAssistenteRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<InteracaoAssistente?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.InteracoesAssistente.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<List<InteracaoAssistente>> ListarDesdeAsync(DateTime desde, int limite, CancellationToken ct = default) =>
        _ctx.InteracoesAssistente.AsNoTracking().Where(i => i.CreatedAt >= desde)
            .OrderByDescending(i => i.CreatedAt).Take(limite).ToListAsync(ct);

    public async Task AddAsync(InteracaoAssistente interacao, CancellationToken ct = default) =>
        await _ctx.InteracoesAssistente.AddAsync(interacao, ct);
}

public class ConversaRepository : IConversaRepository
{
    private readonly NfeDbContext _ctx;
    public ConversaRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<Conversa?> GetByIdComMensagensAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Conversas.Include(c => c.Mensagens).FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Conversa>> ListarPorUsuarioAsync(Guid usuarioId, Guid empresaId, int limite, CancellationToken ct = default) =>
        _ctx.Conversas.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.EmpresaId == empresaId)
            .OrderByDescending(c => c.UltimaMensagemEm).Take(limite).ToListAsync(ct);

    public async Task AddAsync(Conversa conversa, CancellationToken ct = default) =>
        await _ctx.Conversas.AddAsync(conversa, ct);

    public async Task<int> ExpurgarAnterioresAsync(DateTime limite, CancellationToken ct = default)
    {
        var ids = await _ctx.Conversas.IgnoreQueryFilters().Where(c => c.UltimaMensagemEm < limite).Select(c => c.Id).ToListAsync(ct);
        if (ids.Count == 0) return 0;
        await _ctx.MensagensConversa.Where(m => ids.Contains(m.ConversaId)).ExecuteDeleteAsync(ct);
        return await _ctx.Conversas.IgnoreQueryFilters().Where(c => ids.Contains(c.Id)).ExecuteDeleteAsync(ct);
    }
}

public class ChamadoRepository : IChamadoRepository
{
    private readonly NfeDbContext _ctx;
    public ChamadoRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<Chamado?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Chamados.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Chamado>> ListarAsync(StatusChamado? status, int pagina, int tamanho, CancellationToken ct = default) =>
        _ctx.Chamados.AsNoTracking()
            .Where(c => status == null || c.Status == status)
            .OrderByDescending(c => c.Severidade).ThenByDescending(c => c.CreatedAt)
            .Skip(Math.Max(0, pagina - 1) * tamanho).Take(tamanho).ToListAsync(ct);

    public async Task AddAsync(Chamado chamado, CancellationToken ct = default) =>
        await _ctx.Chamados.AddAsync(chamado, ct);
}

public class SinalProdutoRepository : ISinalProdutoRepository
{
    private readonly NfeDbContext _ctx;
    public SinalProdutoRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<List<SinalProduto>> ListarAsync(TipoSinalProduto? tipo, DateTime desde, int limite, CancellationToken ct = default) =>
        _ctx.SinaisProduto.AsNoTracking()
            .Where(s => (tipo == null || s.Tipo == tipo) && s.CreatedAt >= desde)
            .OrderByDescending(s => s.CreatedAt).Take(limite).ToListAsync(ct);

    public async Task AddAsync(SinalProduto sinal, CancellationToken ct = default) =>
        await _ctx.SinaisProduto.AddAsync(sinal, ct);
}

public class AlertaCsRepository : IAlertaCsRepository
{
    private readonly NfeDbContext _ctx;
    public AlertaCsRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<bool> ExisteChaveAsync(string chave, CancellationToken ct = default) =>
        _ctx.AlertasCs.IgnoreQueryFilters().AnyAsync(a => a.Chave == chave, ct);

    public Task<AlertaCs?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.AlertasCs.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<AlertaCs>> ListarAtivosAsync(Guid escritorioId, Guid? empresaId, bool incluirInternos, CancellationToken ct = default) =>
        _ctx.AlertasCs.AsNoTracking()
            .Where(a => a.EscritorioId == escritorioId && a.DispensadoEm == null
                && (incluirInternos || !a.Interno)
                && (a.EmpresaId == null || empresaId == null || a.EmpresaId == empresaId))
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public Task<List<AlertaCs>> ListarInternosAsync(DateTime desde, CancellationToken ct = default) =>
        _ctx.AlertasCs.AsNoTracking().Where(a => a.Interno && a.CreatedAt >= desde)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public Task<List<AlertaCs>> ListarPendentesDeEmailAsync(IEnumerable<TipoAlertaCs> tipos, CancellationToken ct = default)
    {
        var lista = tipos.ToList();
        return _ctx.AlertasCs.Where(a => a.EmailEnviadoEm == null && a.DispensadoEm == null && !a.Interno && lista.Contains(a.Tipo))
            .OrderBy(a => a.CreatedAt).Take(200).ToListAsync(ct);
    }

    public async Task AddAsync(AlertaCs alerta, CancellationToken ct = default) =>
        await _ctx.AlertasCs.AddAsync(alerta, ct);
}

public class LembreteEmissaoRepository : ILembreteEmissaoRepository
{
    private readonly NfeDbContext _ctx;
    public LembreteEmissaoRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<LembreteEmissao?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.LembretesEmissao.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<List<LembreteEmissao>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken ct = default) =>
        _ctx.LembretesEmissao.AsNoTracking().Where(l => l.EmpresaId == empresaId && !l.IsDeleted)
            .OrderBy(l => l.ProximaEm).ToListAsync(ct);

    public Task<List<LembreteEmissao>> ListarDevidosAsync(DateTime agoraUtc, CancellationToken ct = default) =>
        _ctx.LembretesEmissao.Where(l => l.Ativo && !l.IsDeleted && l.ProximaEm <= agoraUtc).Take(500).ToListAsync(ct);

    public Task<bool> ExisteParaNotaModeloAsync(Guid empresaId, Guid notaModeloId, CancellationToken ct = default) =>
        _ctx.LembretesEmissao.AnyAsync(l => l.EmpresaId == empresaId && l.NotaModeloId == notaModeloId && !l.IsDeleted && l.Ativo, ct);

    public async Task AddAsync(LembreteEmissao lembrete, CancellationToken ct = default) =>
        await _ctx.LembretesEmissao.AddAsync(lembrete, ct);
}

public class FonteMonitoradaRepository : IFonteMonitoradaRepository
{
    private readonly NfeDbContext _ctx;
    public FonteMonitoradaRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<List<FonteMonitorada>> ListarAsync(bool apenasAtivas, CancellationToken ct = default) =>
        _ctx.FontesMonitoradas.Where(f => !apenasAtivas || f.Ativa).OrderBy(f => f.Nivel).ThenBy(f => f.Nome).ToListAsync(ct);

    public Task<FonteMonitorada?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.FontesMonitoradas.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<int> CountAsync(CancellationToken ct = default) => _ctx.FontesMonitoradas.CountAsync(ct);

    public async Task AddAsync(FonteMonitorada fonte, CancellationToken ct = default) =>
        await _ctx.FontesMonitoradas.AddAsync(fonte, ct);
}

public class PublicacaoDetectadaRepository : IPublicacaoDetectadaRepository
{
    private readonly NfeDbContext _ctx;
    public PublicacaoDetectadaRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<PublicacaoDetectada?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.PublicacoesDetectadas.Include(p => p.Fonte).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExisteHashAsync(Guid fonteId, string hash, CancellationToken ct = default) =>
        _ctx.PublicacoesDetectadas.AnyAsync(p => p.FonteId == fonteId && p.Hash == hash, ct);

    public Task<List<PublicacaoDetectada>> ListarAsync(StatusPublicacao? status, int pagina, int tamanho, CancellationToken ct = default) =>
        _ctx.PublicacoesDetectadas.AsNoTracking().Include(p => p.Fonte)
            .Where(p => status == null || p.Status == status)
            .OrderByDescending(p => p.DetectadaEm)
            .Skip(Math.Max(0, pagina - 1) * tamanho).Take(tamanho).ToListAsync(ct);

    public Task<List<PublicacaoDetectada>> ListarNaoClassificadasAsync(int limite, CancellationToken ct = default) =>
        _ctx.PublicacoesDetectadas.Include(p => p.Fonte).Where(p => p.Relevante == null)
            .OrderBy(p => p.DetectadaEm).Take(limite).ToListAsync(ct);

    public async Task AddAsync(PublicacaoDetectada publicacao, CancellationToken ct = default) =>
        await _ctx.PublicacoesDetectadas.AddAsync(publicacao, ct);
}
