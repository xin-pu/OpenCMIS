using OpenCMIS.Shared;

namespace OpenCMIS.Protocol.Abstractions.Models
{
    /// <summary>
    ///     Represents the current module status with full decoded flags and raw debug data.
    /// </summary>
    public class ModuleStatus
    {
        /// <summary>
        ///     Gets or sets the current module state.
        /// </summary>
        public ModuleState CurrentState { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the module is ready.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        ///     Gets or sets the raw status register byte (0x02) for debugging.
        /// </summary>
        public byte RawStatusByte { get; set; }

        /// <summary>
        ///     Gets or sets the raw module state register byte (0x03) for debugging.
        /// </summary>
        public byte RawStateByte { get; set; }

        /// <summary>
        ///     Gets or sets whether the data path firmware has reported a fault.
        /// </summary>
        public bool DataPathFirmwareFault { get; set; }

        /// <summary>
        ///     Gets or sets the decoded interrupt flags.
        /// </summary>
        public CmisInterruptFlags InterruptFlags { get; set; } = new ();

        /// <summary>
        ///     Gets or sets the decoded module flags.
        /// </summary>
        public CmisModuleFlags ModuleFlags { get; set; } = new ();

        /// <summary>
        ///     Gets or sets a value indicating whether there are any active alerts.
        /// </summary>
        public bool HasAlerts { get; set; }

        /// <summary>
        ///     Gets or sets the list of active alerts.
        /// </summary>
        public List<string> ActiveAlerts { get; set; } = new ();

        /// <summary>
        ///     Gets or sets whether the temperature alarm is active.
        /// </summary>
        public bool TempAlarm { get; set; }

        /// <summary>
        ///     Gets or sets whether the temperature warning is active.
        /// </summary>
        public bool TempWarning { get; set; }

        /// <summary>
        ///     Gets or sets whether the VCC alarm is active.
        /// </summary>
        public bool VccAlarm { get; set; }

        /// <summary>
        ///     Gets or sets whether the VCC warning is active.
        /// </summary>
        public bool VccWarning { get; set; }
    }
}
