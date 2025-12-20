namespace OpenCMIS.Core
{
    /// <summary>
    ///     Represents a Configuration Data Block (CDB).
    /// </summary>
    public class ConfigurationDataBlock
    {
        /// <summary>
        ///     Gets or sets the CDB header information.
        /// </summary>
        public CdbHeader Header { get; set; } = new();

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
        public CdbVersion Version { get; set; } = new();
    }

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

    /// <summary>
    ///     Represents a CDB field.
    /// </summary>
    public class CdbField
    {
        /// <summary>
        ///     Gets or sets the field identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the field value.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        ///     Gets or sets the field type.
        /// </summary>
        public CdbFieldType Type { get; set; }
    }

    /// <summary>
    ///     Defines the types of CDB fields.
    /// </summary>
    public enum CdbFieldType
    {
        /// <summary>
        ///     Byte field type.
        /// </summary>
        Byte = 0,

        /// <summary>
        ///     Word (16-bit) field type.
        /// </summary>
        Word = 1,

        /// <summary>
        ///     DWord (32-bit) field type.
        /// </summary>
        DWord = 2,

        /// <summary>
        ///     String field type.
        /// </summary>
        String = 3
    }

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