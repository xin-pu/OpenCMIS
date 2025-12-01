namespace OpenCMIS.Core;

/// <summary>
/// Represents device capabilities and features.
/// </summary>
public class DeviceCapabilities
{
    /// <summary>
    /// Gets or sets a value indicating whether CDB is supported.
    /// </summary>
    public bool SupportsCdb { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether diagnostic monitoring is supported.
    /// </summary>
    public bool SupportsDiagnosticMonitoring { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether state control is supported.
    /// </summary>
    public bool SupportsStateControl { get; set; }

    /// <summary>
    /// Gets or sets the maximum data rate.
    /// </summary>
    public int MaxDataRate { get; set; }
}

