namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides implementation for register access operations.
    ///     This implementation supports CMIS protocol page-based register access.
    /// </summary>
    public class RegisterAccess : IRegisterAccess
    {
        private const byte PageSelectRegister = 0x7F;
        private readonly IDeviceConnection _deviceConnection;
        private byte _currentPage = 0xFF;

        /// <summary>
        ///     Initializes a new instance of the RegisterAccess class.
        /// </summary>
        /// <param name="deviceConnection">The device connection interface.</param>
        public RegisterAccess(IDeviceConnection deviceConnection)
        {
            _deviceConnection = deviceConnection ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(deviceConnection));
        }

        /// <inheritdoc />
        public async Task<byte> ReadByteAsync(byte page, byte address)
        {
            CmisException.ThrowIf(!_deviceConnection.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(address > 0x7F && address != PageSelectRegister, CmisErrorCode.InvalidRegister, address, page);

            await EnsurePageAsync(page);

            if (_deviceConnection is I2CDeviceConnection i2cConnection)
            {
                return await i2cConnection.ReadRegisterAsync(address);
            }

            var readData = new byte[] { address };
            await _deviceConnection.WriteAsync(readData);
            var result = await _deviceConnection.ReadAsync(1);
            return result.Length > 0 ? result[0] : (byte) 0;
        }

        /// <inheritdoc />
        public async Task WriteByteAsync(byte page, byte address, byte value)
        {
            CmisException.ThrowIf(!_deviceConnection.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(address > 0x7F && address != PageSelectRegister, CmisErrorCode.InvalidRegister, address, page);

            await EnsurePageAsync(page);

            if (_deviceConnection is I2CDeviceConnection i2cConnection)
            {
                await i2cConnection.WriteRegisterAsync(address, value);
            }
            else
            {
                var writeData = new byte[] { address, value };
                await _deviceConnection.WriteAsync(writeData);
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length)
        {
            CmisException.ThrowIf(!_deviceConnection.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(length <= 0, CmisErrorCode.InvalidParameterValue, nameof(length));
            CmisException.ThrowIf(startAddress + length > 0x80, CmisErrorCode.InvalidRegister, startAddress, page);

            await EnsurePageAsync(page);

            if (_deviceConnection is I2CDeviceConnection i2cConnection)
            {
                return await i2cConnection.ReadRegisterBlockAsync(startAddress, length);
            }

            var readData = new byte[] { startAddress };
            await _deviceConnection.WriteAsync(readData);
            return await _deviceConnection.ReadAsync(length);
        }

        /// <inheritdoc />
        public async Task WriteBlockAsync(byte page, byte startAddress, byte[] data)
        {
            CmisException.ThrowIf(!_deviceConnection.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(data == null || data.Length == 0, CmisErrorCode.InvalidParameterValue, nameof(data));
            CmisException.ThrowIf(startAddress + data.Length > 0x80, CmisErrorCode.InvalidRegister, startAddress, page);

            await EnsurePageAsync(page);

            if (_deviceConnection is I2CDeviceConnection i2cConnection)
            {
                await i2cConnection.WriteRegisterBlockAsync(startAddress, data);
            }
            else
            {
                var writeData = new byte[data.Length + 1];
                writeData[0] = startAddress;
                Array.Copy(data, 0, writeData, 1, data.Length);
                await _deviceConnection.WriteAsync(writeData);
            }
        }

        /// <summary>
        ///     Ensures the specified page is selected.
        /// </summary>
        /// <param name="page">The target page number.</param>
        private async Task EnsurePageAsync(byte page)
        {
            if (_currentPage == page)
                return;

            if (_deviceConnection is I2CDeviceConnection i2cConnection)
            {
                await i2cConnection.WriteRegisterAsync(PageSelectRegister, page);
            }
            else
            {
                var pageSelectData = new byte[] { PageSelectRegister, page };
                await _deviceConnection.WriteAsync(pageSelectData);
            }

            _currentPage = page;
        }
    }
}