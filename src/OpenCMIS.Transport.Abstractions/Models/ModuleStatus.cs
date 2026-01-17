using OpenCMIS.Shared;

namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Represents the current module status.
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
        ///     Gets or sets a value indicating whether there are any active alerts.
        /// </summary>
        public bool HasAlerts { get; set; }

        /// <summary>
        ///     Gets or sets the list of active alerts.
        /// </summary>
        public List<string> ActiveAlerts { get; set; } = new();
    }
}