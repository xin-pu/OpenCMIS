namespace OpenCMIS.Core;

/// <summary>
/// Provides interface for device management operations.
/// </summary>
public interface IDeviceManager
{
    /// <summary>
    /// Enumerates all available devices.
    /// </summary>
    /// <returns>A collection of device information.</returns>
    Task<IEnumerable<DeviceInfo>> EnumerateDevicesAsync();

    /// <summary>
    /// Opens a device connection using the specified device information.
    /// </summary>
    /// <param name="deviceInfo">The device information.</param>
    /// <returns>The CMIS device interface.</returns>
    Task<ICmisDevice> OpenDeviceAsync(DeviceInfo deviceInfo);

    /// <summary>
    /// Closes the specified device connection.
    /// </summary>
    /// <param name="device">The CMIS device to close.</param>
    Task CloseDeviceAsync(ICmisDevice device);
}

