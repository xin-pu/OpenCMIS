using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Serial.Codecs;

/// <summary>
/// Encodes and validates the HM serial-to-I2C wire protocol.
/// </summary>
public static class HmI2cCodec
{
    public const byte DefaultReadCommand = 0xD3;
    public const byte DefaultWriteCommand = 0xD2;
    private const byte StatusSuccess = 0x01;

    public static byte[] EncodeRead(
        I2cDeviceAddress device,
        RegisterOffset offset,
        int length,
        byte command = DefaultReadCommand)
    {
        ValidateLength(length);

        return
        [
            (byte)length,
            command,
            0x00,
            0xAA,
            device.ToWriteAddress8Bit(),
            offset.Value
        ];
    }

    public static byte[] EncodeWrite(
        I2cDeviceAddress device,
        RegisterOffset offset,
        ReadOnlySpan<byte> data,
        byte command = DefaultWriteCommand)
    {
        ValidateLength(data.Length);

        var frame = new byte[data.Length + 6];
        frame[0] = (byte)data.Length;
        frame[1] = command;
        frame[2] = 0x00;
        frame[3] = 0xAA;
        frame[4] = device.ToWriteAddress8Bit();
        frame[5] = offset.Value;
        data.CopyTo(frame.AsSpan(6));
        return frame;
    }

    public static byte[] ParseRead(
        ReadOnlySpan<byte> response,
        int expectedLength)
    {
        ValidateLength(expectedLength);

        if (response.Length != expectedLength + 6 ||
            response[2] != StatusSuccess)
        {
            throw InvalidResponse();
        }

        return response.Slice(6, expectedLength).ToArray();
    }

    public static void ValidateWrite(ReadOnlySpan<byte> response)
    {
        if (response.Length != 1 || response[0] != StatusSuccess)
        {
            throw InvalidResponse();
        }
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

    private static CmisException InvalidResponse()
    {
        return new CmisException(CmisErrorCode.I2cInvalidResponse);
    }
}
