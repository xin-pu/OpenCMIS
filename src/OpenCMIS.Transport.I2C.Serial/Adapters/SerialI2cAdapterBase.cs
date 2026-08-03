using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Adapters
{
    /// <summary>
    ///     Provides segmented, short-lived serial sessions for one bridge protocol.
    /// </summary>
    public abstract class SerialI2cAdapterBase : II2cRegisterBus
    {
        private readonly ISerialSessionFactory _sessionFactory;
        private readonly SerialTransferRetry   _retry;
        private readonly SemaphoreSlim         _operationGate = new (1, 1);
        private          bool                  _disposed;

        protected SerialI2cAdapterBase(ISerialSessionFactory sessionFactory,
                                       string                portName,
                                       int                   baudRate,
                                       I2cRetryOptions       retryOptions,
                                       TimeProvider          timeProvider)
        {
            _sessionFactory = sessionFactory ??
                              throw new ArgumentNullException(nameof(sessionFactory));
            ArgumentException.ThrowIfNullOrWhiteSpace(portName);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);
            ArgumentNullException.ThrowIfNull(retryOptions);
            ArgumentNullException.ThrowIfNull(timeProvider);

            PortName = portName;
            BaudRate = baudRate;
            _retry   = new (retryOptions, timeProvider);
        }

        protected string PortName { get; }

        protected int BaudRate { get; }

        public bool IsOpen { get; private set; }

        public I2cTransferCapabilities Capabilities { get; } = new (255, 255);

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

        public async ValueTask ReadAsync(I2cDeviceAddress  device,
                                         RegisterOffset    offset,
                                         Memory<byte>      destination,
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
                    var chunkOffset = new RegisterOffset((byte) (offset.Value + completed));
                    var chunk = await _retry.ExecuteAsync(
                                                     token => ReadChunkAsync(device, chunkOffset, chunkLength, token),
                                                     cancellationToken)
                                            .ConfigureAwait(false);
                    chunk.CopyTo(destination[completed..]);
                    completed += chunkLength;
                }
            }
            finally
            {
                _operationGate.Release();
            }
        }

        public async ValueTask WriteAsync(I2cDeviceAddress     device,
                                          RegisterOffset       offset,
                                          ReadOnlyMemory<byte> data,
                                          CancellationToken    cancellationToken = default)
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
                    var chunkOffset = new RegisterOffset((byte) (offset.Value + completed));
                    var chunk       = data.Slice(completed, chunkLength);
                    await _retry.ExecuteAsync(
                                         async token =>
                                             {
                                                 await WriteChunkAsync(device, chunkOffset, chunk, token)
                                                        .ConfigureAwait(false);
                                                 return true;
                                             },
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

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                IsOpen = false;
                _operationGate.Dispose();
                _disposed = true;
            }

            return ValueTask.CompletedTask;
        }

        protected abstract byte[] EncodeRead(I2cDeviceAddress device,
                                             RegisterOffset   offset,
                                             int              length);

        protected abstract byte[] EncodeWrite(I2cDeviceAddress   device,
                                              RegisterOffset     offset,
                                              ReadOnlySpan<byte> data);

        protected abstract byte[] ParseRead(ReadOnlySpan<byte> response,
                                            int                expectedLength);

        protected abstract void ValidateWrite(ReadOnlySpan<byte> response);

        protected abstract int GetWriteResponseLength();

        private async ValueTask<byte[]> ReadChunkAsync(I2cDeviceAddress  device,
                                                       RegisterOffset    offset,
                                                       int               length,
                                                       CancellationToken cancellationToken)
        {
            await using var session = _sessionFactory.Create(PortName, BaudRate);
            await session.OpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await session.WriteAsync(
                                      EncodeRead(device, offset, length),
                                      cancellationToken)
                             .ConfigureAwait(false);
                var response = new byte[length + 6];
                await session.ReadExactlyAsync(response, cancellationToken)
                             .ConfigureAwait(false);
                return ParseRead(response, length);
            }
            finally
            {
                await session.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private async ValueTask WriteChunkAsync(I2cDeviceAddress     device,
                                                RegisterOffset       offset,
                                                ReadOnlyMemory<byte> data,
                                                CancellationToken    cancellationToken)
        {
            await using var session = _sessionFactory.Create(PortName, BaudRate);
            await session.OpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await session.WriteAsync(
                                      EncodeWrite(device, offset, data.Span),
                                      cancellationToken)
                             .ConfigureAwait(false);
                var response = new byte[GetWriteResponseLength()];
                await session.ReadExactlyAsync(response, cancellationToken)
                             .ConfigureAwait(false);
                ValidateWrite(response);
            }
            finally
            {
                await session.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private static void ValidateRange(RegisterOffset offset,
                                          int            length,
                                          string         parameterName)
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

        private void EnsureReady()
        {
            ThrowIfDisposed();
            if (!IsOpen)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
