using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Module.Core.Msa
{
    /// <summary>
    ///     Provides page-aware access to optical-module MSA memory.
    /// </summary>
    public interface IMsaMemoryAccessor
    {
        ValueTask<byte[]> ReadAsync(I2cDeviceAddress  device,
                                    ModulePage        page,
                                    RegisterOffset    offset,
                                    int               length,
                                    CancellationToken cancellationToken = default);

        ValueTask WriteAsync(I2cDeviceAddress     device,
                             ModulePage           page,
                             RegisterOffset       offset,
                             ReadOnlyMemory<byte> data,
                             CancellationToken    cancellationToken = default);
    }
}
