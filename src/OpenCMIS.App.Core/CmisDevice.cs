using System.Text;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Shared.Models;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core
{
    /// <summary>
    ///     Provides implementation for CMIS device operations.
    /// </summary>
    public class CmisDevice : ICmisDevice
    {
        // Use CmisConstants for addresses
        private const byte PageUpperVendorInfo = 0x01;
        private const int  IdStringLength = 16;

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

            // Read vendor name from upper page 0x01, registers 0x81-0x90 (16 bytes)
            var vendorBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegVendorNameStart, IdStringLength);
            info.VendorName = ReadAsciiString(vendorBytes);

            // Read part number from upper page 0x01, registers 0x94-0xA3 (16 bytes)
            var partBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegPartNumberStart, IdStringLength);
            info.PartNumber = ReadAsciiString(partBytes);

            // Read serial number from upper page 0x01, registers 0xA0-0xAF (16 bytes)
            var serialBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegSerialNumberStart, IdStringLength);
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

        /// <inheritdoc />
        public async Task<ModuleIdentity> ReadModuleIdentityAsync()
        {
            EnsureConnected();

            var identity = new ModuleIdentity();

            // Read vendor name from upper page 0x01, registers 0x81-0x90
            var vendorBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegVendorNameStart, IdStringLength);
            identity.VendorName = ReadAsciiString(vendorBytes);

            // Read vendor OUI from upper page 0x01, registers 0x90-0x92 (3 bytes)
            var ouiBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegVendorOUI, 3);
            identity.VendorOUI = $"{ouiBytes[0]:X2}-{ouiBytes[1]:X2}-{ouiBytes[2]:X2}";

            // Read part number from upper page 0x01, registers 0x94-0xA3
            var partBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegPartNumberStart, IdStringLength);
            identity.PartNumber = ReadAsciiString(partBytes);

            // Read serial number from upper page 0x01, registers 0xA0-0xAF
            var serialBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegSerialNumberStart, IdStringLength);
            identity.SerialNumber = ReadAsciiString(serialBytes);

            // Read hardware revision (BCD) from 0xB0-0xB1
            var hwBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegHardwareRevision, 2);
            identity.HardwareRevision = ReadBcdString(hwBytes, 0);

            // Read firmware revision (BCD) from 0xB2-0xB3
            var fwBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegFirmwareRevision, 2);
            identity.FirmwareRevision = ReadBcdString(fwBytes, 1);

            // Read date code from 0xB4-0xBB (8 bytes ASCII)
            var dateBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegDateCode, 8);
            identity.DateCode = ReadAsciiString(dateBytes);

            // Read CLEI code from 0xBC-0xC5 (10 bytes ASCII)
            var cleiBytes = await _registerAccess.ReadBlockAsync(PageUpperVendorInfo, CmisConstants.RegCLEICode, 10);
            identity.CLEICode = ReadAsciiString(cleiBytes);

            // Read identifier for module type and connector type
            var identifier = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegIdentifier);
            identity.ModuleType = GetModuleTypeName(identifier);
            identity.ConnectorType = GetConnectorTypeName(identifier);

            // Read CMIS revision
            var revision = await _registerAccess.ReadByteAsync(0x00, CmisConstants.RegRevision);
            identity.CmisVersion = $"{revision >> 4}.{revision & 0x0F}";

            return identity;
        }

        /// <inheritdoc />
        public async Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 4)
        {
            EnsureConnected();

            var monitors = new ModuleMonitors();

            // Read temperature from lower page 0x00, 0x0E-0x0F (signed int16, LSB=1/256°C)
            var tempBytes = await _registerAccess.ReadBlockAsync(0x00, CmisConstants.RegTemperatureMSB, 2);
            monitors.Temperature = new MonitorValue
            {
                Value = ParseTemperature(tempBytes),
                Unit = "°C"
            };

            // Read VCC from lower page 0x00, 0x10-0x11 (unsigned int16, LSB=100µV)
            var vccBytes = await _registerAccess.ReadBlockAsync(0x00, CmisConstants.RegVccMSB, 2);
            monitors.VCC = new MonitorValue
            {
                Value = ParseVcc(vccBytes),
                Unit = "V"
            };

            // Read per-lane monitors
            for (var lane = 0; lane < laneCount && lane < CmisConstants.MaxLanes; lane++)
            {
                var lanePage = (byte)(CmisConstants.FirstLanePage + lane);

                // TX bias: 2 bytes unsigned, LSB=2µA → convert to mA
                var txBiasBytes = await _registerAccess.ReadBlockAsync(lanePage, CmisConstants.RegLaneTxBiasMSB, 2);
                var txBiasValue = ParseCurrent(txBiasBytes, 2e-3);
                monitors.TxBiasPerLane.Add(new MonitorValue { Value = txBiasValue, Unit = "mA" });

                // TX power: 2 bytes unsigned, LSB=0.1µW → convert to mW
                var txPowerBytes = await _registerAccess.ReadBlockAsync(lanePage, CmisConstants.RegLaneTxPowerMSB, 2);
                var txPowerValue = ParsePower(txPowerBytes, 1e-4);
                monitors.TxPowerPerLane.Add(new MonitorValue { Value = txPowerValue, Unit = "mW" });

                // RX power: 2 bytes unsigned, LSB=0.1µW → convert to mW
                var rxPowerBytes = await _registerAccess.ReadBlockAsync(lanePage, CmisConstants.RegLaneRxPowerMSB, 2);
                var rxPowerValue = ParsePower(rxPowerBytes, 1e-4);
                monitors.RxPowerPerLane.Add(new MonitorValue { Value = rxPowerValue, Unit = "mW" });
            }

            monitors.ComputeTotals();
            return monitors;
        }

        /// <inheritdoc />
        public async Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 4)
        {
            EnsureConnected();

            var lanes = new List<LaneStatus>();

            for (var lane = 0; lane < laneCount && lane < CmisConstants.MaxLanes; lane++)
            {
                var lanePage = (byte)(CmisConstants.FirstLanePage + lane);

                var flagsByte = await _registerAccess.ReadByteAsync(lanePage, CmisConstants.RegLaneStatusFlags);
                var isEnabled = (flagsByte & 0x01) != 0;
                var hasFault = (flagsByte & 0x02) != 0;

                var txPowerBytes = await _registerAccess.ReadBlockAsync(lanePage, CmisConstants.RegLaneTxPowerMSB, 2);
                var txPower = ParsePower(txPowerBytes, 1e-4);

                var rxPowerBytes = await _registerAccess.ReadBlockAsync(lanePage, CmisConstants.RegLaneRxPowerMSB, 2);
                var rxPower = ParsePower(rxPowerBytes, 1e-4);

                var txBiasBytes = await _registerAccess.ReadBlockAsync(lanePage, CmisConstants.RegLaneTxBiasMSB, 2);
                var txBias = ParseCurrent(txBiasBytes, 2e-3);

                var status = new LaneStatus
                {
                    LaneNumber = lane + 1,
                    TxPower = Math.Round(txPower, 4),
                    RxPower = Math.Round(rxPower, 4),
                    TxBias = Math.Round(txBias, 3),
                    IsEnabled = isEnabled,
                    HasFault = hasFault
                };
                status.StatusText = status.GetStateText();
                lanes.Add(status);
            }

            return lanes;
        }

        /// <inheritdoc />
        public async Task<ModuleDashData> ReadModuleDashDataAsync(int laneCount = 4)
        {
            var identity = await ReadModuleIdentityAsync();
            var monitors = await ReadModuleMonitorsAsync(laneCount);
            var lanes = await ReadLaneStatusAsync(laneCount);
            var moduleStatus = await GetStatusAsync();

            return new ModuleDashData
            {
                Identity = identity,
                Monitors = monitors,
                Lanes = lanes,
                CurrentState = moduleStatus.CurrentState,
                IsReady = moduleStatus.IsReady,
                StatusTimestamp = DateTime.Now
            };
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

        private static string ReadBcdString(byte[] bytes, int decimalPlaces)
        {
            var value = 0;
            foreach (var b in bytes)
            {
                value = value * 100 + ((b >> 4) * 10) + (b & 0x0F);
            }

            if (decimalPlaces <= 0)
                return value.ToString();

            var divisor = (int)Math.Pow(10, decimalPlaces);
            var intPart = value / divisor;
            var fracPart = value % divisor;
            var format = new string('0', decimalPlaces);
            return $"{intPart}.{fracPart.ToString(format)}";
        }

        private static double ParseTemperature(byte[] bytes)
        {
            var raw = (short)(bytes[0] | (bytes[1] << 8));
            return Math.Round(raw / 256.0, 2);
        }

        private static double ParseVcc(byte[] bytes)
        {
            var raw = (ushort)(bytes[0] | (bytes[1] << 8));
            return Math.Round(raw * 100.0 / 1_000_000.0, 4);
        }

        private static double ParseCurrent(byte[] bytes, double lsbMa)
        {
            var raw = (ushort)(bytes[0] | (bytes[1] << 8));
            return Math.Round(raw * lsbMa, 3);
        }

        private static double ParsePower(byte[] bytes, double lsbMw)
        {
            var raw = (ushort)(bytes[0] | (bytes[1] << 8));
            return Math.Round(raw * lsbMw, 4);
        }

        private static string GetConnectorTypeName(byte identifier)
        {
            return identifier switch
            {
                0x1E => "QSFP-DD (76-pin)",
                0x1F => "OSFP (60-pin)",
                0x18 => "QSFP28 (38-pin)",
                0x0D => "QSFP+ (38-pin)",
                0x0C => "CFP2 (104-pin)",
                0x0B => "CFP4 (56-pin)",
                0x06 => "SFP+ (20-pin)",
                0x03 => "SFP (20-pin)",
                _    => $"Connector 0x{identifier:X2}"
            };
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
