using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
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
        // Data Protection — usado para cifrar secrets em repouso (senha do certificado, token CSC).
        // Docker Compose local: chaves em volume persistente (ver docker-compose.yml: `dp_keys`).
        // Hosts sem disco persistente (ex.: Render free tier): fallback para a tabela
        // DataProtectionKeys na própria base Postgres (ver NfeDbContext : IDataProtectionKeyContext).
        // Resolução de NfeDbContext pelo PersistKeysToDbContext é preguiçosa (só na 1ª leitura/escrita
        // de chave) — não há dependência circular com o construtor de NfeDbContext que recebe
        // IDataProtectionProvider, pois este já está construído (singleton) quando isso acontece.
        var dpKeysPath = config["DataProtection:KeysPath"];
        var dpBuilder = services.AddDataProtection().SetApplicationName("NfeSaas");
        if (!string.IsNullOrWhiteSpace(dpKeysPath) && Directory.Exists(dpKeysPath))
        {
            dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));
        }
        else
        {
            dpBuilder.PersistKeysToDbContext<NfeDbContext>();
        }

        // Cifra as próprias chaves de Data Protection em repouso (defesa em profundidade: sem
        // isso, quem tiver acesso de leitura à tabela DataProtectionKeys consegue decifrar
        // CertificadoSenha/CscToken de todas as empresas). Opcional — só ativa se configurado.
        var dpCertBase64 = config["DataProtection:CertificateBase64"];
        if (!string.IsNullOrWhiteSpace(dpCertBase64))
        {
            var certBytes = Convert.FromBase64String(dpCertBase64);
            var certPassword = config["DataProtection:CertificatePassword"];
            var cert = new X509Certificate2(certBytes, certPassword, X509KeyStorageFlags.MachineKeySet);
            dpBuilder.ProtectKeysWithCertificate(cert);
        }

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
        services.AddScoped<IEmailService, ResendEmailService>();

        // HttpClient for SEFAZ and ViaCEP
        services.AddHttpClient();

        return services;
    }
}
