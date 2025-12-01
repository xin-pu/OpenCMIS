namespace OpenCMIS.Core;

/// <summary>
/// Provides interface for writing Configuration Data Blocks.
/// </summary>
public interface ICdbWriter
{
    /// <summary>
    /// Writes a CDB to the specified device.
    /// </summary>
    /// <param name="device">The CMIS device.</param>
    /// <param name="cdb">The configuration data block to write.</param>
    Task WriteAsync(ICmisDevice device, ConfigurationDataBlock cdb);
}

