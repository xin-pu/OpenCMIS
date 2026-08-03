using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Codecs;
using Xunit;

namespace OpenCMIS.Transport.I2C.Serial.Tests
{
    public sealed class LinktelI2cCodecTests
    {
        private static readonly I2cDeviceAddress Device = new (0x50);
        private static readonly RegisterOffset   Offset = new (0x80);

        [Fact]
        public void EncodeRead_uses_explicit_device_address()
        {
            var frame = LinktelI2cCodec.EncodeRead(Device, Offset, 4);

            Assert.Equal(
                    new byte[] {0x55, 0x38, 0x11, 0x03, 0xA0, 0x80, 0x04, 0x0D, 0x0A},
                    frame);
        }

        [Fact]
        public void EncodeWrite_includes_payload_and_checksum()
        {
            var frame = LinktelI2cCodec.EncodeWrite(Device, Offset, [0x12, 0x34]);

            Assert.Equal(
                    new byte[]
                    {
                        0x55, 0x7D, 0x10, 0x05, 0xA0, 0x80, 0x02, 0x12, 0x34, 0x0D, 0x0A
                    },
                    frame);
        }

        [Fact]
        public void ParseRead_returns_payload()
        {
            var payload = LinktelI2cCodec.ParseRead(
                    [0xAA, 0x00, 0x00, 0x02, 0x12, 0x34, 0x0D, 0x0A],
                    2);

            Assert.Equal(new byte[] {0x12, 0x34}, payload);
        }

        [Fact]
        public void ParseRead_rejects_invalid_status()
        {
            var error = Assert.Throws<CmisException>(() => LinktelI2cCodec.ParseRead(
                                                             [0xAA, 0x00, 0x01, 0x01, 0x42, 0x0D, 0x0A],
                                                             1));

            Assert.Equal(CmisErrorCode.I2cInvalidResponse, error.ErrorCode);
        }
    }
}
