using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.Infrastructure.Data;
using NfeSaas.Infrastructure.Data.Interceptors;
using Testcontainers.PostgreSql;

namespace NfeSaas.Tests.Integration.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("nfesaas_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                // Program.cs valida Jwt:Secret e ConnectionStrings:DefaultConnection antes do host buildar.
                // Injetamos placeholders válidos aqui — o DbContextOptions é substituído logo abaixo
                // pelo connection string real do Testcontainer.
                builder.UseSetting("Jwt:Secret", "test-jwt-secret-com-no-minimo-32-caracteres-para-validacao");
                builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<NfeDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Re-adiciona o FiscalImmutabilityInterceptor — sem isso, testes de
                    // integração não exercitam a proteção de imutabilidade fiscal de verdade,
                    // igual está registrada em DependencyInjection.cs para produção.
                    services.AddDbContext<NfeDbContext>(opts =>
                        opts.UseNpgsql(_postgres.GetConnectionString())
                            .AddInterceptors(new FiscalImmutabilityInterceptor()));
                });
            });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NfeDbContext>();
        await db.Database.MigrateAsync();

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        // Tolerar falha de InitializeAsync — Client/Factory podem estar nulos.
        Client?.Dispose();
        if (Factory != null) await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public NfeDbContext CreateDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<NfeDbContext>();
    }
}
