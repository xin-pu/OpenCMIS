using Microsoft.Extensions.DependencyInjection;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.Simulated;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenCmisSimulatedAdapters(
        this IServiceCollection services)
    {
        services.AddSingleton<II2cAdapterProvider, SimulatedI2cAdapterProvider>();
        return services;
    }
}
