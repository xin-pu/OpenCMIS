namespace OpenCMIS.CDB.Abstractions
{
    /// <summary>
    ///     Represents a Configuration Data Block (CDB).
    /// </summary>
    public class ConfigurationDataBlock
    {
        /// <summary>
        ///     Gets or sets the CDB header information.
        /// </summary>
        public CdbHeader Header { get; set; } = new ();

        /// <summary>
        ///     Gets or sets the collection of CDB fields.
        /// </summary>
        public ICollection<CdbField> Fields { get; set; } = new List<CdbField>();

        /// <summary>
        ///     Gets or sets the checksum value.
        /// </summary>
        public ushort Checksum { get; set; }

        /// <summary>
        ///     Gets or sets the CDB version.
        /// </summary>
        public CdbVersion Version { get; set; } = new ();
    }
}
