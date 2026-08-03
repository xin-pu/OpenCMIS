using Xunit;

namespace OpenCMIS.Transport.Abstractions.Tests
{
    public sealed class I2cDeviceAddressTests
    {
        [Theory]
        [InlineData(0x00)]
        [InlineData(0x50)]
        [InlineData(0x7F)]
        public void Constructor_accepts_7_bit_values(byte value)
        {
            Assert.Equal(value, new I2cDeviceAddress(value).Value);
        }

        [Fact]
        public void Constructor_rejects_8_bit_value()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new I2cDeviceAddress(0xA0));
        }

        [Fact]
        public void FromLegacy8Bit_converts_A0_to_50()
        {
            Assert.Equal(0x50, I2cDeviceAddress.FromLegacy8Bit(0xA0).Value);
        }

        [Fact]
        public void FromLegacy8Bit_rejects_read_address()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => I2cDeviceAddress.FromLegacy8Bit(0xA1));
        }

        [Fact]
        public void ToWriteAddress8Bit_converts_50_to_A0()
        {
            Assert.Equal(0xA0, new I2cDeviceAddress(0x50).ToWriteAddress8Bit());
        }
    }
}
