namespace OpenCMIS.UI.WPF.Models;

public sealed record MsaWriteSegment
{
    public MsaWriteSegment(byte startAddress, IEnumerable<byte> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        StartAddress = startAddress;
        Data = data.ToArray();
    }

    public byte StartAddress { get; }
    public byte[] Data { get; }
}
