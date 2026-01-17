using System.IO.Ports;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C
{
    /// <summary>
    ///     Provides I2C-over-Serial connection implementation for Type A(Link) devices.
    ///     Supports short-connection mode for multi-application access.
    /// </summary>
    public class I2CConnectorTypeA : SerialDeviceConnectionBase, IRegisterTransport
    {
        private const byte CommandRead = 0x11;
        private const byte CommandWrite = 0x10;
        private const byte Header = 0x55;
        private const byte ResponseHeader = 0xaa;
        private const byte DefaultSlaveAddress = 0xa0;
        private const byte StatusSuccess = 0x00;
        private const byte EndByte1 = 0x0d;
        private const byte EndByte2 = 0x0a;

        private readonly byte _slaveAddress;

        /// <summary>
        ///     Initializes a new instance of the <see cref="I2CConnectorTypeA" /> class.
        /// </summary>
        /// <param name="portName">The name of the serial port.</param>
        /// <param name="baudRate">The baud rate for the serial port.</param>
        /// <param name="slaveAddress">The I2C slave address (default is 0xA0).</param>
        public I2CConnectorTypeA(string portName, int baudRate = 115200, byte slaveAddress = DefaultSlaveAddress)
            : base(portName, baudRate)
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
            if (length <= 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(length));

            return await ExecuteAsync(async (port) =>
            {
                var readCmd = BuildReadCommand(registerAddress, length);
                var expectedResponseLength = length + 6;

                await WriteToPortAsync(port, readCmd);
                var response = await ReadFromPortAsync(port, expectedResponseLength);

                return ParseReadResponse(response, length);
            });
        }

        /// <inheritdoc />
        public async Task WriteRegisterBlockAsync(byte registerAddress, byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(data));

            await ExecuteAsync(async (port) =>
            {
                var writeCmd = BuildWriteCommand(registerAddress, data);

                await WriteToPortAsync(port, writeCmd);
                var response = await ReadFromPortAsync(port, 6);

                if (!ValidateWriteResponse(response))
                {
                    throw new CmisException(CmisErrorCode.RegisterWriteFailed, null, registerAddress);
                }
            });
        }

        private byte[] BuildReadCommand(byte registerAddress, int dataLength)
        {
            var cmd = new byte[9];
            cmd[0] = Header;
            cmd[2] = CommandRead;
            cmd[3] = 3;
            cmd[4] = _slaveAddress;
            cmd[5] = registerAddress;
            cmd[6] = (byte)dataLength;
            cmd[7] = EndByte1;
            cmd[8] = EndByte2;

            var checksumData = new[] { cmd[2], cmd[3], cmd[4], cmd[5], cmd[6] };
            cmd[1] = CalculateChecksum(checksumData);

            return cmd;
        }

        private byte[] BuildWriteCommand(byte registerAddress, byte[] data)
        {
            var headerLength = 7;
            var bodyLength = data.Length;
            var endLength = 2;
            var totalLength = headerLength + bodyLength + endLength;

            var cmd = new byte[totalLength];
            cmd[0] = Header;
            cmd[2] = CommandWrite;
            cmd[3] = (byte)(3 + bodyLength);
            cmd[4] = _slaveAddress;
            cmd[5] = registerAddress;
            cmd[6] = (byte)bodyLength;

            Array.Copy(data, 0, cmd, headerLength, bodyLength);

            cmd[totalLength - 2] = EndByte1;
            cmd[totalLength - 1] = EndByte2;

            var checksumData = new byte[5 + bodyLength];
            checksumData[0] = CommandWrite;
            checksumData[1] = (byte)(3 + bodyLength);
            checksumData[2] = _slaveAddress;
            checksumData[3] = registerAddress;
            checksumData[4] = (byte)bodyLength;
            Array.Copy(data, 0, checksumData, 5, bodyLength);

            cmd[1] = CalculateChecksum(checksumData);

            return cmd;
        }

        private byte[] ParseReadResponse(byte[] response, int expectedDataLength)
        {
            if (response.Length < 6)
                throw new CmisException(CmisErrorCode.DeviceCommunicationError);

            if (response[0] != ResponseHeader || response[^2] != EndByte1 || response[^1] != EndByte2)
                throw new CmisException(CmisErrorCode.DeviceCommunicationError);

            if (response[2] != StatusSuccess)
                throw new CmisException(CmisErrorCode.RegisterReadFailed);

            var bodyLength = response[3];
            if (bodyLength != expectedDataLength)
                throw new CmisException(CmisErrorCode.DeviceCommunicationError);

            var result = new byte[bodyLength];
            Array.Copy(response, 4, result, 0, bodyLength);
            return result;
        }

        private bool ValidateWriteResponse(byte[] response)
        {
            if (response.Length != 6) return false;
            if (response[0] != ResponseHeader || response[4] != EndByte1 || response[5] != EndByte2) return false;
            return response[2] == StatusSuccess;
        }

        private byte CalculateChecksum(byte[] data)
        {
            byte sum = 0;
            foreach (var b in data) sum += b;
            return sum;
        }
    }
}
