using Microsoft.Extensions.DependencyInjection;

namespace NfeSaas.WebUI.Services.Assistente;

public static class RegistroAssistenteUi
{
    public static IServiceCollection AddAssistenteUi(this IServiceCollection services)
    {
        services.AddScoped<IContextoAssistente, ContextoAssistente>();
        services.AddScoped<IOriControle, OriControle>();
        services.AddScoped<IPreferenciasOriApi, PreferenciasOriApi>();
        services.AddScoped<IOriApi, OriApi>();
        services.AddScoped<IAutomacoesApi, AutomacoesApi>();
        services.AddScoped<ICsApi, CsApi>();
        services.AddScoped<ICuradoriaApi, CuradoriaApi>();
        services.AddScoped<ITelemetriaService, TelemetriaService>();
        return services;
    }
}
