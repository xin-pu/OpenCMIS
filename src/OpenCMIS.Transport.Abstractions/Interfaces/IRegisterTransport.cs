namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Provides register-level access on top of a device connection.
    /// </summary>
    public interface IRegisterTransport : IDeviceConnection
    {
        /// <summary>
        ///     Reads a byte from the specified register address.
        /// </summary>
        /// <param name="registerAddress">The register address.</param>
        /// <returns>The byte value read from the register.</returns>
        Task<byte> ReadRegisterAsync(byte registerAddress);

        /// <summary>
        ///     Writes a byte to the specified register address.
        /// </summary>
        /// <param name="registerAddress">The register address.</param>
        /// <param name="value">The byte value to write.</param>
        Task WriteRegisterAsync(byte registerAddress, byte value);

        /// <summary>
        ///     Reads a block of data from the specified register address range.
        /// </summary>
        /// <param name="registerAddress">The starting register address.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The byte array containing the read data.</returns>
        Task<byte[]> ReadRegisterBlockAsync(byte registerAddress, int length);

        /// <summary>
        ///     Writes a block of data to the specified register address range.
        /// </summary>
        /// <param name="registerAddress">The starting register address.</param>
        /// <param name="data">The byte array containing the data to write.</param>
        Task WriteRegisterBlockAsync(byte registerAddress, byte[] data);
    }
}
