namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides implementation for writing Configuration Data Blocks.
    /// </summary>
    public class CdbWriter : ICdbWriter
    {
        /// <inheritdoc />
        public async Task WriteAsync(ICmisDevice device, ConfigurationDataBlock cdb)
        {
            // TODO: Implement CDB writing logic
            await Task.CompletedTask;
        }
    }
}