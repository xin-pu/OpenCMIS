namespace OpenCMIS.CDB.Abstractions
{
    /// <summary>
    ///     Represents the CDB header.
    /// </summary>
    public class CdbHeader
    {
        /// <summary>
        ///     Gets or sets the header length.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        ///     Gets or sets the header version.
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        ///     Gets or sets the header flags.
        /// </summary>
        public byte Flags { get; set; }
    }
}