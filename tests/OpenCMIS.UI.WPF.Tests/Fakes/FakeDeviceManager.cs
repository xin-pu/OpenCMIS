using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.UI.WPF.Tests.Fakes
{
    internal sealed class FakeDeviceManager(params DeviceInfo[] devices) : IDeviceManager
    {
        public IReadOnlyList<DeviceInfo> Devices             { get; } = devices;
        public DeviceInfo?               OpenedDeviceInfo    { get; private set; }
        public Exception?                OpenException       { get; set; }
        public Exception?                ModuleInfoException { get; set; }
        public ICmisDevice?              OpenedDevice        { get; private set; }

        public Task<IEnumerable<DeviceInfo>> EnumerateDevicesAsync()
        {
            return Task.FromResult<IEnumerable<DeviceInfo>>(Devices);
        }

        public Task<ICmisDevice> OpenDeviceAsync(DeviceInfo deviceInfo)
        {
            OpenedDeviceInfo = deviceInfo;
            if (OpenException is not null)
                return Task.FromException<ICmisDevice>(OpenException);

            OpenedDevice = new FakeCmisDevice(deviceInfo, ModuleInfoException);
            return Task.FromResult(OpenedDevice);
        }

        public Task CloseDeviceAsync(ICmisDevice device)
        {
            return device.CloseAsync();
        }
    }
}
