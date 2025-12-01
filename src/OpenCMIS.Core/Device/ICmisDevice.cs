namespace OpenCMIS.Core;

/// <summary>
/// Provides interface for CMIS device operations.
/// </summary>
public interface ICmisDevice
{
    /// <summary>
    /// Gets the device information.
    /// </summary>
    DeviceInfo DeviceInfo { get; }

    /// <summary>
    /// Gets a value indicating whether the device is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the module information.
    /// </summary>
    /// <returns>The module information.</returns>
    Task<ModuleInfo> GetModuleInfoAsync();

    /// <summary>
    /// Gets the current module status.
    /// </summary>
    /// <returns>The module status.</returns>
    Task<ModuleStatus> GetStatusAsync();

    /// <summary>
    /// Sets the module state.
    /// </summary>
    /// <param name="state">The target module state.</param>
    Task SetStateAsync(ModuleState state);

    /// <summary>
    /// Closes the device connection.
    /// </summary>
    Task CloseAsync();
}

