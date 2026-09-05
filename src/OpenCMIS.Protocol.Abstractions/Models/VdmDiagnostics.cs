namespace OpenCMIS.Protocol.Abstractions.Models
{
    /// <summary>Descriptor-driven, read-only VDM snapshot.</summary>
    public class VdmDiagnostics
    {
        /// <summary>Descriptor-driven VDM observable instances.</summary>
        public IReadOnlyList<VdmObservable> ObservableInstances { get; init; } = [];

        /// <summary>Whether the module supports any VDM features.</summary>
        public bool IsSupported { get; set; }
    }
}
