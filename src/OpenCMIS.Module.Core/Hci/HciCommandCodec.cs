using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Module.Core.Hci;

/// <summary>
/// Encodes and validates the vendor HCI command format used by Pulse modules.
/// </summary>
public static class HciCommandCodec
{
    private const byte ReadCommand = 0x00;
    private const byte WriteCommand = 0x01;
    private const byte ControlFlag = 0x80;
    private const int HeaderLength = 7;
    private const int ResponsePrefixLength = 8;

    public static byte[] EncodeRead(
        HciTableId table,
        RegisterOffset offset,
        int length)
    {
        ValidateLength(length);
        return
        [
            ReadCommand,
            0x00,
            0x00,
            table.Value,
            offset.Value,
            ControlFlag,
            (byte)length
        ];
    }

    public static byte[] EncodeWrite(
        HciTableId table,
        RegisterOffset offset,
        ReadOnlySpan<byte> data)
    {
        ValidateLength(data.Length);
        var packet = new byte[HeaderLength + data.Length];
        packet[0] = WriteCommand;
        packet[3] = table.Value;
        packet[4] = offset.Value;
        packet[5] = ControlFlag;
        packet[6] = (byte)data.Length;
        data.CopyTo(packet.AsSpan(HeaderLength));
        return packet;
    }

    public static byte[] ExtractReadPayload(
        ReadOnlySpan<byte> response,
        int requestedLength)
    {
        ValidateLength(requestedLength);
        if (response.Length != ResponsePrefixLength + requestedLength)
        {
            throw new CmisException(CmisErrorCode.HciInvalidResponse);
        }

        return response.Slice(ResponsePrefixLength, requestedLength).ToArray();
    }

    private static void ValidateLength(int length)
    {
        if (length is < 1 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                "HCI payload length must be between 1 and 255.");
        }
    }
}
