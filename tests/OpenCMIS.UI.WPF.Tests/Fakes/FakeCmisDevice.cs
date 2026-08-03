using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.UI.WPF.Tests.Fakes
{
    internal sealed class FakeCmisDevice(DeviceInfo deviceInfo,
                                         Exception? moduleInfoException = null) : ICmisDevice
    {
        public DeviceInfo          DeviceInfo     { get; }              = deviceInfo;
        public bool                IsConnected    { get; private set; } = true;
        public IRegisterAccess     RegisterAccess => throw new NotSupportedException();
        public IHciMemoryAccessor? HciAccess      => null;

        public Task<ModuleInfo> GetModuleInfoAsync()
        {
            return moduleInfoException is null
                           ? Task.FromResult(new ModuleInfo
                                             {
                                                 VendorName   = "Test Vendor",
                                                 PartNumber   = "TEST-PN",
                                                 SerialNumber = "TEST-SN"
                                             })
                           : Task.FromException<ModuleInfo>(moduleInfoException);
        }

        public Task CloseAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
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

        public Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 4)
        {
            throw new NotSupportedException();
        }

        public Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 4)
        {
            throw new NotSupportedException();
        }

        public Task<ModuleDashData> ReadModuleDashDataAsync(int laneCount = 4)
        {
            throw new NotSupportedException();
        }
    }
}
