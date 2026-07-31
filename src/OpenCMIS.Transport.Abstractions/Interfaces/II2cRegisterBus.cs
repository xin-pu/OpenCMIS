namespace OpenCMIS.Transport.Abstractions;

/// <summary>
/// Provides target-address-aware register access over an I2C adapter.
/// </summary>
public interface II2cRegisterBus : IAsyncDisposable
{
    bool IsOpen { get; }

    I2cTransferCapabilities Capabilities { get; }

    ValueTask OpenAsync(CancellationToken cancellationToken = default);

    ValueTask CloseAsync(CancellationToken cancellationToken = default);

    ValueTask ReadAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default);

    ValueTask WriteAsync(
        I2cDeviceAddress device,
        RegisterOffset offset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);
}
