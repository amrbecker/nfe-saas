using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NfeSaas.Infrastructure.Data;

/// <summary>
/// Factory usada apenas pelo tooling do EF (`dotnet ef migrations add` / `script`). Constrói o
/// DbContext sem Data Protection (não é necessário para gerar SQL de schema) e sem ler
/// connection string real — a string aqui só precisa ser válida sintaticamente para o provider
/// gerar SQL idempotente.
/// </summary>
public class NfeDbContextDesignFactory : IDesignTimeDbContextFactory<NfeDbContext>
{
    public NfeDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<NfeDbContext>();
        builder.UseNpgsql(
            "Host=localhost;Database=nfesaas_design;Username=design;Password=design",
            x => x.MigrationsAssembly("NfeSaas.Infrastructure"));
        return new NfeDbContext(builder.Options);
    }
}
