namespace OpenCMIS.Protocol.Abstractions.Models
{
    /// <summary>
    ///     Complete VDM (Versatile Diagnostics Monitor) snapshot.
    ///     Aggregates module-level monitors, per-lane monitors, FEC statistics,
    ///     alarm/warning flags, configuration, and operational status.
    /// </summary>
    public class VdmDiagnostics
    {
        /// <summary>Descriptor-driven VDM observable instances.</summary>
        public IReadOnlyList<VdmObservable> ObservableInstances { get; init; } = [];

        /// <summary>Module-level monitor values (temperature, VCC, laser age, etc.).</summary>
        public VdmModuleMonitor Module { get; set; } = new();

        /// <summary>Per-lane VDM monitor values.</summary>
        public List<VdmLaneMonitor> Lanes { get; set; } = [];

        /// <summary>Per-lane FEC statistics.</summary>
        public List<VdmFecStats> FecStats { get; set; } = [];

        /// <summary>Module-level VDM alarm/warning flags.</summary>
        public VdmFlags Flags { get; set; } = new();

        /// <summary>VDM configuration as currently active on the module.</summary>
        public VdmConfig Config { get; set; } = new();

        /// <summary>VDM operational status.</summary>
        public VdmStatus Status { get; set; } = new();

        /// <summary>Timestamp when this snapshot was captured.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Whether the module supports any VDM features.</summary>
        public bool IsSupported { get; set; }

        /// <summary>Human-readable summary of overall VDM health.</summary>
        public string OverallStatus => !IsSupported  ? "VDM Not Supported" :
                                       Status.HasError ? "VDM Error" :
                                       Flags.HasAnyFlag ? "Alarms Active" :
                                       "Nominal";
    }
}
