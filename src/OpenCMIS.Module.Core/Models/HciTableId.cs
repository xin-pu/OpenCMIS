namespace OpenCMIS.Module.Core.Hci;

/// <summary>
/// Represents a vendor HCI table identifier.
/// </summary>
public readonly record struct HciTableId(byte Value)
{
    public override string ToString()
    {
        return $"0x{Value:X2}";
    }
}
