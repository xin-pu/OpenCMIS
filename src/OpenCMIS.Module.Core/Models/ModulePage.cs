namespace OpenCMIS.Module.Core;

/// <summary>
/// Represents an MSA memory page.
/// </summary>
public readonly record struct ModulePage(byte Value)
{
    public override string ToString()
    {
        return $"0x{Value:X2}";
    }
}
