using Microsoft.EntityFrameworkCore;
using NfeSaas.Domain.Entities;

namespace NfeSaas.Infrastructure.Data;

public class NfeDbContext : DbContext
{
    public NfeDbContext(DbContextOptions<NfeDbContext> options) : base(options) { }

    public DbSet<Escritorio> Escritorios => Set<Escritorio>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<ItemNotaFiscal> ItensNotaFiscal => Set<ItemNotaFiscal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NfeDbContext).Assembly);

        modelBuilder.Entity<Escritorio>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Empresa>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<NotaFiscal>().HasQueryFilter(n => !n.IsDeleted);
        modelBuilder.Entity<ItemNotaFiscal>().HasQueryFilter(i => !i.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified))
        {
            if (entry.Entity is NfeSaas.Domain.Common.BaseEntity entity)
                entity.SetUpdated();
        }
        return await base.SaveChangesAsync(ct);
    }
}
