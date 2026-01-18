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