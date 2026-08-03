using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C
{
    /// <summary>
    ///     Bridges the original register transport API to the address-aware I2C bus.
    /// </summary>
    [Obsolete(
            "Use II2cRegisterBus and a typed I2cConnectionProfile. " +
            "This compatibility adapter will be removed in a future release.")]
    public abstract class LegacyRegisterTransportAdapter : IRegisterTransport
    {
        private readonly II2cRegisterBus  _inner;
        private readonly I2cDeviceAddress _device;
        private          bool             _disposed;

        protected LegacyRegisterTransportAdapter(II2cRegisterBus inner,
                                                 byte            legacySlaveAddress)
        {
            _inner  = inner ?? throw new ArgumentNullException(nameof(inner));
            _device = I2cDeviceAddress.FromLegacy8Bit(legacySlaveAddress);
        }

        public bool IsConnected => !_disposed && _inner.IsOpen;

        public async Task<bool> OpenAsync()
        {
            ThrowIfDisposed();
            await _inner.OpenAsync().ConfigureAwait(false);
            return _inner.IsOpen;
        }

        public Task CloseAsync()
        {
            ThrowIfDisposed();
            return _inner.CloseAsync().AsTask();
        }

        public async Task<byte> ReadRegisterAsync(byte registerAddress)
        {
            var data = await ReadRegisterBlockAsync(registerAddress, 1)
                              .ConfigureAwait(false);
            return data[0];
        }

        public async Task WriteRegisterAsync(byte registerAddress, byte value)
        {
            await WriteRegisterBlockAsync(registerAddress, [value])
                   .ConfigureAwait(false);
        }

        public async Task<byte[]> ReadRegisterBlockAsync(byte registerAddress,
                                                         int  length)
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
            var data = new byte[length];
            await _inner.ReadAsync(
                                 _device,
                                 new (registerAddress),
                                 data)
                        .ConfigureAwait(false);
            return data;
        }

        public Task WriteRegisterBlockAsync(byte   registerAddress,
                                            byte[] data)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(data);
            return _inner.WriteAsync(
                                  _device,
                                  new (registerAddress),
                                  data)
                         .AsTask();
        }

        public Task<byte[]> ReadAsync(int length)
        {
            throw new NotSupportedException(
                    "Raw serial reads are not part of II2cRegisterBus.");
        }

        public Task WriteAsync(byte[] data)
        {
            throw new NotSupportedException(
                    "Raw serial writes are not part of II2cRegisterBus.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _inner.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
