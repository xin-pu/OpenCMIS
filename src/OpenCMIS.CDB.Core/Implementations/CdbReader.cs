using System.Text;
using OpenCMIS.CDB.Abstractions;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.CDB.Core
{
    /// <summary>
    ///     Provides implementation for reading Configuration Data Blocks from CMIS devices.
    /// </summary>
    public class CdbReader : ICdbReader
    {
        private const byte CdbPage         = 0x9F;
        private const byte CdbExtPageStart = 0xA0;
        private const byte CdbHeaderOffset = 0x80;
        private const int  UpperPageSize   = 128;
        private const int  MaxCdbSize      = 4096;

        /// <inheritdoc />
        public async Task<ConfigurationDataBlock> ReadAsync(ICmisDevice device)
        {
            if (device == null)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(device));

            if (!device.IsConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            try
            {
                // Read CDB header: 4 bytes (Length[2], Version[1], Flags[1])
                var headerBytes = await device.RegisterAccess.ReadBlockAsync(CdbPage, CdbHeaderOffset, 4);

                var totalLength = (ushort) (headerBytes[0] | headerBytes[1] << 8);
                var version     = headerBytes[2];
                var flags       = headerBytes[3];

                if (totalLength < 8 || totalLength > MaxCdbSize)
                    throw new CmisException(CmisErrorCode.CdbFormatError, totalLength);

                // Read the complete CDB (header + body + checksum) across pages
                var cdbBytes = await ReadCdbAcrossPagesAsync(device, totalLength);

                // Parse header
                var cdb = new ConfigurationDataBlock
                          {
                              Header = new()
                                       {
                                           Length  = totalLength,
                                           Version = version,
                                           Flags   = flags
                                       },
                              Version = new()
                                        {
                                            Major = (byte) (version >> 4),
                                            Minor = (byte) (version & 0x0F)
                                        }
                          };

                // Extract checksum (last 2 bytes)
                var bodyLength = totalLength - 2;
                cdb.Checksum = (ushort) (cdbBytes[bodyLength] | cdbBytes[bodyLength + 1] << 8);

                // Parse fields from body (skip 4-byte header)
                var fieldOffset = 4;
                while (fieldOffset < bodyLength)
                {
                    var field = ParseField(cdbBytes, ref fieldOffset);
                    if (field != null)
                        cdb.Fields.Add(field);
                    else
                        break;
                }

                return cdb;
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.CdbReadFailed, ex);
            }
        }

        private static CdbField? ParseField(byte[] data, ref int offset)
        {
            if (offset + 2 > data.Length)
                return null;

            // Field format: [Type:1][IdLength:1][Id:N][ValueLength:2][Value:N]
            var type     = (CdbFieldType) data[offset];
            var idLength = data[offset + 1];
            offset += 2;

            if (offset + idLength > data.Length)
                return null;

            var fieldId = Encoding.ASCII.GetString(data, offset, idLength);
            offset += idLength;

            if (offset + 2 > data.Length)
                return null;

            var valueLength = (ushort) (data[offset] | data[offset + 1] << 8);
            offset += 2;

            if (offset + valueLength > data.Length)
                return null;

            var valueBytes = new byte[valueLength];
            Array.Copy(data, offset, valueBytes, 0, valueLength);
            offset += valueLength;

            return new()
                   {
                       Id    = fieldId,
                       Type  = type,
                       Value = ConvertFieldValue(type, valueBytes)
                   };
        }

        private static object ConvertFieldValue(CdbFieldType type, byte[] value)
        {
            return type switch
                   {
                       CdbFieldType.Byte   => value.Length > 0 ? value[0] : 0,
                       CdbFieldType.Word   => value.Length >= 2 ? (ushort) (value[0] | value[1] << 8) : 0,
                       CdbFieldType.DWord  => value.Length >= 4 ? (uint) (value[0]   | value[1] << 8 | value[2] << 16 | value[3] << 24) : 0U,
                       CdbFieldType.String => Encoding.ASCII.GetString(value).TrimEnd('\0'),
                       _                   => value
                   };
        }

        private static async Task<byte[]> ReadCdbAcrossPagesAsync(ICmisDevice device, int totalLength)
        {
            var result    = new byte[totalLength];
            var bytesRead = 0;

            // First chunk: page 9Fh at offset 0x80 (up to UpperPageSize bytes)
            var firstChunkSize = Math.Min(totalLength, UpperPageSize);
            var firstChunk     = await device.RegisterAccess.ReadBlockAsync(
                                         CdbPage, CdbHeaderOffset, firstChunkSize);
            Array.Copy(firstChunk, 0, result, 0, firstChunk.Length);
            bytesRead += firstChunk.Length;

            // Extended payload pages: A0h, A1h, ... AFh (CMIS 5.3)
            var extPage = CdbExtPageStart;
            while (bytesRead < totalLength)
            {
                var chunkSize = Math.Min(totalLength - bytesRead, UpperPageSize);
                var chunk     = await device.RegisterAccess.ReadBlockAsync(
                                        extPage, CdbHeaderOffset, chunkSize);
                Array.Copy(chunk, 0, result, bytesRead, chunk.Length);
                bytesRead += chunk.Length;
                extPage++;
            }

            return result;
        }
    }
}
