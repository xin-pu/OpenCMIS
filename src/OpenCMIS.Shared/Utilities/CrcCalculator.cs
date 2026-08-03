namespace OpenCMIS.Shared
{
    /// <summary>
    ///     Provides CRC-16 calculation utilities for CMIS protocol data integrity.
    /// </summary>
    public static class CrcCalculator
    {
        private const ushort Polynomial   = 0x1021;
        private const ushort InitialValue = 0xFFFF;

        /// <summary>
        ///     Calculates the CRC-16/CCITT checksum for the given data.
        /// </summary>
        /// <param name="data">The data to calculate the CRC for.</param>
        /// <returns>The 16-bit CRC value.</returns>
        public static ushort CalculateCrc16(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            var crc = InitialValue;

            foreach (var b in data)
            {
                crc ^= (ushort) (b << 8);

                for (var i = 0; i < 8; i++)
                    if ((crc & 0x8000) != 0)
                        crc = (ushort) (crc << 1 ^ Polynomial);
                    else
                        crc <<= 1;
            }

            return crc;
        }

        /// <summary>
        ///     Verifies the CRC-16 checksum of the given data against the expected value.
        /// </summary>
        /// <param name="data">The data to verify.</param>
        /// <param name="expectedCrc">The expected CRC value.</param>
        /// <returns>True if the CRC matches; otherwise, false.</returns>
        public static bool VerifyCrc16(byte[] data, ushort expectedCrc)
        {
            var calculated = CalculateCrc16(data);
            return calculated == expectedCrc;
        }
    }
}
