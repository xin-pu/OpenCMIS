namespace OpenCMIS.Core
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
        ///     Default I2C address for CMIS modules.
        /// </summary>
        public const byte DefaultI2cAddress = 0x50;

        /// <summary>
        ///     Default timeout for device operations in milliseconds.
        /// </summary>
        public const int DefaultTimeoutMs = 1000;
    }
}