namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides interface for reading Configuration Data Blocks.
    /// </summary>
    public interface ICdbReader
    {
        /// <summary>
        ///     Reads a CDB from the specified device.
        /// </summary>
        /// <param name="device">The CMIS device.</param>
        /// <returns>The configuration data block.</returns>
        Task<ConfigurationDataBlock> ReadAsync(ICmisDevice device);
    }
}