namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides utility methods for endianness conversion.
    /// </summary>
    public static class EndianConverter
    {
        /// <summary>
        ///     Converts a 16-bit value from little-endian to big-endian or vice versa.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static ushort SwapBytes(ushort value)
        {
            return (ushort)(value >> 8 | value << 8);
        }

        /// <summary>
        ///     Converts a 32-bit value from little-endian to big-endian or vice versa.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static uint SwapBytes(uint value)
        {
            return value >> 24 |
                   value >> 8 & 0x0000FF00 |
                   value << 8 & 0x00FF0000 |
                   value << 24;
        }
    }
}