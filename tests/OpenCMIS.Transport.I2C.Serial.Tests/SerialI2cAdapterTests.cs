using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Adapters;
using OpenCMIS.Transport.I2C.Serial.Tests.Fakes;
using Xunit;

namespace OpenCMIS.Transport.I2C.Serial.Tests;

public sealed class SerialI2cAdapterTests
{
    private static readonly I2cDeviceAddress Address = new(0x50);
    private static readonly RegisterOffset Offset = new(0x80);
    private static readonly SerialI2cConnectionProfile LinktelProfile =
        new("linktel", "COM7", 115200, Address);
    private static readonly SerialI2cConnectionProfile HmProfile =
        new("hm", "COM8", 1500000, Address);

    [Fact]
    public async Task Linktel_read_honors_explicit_device_address()
    {
        var sessions = new ScriptedSerialSessionFactory(
            new SerialSessionScript(LinktelReadResponse([0x42])));
        await using var adapter = CreateLinktel(sessions);
        await adapter.OpenAsync();
        var destination = new byte[1];

        await adapter.ReadAsync(new I2cDeviceAddress(0x51), Offset, destination);

        Assert.Equal(0x42, destination[0]);
        Assert.Equal(0xA2, sessions.Writes.Single()[4]);
    }

    [Fact]
    public async Task Linktel_read_segments_at_255_bytes()
    {
        var first = Enumerable.Range(0, 255).Select(value => (byte)value).ToArray();
        var sessions = new ScriptedSerialSessionFactory(
            new SerialSessionScript(LinktelReadResponse(first)),
            new SerialSessionScript(LinktelReadResponse([0xFF])));
        await using var adapter = CreateLinktel(sessions);
        await adapter.OpenAsync();
        var destination = new byte[256];

        await adapter.ReadAsync(Address, new RegisterOffset(0x00), destination);

        Assert.Equal(2, sessions.CreateCount);
        Assert.Equal(0x00, sessions.Writes[0][5]);
        Assert.Equal(0xFF, sessions.Writes[0][6]);
        Assert.Equal(0xFF, sessions.Writes[1][5]);
        Assert.Equal(0x01, sessions.Writes[1][6]);
        Assert.Equal(first, destination[..255]);
        Assert.Equal(0xFF, destination[255]);
    }

    [Fact]
    public async Task Adapter_retries_transient_IO_failure()
    {
        var sessions = new ScriptedSerialSessionFactory(
            new SerialSessionScript([], ReadException: new IOException("temporary")),
            new SerialSessionScript(LinktelReadResponse([0x42])));
        await using var adapter = CreateLinktel(
            sessions,
            new I2cRetryOptions(2, TimeSpan.Zero));
        await adapter.OpenAsync();
        var destination = new byte[1];

        await adapter.ReadAsync(Address, Offset, destination);

        Assert.Equal(2, sessions.CreateCount);
        Assert.Equal(0x42, destination[0]);
    }

    [Fact]
    public async Task Adapter_does_not_retry_invalid_response()
    {
        var sessions = new ScriptedSerialSessionFactory(
            new SerialSessionScript([0x00, 0x00, 0x00, 0x01, 0x42, 0x0D, 0x0A]),
            new SerialSessionScript(LinktelReadResponse([0x42])));
        await using var adapter = CreateLinktel(
            sessions,
            new I2cRetryOptions(3, TimeSpan.Zero));
        await adapter.OpenAsync();

        var error = await Assert.ThrowsAsync<CmisException>(
            () => adapter.ReadAsync(Address, Offset, new byte[1]).AsTask());

        Assert.Equal(CmisErrorCode.I2cInvalidResponse, error.ErrorCode);
        Assert.Equal(1, sessions.CreateCount);
    }

    [Theory]
    [InlineData(1, 0xE2, 0xE1)]
    [InlineData(2, 0xE4, 0xE3)]
    [InlineData(3, 0xD3, 0xD2)]
    [InlineData(4, 0xE6, 0xE5)]
    [InlineData(5, 0xE8, 0xE7)]
    public async Task Hm_multichannel_uses_channel_specific_commands(
        byte channel,
        byte readCommand,
        byte writeCommand)
    {
        var sessions = new ScriptedSerialSessionFactory(
            new SerialSessionScript(HmReadResponse([0x42])),
            new SerialSessionScript([0x01]));
        var profile = new HmMultiChannelConnectionProfile(
            "hm-multichannel",
            "COM9",
            1500000,
            channel,
            Address);
        await using var adapter = new HmMultiChannelI2cAdapter(
            sessions,
            profile,
            new I2cRetryOptions(1, TimeSpan.Zero),
            TimeProvider.System);
        await adapter.OpenAsync();

        await adapter.ReadAsync(Address, Offset, new byte[1]);
        await adapter.WriteAsync(Address, Offset, new byte[] { 0x12 });

        Assert.Equal(readCommand, sessions.Writes[0][1]);
        Assert.Equal(writeCommand, sessions.Writes[1][1]);
    }

    [Fact]
    public async Task Hm_write_rejects_failed_acknowledgement()
    {
        var sessions = new ScriptedSerialSessionFactory(
            new SerialSessionScript([0x00]));
        await using var adapter = new HmSerialI2cAdapter(
            sessions,
            HmProfile,
            new I2cRetryOptions(1, TimeSpan.Zero),
            TimeProvider.System);
        await adapter.OpenAsync();

        var error = await Assert.ThrowsAsync<CmisException>(
            () => adapter.WriteAsync(Address, Offset, new byte[] { 0x12 }).AsTask());

        Assert.Equal(CmisErrorCode.I2cInvalidResponse, error.ErrorCode);
    }

    private static LinktelSerialI2cAdapter CreateLinktel(
        ScriptedSerialSessionFactory sessions,
        I2cRetryOptions? retry = null)
    {
        return new LinktelSerialI2cAdapter(
            sessions,
            LinktelProfile,
            retry ?? new I2cRetryOptions(1, TimeSpan.Zero),
            TimeProvider.System);
    }

    private static byte[] LinktelReadResponse(byte[] payload)
    {
        return [0xAA, 0x00, 0x00, (byte)payload.Length, .. payload, 0x0D, 0x0A];
    }

    private static byte[] HmReadResponse(byte[] payload)
    {
        return [0x00, 0x00, 0x01, 0x00, 0x00, 0x00, .. payload];
    }
}
