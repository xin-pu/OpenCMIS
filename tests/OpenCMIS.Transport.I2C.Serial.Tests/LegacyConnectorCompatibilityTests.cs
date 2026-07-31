using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C;
using Xunit;

namespace OpenCMIS.Transport.I2C.Serial.Tests;

public sealed class LegacyConnectorCompatibilityTests
{
    [Fact]
    public async Task TypeA_wrapper_converts_legacy_address_and_forwards_read()
    {
        var bus = new RecordingI2cBus([0x42]);
        using var connector = new I2CConnectorTypeA(bus, 0xA0);
        await connector.OpenAsync();

        var result = await connector.ReadRegisterBlockAsync(0x80, 1);

        Assert.Equal(new byte[] { 0x42 }, result);
        Assert.Equal(new I2cDeviceAddress(0x50), bus.LastDevice);
        Assert.Equal(new RegisterOffset(0x80), bus.LastOffset);
    }

    [Fact]
    public async Task TypeB_wrapper_converts_legacy_address_and_forwards_write()
    {
        var bus = new RecordingI2cBus([]);
        using var connector = new I2CConnectorTypeB(bus, 0xA0);
        await connector.OpenAsync();

        await connector.WriteRegisterBlockAsync(0x81, [0x12, 0x34]);

        Assert.Equal(new I2cDeviceAddress(0x50), bus.LastDevice);
        Assert.Equal(new RegisterOffset(0x81), bus.LastOffset);
        Assert.Equal(new byte[] { 0x12, 0x34 }, bus.LastWrite);
    }

    private sealed class RecordingI2cBus(byte[] readResult) : II2cRegisterBus
    {
        public bool IsOpen { get; private set; }
        public I2cTransferCapabilities Capabilities =>
            I2cTransferCapabilities.Unbounded;
        public I2cDeviceAddress LastDevice { get; private set; }
        public RegisterOffset LastOffset { get; private set; }
        public byte[] LastWrite { get; private set; } = [];

        public ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            IsOpen = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            IsOpen = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask ReadAsync(
            I2cDeviceAddress device,
            RegisterOffset offset,
            Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            LastDevice = device;
            LastOffset = offset;
            readResult.CopyTo(destination);
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteAsync(
            I2cDeviceAddress device,
            RegisterOffset offset,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default)
        {
            LastDevice = device;
            LastOffset = offset;
            LastWrite = data.ToArray();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            IsOpen = false;
            return ValueTask.CompletedTask;
        }
    }
}
