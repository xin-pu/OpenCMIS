namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides interface for validating Configuration Data Blocks.
    /// </summary>
    public interface ICdbValidator
    {
        /// <summary>
        ///     Validates a CDB.
        /// </summary>
        /// <param name="cdb">The configuration data block to validate.</param>
        /// <returns>True if the CDB is valid; otherwise, false.</returns>
        bool Validate(ConfigurationDataBlock cdb);
    }
}