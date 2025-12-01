namespace OpenCMIS.Core;

/// <summary>
/// Provides interface for device connection operations.
/// </summary>
public interface IDeviceConnection : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the device is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Opens the connection to the device.
    /// </summary>
    /// <returns>True if the connection was opened successfully; otherwise, false.</returns>
    Task<bool> OpenAsync();

    /// <summary>
    /// Closes the connection to the device.
    /// </summary>
    Task CloseAsync();

    /// <summary>
    /// Reads data from the device.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The byte array containing the read data.</returns>
    Task<byte[]> ReadAsync(int length);

    /// <summary>
    /// Writes data to the device.
    /// </summary>
    /// <param name="data">The byte array containing the data to write.</param>
    Task WriteAsync(byte[] data);
}

