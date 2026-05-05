using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace NfeSaas.Tests.BDD.Support;

[Binding]
public static class DependencyConfiguration
{
    [ScenarioDependencies]
    public static void CreateServices(IServiceCollection services)
    {
        services.AddSingleton<ScenarioState>();
    }
}
