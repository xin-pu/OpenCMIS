using System.Text;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.Simulated
{
    /// <summary>
    ///     In-memory I2C register bus simulating a CMIS optical module.
    /// </summary>
    public sealed class SimulatedI2cRegisterBus : II2cRegisterBus
    {
        internal const byte RegBankSelect = 0x7E;

        internal const byte RegPageSelect = 0x7F;

        // 3D memory: [bank, page, address] → byte
        private readonly Dictionary<(byte Bank, byte Page, byte Address), byte> _memory = new ();
        private readonly int                                                    _seed;
        private          Random                                                 _noiseRandom;
        private          bool                                                   _noiseEnabled;
        private          bool                                                   _disposed;
        private          byte                                                   _selectedBank;
        private          byte                                                   _selectedPage;

        /// <param name="profile">Module profile name.</param>
        /// <param name="seed">Deterministic seed for noise.</param>
        /// <param name="noiseEnabled">Whether noise is initially enabled.</param>
        internal SimulatedI2cRegisterBus(string profile,
                                         int    seed         = 42,
                                         bool   noiseEnabled = true)
        {
            _seed         = seed;
            _noiseRandom  = new (seed);
            _noiseEnabled = noiseEnabled;
            PopulateIdentity(profile);
        }

        public bool IsOpen { get; private set; }

        public I2cTransferCapabilities Capabilities { get; } =
            I2cTransferCapabilities.Unbounded;

        public ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
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
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!IsOpen)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            var addr = offset.Value;
            for (var i = 0; i < destination.Length; i++)
            {
                var effectiveAddr = (byte) (addr + i);
                var (bank, page) = ResolveEffectivePage(effectiveAddr);
                var key = (bank, page, effectiveAddr);

                if (!_memory.TryGetValue(key, out var value))
                {
                    // Unpopulated: return 0x00 for lower, 0xFF for upper
                    value = effectiveAddr < 0x80 ? (byte) 0x00 : (byte) 0xFF;
                }
                else if (_noiseEnabled && IsNoiseRegister(bank, page, effectiveAddr))
                    value = ApplyNoise(value, bank, page, effectiveAddr);

                destination.Span[i] = value;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask WriteAsync(I2cDeviceAddress     device,
                                    RegisterOffset       offset,
                                    ReadOnlyMemory<byte> data,
                                    CancellationToken    cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!IsOpen)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            var addr = offset.Value;

            // Track bank/page selection writes
            if (data.Length == 1)
            {
                if (addr == RegBankSelect)
                {
                    _selectedBank = data.Span[0];
                    return ValueTask.CompletedTask;
                }

                if (addr == RegPageSelect)
                {
                    _selectedPage = data.Span[0];
                    return ValueTask.CompletedTask;
                }
            }

            for (var i = 0; i < data.Length; i++)
            {
                var effectiveAddr = (byte) (addr + i);

                // Prevent writes to read-only simulator internals
                if (IsReadOnlyRegister(effectiveAddr))
                    continue;

                var (bank, page)                     = ResolveEffectivePage(effectiveAddr);
                _memory[(bank, page, effectiveAddr)] = data.Span[i];
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            IsOpen    = false;
            _disposed = true;
            await ValueTask.CompletedTask;
        }

        // ---- noise control (package-visible for tests) ----

        internal void SetNoiseEnabled(bool enabled)
        {
            _noiseEnabled = enabled;
        }

        internal void ResetNoise()
        {
            _noiseRandom = new (_seed);
        }

        // ---- helpers ----

        private (byte Bank, byte Page) ResolveEffectivePage(byte address)
        {
            // Lower page (0x00–0x7F) always maps to bank 0, page 0
            if (address <= CmisConstants.LowerPageEnd)
                return (0, 0);

            // Upper page (0x80–0xFF) maps to selected bank/page
            return (_selectedBank, _selectedPage);
        }

        private static bool IsNoiseRegister(byte bank, byte page, byte address)
        {
            // Temperature (0x0E–0x0F) and VCC (0x10–0x11) in page 0
            if (bank == 0 && page == 0)
            {
                if (address is >= CmisConstants.RegTemperatureMSB
                           and <= CmisConstants.RegTemperatureMSB + 1)
                    return true;

                if (address is >= CmisConstants.RegVccMSB
                           and <= CmisConstants.RegVccMSB + 1)
                    return true;
            }

            // Per-lane monitors in pages 0x10–0x17, addresses 0xA0–0xA5
            if (bank == 0
             && page is >= CmisConstants.FirstLanePage
                    and < CmisConstants.FirstLanePage + CmisConstants.MaxLanes)
            {
                if (address is >= CmisConstants.RegLaneTxBiasMSB
                           and <= CmisConstants.RegLaneTxBiasMSB + 1)
                    return true;

                if (address is >= CmisConstants.RegLaneTxPowerMSB
                           and <= CmisConstants.RegLaneTxPowerMSB + 1)
                    return true;

                if (address is >= CmisConstants.RegLaneRxPowerMSB
                           and <= CmisConstants.RegLaneRxPowerMSB + 1)
                    return true;
            }

            return false;
        }

        private byte ApplyNoise(byte original, byte bank, byte page, byte address)
        {
            // Only apply noise to LSB of two-byte monitor values
            // The MSB carries the sign/magnitude; jitter is in the LSB
            var isLsb = IsLsbOfMonitor(bank, page, address);
            if (!isLsb)
                return original;

            // Small signed jitter: -2..+2
            var jitter = _noiseRandom.Next(-2, 3);
            var result = original + jitter;
            return (byte) Math.Clamp(result, 0, 255);
        }

        private static bool IsLsbOfMonitor(byte bank, byte page, byte address)
        {
            // Monitor register MSBs are at even addresses; LSBs at odd addresses
            if (bank == 0 && page == 0)
            {
                if (address == CmisConstants.RegTemperatureMSB + 1
                 || address == CmisConstants.RegVccMSB         + 1)
                    return true;
            }

            if (bank == 0
             && page is >= CmisConstants.FirstLanePage
                    and < CmisConstants.FirstLanePage + CmisConstants.MaxLanes)
            {
                // TX bias MSB at A0, LSB at A1; TX power MSB at A2, LSB at A3; etc.
                if (address is CmisConstants.RegLaneTxBiasMSB  + 1
                            or CmisConstants.RegLaneTxPowerMSB + 1
                            or CmisConstants.RegLaneRxPowerMSB + 1)
                    return true;
            }

            return false;
        }

        private static bool IsReadOnlyRegister(byte address)
        {
            // Protect page/bank select registers from user corruption
            return address == RegBankSelect || address == RegPageSelect;
        }

        // ---- memory population ----

        private void PopulateIdentity(string profile)
        {
            var is1p6t    = profile.Contains("1p6t", StringComparison.OrdinalIgnoreCase);
            var laneCount = is1p6t ? 8 : 8;                       // keep 8 lanes for both by default
            SetByte(0, 0x00, CmisConstants.RegIdentifier,  0x18); // QSFP-DD
            SetByte(0, 0x00, CmisConstants.RegRevision,    0x52); // CMIS 5.2
            SetByte(0, 0x00, CmisConstants.RegStatus,      0x03); // ready + dp_ready
            SetByte(0, 0x00, CmisConstants.RegModuleState, 0x03); // ModuleReady

            // Interrupt flags: all clear
            SetByte(0, 0x00, CmisConstants.RegInterruptFlags,     0x00);
            SetByte(0, 0x00, CmisConstants.RegInterruptFlags + 1, 0x00);

            // Module flags: CDB + monitoring + state control, max data rate
            var maxRate = is1p6t ? (byte) 0x0A : (byte) 0x08;
            SetByte(0, 0x00, CmisConstants.RegModuleFlags,     0x07);
            SetByte(0, 0x00, CmisConstants.RegModuleFlags + 1, maxRate);

            // Monitor values (big-endian)
            SetTemperature(42.0); // 42.0°C baseline
            SetVcc(3.300);        // 3.300V baseline

            // Alarm/warning thresholds on dedicated threshold page 0x02
            SetTemperatureRaw(0,
                              CmisConstants.ThresholdPage,
                              CmisConstants.RegTempHighAlarmMSB,
                              70.0);
            SetTemperatureRaw(0,
                              CmisConstants.ThresholdPage,
                              CmisConstants.RegTempLowAlarmMSB,
                              -5.0);
            SetTemperatureRaw(0,
                              CmisConstants.ThresholdPage,
                              CmisConstants.RegTempHighWarnMSB,
                              65.0);
            SetTemperatureRaw(0,
                              CmisConstants.ThresholdPage,
                              CmisConstants.RegTempLowWarnMSB,
                              0.0);
            SetVccRaw(0,
                      CmisConstants.ThresholdPage,
                      CmisConstants.RegVccHighAlarmMSB,
                      3.500);
            SetVccRaw(0,
                      CmisConstants.ThresholdPage,
                      CmisConstants.RegVccLowAlarmMSB,
                      3.100);
            SetVccRaw(0,
                      CmisConstants.ThresholdPage,
                      CmisConstants.RegVccHighWarnMSB,
                      3.400);

            // Vendor page 0x01
            SetAscii(0, 0x01, CmisConstants.RegVendorNameStart, 16, "OpenCMIS-Sim");
            SetBytes(0, 0x01, CmisConstants.RegVendorOUI, [0x00, 0x11, 0x22]);
            SetAscii(0,
                     0x01,
                     CmisConstants.RegPartNumberStart,
                     16,
                     is1p6t ? "1.6T-OSFP-SIM   " : "800G-QSFPDD-SIM ");
            SetAscii(0,
                     0x01,
                     CmisConstants.RegSerialNumberStart,
                     16,
                     is1p6t ? "SIM-1P6T000001  " : "SIM-800G000001  ");
            SetBytes(0, 0x01, CmisConstants.RegHardwareRevision, [0x01, 0x00]);
            SetBytes(0, 0x01, CmisConstants.RegFirmwareRevision, [0x01, 0x00]);
            SetAscii(0, 0x01, CmisConstants.RegDateCode, 8,  "20260802");
            SetAscii(0, 0x01, CmisConstants.RegCLEICode, 10, "SIMCLEI01");

            // Per-lane pages 0x10–0x17 (lanes 0–7)
            for (var lane = 0; lane < laneCount; lane++)
            {
                var page = (byte) (CmisConstants.FirstLanePage + lane);
                SetCurrent(0, page, CmisConstants.RegLaneTxBiasMSB, 65.0);
                SetPower(0, page, CmisConstants.RegLaneTxPowerMSB, 0.708);
                SetPower(0, page, CmisConstants.RegLaneRxPowerMSB, 0.708);
                SetByte(0, page, CmisConstants.RegLaneStatusFlags, 0x01); // enabled
            }
        }

        // ---- raw byte helpers ----

        private void SetByte(byte bank, byte page, byte address, byte value)
        {
            _memory[(bank, page, address)] = value;
        }

        private void SetBytes(byte bank, byte page, byte startAddress, byte[] values)
        {
            for (var i = 0; i < values.Length; i++)
                _memory[(bank, page, (byte) (startAddress + i))] = values[i];
        }

        private void SetAscii(byte bank, byte page, byte startAddress, int maxLength, string text)
        {
            var bytes = new byte[maxLength];
            Encoding.ASCII.GetBytes(text.AsSpan(), bytes);
            SetBytes(bank, page, startAddress, bytes);
        }

        // ---- value encoding / decoding ----

        private void SetTemperature(double celsius)
        {
            SetTemperatureRaw(0, 0x00, CmisConstants.RegTemperatureMSB, celsius);
        }

        private void SetTemperatureRaw(byte bank, byte page, byte msbAddress, double celsius)
        {
            var raw = (short) Math.Round(celsius * 256.0);
            SetBytes(bank, page, msbAddress, [(byte) (raw >> 8), (byte) raw]);
        }

        private void SetVcc(double volts)
        {
            SetVccRaw(0, 0x00, CmisConstants.RegVccMSB, volts);
        }

        private void SetVccRaw(byte bank, byte page, byte msbAddress, double volts)
        {
            var raw = (ushort) Math.Round(volts * 10_000.0);
            SetBytes(bank, page, msbAddress, [(byte) (raw >> 8), (byte) raw]);
        }

        private void SetCurrent(byte bank, byte page, byte msbAddress, double mA)
        {
            var raw = (ushort) Math.Round(mA / 2e-3);
            SetBytes(bank, page, msbAddress, [(byte) (raw >> 8), (byte) raw]);
        }

        private void SetPower(byte bank, byte page, byte msbAddress, double mW)
        {
            var raw = (ushort) Math.Round(mW / 1e-4);
            SetBytes(bank, page, msbAddress, [(byte) (raw >> 8), (byte) raw]);
        }
    }
}
