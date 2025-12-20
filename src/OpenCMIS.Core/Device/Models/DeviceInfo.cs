namespace OpenCMIS.Core
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
        public Dictionary<string, string> ConnectionParameters { get; set; } = new();
    }

    /// <summary>
    ///     Defines the types of device connections.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        ///     I2C connection.
        /// </summary>
        I2C = 0,

        /// <summary>
        ///     USB connection.
        /// </summary>
        USB = 1,

        /// <summary>
        ///     Serial port connection.
        /// </summary>
        Serial = 2,

        /// <summary>
        ///     SPI connection.
        /// </summary>
        SPI = 3
    }
}