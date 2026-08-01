using Microsoft.Extensions.DependencyInjection;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Cypress;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenCmisCypressAdapters(
        this IServiceCollection services)
    {
        services.AddSingleton<ICypressDeviceApiFactory, CypressDeviceApiFactory>();
        services.AddSingleton<II2cAdapterProvider, CypressI2cAdapterProvider>();
        return services;
    }
}
