using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.Module.Core.Tests;

public sealed class HciCommandCodecTests
{
    [Fact]
    public void EncodeRead_preserves_vendor_wire_format()
    {
        var packet = HciCommandCodec.EncodeRead(
            new HciTableId(0xAE),
            new RegisterOffset(0x0A),
            2);

        Assert.Equal(
            new byte[] { 0x00, 0x00, 0x00, 0xAE, 0x0A, 0x80, 0x02 },
            packet);
    }

    [Fact]
    public void EncodeWrite_appends_payload()
    {
        var packet = HciCommandCodec.EncodeWrite(
            new HciTableId(0xA4),
            new RegisterOffset(0x08),
            [0x12, 0x34]);

        Assert.Equal(
            new byte[]
            {
                0x01, 0x00, 0x00, 0xA4, 0x08, 0x80, 0x02, 0x12, 0x34
            },
            packet);
    }

    [Fact]
    public void ExtractReadPayload_skips_eight_byte_prefix()
    {
        var payload = HciCommandCodec.ExtractReadPayload(
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x34],
            2);

        Assert.Equal(new byte[] { 0x12, 0x34 }, payload);
    }

    [Fact]
    public void ExtractReadPayload_rejects_invalid_length()
    {
        var error = Assert.Throws<CmisException>(
            () => HciCommandCodec.ExtractReadPayload([0x00], 1));

        Assert.Equal(CmisErrorCode.HciInvalidResponse, error.ErrorCode);
    }
}
