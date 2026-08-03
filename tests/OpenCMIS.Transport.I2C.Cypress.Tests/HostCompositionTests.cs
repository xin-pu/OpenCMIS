using Microsoft.Extensions.DependencyInjection;
using OpenCMIS.App.Core;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial;
using Xunit;

namespace OpenCMIS.Transport.I2C.Cypress.Tests
{
    public sealed class HostCompositionTests
    {
        [Fact]
        public void Serial_only_host_resolves_only_cross_platform_providers()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSerialAdapters();
            using var provider = services.BuildServiceProvider();

            var adapterIds = provider
                            .GetServices<II2cAdapterProvider>()
                            .Select(adapter => adapter.AdapterId)
                            .Order()
                            .ToArray();

            Assert.Equal(
                    new[] {"hm", "hm-multichannel", "linktel"},
                    adapterIds);
        }

        [Fact]
        public void Windows_host_adds_cypress_provider()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSerialAdapters();
            services.AddOpenCmisCypressAdapters();
            using var provider = services.BuildServiceProvider();

            var adapterIds = provider
                            .GetServices<II2cAdapterProvider>()
                            .Select(adapter => adapter.AdapterId)
                            .Order()
                            .ToArray();

            Assert.Equal(
                    new[] {"cypress", "hm", "hm-multichannel", "linktel"},
                    adapterIds);
        }
    }
}
