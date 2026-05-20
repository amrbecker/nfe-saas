using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace NfeSaas.Tests.BDD.Support;

public class TestWebApplication : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("nfesaas_bdd")
        .WithUsername("bdduser")
        .WithPassword("bddpass")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Program.cs valida Jwt:Secret e ConnectionStrings:DefaultConnection antes do host buildar.
        // Injetamos placeholders válidos aqui — o DbContextOptions é substituído logo abaixo
        // pelo connection string real do Testcontainer.
        builder.UseSetting("Jwt:Secret", "test-jwt-secret-com-no-minimo-32-caracteres-para-validacao");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<NfeDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<NfeDbContext>(opts =>
                opts.UseNpgsql(ConnectionString));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();

        // Trigger factory build and apply migrations
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NfeDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public NfeDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<NfeDbContext>();
    }
}
