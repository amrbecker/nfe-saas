using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NfeSaas.Application.Behaviors;
using NfeSaas.Application.Services;
using System.Reflection;

namespace NfeSaas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<INcmUpdater, NcmUpdater>();

        return services;
    }
}
