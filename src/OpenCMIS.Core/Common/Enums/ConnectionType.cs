namespace OpenCMIS.Core
{
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

