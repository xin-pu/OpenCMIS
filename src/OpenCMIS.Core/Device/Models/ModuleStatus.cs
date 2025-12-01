namespace OpenCMIS.Core;

/// <summary>
/// Represents the current module status.
/// </summary>
public class ModuleStatus
{
    /// <summary>
    /// Gets or sets the current module state.
    /// </summary>
    public ModuleState CurrentState { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the module is ready.
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there are any active alerts.
    /// </summary>
    public bool HasAlerts { get; set; }

    /// <summary>
    /// Gets or sets the list of active alerts.
    /// </summary>
    public List<string> ActiveAlerts { get; set; } = new();
}

/// <summary>
/// Defines the module state machine states.
/// </summary>
public enum ModuleState
{
    /// <summary>
    /// Initialization state.
    /// </summary>
    Initialization = 0,

    /// <summary>
    /// Low power state.
    /// </summary>
    LowPwr = 1,

    /// <summary>
    /// Power up state.
    /// </summary>
    PwrUp = 2,

    /// <summary>
    /// Ready state.
    /// </summary>
    Ready = 3,

    /// <summary>
    /// Power down state.
    /// </summary>
    PwrDn = 4,

    /// <summary>
    /// Fault state.
    /// </summary>
    Fault = 5
}

