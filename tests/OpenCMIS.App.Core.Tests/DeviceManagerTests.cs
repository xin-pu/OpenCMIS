using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.App.Core.Tests
{
    public sealed class DeviceManagerTests
    {
        private static readonly I2cDeviceAddress Address = new (0x50);

        [Fact]
        public async Task OpenDevice_selects_provider_from_typed_profile()
        {
            var bus      = new StubI2cBus();
            var provider = new StubProvider("linktel", bus);
            var factory  = new StubOpticalModuleFactory();
            var manager  = new DeviceManager([provider], factory);
            var profile = new SerialI2cConnectionProfile(
                    "linktel",
                    "COM7",
                    115200,
                    Address);
            var info = new DeviceInfo
                       {
                           Id      = "COM7",
                           Name    = "Linktel COM7",
                           Profile = profile
                       };

            await manager.OpenDeviceAsync(info);

            Assert.Same(profile, provider.OpenedProfile);
            Assert.Same(bus,     factory.OpenedBus);
            Assert.Same(info,    factory.OpenedDeviceInfo);
        }

        [Fact]
        public async Task Enumeration_combines_providers_and_records_failures()
        {
            var descriptor = new I2cAdapterDescriptor(
                    "linktel",
                    "COM7",
                    "Linktel COM7",
                    new SerialI2cConnectionProfile(
                            "linktel",
                            "COM7",
                            115200,
                            Address));
            var manager = new DeviceManager(
                    [new StubProvider("linktel", new StubI2cBus(), descriptor), new StubProvider("hm", new IOException("Access denied"))],
                    new StubOpticalModuleFactory());

            var devices = (await manager.EnumerateDevicesAsync()).ToList();

            Assert.Collection(
                    devices,
                    device =>
                        {
                            Assert.Equal("COM7", device.Id);
                            Assert.Same(descriptor.Profile, device.Profile);
                        });
            var failure = Assert.Single(manager.LastProbeFailures);
            Assert.Equal("hm", failure.AdapterId);
            Assert.Contains("Access denied", failure.Message);
        }

        private sealed class StubProvider : II2cAdapterProvider
        {
            private readonly II2cRegisterBus?       _bus;
            private readonly I2cAdapterDescriptor[] _descriptors;
            private readonly Exception?             _discoveryException;

            public StubProvider(string                        adapterId,
                                II2cRegisterBus               bus,
                                params I2cAdapterDescriptor[] descriptors)
            {
                AdapterId    = adapterId;
                _bus         = bus;
                _descriptors = descriptors;
            }

            public StubProvider(string adapterId, Exception discoveryException)
            {
                AdapterId           = adapterId;
                _discoveryException = discoveryException;
                _descriptors        = [];
            }

            public I2cConnectionProfile? OpenedProfile { get; private set; }

            public string AdapterId { get; }

            public ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(CancellationToken cancellationToken = default)
            {
                if (_discoveryException is not null)
                {
                    return ValueTask.FromException<IReadOnlyList<I2cAdapterDescriptor>>(
                            _discoveryException);
                }

                return ValueTask.FromResult<IReadOnlyList<I2cAdapterDescriptor>>(
                        _descriptors);
            }

            public ValueTask<II2cRegisterBus> OpenAsync(I2cConnectionProfile profile,
                                                        CancellationToken    cancellationToken = default)
            {
                OpenedProfile = profile;
                return ValueTask.FromResult(_bus!);
            }
        }

        private sealed class StubOpticalModuleFactory : IOpticalModuleFactory
        {
            public DeviceInfo?      OpenedDeviceInfo { get; private set; }
            public II2cRegisterBus? OpenedBus        { get; private set; }

            public ValueTask<ICmisDevice> CreateAsync(DeviceInfo        deviceInfo,
                                                      II2cRegisterBus   bus,
                                                      CancellationToken cancellationToken = default)
            {
                OpenedDeviceInfo = deviceInfo;
                OpenedBus        = bus;
                return ValueTask.FromResult<ICmisDevice>(new StubCmisDevice(deviceInfo));
            }
        }

        private sealed class StubI2cBus : II2cRegisterBus
        {
            public bool IsOpen { get; private set; }

            public I2cTransferCapabilities Capabilities => I2cTransferCapabilities.Unbounded;

            public ValueTask OpenAsync(CancellationToken cancellationToken = default)
            {
                IsOpen = true;
                return ValueTask.CompletedTask;
            }

            public ValueTask CloseAsync(CancellationToken cancellationToken = default)
            {
                IsOpen = false;
                return ValueTask.CompletedTask;
            }

            public ValueTask ReadAsync(I2cDeviceAddress  device,
                                       RegisterOffset    offset,
                                       Memory<byte>      destination,
                                       CancellationToken cancellationToken = default)
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask WriteAsync(I2cDeviceAddress     device,
                                        RegisterOffset       offset,
                                        ReadOnlyMemory<byte> data,
                                        CancellationToken    cancellationToken = default)
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }

        private sealed class StubCmisDevice(DeviceInfo info) : ICmisDevice
        {
            public DeviceInfo DeviceInfo  => info;
            public bool       IsConnected => true;

            public IRegisterAccess RegisterAccess => throw new NotSupportedException();

            public IHciMemoryAccessor? HciAccess => null;

            public Task<ModuleInfo> GetModuleInfoAsync()
            {
                throw new NotSupportedException();
            }

            public Task<ModuleStatus> GetStatusAsync()
            {
                throw new NotSupportedException();
            }

            public Task SetStateAsync(ModuleState state)
            {
                throw new NotSupportedException();
            }

            public Task<ModuleIdentity> ReadModuleIdentityAsync()
            {
                throw new NotSupportedException();
            }

            public Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 8)
            {
                throw new NotSupportedException();
            }

            public Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 8)
            {
                throw new NotSupportedException();
            }

            public Task<ModuleDashData> ReadModuleDashDataAsync(int laneCount = 8)
            {
                throw new NotSupportedException();
            }

            public Task<int> ReadMediaLaneCountAsync()
            {
                throw new NotSupportedException();
            }

            public Task CloseAsync()
            {
                return Task.CompletedTask;
            }

            public Task<bool> IsVdmSupportedAsync()
            {
                return Task.FromResult(false);
            }

            public Task<VdmDiagnostics> ReadVdmDiagnosticsAsync()
            {
                throw new NotSupportedException();
            }
        }
    }
}
