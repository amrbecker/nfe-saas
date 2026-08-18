using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace NfeSaas.Tests.BDD.Support;

// Sem [Binding]: essa classe é estática e o plugin de DI do Reqnroll tenta registrar toda classe
// [Binding] como serviço no container (para permitir injeção nos construtores dos steps) — uma
// classe estática não pode ser instanciada e isso quebra a resolução do container. O método
// [ScenarioDependencies] abaixo é descoberto pelo plugin independente de [Binding] na classe.
public static class DependencyConfiguration
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ScenarioState>();
        return services;
    }
}
