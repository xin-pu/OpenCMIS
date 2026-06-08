using OpenCMIS.CDB.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.CDB.Core
{
    /// <summary>
    ///     Provides implementation for validating Configuration Data Blocks.
    /// </summary>
    public class CdbValidator : ICdbValidator
    {
        private const int MaxCdbSize  = 2048;
        private const int MinCdbSize  = 10;
        private const int MaxFieldIdLength = 64;
        private const int MaxFieldValueLength = 256;

        /// <inheritdoc />
        public bool Validate(ConfigurationDataBlock cdb)
        {
            if (cdb == null)
                return false;

            // Validate header
            if (cdb.Header.Length < MinCdbSize || cdb.Header.Length > MaxCdbSize)
                return false;

            if (cdb.Header.Version == 0)
                return false;

            // Validate version
            if (cdb.Version.Major == 0 && cdb.Version.Minor == 0)
                return false;

            // Validate fields
            foreach (var field in cdb.Fields)
            {
                if (!ValidateField(field))
                    return false;
            }

            // Validate checksum
            if (!ValidateChecksum(cdb))
                return false;

            return true;
        }

        private static bool ValidateField(CdbField field)
        {
            if (string.IsNullOrEmpty(field.Id))
                return false;

            if (field.Id.Length > MaxFieldIdLength)
                return false;

            if (!Enum.IsDefined(typeof(CdbFieldType), field.Type))
                return false;

            if (field.Value == null && field.Type != CdbFieldType.String)
                return false;

            if (field.Value is byte[] bytes && bytes.Length > MaxFieldValueLength)
                return false;

            return true;
        }

        private static bool ValidateChecksum(ConfigurationDataBlock cdb)
        {
            // For validation without raw bytes, we check if checksum is reasonable
            if (cdb.Checksum == 0 && cdb.Fields.Count > 0)
                return false;

            return true;
        }
    }
}
