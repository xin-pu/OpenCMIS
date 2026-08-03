using System.Text;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services
{
    internal sealed class CmisIdentityReader(IRegisterAccess registers)
    {
        private const byte VendorPage   = 0x01;
        private const int  StringLength = 16;

        public async Task<ModuleInfo> ReadSummaryAsync()
        {
            var vendor = await registers.ReadBlockAsync(
                                 VendorPage,
                                 CmisConstants.RegVendorNameStart,
                                 StringLength);
            var part = await registers.ReadBlockAsync(
                               VendorPage,
                               CmisConstants.RegPartNumberStart,
                               StringLength);
            var serial = await registers.ReadBlockAsync(
                                 VendorPage,
                                 CmisConstants.RegSerialNumberStart,
                                 StringLength);
            var identifier = await registers.ReadByteAsync(
                                     0x00,
                                     CmisConstants.RegIdentifier);
            var revision = await registers.ReadByteAsync(
                                   0x00,
                                   CmisConstants.RegRevision);
            var flags = await registers.ReadBlockAsync(
                                0x00,
                                CmisConstants.RegModuleFlags,
                                2);

            return new()
                   {
                       VendorName   = ReadAscii(vendor),
                       PartNumber   = ReadAscii(part),
                       SerialNumber = ReadAscii(serial),
                       ModuleType   = GetModuleTypeName(identifier),
                       CmisVersion  = $"{revision >> 4}.{revision & 0x0F}",
                       Capabilities = new()
                                      {
                                          SupportsCdb                  = (flags[0] & 0x01) != 0,
                                          SupportsDiagnosticMonitoring = (flags[0] & 0x02) != 0,
                                          SupportsStateControl         = (flags[0] & 0x04) != 0,
                                          MaxDataRate                  = flags[1]
                                      }
                   };
        }

        public async Task<ModuleIdentity> ReadAsync()
        {
            var vendor = await registers.ReadBlockAsync(
                                 VendorPage,
                                 CmisConstants.RegVendorNameStart,
                                 StringLength);
            var oui = await registers.ReadBlockAsync(
                              VendorPage,
                              CmisConstants.RegVendorOUI,
                              3);
            var part = await registers.ReadBlockAsync(
                               VendorPage,
                               CmisConstants.RegPartNumberStart,
                               StringLength);
            var serial = await registers.ReadBlockAsync(
                                 VendorPage,
                                 CmisConstants.RegSerialNumberStart,
                                 StringLength);
            var hardware = await registers.ReadBlockAsync(
                                   VendorPage,
                                   CmisConstants.RegHardwareRevision,
                                   2);
            var firmware = await registers.ReadBlockAsync(
                                   VendorPage,
                                   CmisConstants.RegFirmwareRevision,
                                   2);
            var date = await registers.ReadBlockAsync(
                               VendorPage,
                               CmisConstants.RegDateCode,
                               8);
            var clei = await registers.ReadBlockAsync(
                               VendorPage,
                               CmisConstants.RegCLEICode,
                               10);
            var identifier = await registers.ReadByteAsync(
                                     0x00,
                                     CmisConstants.RegIdentifier);
            var revision = await registers.ReadByteAsync(
                                   0x00,
                                   CmisConstants.RegRevision);

            return new()
                   {
                       VendorName       = ReadAscii(vendor),
                       VendorOUI        = $"{oui[0]:X2}-{oui[1]:X2}-{oui[2]:X2}",
                       PartNumber       = ReadAscii(part),
                       SerialNumber     = ReadAscii(serial),
                       HardwareRevision = ReadBcd(hardware, 0),
                       FirmwareRevision = ReadBcd(firmware, 1),
                       DateCode         = ReadAscii(date),
                       CLEICode         = ReadAscii(clei),
                       ModuleType       = GetModuleTypeName(identifier),
                       ConnectorType    = GetConnectorTypeName(identifier),
                       CmisVersion      = $"{revision >> 4}.{revision & 0x0F}"
                   };
        }

        private static string ReadAscii(byte[] bytes)
        {
            var endIndex = Array.IndexOf(bytes, (byte) 0);
            if (endIndex < 0)
                endIndex = bytes.Length;

            var printable = bytes[..endIndex]
                           .Where(value => value is >= 0x20 and <= 0x7E)
                           .ToArray();
            return Encoding.ASCII.GetString(printable).TrimEnd();
        }

        private static string ReadBcd(byte[] bytes, int decimalPlaces)
        {
            var value = 0;
            foreach (var item in bytes)
                value = value * 100 + (item >> 4) * 10 + (item & 0x0F);

            if (decimalPlaces == 0)
                return value.ToString();

            var divisor = (int) Math.Pow(10, decimalPlaces);
            return $"{value / divisor}.{(value % divisor).ToString(new string('0', decimalPlaces))}";
        }

        private static string GetModuleTypeName(byte identifier)
        {
            return identifier switch
                   {
                       0x1E => "QSFP-DD",
                       0x1F => "OSFP",
                       0x18 => "QSFP28",
                       0x0D => "QSFP+",
                       0x0C => "CFP2",
                       0x0B => "CFP4",
                       0x06 => "SFP+",
                       _    => $"Unknown (0x{identifier:X2})"
                   };
        }

        private static string GetConnectorTypeName(byte identifier)
        {
            return identifier switch
                   {
                       0x1E => "QSFP-DD (76-pin)",
                       0x1F => "OSFP (60-pin)",
                       0x18 => "QSFP28 (38-pin)",
                       0x0D => "QSFP+ (38-pin)",
                       0x0C => "CFP2 (104-pin)",
                       0x0B => "CFP4 (56-pin)",
                       0x06 => "SFP+ (20-pin)",
                       0x03 => "SFP (20-pin)",
                       _    => $"Connector 0x{identifier:X2}"
                   };
        }
    }
}
