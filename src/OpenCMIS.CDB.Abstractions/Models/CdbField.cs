using OpenCMIS.Shared;

namespace OpenCMIS.CDB.Abstractions
{
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
}
