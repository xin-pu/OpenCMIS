using OpenCMIS.App.Core.Services;
using OpenCMIS.Module.Core;
using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Protocol.Core;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core;

/// <summary>
/// Provides the compatibility facade for high-level CMIS workflows.
/// </summary>
public sealed class CmisDevice : ICmisDevice
{
    private readonly IDeviceConnection? _legacyConnection;
    private readonly OpticalModuleSession? _moduleSession;
    private readonly CmisIdentityReader _identityReader;
    private readonly CmisMonitorReader _monitorReader;
    private readonly CmisLaneReader _laneReader;
    private readonly CmisStatusService _statusService;

    public CmisDevice(
        DeviceInfo deviceInfo,
        IDeviceConnection deviceConnection,
        IRegisterAccess registerAccess)
    {
        DeviceInfo = deviceInfo ??
                     throw InvalidParameter(nameof(deviceInfo));
        _legacyConnection = deviceConnection ??
                            throw InvalidParameter(nameof(deviceConnection));
        RegisterAccess = registerAccess ??
                         throw InvalidParameter(nameof(registerAccess));
        _identityReader = new CmisIdentityReader(RegisterAccess);
        _monitorReader = new CmisMonitorReader(RegisterAccess);
        _laneReader = new CmisLaneReader(RegisterAccess);
        _statusService = new CmisStatusService(
            RegisterAccess,
            TimeProvider.System);
    }

    public CmisDevice(
        DeviceInfo deviceInfo,
        OpticalModuleSession moduleSession,
        I2cDeviceAddress deviceAddress,
        IMsaMemoryAccessor msaMemory,
        IHciMemoryAccessor hciMemory)
    {
        DeviceInfo = deviceInfo ??
                     throw InvalidParameter(nameof(deviceInfo));
        _moduleSession = moduleSession ??
                         throw InvalidParameter(nameof(moduleSession));
        ArgumentNullException.ThrowIfNull(msaMemory);
        HciAccess = hciMemory ??
                    throw InvalidParameter(nameof(hciMemory));
        RegisterAccess = new RegisterAccess(msaMemory, deviceAddress);
        _identityReader = new CmisIdentityReader(RegisterAccess);
        _monitorReader = new CmisMonitorReader(RegisterAccess);
        _laneReader = new CmisLaneReader(RegisterAccess);
        _statusService = new CmisStatusService(
            RegisterAccess,
            TimeProvider.System);
    }

    public DeviceInfo DeviceInfo { get; }

    public bool IsConnected =>
        _moduleSession?.IsOpen ?? _legacyConnection?.IsConnected == true;

    public IRegisterAccess RegisterAccess { get; }

    /// <summary>
    /// Gets vendor HCI access when the device was created through Module.Core.
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

    public Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 4)
    {
        EnsureConnected();
        return _monitorReader.ReadAsync(laneCount);
    }

    public Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 4)
    {
        EnsureConnected();
        return _laneReader.ReadAsync(laneCount);
    }

    public async Task<ModuleDashData> ReadModuleDashDataAsync(
        int laneCount = 4)
    {
        EnsureConnected();
        var identity = await _identityReader.ReadAsync();
        var monitors = await _monitorReader.ReadAsync(laneCount);
        var lanes = await _laneReader.ReadAsync(laneCount);
        var status = await _statusService.ReadAsync();
        return new ModuleDashData
        {
            Identity = identity,
            Monitors = monitors,
            Lanes = lanes,
            CurrentState = status.CurrentState,
            IsReady = status.IsReady,
            StatusTimestamp = DateTime.Now
        };
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
        return new CmisException(
            CmisErrorCode.InvalidParameterValue,
            parameterName);
    }
}
