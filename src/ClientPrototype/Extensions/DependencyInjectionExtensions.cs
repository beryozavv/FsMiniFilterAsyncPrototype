using ClientPrototype.Abstractions;
using ClientPrototype.Clients;
using ClientPrototype.Flow;
using ClientPrototype.Settings;
using ClientPrototype.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClientPrototype.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDriverAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DriverSettings>(configuration.GetSection(nameof(DriverSettings)));

        services
            .AddSingleton<IDriverWorker, WinDriverWorker>()
            .AddSingleton<INotificationFlow, DataFlowPrototype>()
            .AddSingleton<IDriverClient, WinDriverClient>();

        return services;
    }
}
