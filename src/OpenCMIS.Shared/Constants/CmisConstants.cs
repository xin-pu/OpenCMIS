namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Defines constants used in CMIS protocol.
    /// </summary>
    /// <remarks>
    ///     Spec traceability (CMIS 5.2 / OIF-CMIS-05.2):
    ///     Page  | Address       | Field                        | Source
    ///     ----- | ------------- | ---------------------------- | ------
    ///     0x00  | 0x00          | Module Identifier            | Table 8-1
    ///     0x00  | 0x01          | CMIS Revision                | Table 8-1
    ///     0x00  | 0x02          | Module Status                | Table 8-2
    ///     0x00  | 0x03          | Module State                 | Table 8-2
    ///     0x00  | 0x04-0x05     | Interrupt Flags              | Table 8-3
    ///     0x00  | 0x06-0x07     | Module Flags                 | Table 8-4
    ///     0x00  | 0x0E-0x0F     | Module Temperature           | Table 8-6 (Monitors)
    ///     0x00  | 0x10-0x11     | Module VCC                   | Table 8-6 (Monitors)
    ///     0x00  | 0x7F          | Page Select Byte             | Table 8-7
    ///     0x01  | 0x81-0x90     | Vendor Name                  | Table 8-8 (Identity)
    ///     0x01  | 0x90-0x92     | Vendor OUI                   | Table 8-8 (Identity)
    ///     0x01  | 0x94-0xA3     | Vendor Part Number           | Table 8-8 (Identity)
    ///     0x01  | 0xB0-0xB1     | Hardware Revision (BCD)      | Vendor ext. (not in base 5.2)
    ///     0x01  | 0xB2-0xB3     | Firmware Revision (BCD)      | Vendor ext.
    ///     0x01  | 0xB4-0xBB     | Date Code (ASCII)            | Vendor ext.
    ///     0x01  | 0xBC-0xC5     | CLEI Code (ASCII)            | Vendor ext.
    ///     0x01  | 0xC6-0xD5     | Serial Number (ASCII)        | Project-local (avoids overlap)
    ///     0x02  | 0x80-0x8D     | Alarm/Warning Thresholds     | Table 8-12 (Module Thresholds)
    ///     0x10+ | 0xA0-0xA6     | Per-Lane Monitors            | Table 8-18 (Lane Monitors)
    ///     NOTE: CMIS 5.2 standard places Vendor Serial Number at upper page
    ///     0x01, address 0xA8 (16 bytes). This project uses custom vendor
    ///     extensions at 0xB0-0xC5 that would overlap the standard serial
    ///     location, so the serial is placed at 0xC6 as a project-local
    ///     accommodation. Threshold constants use page 0x02 at upper-page
    ///     addresses 0x80-0x8C, consistent with CMIS 5.2 Table 8-12.
    ///     Vendor extensions (HW/FW rev, date code, CLEI) are carried at
    ///     addresses that do not conflict with base CMIS identity fields.
    ///     These should be verified against vendor-specific memory maps
    ///     when real hardware is available.
    /// </remarks>
    public static class CmisConstants
    {
        /// <summary>
        ///     CMIS protocol version 5.2.
        /// </summary>
        public const string CmisVersion52 = "5.2";

        /// <summary>
        ///     CMIS protocol version 5.3.
        /// </summary>
        public const string CmisVersion53 = "5.3";

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

        /// <summary>
        ///     Default I2C address for CMIS modules.
        /// </summary>
        public const byte DefaultI2cAddress = 0x50;

        /// <summary>
        ///     Default timeout for device operations in milliseconds.
        /// </summary>
        public const int DefaultTimeoutMs = 1000;

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

        #region Alarm / Warning Thresholds (Upper Page 0x02)
        /// <summary>Upper page for module-level alarm/warning thresholds.</summary>
        public const byte ThresholdPage = 0x02;

        /// <summary>Temperature High Alarm threshold (signed int16, /256).</summary>
        public const byte RegTempHighAlarmMSB = 0x80;

        /// <summary>Temperature Low Alarm threshold (signed int16, /256).</summary>
        public const byte RegTempLowAlarmMSB = 0x82;

        /// <summary>Temperature High Warning threshold (signed int16, /256).</summary>
        public const byte RegTempHighWarnMSB = 0x84;

        /// <summary>Temperature Low Warning threshold (signed int16, /256).</summary>
        public const byte RegTempLowWarnMSB = 0x86;

        /// <summary>VCC High Alarm threshold (unsigned int16, 100µV).</summary>
        public const byte RegVccHighAlarmMSB = 0x88;

        /// <summary>VCC Low Alarm threshold (unsigned int16, 100µV).</summary>
        public const byte RegVccLowAlarmMSB = 0x8A;

        /// <summary>VCC High Warning threshold (unsigned int16, 100µV).</summary>
        public const byte RegVccHighWarnMSB = 0x8C;
        #endregion

        /// <summary>Vendor name start, upper page 0x01 (0x81, 16 bytes).</summary>
        public const byte RegVendorNameStart = 0x81;

        /// <summary>Vendor OUI, upper page 0x01 (0x90, 3 bytes).</summary>
        public const byte RegVendorOUI = 0x90;

        /// <summary>Part number start, upper page 0x01 (0x94, 16 bytes).</summary>
        public const byte RegPartNumberStart = 0x94;

        /// <summary>
        ///     Serial number start, upper page 0x01 (0xC6, 16 bytes).
        ///     NOTE: Moved from 0xA0 to 0xA8 (conflict with Vendor Part Number),
        ///     then to 0xC6 to avoid overlap with Hardware/Firmware Revision
        ///     (0xB0-0xB3), Date Code (0xB4-0xBB), and CLEI Code (0xBC-0xC5).
        /// </summary>
        public const byte RegSerialNumberStart = 0xC6;

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

        /// <summary>Maximum supported lanes (CMIS 5.3 supports up to 16 lanes for 1.6T modules).</summary>
        public const byte MaxLanes = 16;

        /// <summary>Default lane count used when actual count cannot be determined.</summary>
        public const byte DefaultLaneCount = 8;

        /// <summary>Per-lane TX bias monitor (2 bytes, unsigned int16, LSB=2µA).</summary>
        public const byte RegLaneTxBiasMSB = 0xA0;

        /// <summary>Per-lane TX power monitor (2 bytes, unsigned int16, LSB=0.1µW).</summary>
        public const byte RegLaneTxPowerMSB = 0xA2;

        /// <summary>Per-lane RX power monitor (2 bytes, unsigned int16, LSB=0.1µW).</summary>
        public const byte RegLaneRxPowerMSB = 0xA4;

        /// <summary>Per-lane status flags (1 byte, bit0=enabled, bit1=fault).</summary>
        public const byte RegLaneStatusFlags = 0xA6;

        /// <summary>
        ///     Media lane count register (Page 0x00, address 0x70).
        ///     Indicates the number of media lanes supported by the module.
        ///     CMIS 5.3 defines this for dynamic lane detection.
        /// </summary>
        public const byte RegMediaLaneCount = 0x70;
        #endregion

        #region VDM Registers (CMIS 5.2, Versatile Diagnostics Monitor, Pages 0x20-0x2F)

        /// <summary>CMIS Page 01h, where general VDM support is advertised.</summary>
        public const byte VdmCapabilityPage = 0x01;

        /// <summary>Page 01h byte 142 (0x8E), containing the VDM capability bit.</summary>
        public const byte VdmCapabilityByte = 0x8E;

        /// <summary>Bit 6 of Page 01h byte 142 indicates VDM support.</summary>
        public const byte VdmCapabilityBit = 0x40;

        /// <summary>First and last CMIS VDM descriptor pages (20h-23h).</summary>
        public const byte VdmDescriptorPageStart = 0x20;
        public const byte VdmDescriptorPageEnd = 0x23;

        /// <summary>First and last CMIS VDM sample pages (24h-27h).</summary>
        public const byte VdmSamplePageStart = 0x24;
        public const byte VdmSamplePageEnd = 0x27;

        /// <summary>CMIS VDM flags page (2Ch).</summary>
        public const byte VdmFlagsPage = 0x2C;

        /// <summary>First upper-page byte containing VDM descriptor/sample slots.</summary>
        public const byte VdmObservableOffset = 0x80;

        /// <summary>Descriptor and sample slot width in bytes.</summary>
        public const byte VdmObservableSlotSize = 2;

        // --- VDM Configuration Page (0x20) ---
        /// <summary>VDM configuration and control page.</summary>
        public const byte VdmConfigPage = 0x20;

        /// <summary>VDM control register (0x80-0x81, 2 bytes: enable/disable, averaging control).</summary>
        public const byte RegVdmControl = 0x80;

        /// <summary>VDM status register (0x82-0x83, 2 bytes: active, error, data-ready).</summary>
        public const byte RegVdmStatus = 0x82;

        /// <summary>VDM monitoring period (0x84, 1 byte, ms).</summary>
        public const byte RegVdmMonitorPeriod = 0x84;

        /// <summary>VDM averaging time (0x85-0x86, 2 bytes, ms).</summary>
        public const byte RegVdmAveragingTime = 0x85;

        /// <summary>Number of media lanes for VDM reporting (0x87, 1 byte).</summary>
        public const byte RegVdmMediaLaneCount = 0x87;

        /// <summary>Laser age in hours (0x88-0x89, 2 bytes, optional).</summary>
        public const byte RegVdmLaserAge = 0x88;

        /// <summary>VDM group capabilities bitmap (0x8A-0x8D, 4 bytes).</summary>
        public const byte RegVdmGroupCapabilities = 0x8A;

        // --- VDM Module Monitors Page (0x21) ---
        /// <summary>VDM module-level monitors page.</summary>
        public const byte VdmModulePage = 0x21;

        /// <summary>Module VDM alarm/warning flags (0x80-0x81, 2 bytes).</summary>
        public const byte RegVdmFlags = 0x80;

        /// <summary>VDM module temperature (0x82-0x83, 2 bytes, signed int16 /256 °C).</summary>
        public const byte RegVdmTemp = 0x82;

        /// <summary>VDM primary VCC (0x84-0x85, 2 bytes, unsigned int16, 100 µV).</summary>
        public const byte RegVdmVccPrimary = 0x84;

        /// <summary>VDM secondary VCC (0x86-0x87, 2 bytes, unsigned int16, 100 µV, optional).</summary>
        public const byte RegVdmVccSecondary = 0x86;

        /// <summary>VDM laser temperature (0x88-0x89, 2 bytes, signed int16 /256 °C, optional).</summary>
        public const byte RegVdmLaserTemp = 0x88;

        /// <summary>VDM TEC current (0x8A-0x8B, 2 bytes, optional).</summary>
        public const byte RegVdmTecCurrent = 0x8A;

        // --- Per-Lane VDM Pages (0x22-0x2F) ---
        /// <summary>First VDM per-lane page (2 pages per lane, up to 8 lanes).</summary>
        public const byte VdmFirstLanePage = 0x22;

        /// <summary>Maximum number of VDM-supported lanes.</summary>
        public const byte VdmMaxLanes = 8;

        /// <summary>Per-lane VDM flags (0x80-0x81, 2 bytes).</summary>
        public const byte RegVdmLaneFlags = 0x80;

        /// <summary>Per-lane TX optical power (0x82-0x83, 2 bytes, unsigned int16, LSB=0.1 µW).</summary>
        public const byte RegVdmLaneTxPower = 0x82;

        /// <summary>Per-lane RX optical power (0x84-0x85, 2 bytes, unsigned int16, LSB=0.1 µW).</summary>
        public const byte RegVdmLaneRxPower = 0x84;

        /// <summary>Per-lane TX bias current (0x86-0x87, 2 bytes, unsigned int16, LSB=2 µA).</summary>
        public const byte RegVdmLaneTxBias = 0x86;

        /// <summary>Per-lane TX laser temperature (0x88-0x89, 2 bytes, optional).</summary>
        public const byte RegVdmLaneTxLaserTemp = 0x88;

        /// <summary>Per-lane RX LOS counter (0x8A-0x8B, 2 bytes).</summary>
        public const byte RegVdmLaneRxLosCounter = 0x8A;

        // --- Per-Lane FEC Statistics (odd pages: 0x23, 0x25, ..., 0x2F) ---
        /// <summary>FEC corrected codewords low word (0x80-0x81, 2 bytes).</summary>
        public const byte RegVdmFecCorrectedLo = 0x80;

        /// <summary>FEC corrected codewords high word (0x82-0x83, 2 bytes).</summary>
        public const byte RegVdmFecCorrectedHi = 0x82;

        /// <summary>FEC uncorrectable codewords (0x84-0x85, 2 bytes).</summary>
        public const byte RegVdmFecUncorrectable = 0x84;

        /// <summary>FEC symbol errors count (0x86-0x87, 2 bytes).</summary>
        public const byte RegVdmFecSymbolErrors = 0x86;

        /// <summary>Pre-FEC BER mantissa (0x88-0x89, 2 bytes).</summary>
        public const byte RegVdmFecPreBerMantissa = 0x88;

        /// <summary>Pre-FEC BER exponent (0x8A, 1 byte).</summary>
        public const byte RegVdmFecPreBerExponent = 0x8A;

        #endregion
    }
}
