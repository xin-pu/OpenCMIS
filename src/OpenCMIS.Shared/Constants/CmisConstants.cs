namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Defines constants used in CMIS protocol.
    /// </summary>
    public static class CmisConstants
    {
        /// <summary>
        ///     CMIS protocol version 5.2.
        /// </summary>
        public const string CmisVersion52 = "5.2";

        /// <summary>
        ///     Lower page address range start (0x00).
        /// </summary>
        public const byte LowerPageStart = 0x00;

        /// <summary>
        ///     Lower page address range end (0x7F).
        /// </summary>
        public const byte LowerPageEnd = 0x7F;

        /// <summary>
        ///     Upper page address range start (0x80).
        /// </summary>
        public const byte UpperPageStart = 0x80;

        /// <summary>
        ///     Upper page address range end (0xFF).
        /// </summary>
        public const byte UpperPageEnd = 0xFF;

        /// <summary>
        ///     Page select register address (0x7F).
        /// </summary>
        public const byte PageSelectRegister = 0x7F;

        #region Common Register Addresses (Lower Page)

        /// <summary>
        ///     Identifier register (0x00).
        /// </summary>
        public const byte RegIdentifier = 0x00;

        /// <summary>
        ///     Revision register (0x01).
        /// </summary>
        public const byte RegRevision = 0x01;

        /// <summary>
        ///     Status register (0x02).
        /// </summary>
        public const byte RegStatus = 0x02;

        /// <summary>
        ///     Module state register (0x03).
        /// </summary>
        public const byte RegModuleState = 0x03;

        /// <summary>
        ///     Interrupt flags (0x04-0x05).
        /// </summary>
        public const byte RegInterruptFlags = 0x04;

        /// <summary>
        ///     Module flags (0x06-0x07).
        /// </summary>
        public const byte RegModuleFlags = 0x06;

        #endregion

        #region Monitor and Identity Registers

        /// <summary>Temperature monitor, lower page 0x00 (0x0E-0x0F, 2 bytes, signed int16, LSB=1/256°C).</summary>
        public const byte RegTemperatureMSB = 0x0E;

        /// <summary>VCC monitor, lower page 0x00 (0x10-0x11, 2 bytes, unsigned int16, LSB=100µV).</summary>
        public const byte RegVccMSB = 0x10;

        #region Alarm / Warning Thresholds (Lower Page 0x00)

        /// <summary>Temperature High Alarm threshold (signed int16, /256).</summary>
        public const byte RegTempHighAlarmMSB = 0x00;

        /// <summary>Temperature Low Alarm threshold (signed int16, /256).</summary>
        public const byte RegTempLowAlarmMSB = 0x02;

        /// <summary>Temperature High Warning threshold (signed int16, /256).</summary>
        public const byte RegTempHighWarnMSB = 0x04;

        /// <summary>Temperature Low Warning threshold (signed int16, /256).</summary>
        public const byte RegTempLowWarnMSB = 0x06;

        /// <summary>VCC High Alarm threshold (unsigned int16, 100µV).</summary>
        public const byte RegVccHighAlarmMSB = 0x08;

        /// <summary>VCC Low Alarm threshold (unsigned int16, 100µV).</summary>
        public const byte RegVccLowAlarmMSB = 0x0A;

        /// <summary>VCC High Warning threshold (unsigned int16, 100µV).</summary>
        public const byte RegVccHighWarnMSB = 0x0C;

        #endregion

        /// <summary>Vendor name start, upper page 0x01 (0x81, 16 bytes).</summary>
        public const byte RegVendorNameStart = 0x81;

        /// <summary>Vendor OUI, upper page 0x01 (0x90, 3 bytes).</summary>
        public const byte RegVendorOUI = 0x90;

        /// <summary>Part number start, upper page 0x01 (0x94, 16 bytes).</summary>
        public const byte RegPartNumberStart = 0x94;

        /// <summary>Serial number start, upper page 0x01 (0xA0, 16 bytes).</summary>
        public const byte RegSerialNumberStart = 0xA0;

        /// <summary>Hardware revision, upper page 0x01 (0xB0, 2 bytes BCD).</summary>
        public const byte RegHardwareRevision = 0xB0;

        /// <summary>Firmware revision, upper page 0x01 (0xB2, 2 bytes BCD).</summary>
        public const byte RegFirmwareRevision = 0xB2;

        /// <summary>Date code, upper page 0x01 (0xB4, 8 bytes ASCII).</summary>
        public const byte RegDateCode = 0xB4;

        /// <summary>CLEI code, upper page 0x01 (0xBC, 10 bytes ASCII).</summary>
        public const byte RegCLEICode = 0xBC;

        /// <summary>First per-lane upper page number.</summary>
        public const byte FirstLanePage = 0x10;

        /// <summary>Maximum supported lanes.</summary>
        public const byte MaxLanes = 8;

        /// <summary>Per-lane TX bias monitor (2 bytes, unsigned int16, LSB=2µA).</summary>
        public const byte RegLaneTxBiasMSB = 0xA0;

        /// <summary>Per-lane TX power monitor (2 bytes, unsigned int16, LSB=0.1µW).</summary>
        public const byte RegLaneTxPowerMSB = 0xA2;

        /// <summary>Per-lane RX power monitor (2 bytes, unsigned int16, LSB=0.1µW).</summary>
        public const byte RegLaneRxPowerMSB = 0xA4;

        /// <summary>Per-lane status flags (1 byte, bit0=enabled, bit1=fault).</summary>
        public const byte RegLaneStatusFlags = 0xA6;

        #endregion

        /// <summary>
        ///     Default I2C address for CMIS modules.
        /// </summary>
        public const byte DefaultI2cAddress = 0x50;

        /// <summary>
        ///     Default timeout for device operations in milliseconds.
        /// </summary>
        public const int DefaultTimeoutMs = 1000;
    }
}