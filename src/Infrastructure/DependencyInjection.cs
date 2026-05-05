using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Infrastructure.Data;
using NfeSaas.Infrastructure.Repositories;
using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<NfeDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                x => x.MigrationsAssembly("NfeSaas.Infrastructure")));

        // Repositories
        services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<ISefazService, SefazService>();
        services.AddScoped<IXmlNFeService, XmlNFeService>();
        services.AddScoped<IDanfeService, DanfeService>();
        services.AddScoped<ICertificadoService, CertificadoService>();
        services.AddScoped<IImpostoCalculoService, ImpostoCalculoService>();
        services.AddScoped<ITokenService, TokenService>();

        // HttpClient for SEFAZ
        services.AddHttpClient();

        return services;
    }
}
