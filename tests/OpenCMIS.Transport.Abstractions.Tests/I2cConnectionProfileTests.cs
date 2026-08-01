using OpenCMIS.Transport.Abstractions;
using Xunit;

namespace OpenCMIS.Transport.Abstractions.Tests;

public sealed class I2cConnectionProfileTests
{
    private static readonly I2cDeviceAddress ModuleAddress = new(0x50);

    [Fact]
    public void Serial_profile_preserves_typed_connection_values()
    {
        var profile = new SerialI2cConnectionProfile(
            "linktel",
            "COM7",
            115200,
            ModuleAddress);

        Assert.Equal("linktel", profile.AdapterId);
        Assert.Equal("COM7", profile.PortName);
        Assert.Equal(115200, profile.BaudRate);
        Assert.Equal(ModuleAddress, profile.DeviceAddress);
    }

    [Fact]
    public void Serial_profile_rejects_empty_port()
    {
        Assert.Throws<ArgumentException>(
            () => new SerialI2cConnectionProfile(
                "linktel",
                "",
                115200,
                ModuleAddress));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Multi_channel_profile_rejects_channel_outside_1_to_5(byte channel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HmMultiChannelConnectionProfile(
                "hm-multichannel",
                "COM8",
                1500000,
                channel,
                ModuleAddress));
    }

    [Fact]
    public void Transfer_capabilities_reject_non_positive_limits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new I2cTransferCapabilities(0, 32));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new I2cTransferCapabilities(32, 0));
    }

    [Fact]
    public void Retry_options_reject_non_positive_attempt_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new I2cRetryOptions(0, TimeSpan.Zero));
    }

    [Fact]
    public void Retry_options_reject_negative_delay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new I2cRetryOptions(1, TimeSpan.FromMilliseconds(-1)));
    }
}
