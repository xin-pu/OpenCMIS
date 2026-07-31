using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Cypress.Tests.Fakes;
using Xunit;

namespace OpenCMIS.Transport.I2C.Cypress.Tests;

public sealed class CypressI2cAdapterTests
{
    private static readonly I2cDeviceAddress Address = new(0x50);

    [Fact]
    public async Task Fic2Usb_write_maps_port_speed_address_and_register_prefix()
    {
        var api = new MockCypressDeviceApi();
        await using var adapter = new Fic2UsbI2cAdapter(api, port: 1, speedKhz: 100);
        await adapter.OpenAsync();

        await adapter.WriteAsync(
            Address,
            new RegisterOffset(0x80),
            new byte[] { 0x12, 0x34 });

        var call = Assert.Single(api.Calls);
        Assert.Equal(CypressTransferDirection.Write, call.Direction);
        Assert.Equal(1, call.Port);
        Assert.Equal(100, call.SpeedKhz);
        Assert.Equal(0xA0, call.Address8Bit);
        Assert.Equal(new byte[] { 0x80, 0x12, 0x34 }, call.Data);
    }

    [Fact]
    public async Task Read_sets_register_pointer_then_copies_exact_result()
    {
        var api = new MockCypressDeviceApi();
        api.ReadResults.Enqueue([0x11, 0x22, 0x33]);
        await using var adapter = new Fic2UsbI2cAdapter(api, port: 0, speedKhz: 400);
        await adapter.OpenAsync();
        var destination = new byte[3];

        await adapter.ReadAsync(
            Address,
            new RegisterOffset(0x21),
            destination);

        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, destination);
        Assert.Collection(
            api.Calls,
            pointer =>
            {
                Assert.Equal(CypressTransferDirection.Write, pointer.Direction);
                Assert.Equal(new byte[] { 0x21 }, pointer.Data);
            },
            read =>
            {
                Assert.Equal(CypressTransferDirection.Read, read.Direction);
                Assert.Equal(3, read.Length);
            });
    }

    [Fact]
    public async Task Eui3_false_transfer_is_converted_to_i2c_transfer_failed()
    {
        var api = new MockCypressDeviceApi { TransferResult = false };
        await using var adapter = new Eui3I2cAdapter(api, port: 0, speedKhz: 400);
        await adapter.OpenAsync();

        var error = await Assert.ThrowsAsync<CmisException>(
            () => adapter.WriteAsync(
                    Address,
                    new RegisterOffset(0x10),
                    new byte[] { 0x01 })
                .AsTask());

        Assert.Equal(CmisErrorCode.I2cTransferFailed, error.ErrorCode);
    }

    [Fact]
    public async Task Eui3_write_segments_payload_and_advances_register_offset()
    {
        var api = new MockCypressDeviceApi();
        await using var adapter = new Eui3I2cAdapter(api, port: 0, speedKhz: 90);
        await adapter.OpenAsync();
        var payload = Enumerable.Range(0, 50).Select(value => (byte)value).ToArray();

        await adapter.WriteAsync(
            Address,
            new RegisterOffset(0x20),
            payload);

        Assert.Collection(
            api.Calls,
            first =>
            {
                Assert.Equal(49, first.Data.Length);
                Assert.Equal(0x20, first.Data[0]);
                Assert.Equal(payload[..48], first.Data[1..]);
            },
            second =>
            {
                Assert.Equal(new byte[] { 0x50, 48, 49 }, second.Data);
            });
    }

    [Fact]
    public async Task Canceled_transfer_does_not_call_low_level_api()
    {
        var api = new MockCypressDeviceApi();
        await using var adapter = new Fic2UsbI2cAdapter(api, port: 0, speedKhz: 100);
        await adapter.OpenAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => adapter.ReadAsync(
                    Address,
                    new RegisterOffset(0),
                    new byte[1],
                    cancellation.Token)
                .AsTask());

        Assert.Empty(api.Calls);
    }

    [Fact]
    public async Task Cancellation_during_blocking_call_is_observed_after_call()
    {
        using var cancellation = new CancellationTokenSource();
        var api = new MockCypressDeviceApi
        {
            OnWrite = cancellation.Cancel
        };
        await using var adapter = new Fic2UsbI2cAdapter(api, port: 0, speedKhz: 100);
        await adapter.OpenAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => adapter.WriteAsync(
                    Address,
                    new RegisterOffset(0),
                    new byte[] { 1 },
                    cancellation.Token)
                .AsTask());

        Assert.Single(api.Calls);
    }

    [Fact]
    public async Task Short_low_level_read_is_rejected_as_transfer_failure()
    {
        var api = new MockCypressDeviceApi();
        api.ReadResults.Enqueue([0x11]);
        await using var adapter = new Fic2UsbI2cAdapter(api, port: 0, speedKhz: 100);
        await adapter.OpenAsync();

        var error = await Assert.ThrowsAsync<CmisException>(
            () => adapter.ReadAsync(
                    Address,
                    new RegisterOffset(0),
                    new byte[2])
                .AsTask());

        Assert.Equal(CmisErrorCode.I2cTransferFailed, error.ErrorCode);
    }

    [Fact]
    public async Task Dispose_closes_and_disposes_low_level_api_once()
    {
        var api = new MockCypressDeviceApi();
        var adapter = new Fic2UsbI2cAdapter(api, port: 0, speedKhz: 100);
        await adapter.OpenAsync();

        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        Assert.Equal(1, api.CloseCount);
        Assert.True(api.IsDisposed);
    }
}
