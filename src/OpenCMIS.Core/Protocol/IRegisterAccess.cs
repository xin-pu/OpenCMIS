namespace OpenCMIS.Core;

/// <summary>
/// Provides interface for register access operations in CMIS protocol.
/// </summary>
public interface IRegisterAccess
{
    /// <summary>
    /// Reads a single byte from the specified register.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="address">The register address.</param>
    /// <returns>The byte value read from the register.</returns>
    Task<byte> ReadByteAsync(byte page, byte address);

    /// <summary>
    /// Writes a single byte to the specified register.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="address">The register address.</param>
    /// <param name="value">The byte value to write.</param>
    Task WriteByteAsync(byte page, byte address, byte value);

    /// <summary>
    /// Reads a block of data from the specified register range.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="startAddress">The starting register address.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The byte array containing the read data.</returns>
    Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length);

    /// <summary>
    /// Writes a block of data to the specified register range.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="startAddress">The starting register address.</param>
    /// <param name="data">The byte array containing the data to write.</param>
    Task WriteBlockAsync(byte page, byte startAddress, byte[] data);
}

