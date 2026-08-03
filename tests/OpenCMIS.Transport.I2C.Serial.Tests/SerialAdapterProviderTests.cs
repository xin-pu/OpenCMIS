using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Providers;
using OpenCMIS.Transport.I2C.Serial.Serial;
using Xunit;

namespace OpenCMIS.Transport.I2C.Serial.Tests
{
    public sealed class SerialAdapterProviderTests
    {
        private static readonly I2cDeviceAddress Address = new (0x50);

        [Fact]
        public async Task Linktel_discovery_maps_each_serial_port()
        {
            var provider = new LinktelSerialAdapterProvider(
                    new UnusedSessionFactory(),
                    new StubSerialPortCatalog("COM7", "COM8"),
                    new (1, TimeSpan.Zero),
                    TimeProvider.System);

            var descriptors = await provider.DiscoverAsync();

            Assert.Equal(2, descriptors.Count);
            Assert.All(
                    descriptors,
                    descriptor =>
                        {
                            Assert.Equal("linktel", descriptor.AdapterId);
                            Assert.Equal(Address,   descriptor.Profile.DeviceAddress);
                        });
        }

        [Fact]
        public async Task Hm_multichannel_discovery_creates_five_channels_per_port()
        {
            var provider = new HmMultiChannelAdapterProvider(
                    new UnusedSessionFactory(),
                    new StubSerialPortCatalog("COM9"),
                    new (1, TimeSpan.Zero),
                    TimeProvider.System);

            var descriptors = await provider.DiscoverAsync();

            Assert.Equal(5, descriptors.Count);
            Assert.Equal(
                    new byte[] {1, 2, 3, 4, 5},
                    descriptors
                           .Select(item => (HmMultiChannelConnectionProfile) item.Profile)
                           .Select(profile => profile.Channel));
        }

        private sealed class StubSerialPortCatalog(params string[] ports)
                : ISerialPortCatalog
        {
            public IReadOnlyList<string> GetPortNames()
            {
                return ports;
            }
        }

        private sealed class UnusedSessionFactory : ISerialSessionFactory
        {
            public ISerialSession Create(string portName, int baudRate)
            {
                throw new NotSupportedException();
            }
        }
    }
}
