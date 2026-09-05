namespace OpenCMIS.Protocol.Abstractions.Models;

/// <summary>Threshold-crossing flags; null means the flag could not be read.</summary>
public sealed class VdmObservableFlags
{
    public bool? HighAlarm { get; init; }
    public bool? HighWarning { get; init; }
    public bool? LowWarning { get; init; }
    public bool? LowAlarm { get; init; }
}
