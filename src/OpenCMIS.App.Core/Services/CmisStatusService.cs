using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services;

internal sealed class CmisStatusService(
    IRegisterAccess registers,
    TimeProvider timeProvider)
{
    public async Task<ModuleStatus> ReadAsync()
    {
        var state = await registers.ReadByteAsync(
            0x00,
            CmisConstants.RegModuleState);
        var status = await registers.ReadByteAsync(
            0x00,
            CmisConstants.RegStatus);
        var interruptBytes = await registers.ReadBlockAsync(
            0x00,
            CmisConstants.RegInterruptFlags,
            2);
        var flags = (ushort)(interruptBytes[0] | interruptBytes[1] << 8);

        return new ModuleStatus
        {
            CurrentState = state switch
            {
                0 => ModuleState.Initialization,
                1 => ModuleState.LowPwr,
                2 => ModuleState.PwrUp,
                3 => ModuleState.Ready,
                4 => ModuleState.PwrDn,
                _ => ModuleState.Fault
            },
            IsReady = (status & 0x01) != 0,
            HasAlerts = flags != 0,
            ActiveAlerts = ParseAlerts(flags)
        };
    }

    public async Task SetAsync(ModuleState target)
    {
        var current = await ReadAsync();
        ValidateTransition(current.CurrentState, target);
        await registers.WriteByteAsync(
            0x00,
            CmisConstants.RegModuleState,
            (byte)target);

        var started = timeProvider.GetTimestamp();
        while (timeProvider.GetElapsedTime(started) <
               TimeSpan.FromMilliseconds(CmisConstants.DefaultTimeoutMs))
        {
            var value = await registers.ReadByteAsync(
                0x00,
                CmisConstants.RegModuleState);
            if ((ModuleState)value == target)
            {
                return;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(10),
                timeProvider);
        }

        throw new CmisException(CmisErrorCode.ModuleStateMachineError, target);
    }

    private static void ValidateTransition(
        ModuleState current,
        ModuleState target)
    {
        if (current == target)
        {
            return;
        }

        if (current == ModuleState.Fault)
        {
            throw new CmisException(
                CmisErrorCode.InvalidStateTransition,
                current,
                target);
        }

        var valid = target switch
        {
            ModuleState.LowPwr => current == ModuleState.Initialization,
            ModuleState.PwrUp =>
                current is ModuleState.LowPwr or ModuleState.Ready,
            ModuleState.Ready => current == ModuleState.PwrUp,
            ModuleState.PwrDn => current == ModuleState.Ready,
            ModuleState.Initialization => current == ModuleState.PwrDn,
            _ => false
        };

        if (!valid)
        {
            throw new CmisException(
                CmisErrorCode.InvalidStateTransition,
                current,
                target);
        }
    }

    private static List<string> ParseAlerts(ushort flags)
    {
        var alerts = new List<string>();
        AddIf(0x0001, "Temperature high alarm");
        AddIf(0x0002, "Temperature low alarm");
        AddIf(0x0004, "VCC high alarm");
        AddIf(0x0008, "VCC low alarm");
        AddIf(0x0010, "TX power high alarm");
        AddIf(0x0020, "TX power low alarm");
        AddIf(0x0040, "RX power high alarm");
        AddIf(0x0080, "RX power low alarm");
        AddIf(0x0100, "TX bias high alarm");
        AddIf(0x0200, "TX bias low alarm");
        AddIf(0x0400, "TX fault");
        AddIf(0x0800, "RX LOS");
        return alerts;

        void AddIf(ushort mask, string message)
        {
            if ((flags & mask) != 0)
            {
                alerts.Add(message);
            }
        }
    }
}
