using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Servicos;
using NfeSaas.Infrastructure.Assistente;

namespace NfeSaas.Infrastructure;

// Registro do assistente Ori. Repositórios: varredura — toda interface em
// NfeSaas.Domain.Interfaces.Assistente com implementação em NfeSaas.Infrastructure.Repositories.Assistente
// vira Scoped (novos repositórios não precisam editar este arquivo).
public static class AssistenteDependencyInjection
{
    public static IServiceCollection AddAssistenteInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AssistenteOptions>(config.GetSection("Assistente"));

        var infra = typeof(AssistenteDependencyInjection).Assembly;
        var interfaces = typeof(NfeSaas.Domain.Interfaces.Assistente.IAlertaCsRepository).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == "NfeSaas.Domain.Interfaces.Assistente");
        foreach (var iface in interfaces)
        {
            var impl = infra.GetTypes().FirstOrDefault(t => t is { IsClass: true, IsAbstract: false }
                && t.Namespace == "NfeSaas.Infrastructure.Repositories.Assistente" && iface.IsAssignableFrom(t));
            if (impl != null) services.AddScoped(iface, impl);
        }

        services.AddSingleton<IBaseConhecimento, BaseConhecimento>();
        services.AddSingleton<IAssistenteIA, AssistenteIA>();
        services.AddSingleton<ISanitizadorIA, SanitizadorIA>();
        services.AddScoped<ICotaAssistente, CotaAssistente>();
        services.AddSingleton<NfeSaas.Application.Assistente.Ia.IPromptSistema, PromptSistemaEmbutido>();
        services.AddSingleton<NfeSaas.Application.Assistente.Ia.MontadorPrompt>();
        services.AddScoped<NfeSaas.Application.Assistente.Ia.MontadorContexto>();
        services.AddScoped<NfeSaas.Application.Assistente.Ia.FerramentasOri>();
        services.AddScoped<NfeSaas.Application.Assistente.Ia.ServicoRespostaOri>();
        services.AddScoped<NfeSaas.Application.Assistente.Monitoramento.ClassificadorPublicacoes>();
        services.AddScoped<NfeSaas.Application.Assistente.Insights.RelatoriosSemanais>();

        return services;
    }
}
