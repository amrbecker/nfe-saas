using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Infrastructure.Data;
using NfeSaas.Infrastructure.Data.Interceptors;
using NfeSaas.Infrastructure.Repositories;
using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Database + interceptor de imutabilidade fiscal
        services.AddSingleton<FiscalImmutabilityInterceptor>();
        services.AddDbContext<NfeDbContext>((sp, opts) =>
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                x => x.MigrationsAssembly("NfeSaas.Infrastructure"))
                .AddInterceptors(sp.GetRequiredService<FiscalImmutabilityInterceptor>()));

        // Repositories
        services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();
        services.AddScoped<IEscritorioRepository, EscritorioRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IConfiguracaoEmpresaRepository, ConfiguracaoEmpresaRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IEventoFiscalRepository, EventoFiscalRepository>();
        services.AddScoped<INcmRepository, NcmRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<ISefazService, SefazService>();
        services.AddSingleton<IXsdValidationService, XsdValidationService>();
        services.AddScoped<IXmlNFeService, XmlNFeService>();
        services.AddScoped<IDanfeService, DanfeService>();
        services.AddScoped<ICertificadoService, CertificadoService>();
        services.AddScoped<IImpostoCalculoService, ImpostoCalculoService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICepValidationService, CepValidationService>();
        services.AddScoped<IAuditService, AuditService>();

        // HttpClient for SEFAZ and ViaCEP
        services.AddHttpClient();

        return services;
    }
}
