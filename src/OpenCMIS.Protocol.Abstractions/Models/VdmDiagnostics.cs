namespace OpenCMIS.Protocol.Abstractions.Models
{
    public enum VdmReadStatus { Complete, Partial, Unavailable }

    /// <summary>Descriptor-driven, read-only VDM snapshot.</summary>
    public class VdmDiagnostics
    {
        /// <summary>Whether all advertised data could be read. Unavailable may mean support is unknown.</summary>
        public VdmReadStatus ReadStatus { get; init; }

        /// <summary>Descriptor-driven VDM observable instances.</summary>
        public IReadOnlyList<VdmObservable> ObservableInstances { get; init; } = [];

        /// <summary>Whether the module supports any VDM features.</summary>
        public bool IsSupported { get; set; }
    }
}
