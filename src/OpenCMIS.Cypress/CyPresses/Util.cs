/*
 ## Cypress CyUSB C# library source file (Util.cs)
 ## =======================================================
 ##
 ##  Copyright Cypress Semiconductor Corporation, 2009-2012,
 ##  All Rights Reserved
 ##  UNPUBLISHED, LICENSED SOFTWARE.
 ##
 ##  CONFIDENTIAL AND PROPRIETARY INFORMATION
 ##  WHICH IS THE PROPERTY OF CYPRESS.
 ##
 ##  Use of this file is governed
 ##  by the license agreement included in the file
 ##
 ##  <install>/license/license.rtf
 ##
 ##  where <install> is the Cypress software
 ##  install root directory path.
 ##
 ## =======================================================
*/

using System.Collections;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Summary description for Util.
    /// </summary>
    public static class Util
    {
        public static ushort MaxFwSize { get; set; } = 0xFFFF;

        public static string Assemblies
        {
            get
            {
                // Get all the assemblies currently loaded in the application domain.
                var myAssemblies = Thread.GetDomain().GetAssemblies();

                var assemblyList = ""; // "Assemblies\r\n----------\r\n\n";

                foreach (var a in myAssemblies)
                {
                    var assName = a.GetName().Name;
                    var assVer = a.GetName().Version.ToString();

                    var a1 = assName.Length;
                    var a2 = assVer.Length;

                    if (assName.IndexOf("System")    == -1 &&
                        assName.IndexOf("mscorlib")  == -1 &&
                        assName.IndexOf("Microsoft") == -1 &&
                        assName.IndexOf("vshost")    == -1)
                        assemblyList += $"Assembly:  {assName}  ({assVer})\r\n";
                }

                return assemblyList;
            }
        }

        public static int ReverseBytes(byte[] dta, int xStart, int bytes)
        {
            if (bytes < 0) return 0;

            var tmp = new byte[bytes];

            for (byte i = 0; i < bytes; i++) tmp[i] = dta[xStart + bytes - i - 1];      // Reverse
            for (byte i = 0; i < bytes; i++) dta[xStart                  + i] = tmp[i]; // Copy back

            var v = bytes > 2 ? BitConverter.ToInt32(tmp, 0) : BitConverter.ToInt16(tmp, 0);

            return v;
        }

        public static unsafe int ReverseBytes(byte * dta, int bytes)
        {
            if (bytes < 0) return 0;

            var tmp = new byte[bytes];

            for (byte i = 0; i < bytes; i++) tmp[i] = *(dta + bytes - i - 1);      // Reverse
            for (byte i = 0; i < bytes; i++) *(dta                  + i) = tmp[i]; // Copy back

            var v = bytes > 2 ? BitConverter.ToInt32(tmp, 0) : BitConverter.ToInt16(tmp, 0);

            return v;
        }

        public static ulong HexToInt(string hexString)
        {
            var HexChars = "0123456789abcdef";

            var s = hexString.ToLower();

            // Trim off the 0x prefix
            if (s.Length > 2)
                if (s.Substring(0, 2).Equals("0x"))
                    s = s.Substring(2, s.Length - 2);

            var _s = "";
            var len = s.Length;

            // Reverse the digits
            for (var i = len - 1; i >= 0; i--) _s += s[i];

            ulong sum = 0;
            ulong pwrF = 1;
            for (var i = 0; i < len; i++)
            {
                var ordinal = (uint) HexChars.IndexOf(_s[i]);
                sum  += i == 0 ? ordinal : pwrF * ordinal;
                pwrF *= 16;
            }

            return sum;
        }

        public static bool ParseHexFile(string fName, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            if (!File.Exists(fName)) return false;

            var rawList = new ArrayList();

            string line;

            var srcStream = new StreamReader(fName);
            if (srcStream == null) return false;
            while ((line = srcStream.ReadLine()) != null)
                rawList.Add(line);

            srcStream.Close();
            return ParseHexData(rawList, FwBuf, ref FwLen, ref FwOff);
        }

        public static bool ParseHexData(ArrayList rawList, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            string line, tmp;
            int v;

            // Delete non-data records
            for (var i = rawList.Count - 1; i >= 0; i--)
            {
                line = (string) rawList[i];
                if (line.Length > 0)
                {
                    tmp = line.Substring(7, 2); // Get the Record Type into v
                    v   = (int) HexToInt(tmp);
                    if (v != 0) rawList.Remove(rawList[i]); // Data records are type == 0
                }
            }

            FwLen = 0;
            FwOff = MaxFwSize;

            // Initialize the FwImage[] buffer
            for (var i = 0; i < MaxFwSize; i++) FwBuf[i] = 0xFF;

            // Extract the FW data bytes, placing into location of FwImage indicated
            for (var i = 0; i < rawList.Count; i++)
            {
                line = (string) rawList[i];

                // Remove comments
                v = line.IndexOf("//");
                if (v > -1)
                    line = line.Substring(0, v - 1);

                // Build string that just contains the offset followed by the data bytes
                if (line.Length > 0)
                {
                    // Get the offset
                    var sOffset = line.Substring(3, 4);
                    var dx = (ushort) HexToInt(sOffset);
                    if (dx >= MaxFwSize) return false;

                    if (dx < FwOff) FwOff = dx;

                    // Get the string of data chars
                    tmp = line.Substring(1, 2);
                    v   = (int) HexToInt(tmp) * 2;
                    var s = line.Substring(9, v);

                    var bytes = v / 2;

                    for (var b = 0; b < bytes; b++, dx++)
                        FwBuf[dx] = (byte) HexToInt(s.Substring(b * 2, 2));

                    if (dx > FwLen) FwLen = dx;
                }
            }

            return true;
        }

        public static bool ParseIICFile(string fName, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            if (!File.Exists(fName)) return false;

            // Suck-in the data from the .iic file
            Stream fStream = new FileStream(fName, FileMode.Open, FileAccess.Read);
            if (fStream == null) return false;
            var fSize = (int) fStream.Length;
            var fData = new byte[fSize];
            fStream.Read(fData, 0, fSize);
            fStream.Close();

            if (fSize > MaxFwSize) return false;

            ParseIICData(fData, FwBuf, ref FwLen, ref FwOff);

            return true;
        }

        public static unsafe void ParseIICData(byte[] fData, byte[] FwBuf, ref ushort FwLen, ref ushort FwOff)
        {
            ushort dx = 8;
            FwLen = 0;
            FwOff = MaxFwSize;

            for (var i = 0; i < MaxFwSize; i++) FwBuf[i] = 0xFF;

            fixed (byte * buf = fData)
            {
                ushort * dLen;
                ushort * addr;

                do
                {
                    ReverseBytes(fData, dx,     2);
                    ReverseBytes(fData, dx + 2, 2);
                    dLen = (ushort *) (buf + dx);
                    addr = (ushort *) (buf + dx + 2);

                    if (*dLen != 0x8001)
                    {
                        Array.Copy(fData, dx + 4, FwBuf, *addr, *dLen);
                        var lastDta = (ushort) (*addr + *dLen);
                        if (lastDta > FwLen) FwLen = lastDta;
                        if (*addr   < FwOff) FwOff = *addr;
                    }

                    dx += (ushort) (*dLen + 4);
                } while (*dLen != 0x8001 && dx < fData.Length);
            }
        }

        public static string byteStr(ushort val)
        {
            var b1 = (byte) ((val >> 8) & 0x00FF);
            var b2 = (byte) (val        & 0x00FF);
            return $"{b1:X2} {b2:X2}";
        }
    }
}