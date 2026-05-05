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
