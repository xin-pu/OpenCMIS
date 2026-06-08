using System.Text;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core
{
    /// <summary>
    ///     Provides implementation for CMIS device operations.
    /// </summary>
    public class CmisDevice : ICmisDevice
    {
        private const byte PageUpperVendorInfo   = 0x01;
        private const byte RegVendorNameStart    = 0x81;
        private const byte RegPartNumberStart    = 0x90;
        private const byte RegSerialNumberStart  = 0xA0;
        private const int  IdStringLength        = 16;

        private readonly IDeviceConnection _deviceConnection;
        private readonly IRegisterAccess   _registerAccess;

        /// <summary>
        ///     Initializes a new instance of the CmisDevice class.
        /// </summary>
        /// <param name="deviceInfo">The device information.</param>
        /// <param name="deviceConnection">The device connection.</param>
        /// <param name="registerAccess">The register access interface.</param>
        public CmisDevice(DeviceInfo deviceInfo, IDeviceConnection deviceConnection, IRegisterAccess registerAccess)
        {
            DeviceInfo        = deviceInfo        ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(deviceInfo));
            _deviceConnection = deviceConnection  ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(deviceConnection));
            _registerAccess   = registerAccess    ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(registerAccess));
        }

        /// <inheritdoc />
        public DeviceInfo DeviceInfo { get; }

        /// <inheritdoc />
        public bool IsConnected => _deviceConnection.IsConnected;

        /// <inheritdoc />
        public IRegisterAccess RegisterAccess => _registerAccess;

        /// <inheritdoc />
        public async Task<ModuleInfo> GetModuleInfoAsync()
        {
            EnsureConnected();

            var info = new ModuleInfo();

            // Read vendor name from upper page 0x01, registers 0x81-0x8F
            var vendorBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, RegVendorNameStart, IdStringLength);
            info.VendorName = ReadAsciiString(vendorBytes);

            // Read part number from registers 0x90-0x9F
            var partBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, RegPartNumberStart, IdStringLength);
            info.PartNumber = ReadAsciiString(partBytes);

            // Read serial number from registers 0xA0-0xAF
            var serialBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, RegSerialNumberStart, IdStringLength);
            info.SerialNumber = ReadAsciiString(serialBytes);

            // Read identifier (page 0x00, register 0x00) to determine module type
            var identifier = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegIdentifier);
            info.ModuleType = GetModuleTypeName(identifier);

            // Read CMIS revision (page 0x00, register 0x01)
            var revision = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegRevision);
            info.CmisVersion = $"{revision >> 4}.{revision & 0x0F}";

            // Read module flags (page 0x00, registers 0x06-0x07)
            var flags = await _registerAccess.ReadBlockAsync(0x00, CmisConstants.RegModuleFlags, 2);
            info.Capabilities = new DeviceCapabilities
            {
                SupportsCdb                  = (flags[0] & 0x01) != 0,
                SupportsDiagnosticMonitoring = (flags[0] & 0x02) != 0,
                SupportsStateControl         = (flags[0] & 0x04) != 0,
                MaxDataRate                  = flags[1]
            };

            return info;
        }

        /// <inheritdoc />
        public async Task<ModuleStatus> GetStatusAsync()
        {
            EnsureConnected();

            var status = new ModuleStatus();

            // Read module state from register 0x03
            var stateByte = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegModuleState);
            status.CurrentState = stateByte switch
            {
                0 => ModuleState.Initialization,
                1 => ModuleState.LowPwr,
                2 => ModuleState.PwrUp,
                3 => ModuleState.Ready,
                4 => ModuleState.PwrDn,
                5 => ModuleState.Fault,
                _ => ModuleState.Fault
            };

            // Read status register 0x02 - bit 0 indicates module is ready
            var statusReg = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegStatus);
            status.IsReady = (statusReg & 0x01) != 0;

            // Read interrupt flags 0x04-0x05 to detect alerts
            var interruptBytes = await _registerAccess.ReadBlockAsync(0x00, CmisConstants.RegInterruptFlags, 2);
            var interruptFlags = (ushort)(interruptBytes[0] | (interruptBytes[1] << 8));
            status.HasAlerts = interruptFlags != 0;
            status.ActiveAlerts = ParseAlerts(interruptFlags);

            return status;
        }

        /// <inheritdoc />
        public async Task SetStateAsync(ModuleState state)
        {
            EnsureConnected();

            // Validate state transition
            var currentStatus = await GetStatusAsync();
            ValidateStateTransition(currentStatus.CurrentState, state);

            // Write target state to module state register 0x03
            var stateValue = (byte)state;
            await _registerAccess.WriteByteAsync(0x00, CmisConstants.RegModuleState, stateValue);

            // Poll for state change confirmation (timeout after default timeout)
            using var cts = new CancellationTokenSource(CmisConstants.DefaultTimeoutMs);
            while (!cts.Token.IsCancellationRequested)
            {
                var newState = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegModuleState);
                if ((ModuleState)newState == state)
                    return;

                await Task.Delay(10, cts.Token);
            }

            throw new CmisException(CmisErrorCode.ModuleStateMachineError, state);
        }

        /// <inheritdoc />
        public async Task CloseAsync()
        {
            await _deviceConnection.CloseAsync();
        }

        private void EnsureConnected()
        {
            CmisException.ThrowIf(!_deviceConnection.IsConnected, CmisErrorCode.DeviceNotConnected);
        }

        private static string ReadAsciiString(byte[] bytes)
        {
            var endIndex = Array.IndexOf(bytes, (byte)0);
            if (endIndex < 0) endIndex = bytes.Length;
            var validBytes = bytes[..endIndex].Where(b => b >= 0x20 && b <= 0x7E).ToArray();
            return Encoding.ASCII.GetString(validBytes);
        }

        private static string GetModuleTypeName(byte identifier)
        {
            return identifier switch
            {
                0x1E => "QSFP-DD",
                0x1F => "OSFP",
                0x18 => "QSFP28",
                0x0D => "QSFP+",
                0x0C => "CFP2",
                0x0B => "CFP4",
                0x06 => "SFP+",
                _    => $"Unknown (0x{identifier:X2})"
            };
        }

        private static void ValidateStateTransition(ModuleState current, ModuleState target)
        {
            if (current == target)
                return;

            if (current == ModuleState.Fault)
                throw new CmisException(CmisErrorCode.InvalidStateTransition, current, target);

            switch (target)
            {
                case ModuleState.LowPwr when current == ModuleState.Initialization:
                case ModuleState.PwrUp when current == ModuleState.LowPwr || current == ModuleState.Ready:
                case ModuleState.Ready when current == ModuleState.PwrUp:
                case ModuleState.PwrDn when current == ModuleState.Ready:
                case ModuleState.Initialization when current == ModuleState.PwrDn:
                    return;
                default:
                    throw new CmisException(CmisErrorCode.InvalidStateTransition, current, target);
            }
        }

        private static List<string> ParseAlerts(ushort flags)
        {
            var alerts = new List<string>();
            if ((flags & 0x0001) != 0) alerts.Add("Temperature high alarm");
            if ((flags & 0x0002) != 0) alerts.Add("Temperature low alarm");
            if ((flags & 0x0004) != 0) alerts.Add("VCC high alarm");
            if ((flags & 0x0008) != 0) alerts.Add("VCC low alarm");
            if ((flags & 0x0010) != 0) alerts.Add("TX power high alarm");
            if ((flags & 0x0020) != 0) alerts.Add("TX power low alarm");
            if ((flags & 0x0040) != 0) alerts.Add("RX power high alarm");
            if ((flags & 0x0080) != 0) alerts.Add("RX power low alarm");
            if ((flags & 0x0100) != 0) alerts.Add("TX bias high alarm");
            if ((flags & 0x0200) != 0) alerts.Add("TX bias low alarm");
            if ((flags & 0x0400) != 0) alerts.Add("TX fault");
            if ((flags & 0x0800) != 0) alerts.Add("RX LOS");

            if (alerts.Count == 0)
                alerts.Add("Unknown alert");

            return alerts;
        }
    }
}
