using OpenCMIS.App.Core.Services;
using OpenCMIS.Module.Core;
using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Protocol.Core;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core
{
    /// <summary>
    ///     Provides the compatibility facade for high-level CMIS workflows.
    /// </summary>
    public sealed class CmisDevice : ICmisDevice
    {
        private readonly IDeviceConnection?    _legacyConnection;
        private readonly OpticalModuleSession? _moduleSession;
        private readonly CmisIdentityReader    _identityReader;
        private readonly CmisMonitorReader     _monitorReader;
        private readonly CmisLaneReader        _laneReader;
        private readonly CmisStatusService     _statusService;
        private readonly VdmReader             _vdmReader;

        public CmisDevice(DeviceInfo        deviceInfo,
                          IDeviceConnection deviceConnection,
                          IRegisterAccess   registerAccess)
        {
            DeviceInfo = deviceInfo ??
                         throw InvalidParameter(nameof(deviceInfo));
            _legacyConnection = deviceConnection ??
                                throw InvalidParameter(nameof(deviceConnection));
            RegisterAccess = registerAccess ??
                             throw InvalidParameter(nameof(registerAccess));
            _identityReader = new (RegisterAccess);
            _monitorReader  = new (RegisterAccess);
            _laneReader     = new (RegisterAccess);
            _statusService = new (
                    RegisterAccess,
                    TimeProvider.System);
            _vdmReader = new (RegisterAccess);
        }

        public CmisDevice(DeviceInfo           deviceInfo,
                          OpticalModuleSession moduleSession,
                          I2cDeviceAddress     deviceAddress,
                          IMsaMemoryAccessor   msaMemory,
                          IHciMemoryAccessor   hciMemory)
        {
            DeviceInfo = deviceInfo ??
                         throw InvalidParameter(nameof(deviceInfo));
            _moduleSession = moduleSession ??
                             throw InvalidParameter(nameof(moduleSession));
            ArgumentNullException.ThrowIfNull(msaMemory);
            HciAccess = hciMemory ??
                        throw InvalidParameter(nameof(hciMemory));
            RegisterAccess  = new RegisterAccess(msaMemory, deviceAddress);
            _identityReader = new (RegisterAccess);
            _monitorReader  = new (RegisterAccess);
            _laneReader     = new (RegisterAccess);
            _statusService = new (
                    RegisterAccess,
                    TimeProvider.System);
            _vdmReader = new (RegisterAccess);
        }

        public DeviceInfo DeviceInfo { get; }

        public bool IsConnected => _moduleSession?.IsOpen ?? _legacyConnection?.IsConnected == true;

        public IRegisterAccess RegisterAccess { get; }

        /// <summary>
        ///     Gets vendor HCI access when the device was created through Module.Core.
        /// </summary>
        public IHciMemoryAccessor? HciAccess { get; }

        public Task<ModuleInfo> GetModuleInfoAsync()
        {
            EnsureConnected();
            return _identityReader.ReadSummaryAsync();
        }

        public Task<ModuleStatus> GetStatusAsync()
        {
            EnsureConnected();
            return _statusService.ReadAsync();
        }

        public Task SetStateAsync(ModuleState state)
        {
            EnsureConnected();
            return _statusService.SetAsync(state);
        }

        public Task<ModuleIdentity> ReadModuleIdentityAsync()
        {
            EnsureConnected();
            return _identityReader.ReadAsync();
        }

        public Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 8)
        {
            EnsureConnected();
            return _monitorReader.ReadAsync(laneCount);
        }

        public Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 8)
        {
            EnsureConnected();
            return _laneReader.ReadAsync(laneCount);
        }

        public async Task<ModuleDashData> ReadModuleDashDataAsync(int laneCount = 8)
        {
            EnsureConnected();
            var identity = await _identityReader.ReadAsync();
            var monitors = await _monitorReader.ReadAsync(laneCount);
            var lanes    = await _laneReader.ReadAsync(laneCount);
            var status   = await _statusService.ReadAsync();
            return new()
                   {
                       Identity        = identity,
                       Monitors        = monitors,
                       Lanes           = lanes,
                       CurrentState    = status.CurrentState,
                       IsReady         = status.IsReady,
                       Status          = status,
                       StatusTimestamp = DateTime.Now
                   };
        }

        public async Task<int> ReadMediaLaneCountAsync()
        {
            EnsureConnected();
            try
            {
                var count = await RegisterAccess.ReadByteAsync(
                                    0x00,
                                    CmisConstants.RegMediaLaneCount);
                return count > 0 && count <= CmisConstants.MaxLanes
                               ? count
                               : CmisConstants.DefaultLaneCount;
            }
            catch
            {
                return CmisConstants.DefaultLaneCount;
            }
        }

        public async Task<bool> IsVdmSupportedAsync()
        {
            EnsureConnected();
            try
            {
                var capability = await RegisterAccess.ReadByteAsync(
                    CmisConstants.VdmCapabilityPage,
                    CmisConstants.VdmCapabilityByte);
                return (capability & CmisConstants.VdmCapabilityBit) != 0;
            }
            catch
            {
                return false;
            }
        }

        public Task<VdmDiagnostics> ReadVdmDiagnosticsAsync()
        {
            EnsureConnected();
            return _vdmReader.ReadAsync();
        }

        public async Task CloseAsync()
        {
            if (_moduleSession is not null)
            {
                await _moduleSession.DisposeAsync();
                return;
            }

            await _legacyConnection!.CloseAsync();
        }

        private void EnsureConnected()
        {
            CmisException.ThrowIf(!IsConnected, CmisErrorCode.DeviceNotConnected);
        }

        private static CmisException InvalidParameter(string parameterName)
        {
            return new (
                    CmisErrorCode.InvalidParameterValue,
                    parameterName);
        }
    }
}
