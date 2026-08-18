using Microsoft.EntityFrameworkCore;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Infrastructure.Data;

namespace NfeSaas.Infrastructure.Repositories;

public class NotaFiscalRepository : INotaFiscalRepository
{
    private readonly NfeDbContext _ctx;
    public NotaFiscalRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<NotaFiscal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _ctx.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<NotaFiscal?> GetByChaveAcessoAsync(string chave, CancellationToken ct = default) =>
        await _ctx.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.ChaveAcesso == chave, ct);

    public async Task<NotaFiscal?> GetBySerieNumeroAsync(Guid empresaId, TipoNota tipo, int serie, int numero, AmbienteSefaz ambiente, CancellationToken ct = default) =>
        await _ctx.NotasFiscais.FirstOrDefaultAsync(n =>
            n.EmpresaId == empresaId && n.Tipo == tipo && n.Serie == serie && n.Numero == numero && n.Ambiente == ambiente, ct);

    public async Task<IEnumerable<NotaFiscal>> GetElegiveisDescarteAsync(Guid empresaId, CancellationToken ct = default)
    {
        var limite = DateTime.UtcNow.AddYears(-5);
        return await _ctx.NotasFiscais
            .Where(n => n.EmpresaId == empresaId
                && ((n.Situacao == SituacaoNota.Autorizada && n.DataAutorizacao != null && n.DataAutorizacao < limite)
                 || (n.Situacao == SituacaoNota.Cancelada && n.DataCancelamento != null && n.DataCancelamento < limite)))
            .OrderBy(n => n.DataEmissao)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<NotaFiscal>> GetByEmpresaAsync(Guid empresaId, int pagina, int tamanho, CancellationToken ct = default) =>
        await _ctx.NotasFiscais
            .Where(n => n.EmpresaId == empresaId)
            .OrderByDescending(n => n.DataEmissao)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);

    public async Task<int> CountByEmpresaAsync(Guid empresaId, CancellationToken ct = default) =>
        await _ctx.NotasFiscais.CountAsync(n => n.EmpresaId == empresaId, ct);

    public async Task<IEnumerable<NotaFiscal>> GetByPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim, CancellationToken ct = default) =>
        await _ctx.NotasFiscais
            .Where(n => n.EmpresaId == empresaId && n.DataEmissao >= inicio && n.DataEmissao <= fim)
            .ToListAsync(ct);

    public async Task<decimal> GetTotalEmitidoMesAsync(Guid empresaId, int ano, int mes, CancellationToken ct = default) =>
        await _ctx.NotasFiscais
            .Where(n => n.EmpresaId == empresaId && n.DataEmissao.Year == ano && n.DataEmissao.Month == mes
                && n.Situacao == SituacaoNota.Autorizada)
            .SumAsync(n => n.TotalNota, ct);

    public async Task<Dictionary<SituacaoNota, int>> GetContagemPorSituacaoAsync(Guid empresaId, CancellationToken ct = default)
    {
        var result = await _ctx.NotasFiscais
            .Where(n => n.EmpresaId == empresaId)
            .GroupBy(n => n.Situacao)
            .Select(g => new { Situacao = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return result.ToDictionary(r => r.Situacao, r => r.Count);
    }

    public async Task AddAsync(NotaFiscal nota, CancellationToken ct = default) =>
        await _ctx.NotasFiscais.AddAsync(nota, ct);

    public Task UpdateAsync(NotaFiscal nota, CancellationToken ct = default)
    {
        _ctx.NotasFiscais.Update(nota);
        return Task.CompletedTask;
    }
}

public class EscritorioRepository : IEscritorioRepository
{
    private readonly NfeDbContext _ctx;
    public EscritorioRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<Escritorio?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _ctx.Escritorios.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Escritorio?> GetByCnpjAsync(string cnpj, CancellationToken ct = default) =>
        await _ctx.Escritorios.FirstOrDefaultAsync(e => e.Cnpj == cnpj, ct);

    public async Task AddAsync(Escritorio escritorio, CancellationToken ct = default) =>
        await _ctx.Escritorios.AddAsync(escritorio, ct);

    public Task UpdateAsync(Escritorio escritorio, CancellationToken ct = default)
    {
        _ctx.Escritorios.Update(escritorio);
        return Task.CompletedTask;
    }
}

public class EmpresaRepository : IEmpresaRepository
{
    private readonly NfeDbContext _ctx;
    public EmpresaRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<Empresa?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _ctx.Empresas.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Empresa?> GetByCnpjAsync(string cnpj, CancellationToken ct = default) =>
        await _ctx.Empresas.FirstOrDefaultAsync(e => e.Cnpj == cnpj, ct);

    public async Task<IEnumerable<Empresa>> GetByEscritorioAsync(Guid escritorioId, CancellationToken ct = default) =>
        await _ctx.Empresas.Where(e => e.EscritorioId == escritorioId).OrderBy(e => e.RazaoSocial).ToListAsync(ct);

    public async Task AddAsync(Empresa empresa, CancellationToken ct = default) =>
        await _ctx.Empresas.AddAsync(empresa, ct);

    public Task UpdateAsync(Empresa empresa, CancellationToken ct = default)
    {
        _ctx.Empresas.Update(empresa);
        return Task.CompletedTask;
    }
}

public class UsuarioRepository : IUsuarioRepository
{
    private readonly NfeDbContext _ctx;
    public UsuarioRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _ctx.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _ctx.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<Usuario?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
        await _ctx.Usuarios.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);

    public async Task<IEnumerable<Usuario>> GetByEscritorioAsync(Guid escritorioId, CancellationToken ct = default) =>
        await _ctx.Usuarios.Where(u => u.EscritorioId == escritorioId).ToListAsync(ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default) =>
        await _ctx.Usuarios.AddAsync(usuario, ct);

    public Task UpdateAsync(Usuario usuario, CancellationToken ct = default)
    {
        _ctx.Usuarios.Update(usuario);
        return Task.CompletedTask;
    }
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly NfeDbContext _ctx;
    public AuditLogRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
        await _ctx.AuditLogs.AddAsync(log, ct);

    public async Task<IEnumerable<AuditLog>> GetByEmpresaAsync(Guid empresaId, int pagina, int tamanho, CancellationToken ct = default) =>
        await _ctx.AuditLogs
            .Where(l => l.EmpresaId == empresaId)
            .OrderByDescending(l => l.Timestamp)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);
}

public class ProdutoRepository : IProdutoRepository
{
    private readonly NfeDbContext _ctx;
    public ProdutoRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<Produto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _ctx.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Produto?> GetByCodigoAsync(Guid empresaId, string codigo, CancellationToken ct = default) =>
        await _ctx.Produtos.FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Codigo == codigo, ct);

    public async Task<IEnumerable<Produto>> GetByEmpresaAsync(Guid empresaId, bool apenasAtivos, CancellationToken ct = default) =>
        await _ctx.Produtos
            .Where(p => p.EmpresaId == empresaId && (!apenasAtivos || p.Ativo))
            .OrderBy(p => p.Descricao)
            .ToListAsync(ct);

    public async Task AddAsync(Produto produto, CancellationToken ct = default) =>
        await _ctx.Produtos.AddAsync(produto, ct);

    public Task UpdateAsync(Produto produto, CancellationToken ct = default)
    {
        _ctx.Produtos.Update(produto);
        return Task.CompletedTask;
    }
}

public class ClienteRepository : IClienteRepository
{
    private readonly NfeDbContext _ctx;
    public ClienteRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _ctx.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Cliente?> GetByCpfCnpjAsync(Guid empresaId, string cpfCnpj, CancellationToken ct = default) =>
        await _ctx.Clientes.FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.CpfCnpj == cpfCnpj, ct);

    public async Task<IEnumerable<Cliente>> GetByEmpresaAsync(Guid empresaId, bool apenasAtivos, CancellationToken ct = default) =>
        await _ctx.Clientes
            .Where(c => c.EmpresaId == empresaId && (!apenasAtivos || c.Ativo))
            .OrderBy(c => c.RazaoSocial)
            .ToListAsync(ct);

    public async Task AddAsync(Cliente cliente, CancellationToken ct = default) =>
        await _ctx.Clientes.AddAsync(cliente, ct);

    public Task UpdateAsync(Cliente cliente, CancellationToken ct = default)
    {
        _ctx.Clientes.Update(cliente);
        return Task.CompletedTask;
    }
}

public class EventoFiscalRepository : IEventoFiscalRepository
{
    private readonly NfeDbContext _ctx;
    public EventoFiscalRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<EventoFiscal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _ctx.EventosFiscais.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IEnumerable<EventoFiscal>> GetByChaveAcessoAsync(Guid empresaId, string chaveAcesso, CancellationToken ct = default) =>
        await _ctx.EventosFiscais
            .Where(e => e.EmpresaId == empresaId && e.ChaveAcesso == chaveAcesso)
            .OrderBy(e => e.DataEvento)
            .ToListAsync(ct);

    public async Task<int> CountCcePorChaveAsync(Guid empresaId, string chaveAcesso, CancellationToken ct = default) =>
        await _ctx.EventosFiscais.CountAsync(e =>
            e.EmpresaId == empresaId && e.ChaveAcesso == chaveAcesso &&
            e.Tipo == TipoEventoFiscal.CartaCorrecao &&
            e.Situacao == SituacaoEventoFiscal.Aceito, ct);

    public async Task<IEnumerable<EventoFiscal>> GetInutilizacoesAsync(Guid empresaId, AmbienteSefaz ambiente, CancellationToken ct = default) =>
        await _ctx.EventosFiscais
            .Where(e => e.EmpresaId == empresaId && e.Ambiente == ambiente && e.Tipo == TipoEventoFiscal.Inutilizacao)
            .OrderByDescending(e => e.DataEvento)
            .ToListAsync(ct);

    public async Task<EventoFiscal?> GetInutilizacaoConflitoAsync(Guid empresaId, AmbienteSefaz ambiente, int ano, TipoNota tipo, int serie, int numIni, int numFin, CancellationToken ct = default) =>
        await _ctx.EventosFiscais.FirstOrDefaultAsync(e =>
            e.EmpresaId == empresaId && e.Ambiente == ambiente &&
            e.Tipo == TipoEventoFiscal.Inutilizacao &&
            e.AnoInutilizacao == ano &&
            e.TipoNotaInutilizacao == tipo &&
            e.SerieInutilizacao == serie &&
            e.Situacao == SituacaoEventoFiscal.Aceito &&
            // overlap: existente.ini <= pedido.fin AND existente.fin >= pedido.ini
            e.NumeroInicialInutilizacao <= numFin &&
            e.NumeroFinalInutilizacao >= numIni, ct);

    public async Task AddAsync(EventoFiscal evento, CancellationToken ct = default) =>
        await _ctx.EventosFiscais.AddAsync(evento, ct);

    public Task UpdateAsync(EventoFiscal evento, CancellationToken ct = default)
    {
        _ctx.EventosFiscais.Update(evento);
        return Task.CompletedTask;
    }
}

public class NcmRepository : INcmRepository
{
    private readonly NfeDbContext _ctx;
    public NcmRepository(NfeDbContext ctx) => _ctx = ctx;

    public Task<Ncm?> GetByCodigoAsync(string codigo, CancellationToken ct = default)
    {
        var d = new string(codigo.Where(char.IsDigit).ToArray());
        return _ctx.Ncms.FirstOrDefaultAsync(n => n.Codigo == d && n.Ativo, ct);
    }

    public async Task<IEnumerable<Ncm>> BuscarAsync(string termo, int limite, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(termo)) return Array.Empty<Ncm>();

        var t = termo.Trim();
        var digitos = new string(t.Where(char.IsDigit).ToArray());

        // Busca por código: prefixo no Codigo (rápido pelo índice).
        if (digitos.Length >= 2 && digitos.Length == t.Length)
        {
            return await _ctx.Ncms
                .Where(n => n.Ativo && n.Codigo.StartsWith(digitos))
                .OrderBy(n => n.Codigo)
                .Take(limite)
                .ToListAsync(ct);
        }

        // Busca por descrição: ILIKE no Postgres (case-insensitive).
        var pattern = $"%{t}%";
        return await _ctx.Ncms
            .Where(n => n.Ativo && EF.Functions.ILike(n.Descricao, pattern))
            .OrderBy(n => n.Codigo)
            .Take(limite)
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct = default) =>
        _ctx.Ncms.Where(n => n.Ativo).CountAsync(ct);

    public async Task<string?> GetVersaoTabelaAtualAsync(CancellationToken ct = default) =>
        await _ctx.Ncms
            .OrderByDescending(n => n.AtualizadoEm)
            .Select(n => n.VersaoTabela)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Ncm ncm, CancellationToken ct = default) =>
        await _ctx.Ncms.AddAsync(ncm, ct);

    public async Task UpsertManyAsync(IEnumerable<Ncm> ncms, string versaoTabela, CancellationToken ct = default)
    {
        var lista = ncms.ToList();
        if (lista.Count == 0) return;

        var codigos = lista.Select(n => n.Codigo).ToHashSet();
        var existentes = await _ctx.Ncms
            .Where(n => codigos.Contains(n.Codigo))
            .ToDictionaryAsync(n => n.Codigo, ct);

        foreach (var ncm in lista)
        {
            if (existentes.TryGetValue(ncm.Codigo, out var ex))
                ex.Atualizar(ncm.Descricao, versaoTabela, ncm.AliquotaIpiPadrao, ncm.ExigeCest);
            else
                await _ctx.Ncms.AddAsync(ncm, ct);
        }
    }
}

public class ConfiguracaoEmpresaRepository : IConfiguracaoEmpresaRepository
{
    private readonly NfeDbContext _ctx;
    public ConfiguracaoEmpresaRepository(NfeDbContext ctx) => _ctx = ctx;

    public async Task<ConfiguracaoEmpresa?> GetByEmpresaAsync(Guid empresaId, CancellationToken ct = default) =>
        await _ctx.ConfiguracoesEmpresa.FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct);

    public async Task AddAsync(ConfiguracaoEmpresa configuracao, CancellationToken ct = default) =>
        await _ctx.ConfiguracoesEmpresa.AddAsync(configuracao, ct);

    public Task UpdateAsync(ConfiguracaoEmpresa configuracao, CancellationToken ct = default)
    {
        _ctx.ConfiguracoesEmpresa.Update(configuracao);
        return Task.CompletedTask;
    }
}
