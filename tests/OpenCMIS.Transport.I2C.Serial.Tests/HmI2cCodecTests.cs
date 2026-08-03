using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Codecs;
using Xunit;

namespace OpenCMIS.Transport.I2C.Serial.Tests
{
    public sealed class HmI2cCodecTests
    {
        private static readonly I2cDeviceAddress Device = new (0x50);
        private static readonly RegisterOffset   Offset = new (0x10);

        [Fact]
        public void EncodeRead_uses_explicit_device_address()
        {
            var frame = HmI2cCodec.EncodeRead(Device, Offset, 2);

            Assert.Equal(new byte[] {0x02, 0xD3, 0x00, 0xAA, 0xA0, 0x10}, frame);
        }

        [Fact]
        public void EncodeWrite_appends_payload()
        {
            var frame = HmI2cCodec.EncodeWrite(Device, Offset, [0x12, 0x34]);

            Assert.Equal(
                    new byte[] {0x02, 0xD2, 0x00, 0xAA, 0xA0, 0x10, 0x12, 0x34},
                    frame);
        }

        [Fact]
        public void ParseRead_returns_payload_after_six_byte_header()
        {
            var payload = HmI2cCodec.ParseRead(
                    [0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x12, 0x34],
                    2);

            Assert.Equal(new byte[] {0x12, 0x34}, payload);
        }

        [Fact]
        public void ParseRead_rejects_failed_status()
        {
            var error = Assert.Throws<CmisException>(() => HmI2cCodec.ParseRead(
                                                             [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12],
                                                             1));

            Assert.Equal(CmisErrorCode.I2cInvalidResponse, error.ErrorCode);
        }
    }
}
