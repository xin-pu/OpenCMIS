namespace OpenCMIS.CDB.Abstractions
{
    /// <summary>
    ///     Represents the CDB version.
    /// </summary>
    public class CdbVersion
    {
        /// <summary>
        ///     Gets or sets the major version number.
        /// </summary>
        public byte Major { get; set; }

        /// <summary>
        ///     Gets or sets the minor version number.
        /// </summary>
        public byte Minor { get; set; }
    }
}
