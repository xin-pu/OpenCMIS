namespace OpenCMIS.Core;

/// <summary>
/// Provides utility methods for byte array operations.
/// </summary>
public static class ByteArrayHelper
{
    /// <summary>
    /// Converts a byte array to a hexadecimal string.
    /// </summary>
    /// <param name="bytes">The byte array to convert.</param>
    /// <returns>The hexadecimal string representation.</returns>
    public static string ToHexString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", " ");
    }

    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hexString">The hexadecimal string to convert.</param>
    /// <returns>The byte array representation.</returns>
    public static byte[] FromHexString(string hexString)
    {
        hexString = hexString.Replace(" ", "").Replace("-", "");
        int length = hexString.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return bytes;
    }
}

