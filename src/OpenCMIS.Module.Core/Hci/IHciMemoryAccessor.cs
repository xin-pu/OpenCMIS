using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Module.Core.Hci;

/// <summary>
/// Provides vendor HCI table access.
/// </summary>
public interface IHciMemoryAccessor
{
    ValueTask<byte[]> ReadAsync(
        I2cDeviceAddress device,
        HciTableId table,
        RegisterOffset offset,
        int length,
        CancellationToken cancellationToken = default);

    ValueTask WriteAsync(
        I2cDeviceAddress device,
        HciTableId table,
        RegisterOffset offset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);
}
