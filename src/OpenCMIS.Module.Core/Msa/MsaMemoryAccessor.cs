using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Module.Core.Msa;

/// <summary>
/// Performs MSA page selection and transfer as one atomic session operation.
/// </summary>
public sealed class MsaMemoryAccessor(OpticalModuleSession session)
    : IMsaMemoryAccessor
{
    private static readonly RegisterOffset PageSelectRegister = new(0x7F);

    public ValueTask<byte[]> ReadAsync(
        I2cDeviceAddress device,
        ModulePage page,
        RegisterOffset offset,
        int length,
        CancellationToken cancellationToken = default)
    {
        ValidateRange(offset, length, nameof(length));

        return session.ExecuteAsync(
            async (bus, token) =>
            {
                await SelectPageAsync(bus, device, page, token)
                    .ConfigureAwait(false);
                var result = new byte[length];
                await bus.ReadAsync(device, offset, result, token)
                    .ConfigureAwait(false);
                return result;
            },
            cancellationToken);
    }

    public async ValueTask WriteAsync(
        I2cDeviceAddress device,
        ModulePage page,
        RegisterOffset offset,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        ValidateRange(offset, data.Length, nameof(data));

        await session.ExecuteAsync(
                async (bus, token) =>
                {
                    await SelectPageAsync(bus, device, page, token)
                        .ConfigureAwait(false);
                    await bus.WriteAsync(device, offset, data, token)
                        .ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static void ValidateRange(
        RegisterOffset offset,
        int length,
        string parameterName)
    {
        if (length <= 0)
        {
            throw new ArgumentException(
                "Transfer length must be positive.",
                parameterName);
        }

        if (offset.Value + length > 256)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                length,
                "Transfer exceeds the eight-bit module page.");
        }
    }

    private static async ValueTask SelectPageAsync(
        II2cRegisterBus bus,
        I2cDeviceAddress device,
        ModulePage page,
        CancellationToken cancellationToken)
    {
        try
        {
            await bus.WriteAsync(
                    device,
                    PageSelectRegister,
                    new byte[] { page.Value },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new OpenCMIS.Shared.CmisException(
                OpenCMIS.Shared.CmisErrorCode.MsaPageSelectionFailed,
                exception);
        }
    }
}
