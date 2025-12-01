namespace OpenCMIS.Core;

/// <summary>
/// Represents module identification and capability information.
/// </summary>
public class ModuleInfo
{
    /// <summary>
    /// Gets or sets the vendor name.
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the part number.
    /// </summary>
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serial number.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module type.
    /// </summary>
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CMIS version.
    /// </summary>
    public string CmisVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device capabilities.
    /// </summary>
    public DeviceCapabilities Capabilities { get; set; } = new();
}

