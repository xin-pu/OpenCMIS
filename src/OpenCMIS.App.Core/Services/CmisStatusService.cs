using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services
{
    internal sealed class CmisStatusService(IRegisterAccess registers,
                                            TimeProvider    timeProvider)
    {
        public async Task<ModuleStatus> ReadAsync()
        {
            var stateByte = await registers.ReadByteAsync(
                                    0x00,
                                    CmisConstants.RegModuleState);
            var statusByte = await registers.ReadByteAsync(
                                     0x00,
                                     CmisConstants.RegStatus);
            var interruptBytes = await registers.ReadBlockAsync(
                                         0x00,
                                         CmisConstants.RegInterruptFlags,
                                         2);
            var flagsWord = (ushort) (interruptBytes[0] | interruptBytes[1] << 8);

            // Also read module flags (0x06-0x07) for capability display
            var moduleFlagBytes = await registers.ReadBlockAsync(
                                          0x00,
                                          CmisConstants.RegModuleFlags,
                                          2);

            var interruptFlags = DecodeInterruptFlags(interruptBytes, flagsWord);
            var moduleFlags    = DecodeModuleFlags(moduleFlagBytes);

            return new()
                   {
                       CurrentState  = DecodeState(stateByte),
                       IsReady       = (statusByte & 0x01) != 0,
                       RawStatusByte = statusByte,
                       RawStateByte  = stateByte,

                       // DataPathFirmwareFault bit not yet confirmed against CMIS 5.2 Status
                       // register (0x02) bit definitions. Preserving raw byte for debug.
                       DataPathFirmwareFault = false,
                       InterruptFlags        = interruptFlags,
                       ModuleFlags           = moduleFlags,
                       HasAlerts             = flagsWord != 0,
                       ActiveAlerts          = interruptFlags.GetActiveFlags().ToList(),
                       TempAlarm             = interruptFlags.TempHighAlarm || interruptFlags.TempLowAlarm,
                       TempWarning           = false, // Latched interrupts only show alarm; warnings are not latched
                       VccAlarm              = interruptFlags.VccHighAlarm || interruptFlags.VccLowAlarm,
                       VccWarning            = false
                   };
        }

        public async Task SetAsync(ModuleState target)
        {
            var current = await ReadAsync();
            ValidateTransition(current.CurrentState, target);
            await registers.WriteByteAsync(
                    0x00,
                    CmisConstants.RegModuleState,
                    (byte) target);

            var started = timeProvider.GetTimestamp();
            while (timeProvider.GetElapsedTime(started) <
                   TimeSpan.FromMilliseconds(CmisConstants.DefaultTimeoutMs))
            {
                var value = await registers.ReadByteAsync(
                                    0x00,
                                    CmisConstants.RegModuleState);
                if ((ModuleState) value == target)
                    return;

                await Task.Delay(
                        TimeSpan.FromMilliseconds(10),
                        timeProvider);
            }

            throw new CmisException(CmisErrorCode.ModuleStateMachineError, target);
        }

        private static ModuleState DecodeState(byte stateByte)
        {
            return stateByte switch
                   {
                       0 => ModuleState.Initialization,
                       1 => ModuleState.LowPwr,
                       2 => ModuleState.PwrUp,
                       3 => ModuleState.Ready,
                       4 => ModuleState.PwrDn,
                       _ => ModuleState.Fault
                   };
        }

        private static CmisInterruptFlags DecodeInterruptFlags(byte[] rawBytes,
                                                               ushort flagsWord)
        {
            return new()
                   {
                       RawBytes         = [rawBytes[0], rawBytes[1]],
                       TempHighAlarm    = (flagsWord              & 0x0001) != 0,
                       TempLowAlarm     = (flagsWord              & 0x0002) != 0,
                       VccHighAlarm     = (flagsWord              & 0x0004) != 0,
                       VccLowAlarm      = (flagsWord              & 0x0008) != 0,
                       TxPowerHighAlarm = (flagsWord              & 0x0010) != 0,
                       TxPowerLowAlarm  = (flagsWord              & 0x0020) != 0,
                       RxPowerHighAlarm = (flagsWord              & 0x0040) != 0,
                       RxPowerLowAlarm  = (flagsWord              & 0x0080) != 0,
                       TxBiasHighAlarm  = (flagsWord              & 0x0100) != 0,
                       TxBiasLowAlarm   = (flagsWord              & 0x0200) != 0,
                       TxFault          = (flagsWord              & 0x0400) != 0,
                       RxLOS            = (flagsWord              & 0x0800) != 0,
                       Reserved         = (byte) (flagsWord >> 12 & 0x0F)
                   };
        }

        private static CmisModuleFlags DecodeModuleFlags(byte[] rawBytes)
        {
            var byte0 = rawBytes.Length > 0 ? rawBytes[0] : (byte) 0;
            var byte1 = rawBytes.Length > 1 ? rawBytes[1] : (byte) 0;

            // CMIS 5.3 extends byte 0 flags: bit 3 = DataPathConfigSupported
            var is53Plus = (rawBytes.Length > 0 && (rawBytes[0] & 0x08) != 0)
                        || true; // Always decode extended bits for forward compatibility

            return new()
                   {
                       RawBytes                 = [byte0, byte1],
                       CdbSupported             = (byte0        & 0x01) != 0,
                       DiagMonSupported         = (byte0        & 0x02) != 0,
                       StateControlSupported    = (byte0        & 0x04) != 0,
                       DataPathConfigSupported  = (byte0        & 0x08) != 0,
                       Byte0Reserved            = (byte) (byte0 & 0xF0),
                       MaxDataRate              = byte1
                   };
        }

        private static void ValidateTransition(ModuleState current,
                                               ModuleState target)
        {
            if (current == target)
                return;

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
                            ModuleState.Ready          => current == ModuleState.PwrUp,
                            ModuleState.PwrDn          => current == ModuleState.Ready,
                            ModuleState.Initialization => current == ModuleState.PwrDn,
                            _                          => false
                        };

            if (!valid)
            {
                throw new CmisException(
                        CmisErrorCode.InvalidStateTransition,
                        current,
                        target);
            }
        }
    }
}
