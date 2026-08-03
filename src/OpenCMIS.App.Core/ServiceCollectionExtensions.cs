using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenCMIS.CDB.Abstractions;
using OpenCMIS.CDB.Core;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Core;
using Serilog;
using Serilog.Events;

namespace OpenCMIS.App.Core
{
    /// <summary>
    ///     Extension methods for registering OpenCMIS services in the DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers all OpenCMIS core services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOpenCmisCore(this IServiceCollection services)
        {
            // Protocol layer services (stateless, can be shared)
            services.AddSingleton<IAddressingStrategy, StandardAddressingStrategy>();

            // App layer services
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IOpticalModuleFactory, OpticalModuleFactory>();
            services.AddSingleton<IDeviceManager, DeviceManager>();

            // CDB layer services
            services.AddSingleton<ICdbReader, CdbReader>();
            services.AddSingleton<ICdbWriter, CdbWriter>();
            services.AddSingleton<ICdbValidator, CdbValidator>();
            services.AddSingleton<CdbManager>();

            return services;
        }

        /// <summary>
        ///     Registers Serilog logging for OpenCMIS.
        /// </summary>
        /// <param name="builder">The host builder.</param>
        /// <returns>The host builder for chaining.</returns>
        public static IHostBuilder UseOpenCmisLogging(this IHostBuilder builder)
        {
            return builder.UseSerilog((context, configuration) =>
                                          {
                                              configuration
                                                     .MinimumLevel.Information()
                                                     .MinimumLevel.Override("OpenCMIS", LogEventLevel.Debug)
                                                     .WriteTo.Console()
                                                     .WriteTo.File("logs/cmis-.log",
                                                                   rollingInterval: RollingInterval.Day,
                                                                   retainedFileCountLimit: 7);
                                          });
        }
    }
}
