using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Serial.Codecs
{
    /// <summary>
    ///     Encodes and validates the Linktel serial-to-I2C wire protocol.
    /// </summary>
    public static class LinktelI2cCodec
    {
        private const byte CommandRead    = 0x11;
        private const byte CommandWrite   = 0x10;
        private const byte Header         = 0x55;
        private const byte ResponseHeader = 0xAA;
        private const byte StatusSuccess  = 0x00;
        private const byte EndByte1       = 0x0D;
        private const byte EndByte2       = 0x0A;

        public static byte[] EncodeRead(I2cDeviceAddress device,
                                        RegisterOffset   offset,
                                        int              length)
        {
            ValidateLength(length);

            var frame = new byte[]
                        {
                            Header,
                            0x00,
                            CommandRead,
                            0x03,
                            device.ToWriteAddress8Bit(),
                            offset.Value,
                            (byte) length,
                            EndByte1,
                            EndByte2
                        };
            frame[1] = CalculateChecksum(frame.AsSpan(2, 5));
            return frame;
        }

        public static byte[] EncodeWrite(I2cDeviceAddress   device,
                                         RegisterOffset     offset,
                                         ReadOnlySpan<byte> data)
        {
            ValidateLength(data.Length);

            var frame = new byte[data.Length + 9];
            frame[0] = Header;
            frame[2] = CommandWrite;
            frame[3] = (byte) (data.Length + 3);
            frame[4] = device.ToWriteAddress8Bit();
            frame[5] = offset.Value;
            frame[6] = (byte) data.Length;
            data.CopyTo(frame.AsSpan(7));
            frame[^2] = EndByte1;
            frame[^1] = EndByte2;
            frame[1]  = CalculateChecksum(frame.AsSpan(2, data.Length + 5));
            return frame;
        }

        public static byte[] ParseRead(ReadOnlySpan<byte> response,
                                       int                expectedLength)
        {
            ValidateLength(expectedLength);

            if (response.Length != expectedLength + 6 ||
                response[0]     != ResponseHeader     ||
                response[^2]    != EndByte1           ||
                response[^1]    != EndByte2           ||
                response[2]     != StatusSuccess      ||
                response[3]     != expectedLength)
                throw InvalidResponse();

            return response.Slice(4, expectedLength).ToArray();
        }

        public static void ValidateWrite(ReadOnlySpan<byte> response)
        {
            if (response.Length != 6              ||
                response[0]     != ResponseHeader ||
                response[2]     != StatusSuccess  ||
                response[4]     != EndByte1       ||
                response[5]     != EndByte2)
                throw InvalidResponse();
        }

        private static void ValidateLength(int length)
        {
            if (length is < 1 or > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                        nameof(length),
                        length,
                        "Transfer length must be between 1 and 255.");
            }
        }

        private static byte CalculateChecksum(ReadOnlySpan<byte> data)
        {
            byte sum = 0;
            foreach (var value in data)
                sum += value;

            return sum;
        }

        private static CmisException InvalidResponse()
        {
            return new (CmisErrorCode.I2cInvalidResponse);
        }
    }
}
