using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.Transport.Simulated.Tests
{
    public sealed class SimulatedI2cAdapterProviderTests
    {
        [Fact]
        public async Task DiscoverAsync_returns_four_devices()
        {
            var provider    = new SimulatedI2cAdapterProvider();
            var descriptors = await provider.DiscoverAsync();

            Assert.Equal(4, descriptors.Count);
            Assert.Contains(descriptors,
                            d => d.DeviceId == "sim-800g-qsfpdd");
            Assert.Contains(descriptors,
                            d => d.DeviceId == "sim-1p6t-osfp");
            Assert.Contains(descriptors,
                            d => d.DeviceId == "sim-800g-qsfpdd-53");
            Assert.Contains(descriptors,
                            d => d.DeviceId == "sim-1p6t-osfp-53");
        }

        [Fact]
        public async Task Discovered_descriptor_has_adapter_id_sim()
        {
            var provider    = new SimulatedI2cAdapterProvider();
            var descriptors = await provider.DiscoverAsync();

            foreach (var d in descriptors)
                Assert.Equal("sim", d.AdapterId);
        }

        [Fact]
        public async Task Discovered_800g_display_name_is_correct()
        {
            var provider    = new SimulatedI2cAdapterProvider();
            var descriptors = await provider.DiscoverAsync();
            var d800g = Assert.Single(descriptors,
                                      d => d.DeviceId == "sim-800g-qsfpdd");
            Assert.Equal("Simulated 800G CMIS Module (5.2)", d800g.DisplayName);
        }

        [Fact]
        public async Task OpenAsync_returns_simulated_bus()
        {
            var provider    = new SimulatedI2cAdapterProvider();
            var descriptors = await provider.DiscoverAsync();
            var d800g       = descriptors.First(d => d.DeviceId == "sim-800g-qsfpdd");

            var bus = await provider.OpenAsync(d800g.Profile);
            Assert.NotNull(bus);
            Assert.True(bus.IsOpen); // provider opens the bus

            await bus.CloseAsync();
            Assert.False(bus.IsOpen);
        }

        [Fact]
        public async Task OpenAsync_with_wrong_adapter_id_throws()
        {
            var provider = new SimulatedI2cAdapterProvider();
            var badProfile = new SerialI2cConnectionProfile(
                    "serial",
                    "COM1",
                    115200,
                    new (0x50));
            await Assert.ThrowsAsync<CmisException>(() => provider.OpenAsync(badProfile).AsTask());
        }
    }
}
