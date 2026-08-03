namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Class of Cypress utilities
    /// </summary>
    public static class CyUtils
    {
        /// <summary>
        ///     Bytes the array to hex string.
        /// </summary>
        /// <param name="ba">The ba.</param>
        /// <returns></returns>
        public static string ByteArrayToHexString(byte[] ba)
        {
            var hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        /// <summary>
        ///     ASCIIs the string to dec string.
        /// </summary>
        /// <param name="asc">The asc.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        public static string AsciiStringToDecString(string asc, int offset)
        {
            var NumberChars = asc.Length;
            var retDec      = string.Empty;

            for (var i = 0; i <= NumberChars - 1; i++)
            {
                // OFFSET the serial number assigned which was ADDED with offset
                var val  = Convert.ToInt32(Convert.ToByte(asc[i])) - offset;
                var temp = val.ToString("D2");
                retDec = retDec + temp;
            }

            return retDec;
        }

        // ASCII characters to byte[]
        /// <summary>
        ///     ASCIIs the string to byte array.
        /// </summary>
        /// <param name="asc">The asc.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        public static byte[] AsciiStringToByteArray(string asc, int offset)
        {
            var ba          = new byte[asc.Length];
            var NumberChars = asc.Length;

            for (var i = 0; i <= NumberChars - 1; i++)
            {
                // OFFSET the serial number assigned which was ADDED with offset
                var val = Convert.ToInt32(Convert.ToByte(asc[i]));
                ba[i] = Convert.ToByte(val - offset);
            }

            return ba;
        }

        /// <summary>
        ///     Bytes to string.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public static string ByteToString(byte bytes)
        {
            var hexString = "";

            //for (int i = 0; i < bytes.Length; i++)
            //{
            hexString += bytes.ToString("X2");

            //}
            return hexString;
        }
    }
}
