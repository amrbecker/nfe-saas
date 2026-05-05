using Microsoft.EntityFrameworkCore.Storage;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Infrastructure.Data;

namespace NfeSaas.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly NfeDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(NfeDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default) =>
        _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
