namespace OpenCMIS.Protocol.Abstractions.Models;

/// <summary>A single CMIS VDM observable described by its raw descriptor.</summary>
public sealed class VdmObservable
{
    public int Instance { get; init; }

    /// <summary>The two descriptor bytes advertised by the module.</summary>
    private byte[] _descriptor = [];

    public byte[] Descriptor
    {
        get => _descriptor.ToArray();
        init => _descriptor = value?.ToArray() ?? [];
    }

    /// <summary>The raw unsigned 16-bit sample associated with the descriptor.</summary>
    public ushort Sample { get; init; }

    public VdmObservableFlags Flags { get; init; } = new();
}
