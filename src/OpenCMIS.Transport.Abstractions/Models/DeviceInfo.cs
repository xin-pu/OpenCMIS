using OpenCMIS.Shared;

namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Represents device information.
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        ///     Gets or sets the device identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the device name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the connection type.
        /// </summary>
        public ConnectionType ConnectionType { get; set; }

        /// <summary>
        ///     Gets or sets the connection parameters.
        /// </summary>
        public Dictionary<string, string> ConnectionParameters { get; set; } = new ();

        /// <summary>
        ///     Gets or sets the typed I2C connection profile.
        /// </summary>
        public I2cConnectionProfile? Profile { get; set; }
    }
}
