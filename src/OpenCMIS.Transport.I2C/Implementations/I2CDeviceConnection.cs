using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C
{
    /// <summary>
    ///     Provides I2C-based device connection implementation for CMIS modules.
    ///     This is a simulated implementation that demonstrates I2C protocol operations.
    /// </summary>
    public class I2CDeviceConnection : DeviceConnection, IRegisterTransport
    {
        private readonly Dictionary<ushort, byte> _registerMemory;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;
        private bool _isConnected;

        /// <summary>
        ///     Initializes a new instance of the I2CDeviceConnection class with default I2C address.
        /// </summary>
        public I2CDeviceConnection()
            : this(CmisConstants.DefaultI2cAddress)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the I2CDeviceConnection class with specified I2C address.
        /// </summary>
        /// <param name="deviceAddress">The I2C device address (7-bit address).</param>
        public I2CDeviceConnection(byte deviceAddress)
        {
            DeviceAddress   = deviceAddress;
            _registerMemory = new Dictionary<ushort, byte>();
            _semaphore      = new SemaphoreSlim(1, 1);
            _isConnected    = false;
            _disposed       = false;
        }

        /// <summary>
        ///     Gets the I2C device address.
        /// </summary>
        public byte DeviceAddress { get; }

        /// <inheritdoc />
        public override bool IsConnected => _isConnected && !_disposed;

        /// <inheritdoc />
        public override async Task<bool> OpenAsync()
        {
            if (_disposed)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            if (_isConnected)
                return true;

            try
            {
                await _semaphore.WaitAsync();

                if (_disposed)
                    return false;

                _isConnected = true;
                InitializeDefaultRegisters();
                return true;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.DeviceConnectionFailed, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public override async Task CloseAsync()
        {
            if (!_isConnected)
                return;

            try
            {
                await _semaphore.WaitAsync();
                _isConnected = false;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.DeviceDisconnectionFailed, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public override async Task<byte[]> ReadAsync(int length)
        {
            if (!_isConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            if (length <= 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(length));

            try
            {
                await _semaphore.WaitAsync();

                if (!_isConnected)
                    throw new CmisException(CmisErrorCode.DeviceNotConnected);

                var result = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    var address = (ushort)i;
                    result[i] = _registerMemory.TryGetValue(address, out var value) ? value : (byte)0;
                }

                return result;
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.DeviceCommunicationError, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] data)
        {
            if (!_isConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            if (data == null || data.Length == 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(data));

            try
            {
                await _semaphore.WaitAsync();

                if (!_isConnected)
                    throw new CmisException(CmisErrorCode.DeviceNotConnected);

                for (var i = 0; i < data.Length; i++)
                {
                    var address = (ushort)i;
                    _registerMemory[address] = data[i];
                }
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.DeviceCommunicationError, ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        ///     Reads a byte from the specified register address.
        /// </summary>
        /// <param name="registerAddress">The register address.</param>
        /// <returns>The byte value read from the register.</returns>
        public async Task<byte> ReadRegisterAsync(byte registerAddress)
        {
            if (!_isConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            try
            {
                await _semaphore.WaitAsync();

                if (!_isConnected)
                    throw new CmisException(CmisErrorCode.DeviceNotConnected);

                var address = (ushort)registerAddress;
                return _registerMemory.TryGetValue(address, out var value) ? value : (byte)0;
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.RegisterReadFailed, ex, registerAddress);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        ///     Writes a byte to the specified register address.
        /// </summary>
        /// <param name="registerAddress">The register address.</param>
        /// <param name="value">The byte value to write.</param>
        public async Task WriteRegisterAsync(byte registerAddress, byte value)
        {
            if (!_isConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            try
            {
                await _semaphore.WaitAsync();

                if (!_isConnected)
                    throw new CmisException(CmisErrorCode.DeviceNotConnected);

                var address = (ushort)registerAddress;
                _registerMemory[address] = value;
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.RegisterWriteFailed, ex, registerAddress, value);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        ///     Reads a block of data from the specified register address range.
        /// </summary>
        /// <param name="registerAddress">The starting register address.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The byte array containing the read data.</returns>
        public async Task<byte[]> ReadRegisterBlockAsync(byte registerAddress, int length)
        {
            if (!_isConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            if (length <= 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(length));

            try
            {
                await _semaphore.WaitAsync();

                if (!_isConnected)
                    throw new CmisException(CmisErrorCode.DeviceNotConnected);

                var result = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    var address = (ushort)(registerAddress + i);
                    result[i] = _registerMemory.TryGetValue(address, out var value) ? value : (byte)0;
                }

                return result;
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.RegisterReadFailed, ex, registerAddress);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        ///     Writes a block of data to the specified register address range.
        /// </summary>
        /// <param name="registerAddress">The starting register address.</param>
        /// <param name="data">The byte array containing the data to write.</param>
        public async Task WriteRegisterBlockAsync(byte registerAddress, byte[] data)
        {
            if (!_isConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            if (data == null || data.Length == 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(data));

            try
            {
                await _semaphore.WaitAsync();

                if (!_isConnected)
                    throw new CmisException(CmisErrorCode.DeviceNotConnected);

                for (var i = 0; i < data.Length; i++)
                {
                    var address = (ushort)(registerAddress + i);
                    _registerMemory[address] = data[i];
                }
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.RegisterWriteFailed, ex, registerAddress);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        ///     Initializes default register values for simulation.
        /// </summary>
        private void InitializeDefaultRegisters()
        {
            _registerMemory[0x00] = 0x18;
            _registerMemory[0x01] = 0x40;
            _registerMemory[0x02] = 0x00;
            _registerMemory[0x03] = 0x00;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _semaphore?.Dispose();
                    _registerMemory?.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}