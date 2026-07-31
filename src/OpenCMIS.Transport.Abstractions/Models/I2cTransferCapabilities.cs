namespace OpenCMIS.Transport.Abstractions;

/// <summary>
/// Describes the maximum payload sizes supported by an I2C adapter.
/// </summary>
public sealed record I2cTransferCapabilities
{
    public I2cTransferCapabilities(int maxReadLength, int maxWriteLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxReadLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWriteLength);

        MaxReadLength = maxReadLength;
        MaxWriteLength = maxWriteLength;
    }

    public int MaxReadLength { get; }

    public int MaxWriteLength { get; }

    public static I2cTransferCapabilities Unbounded { get; } =
        new(int.MaxValue, int.MaxValue);
}
