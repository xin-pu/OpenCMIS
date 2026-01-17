using System.IO.Ports;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C
{
    /// <summary>
    ///     Provides I2C-over-Serial connection implementation for Type B(HOME) devices.
    ///     Supports short-connection mode for multi-application access.
    /// </summary>
    public class I2CConnectorTypeB : SerialDeviceConnectionBase, IRegisterTransport
    {
        private const byte DefaultSlaveAddress = 0xa0;
        private const byte CommandWrite = 0xd2;
        private const byte CommandRead = 0xd3;
        private const byte StatusSuccess = 0x01;

        private readonly byte _slaveAddress;

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CConnectorTypeB" /> class.
        /// </summary>
        /// <param name="portName">The name of the serial port.</param>
        /// <param name="baudRate">The baud rate for the serial port (default is 1,500,000).</param>
        /// <param name="slaveAddress">The I2C slave address (default is 0xA0).</param>
        public I2CConnectorTypeB(string portName, int baudRate = 1500000, byte slaveAddress = DefaultSlaveAddress)
            : base(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            _slaveAddress = slaveAddress;
        }

        /// <inheritdoc />
        public async Task<byte> ReadRegisterAsync(byte registerAddress)
        {
            var data = await ReadRegisterBlockAsync(registerAddress, 1);
            return data.Length > 0 ? data[0] : (byte)0;
        }

        /// <inheritdoc />
        public async Task WriteRegisterAsync(byte registerAddress, byte value)
        {
            await WriteRegisterBlockAsync(registerAddress, new[] { value });
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadRegisterBlockAsync(byte registerAddress, int length)
        {
            if (length <= 0 || length > 255)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(length));

            return await ExecuteAsync(async (port) =>
            {
                var cmd = new byte[]
                {
                    (byte)length, CommandRead, 0x00, 0xaa, _slaveAddress, registerAddress
                };

                await WriteToPortAsync(port, cmd);

                // HM protocol returns length + 6 bytes
                var response = await ReadFromPortAsync(port, length + 6);

                if (response[2] != StatusSuccess)
                    throw new CmisException(CmisErrorCode.RegisterReadFailed, null, registerAddress);

                var result = new byte[length];
                Array.Copy(response, 6, result, 0, length);
                return result;
            });
        }

        /// <inheritdoc />
        public async Task WriteRegisterBlockAsync(byte registerAddress, byte[] data)
        {
            if (data == null || data.Length == 0 || data.Length > 255)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(data));

            await ExecuteAsync(async (port) =>
            {
                var byteLength = (byte)data.Length;
                var cmd = new List<byte>(byteLength + 6)
                {
                    byteLength, CommandWrite, 0x00, 0xaa, _slaveAddress, registerAddress
                };
                cmd.AddRange(data);

                await WriteToPortAsync(port, cmd.ToArray());

                // HM protocol write returns 1 byte status
                var statusResponse = await ReadFromPortAsync(port, 1);
                if (statusResponse[0] != StatusSuccess)
                    throw new CmisException(CmisErrorCode.RegisterWriteFailed, null, registerAddress);
            });
        }
    }
}
