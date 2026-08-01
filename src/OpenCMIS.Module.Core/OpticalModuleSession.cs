using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Module.Core;

/// <summary>
/// Owns one optical-module bus and its shared synchronization boundary.
/// </summary>
public sealed class OpticalModuleSession : IAsyncDisposable
{
    private readonly II2cRegisterBus _bus;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public OpticalModuleSession(II2cRegisterBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public bool IsOpen => !_disposed && _bus.IsOpen;

    public async ValueTask OpenAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _bus.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask CloseAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _bus.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async ValueTask<T> ExecuteAsync<T>(
        Func<II2cRegisterBus, CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            if (!_bus.IsOpen)
            {
                throw new CmisException(CmisErrorCode.DeviceNotConnected);
            }

            return await operation(_bus, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            await _bus.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
