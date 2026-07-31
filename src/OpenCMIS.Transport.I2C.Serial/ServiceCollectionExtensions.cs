using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Providers;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenCmisSerialAdapters(
        this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(I2cRetryOptions.Default);
        services.TryAddSingleton<ISerialSessionFactory, SerialPortSessionFactory>();
        services.TryAddSingleton<ISerialPortCatalog, SystemSerialPortCatalog>();
        services.AddSingleton<II2cAdapterProvider, LinktelSerialAdapterProvider>();
        services.AddSingleton<II2cAdapterProvider, HmSerialAdapterProvider>();
        services.AddSingleton<II2cAdapterProvider, HmMultiChannelAdapterProvider>();
        return services;
    }
}
