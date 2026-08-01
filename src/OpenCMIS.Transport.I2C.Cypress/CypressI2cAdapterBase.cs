using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Cypress;

public abstract class CypressI2cAdapterBase : II2cRegisterBus
{
    private readonly ICypressDeviceApi _api;
    private readonly int _port;
    private readonly int _speedKhz;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private bool _disposed;

    protected CypressI2cAdapterBase(
        ICypressDeviceApi api,
        int port,
        int speedKhz,
        I2cTransferCapabilities capabilities)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        ArgumentOutOfRangeException.ThrowIfNegative(port);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speedKhz);
        Capabilities = capabilities ??
                       throw new ArgumentNullException(nameof(capabilities));
        _port = port;
        _speedKhz = speedKhz;
    }

    public bool IsOpen { get; private set; }

    public I2cTransferCapabilities Capabilities { get; }

    public ValueTask OpenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        IsOpen = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsOpen = false;
        return ValueTask.CompletedTask;
    }

    public async ValueTask ReadAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidateRange(offset, destination.Length, nameof(destination));

        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var completed = 0;
            while (completed < destination.Length)
            {
                var chunkLength = Math.Min(
                    Capabilities.MaxReadLength,
                    destination.Length - completed);
                var chunkOffset = new RegisterOffset(
                    (byte)(offset.Value + completed));

                await ExecuteTransferAsync(
                        () => _api.Write(
                            _port,
                            _speedKhz,
                            device.ToWriteAddress8Bit(),
                            [chunkOffset.Value]),
                        cancellationToken)
                    .ConfigureAwait(false);

                byte[] data = [];
                await ExecuteTransferAsync(
                        () => _api.Read(
                            _port,
                            _speedKhz,
                            device.ToWriteAddress8Bit(),
                            chunkLength,
                            out data),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (data.Length != chunkLength)
                {
                    throw new CmisException(
                        CmisErrorCode.I2cTransferFailed,
                        $"Cypress returned {data.Length} bytes; expected {chunkLength}.");
                }

                data.CopyTo(destination[completed..]);
                completed += chunkLength;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async ValueTask WriteAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidateRange(offset, data.Length, nameof(data));

        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var completed = 0;
            while (completed < data.Length)
            {
                var chunkLength = Math.Min(
                    Capabilities.MaxWriteLength,
                    data.Length - completed);
                var chunkOffset = new RegisterOffset(
                    (byte)(offset.Value + completed));
                var frame = new byte[chunkLength + 1];
                frame[0] = chunkOffset.Value;
                data.Slice(completed, chunkLength).CopyTo(frame.AsMemory(1));

                await ExecuteTransferAsync(
                        () => _api.Write(
                            _port,
                            _speedKhz,
                            device.ToWriteAddress8Bit(),
                            frame),
                        cancellationToken)
                    .ConfigureAwait(false);

                completed += chunkLength;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            IsOpen = false;
            _api.Close();
            await _api.DisposeAsync().ConfigureAwait(false);
            _operationGate.Dispose();
            _disposed = true;
        }
    }

    private static void ValidateRange(
        RegisterOffset offset,
        int length,
        string parameterName)
    {
        if (length <= 0)
        {
            throw new ArgumentException(
                "Transfer buffer cannot be empty.",
                parameterName);
        }

        if (offset.Value + length > 256)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                length,
                "Transfer exceeds the eight-bit register address space.");
        }
    }

    private static async Task ExecuteTransferAsync(
        Func<bool> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var succeeded = await Task.Run(operation, CancellationToken.None)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!succeeded)
            {
                throw new CmisException(CmisErrorCode.I2cTransferFailed);
            }
        }
        catch (CmisException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CmisException(
                CmisErrorCode.I2cTransferFailed,
                exception.Message,
                exception);
        }
    }

    private void EnsureReady()
    {
        ThrowIfDisposed();
        if (!IsOpen)
        {
            throw new CmisException(CmisErrorCode.DeviceNotConnected);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
