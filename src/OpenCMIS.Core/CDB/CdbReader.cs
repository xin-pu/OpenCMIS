namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides implementation for reading Configuration Data Blocks.
    /// </summary>
    public class CdbReader : ICdbReader
    {
        /// <inheritdoc />
        public async Task<ConfigurationDataBlock> ReadAsync(ICmisDevice device)
        {
            // TODO: Implement CDB reading logic
            return await Task.FromResult(new ConfigurationDataBlock());
        }
    }
}