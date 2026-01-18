namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Defines the addressing modes supported by CMIS protocol.
    /// </summary>
    public enum AddressingMode
    {
        /// <summary>
        ///     Standard 128-byte page-based addressing.
        /// </summary>
        StandardPaged = 0,

        /// <summary>
        ///     Flat 256-byte addressing.
        /// </summary>
        Flat256 = 1
    }
}
