using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NfeSaas.Domain.Entities;
using NfeSaas.Infrastructure.Data.Converters;

namespace NfeSaas.Infrastructure.Data;

// IDataProtectionKeyContext: usado como fallback de armazenamento de chaves de cifragem em
// hosts sem disco persistente (ver DependencyInjection.AddInfrastructure) — cria a tabela
// DataProtectionKeys via migration, mas só é efetivamente usada quando DataProtection:KeysPath
// não está configurado/disponível.
public class NfeDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly IDataProtector? _secretsProtector;

    public NfeDbContext(DbContextOptions<NfeDbContext> options) : base(options) { }

    public NfeDbContext(DbContextOptions<NfeDbContext> options, IDataProtectionProvider dataProtection)
        : base(options)
    {
        _secretsProtector = dataProtection.CreateProtector("NfeSaas.Empresa.Secrets.v1");
    }

    public DbSet<Escritorio> Escritorios => Set<Escritorio>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<ItemNotaFiscal> ItensNotaFiscal => Set<ItemNotaFiscal>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ConfiguracaoEmpresa> ConfiguracoesEmpresa => Set<ConfiguracaoEmpresa>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<EventoFiscal> EventosFiscais => Set<EventoFiscal>();
    public DbSet<Ncm> Ncms => Set<Ncm>();
    public DbSet<Cnae> Cnaes => Set<Cnae>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NfeDbContext).Assembly);

        modelBuilder.Entity<Escritorio>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Empresa>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<NotaFiscal>().HasQueryFilter(n => !n.IsDeleted);
        modelBuilder.Entity<ItemNotaFiscal>().HasQueryFilter(i => !i.IsDeleted);
        modelBuilder.Entity<ConfiguracaoEmpresa>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Produto>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Cliente>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<EventoFiscal>().HasQueryFilter(e => !e.IsDeleted);

        // Cifragem em repouso de secrets da Empresa. Quando o DbContext é instanciado pelo design-time
        // do EF (dotnet ef migrations), o protector é null e a conversão é pulada — migrations não
        // dependem do valor real.
        if (_secretsProtector != null)
        {
            var converter = new EncryptedStringConverter(_secretsProtector);
            modelBuilder.Entity<Empresa>()
                .Property(e => e.CertificadoSenha)
                .HasConversion(converter)
                .HasMaxLength(1000); // base64 da cifragem é maior que o texto claro
            modelBuilder.Entity<Empresa>()
                .Property(e => e.CscToken)
                .HasConversion(converter)
                .HasMaxLength(1000);
        }
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
