using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Cypress.Tests.Fakes;
using Xunit;

namespace OpenCMIS.Transport.I2C.Cypress.Tests
{
    public sealed class CypressI2cAdapterProviderTests
    {
        [Fact]
        public async Task Discovery_maps_both_supported_device_kinds_to_typed_profiles()
        {
            var api = new MockCypressDeviceApi();
            api.Devices.Add(new (
                                    "FIC-1",
                                    CypressDeviceKind.Fic2Usb,
                                    "FIC2USB FIC-1"));
            api.Devices.Add(new (
                                    "EUI-1",
                                    CypressDeviceKind.Eui3,
                                    "EUI3 EUI-1"));
            var provider = new CypressI2cAdapterProvider(
                    new StubCypressDeviceApiFactory(api));

            var devices = await provider.DiscoverAsync();

            Assert.Collection(
                    devices.OrderBy(device => device.DeviceId),
                    eui =>
                        {
                            var profile = Assert.IsType<CypressI2cConnectionProfile>(eui.Profile);
                            Assert.Equal("EUI-1", profile.SerialNumber);
                            Assert.Equal(90,      profile.SpeedKhz);
                        },
                    fic =>
                        {
                            var profile = Assert.IsType<CypressI2cConnectionProfile>(fic.Profile);
                            Assert.Equal("FIC-1", profile.SerialNumber);
                            Assert.Equal(100,     profile.SpeedKhz);
                        });
        }

        [Fact]
        public async Task Open_selects_adapter_from_discovered_device_kind()
        {
            var discoveryApi = new MockCypressDeviceApi();
            discoveryApi.Devices.Add(new (
                                             "EUI-7",
                                             CypressDeviceKind.Eui3,
                                             "EUI3 EUI-7"));
            var transferApi = new MockCypressDeviceApi();
            transferApi.Devices.AddRange(discoveryApi.Devices);
            var provider = new CypressI2cAdapterProvider(
                    new StubCypressDeviceApiFactory(discoveryApi, transferApi));
            var profile = new CypressI2cConnectionProfile(
                    "cypress",
                    "EUI-7",
                    0,
                    400,
                    new (0x50));

            await using var adapter = await provider.OpenAsync(profile);

            Assert.IsType<Eui3I2cAdapter>(adapter);
            Assert.True(adapter.IsOpen);
            Assert.Equal("EUI-7", transferApi.OpenedSerialNumber);
        }

        [Fact]
        public async Task Open_rejects_unknown_serial_number()
        {
            var api = new MockCypressDeviceApi();
            var provider = new CypressI2cAdapterProvider(
                    new StubCypressDeviceApiFactory(api));
            var profile = new CypressI2cConnectionProfile(
                    "cypress",
                    "missing",
                    0,
                    100,
                    new (0x50));

            var error = await Assert.ThrowsAsync<CmisException>(() => provider.OpenAsync(profile).AsTask());

            Assert.Equal(CmisErrorCode.I2cAdapterNotFound, error.ErrorCode);
        }

        private sealed class StubCypressDeviceApiFactory(params MockCypressDeviceApi[] instances) : ICypressDeviceApiFactory
        {
            private readonly Queue<MockCypressDeviceApi> _instances = new (instances);

            public ICypressDeviceApi Create()
            {
                return _instances.Count > 1
                               ? _instances.Dequeue()
                               : _instances.Peek();
            }
        }
    }
}
