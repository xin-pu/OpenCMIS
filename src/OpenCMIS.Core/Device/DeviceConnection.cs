namespace OpenCMIS.Core;

/// <summary>
/// Provides base implementation for device connection.
/// </summary>
public abstract class DeviceConnection : IDeviceConnection
{
    private bool _disposed;

    /// <inheritdoc/>
    public abstract bool IsConnected { get; }

    /// <inheritdoc/>
    public abstract Task<bool> OpenAsync();

    /// <inheritdoc/>
    public abstract Task CloseAsync();

    /// <inheritdoc/>
    public abstract Task<byte[]> ReadAsync(int length);

    /// <inheritdoc/>
    public abstract Task WriteAsync(byte[] data);

    /// <summary>
    /// Releases the unmanaged resources used by the DeviceConnection.
    /// </summary>
    /// <param name="disposing">True if called from Dispose method; false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // TODO: Dispose managed resources
            }

            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

