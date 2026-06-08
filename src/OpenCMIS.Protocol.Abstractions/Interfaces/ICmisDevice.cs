using OpenCMIS.Shared;
using OpenCMIS.Shared.Models;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Protocol.Abstractions
{
    /// <summary>
    ///     Provides interface for CMIS device operations.
    /// </summary>
    public interface ICmisDevice
    {
        /// <summary>
        ///     Gets the device information.
        /// </summary>
        DeviceInfo DeviceInfo { get; }

        /// <summary>
        ///     Gets a value indicating whether the device is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Gets the module information.
        /// </summary>
        /// <returns>The module information.</returns>
        Task<ModuleInfo> GetModuleInfoAsync();

        /// <summary>
        ///     Gets the current module status.
        /// </summary>
        /// <returns>The module status.</returns>
        Task<ModuleStatus> GetStatusAsync();

        /// <summary>
        ///     Sets the module state.
        /// </summary>
        /// <param name="state">The target module state.</param>
        Task SetStateAsync(ModuleState state);

        /// <summary>
        ///     Gets the register access interface for low-level operations.
        /// </summary>
        IRegisterAccess RegisterAccess { get; }

        /// <summary>
        ///     Reads the full module identity information.
        /// </summary>
        /// <returns>Module identity with vendor, part, revision, and type details.</returns>
        Task<ModuleIdentity> ReadModuleIdentityAsync();

        /// <summary>
        ///     Reads module monitoring values (temperature, VCC, per-lane power/bias).
        /// </summary>
        /// <param name="laneCount">Number of lanes to read.</param>
        /// <returns>Populated module monitors.</returns>
        Task<ModuleMonitors> ReadModuleMonitorsAsync(int laneCount = 4);

        /// <summary>
        ///     Reads per-lane status information.
        /// </summary>
        /// <param name="laneCount">Number of lanes to read.</param>
        /// <returns>List of lane status entries.</returns>
        Task<List<LaneStatus>> ReadLaneStatusAsync(int laneCount = 4);

        /// <summary>
        ///     Reads all dashboard data: identity, monitors, lanes, and status.
        /// </summary>
        /// <param name="laneCount">Number of lanes to read.</param>
        /// <returns>Composite dashboard snapshot.</returns>
        Task<ModuleDashData> ReadModuleDashDataAsync(int laneCount = 4);

        /// <summary>
        ///     Closes the device connection.
        /// </summary>
        Task CloseAsync();
    }
}