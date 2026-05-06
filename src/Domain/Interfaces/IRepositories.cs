using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Interfaces;

public interface INotaFiscalRepository
{
    Task<NotaFiscal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NotaFiscal?> GetByChaveAcessoAsync(string chaveAcesso, CancellationToken ct = default);
    Task<IEnumerable<NotaFiscal>> GetByEmpresaAsync(Guid empresaId, int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> CountByEmpresaAsync(Guid empresaId, CancellationToken ct = default);
    Task<IEnumerable<NotaFiscal>> GetByPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<decimal> GetTotalEmitidoMesAsync(Guid empresaId, int ano, int mes, CancellationToken ct = default);
    Task<Dictionary<SituacaoNota, int>> GetContagemPorSituacaoAsync(Guid empresaId, CancellationToken ct = default);
    Task AddAsync(NotaFiscal nota, CancellationToken ct = default);
    Task UpdateAsync(NotaFiscal nota, CancellationToken ct = default);
}

public interface IEmpresaRepository
{
    Task<Empresa?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Empresa?> GetByCnpjAsync(string cnpj, CancellationToken ct = default);
    Task<IEnumerable<Empresa>> GetByEscritorioAsync(Guid escritorioId, CancellationToken ct = default);
    Task AddAsync(Empresa empresa, CancellationToken ct = default);
    Task UpdateAsync(Empresa empresa, CancellationToken ct = default);
}

public interface IEscritorioRepository
{
    Task<Escritorio?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Escritorio?> GetByCnpjAsync(string cnpj, CancellationToken ct = default);
    Task AddAsync(Escritorio escritorio, CancellationToken ct = default);
    Task UpdateAsync(Escritorio escritorio, CancellationToken ct = default);
}

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<Usuario>> GetByEscritorioAsync(Guid escritorioId, CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
    Task UpdateAsync(Usuario usuario, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IEnumerable<AuditLog>> GetByEmpresaAsync(Guid empresaId, int pagina, int tamanho, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
