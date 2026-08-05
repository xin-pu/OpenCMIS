using System.Text;
using OpenCMIS.CDB.Abstractions;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.CDB.Core
{
    /// <summary>
    ///     Provides implementation for writing Configuration Data Blocks to CMIS devices.
    /// </summary>
    public class CdbWriter : ICdbWriter
    {
        private const byte CdbPage         = 0x9F;
        private const byte CdbExtPageStart = 0xA0;
        private const byte CdbHeaderOffset = 0x80;
        private const int  UpperPageSize   = 128;

        /// <inheritdoc />
        public async Task WriteAsync(ICmisDevice device, ConfigurationDataBlock cdb)
        {
            if (device == null)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(device));

            if (cdb == null)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(cdb));

            if (!device.IsConnected)
                throw new CmisException(CmisErrorCode.DeviceNotConnected);

            try
            {
                // Serialize CDB to byte array
                var data = SerializeCdb(cdb);

                // Calculate CRC over body (all bytes except the 2-byte checksum at end)
                var bodyLength = data.Length - 2;
                var crcData    = new byte[bodyLength];
                Array.Copy(data, 0, crcData, 0, bodyLength);

                var crc = CrcCalculator.CalculateCrc16(crcData);

                // Write CRC to last 2 bytes
                data[bodyLength]     = (byte) (crc      & 0xFF);
                data[bodyLength + 1] = (byte) (crc >> 8 & 0xFF);

                // Write to device across pages (9Fh + A0h-AFh for CMIS 5.3)
                await WriteCdbAcrossPagesAsync(device, data);

                // Verify by reading back across pages
                var verifyData = await ReadBackCdbAcrossPagesAsync(device, data.Length);
                for (var i = 0; i < data.Length; i++)
                    if (data[i] != verifyData[i])
                        throw new CmisException(CmisErrorCode.CdbWriteFailed, i);
            }
            catch (CmisException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.CdbWriteFailed, ex);
            }
        }

        private static async Task WriteCdbAcrossPagesAsync(ICmisDevice device, byte[] data)
        {
            var bytesWritten = 0;

            // First chunk: page 9Fh at offset 0x80
            var firstChunkSize = Math.Min(data.Length, UpperPageSize);
            var firstChunk     = new byte[firstChunkSize];
            Array.Copy(data, 0, firstChunk, 0, firstChunkSize);
            await device.RegisterAccess.WriteBlockAsync(CdbPage, CdbHeaderOffset, firstChunk);
            bytesWritten += firstChunkSize;

            // Extended payload pages: A0h, A1h, ... AFh
            var extPage = CdbExtPageStart;
            while (bytesWritten < data.Length)
            {
                var chunkSize = Math.Min(data.Length - bytesWritten, UpperPageSize);
                var chunk     = new byte[chunkSize];
                Array.Copy(data, bytesWritten, chunk, 0, chunkSize);
                await device.RegisterAccess.WriteBlockAsync(extPage, CdbHeaderOffset, chunk);
                bytesWritten += chunkSize;
                extPage++;
            }
        }

        private static async Task<byte[]> ReadBackCdbAcrossPagesAsync(ICmisDevice device, int totalLength)
        {
            var result    = new byte[totalLength];
            var bytesRead = 0;

            var firstChunkSize = Math.Min(totalLength, UpperPageSize);
            var firstChunk     = await device.RegisterAccess.ReadBlockAsync(
                                         CdbPage, CdbHeaderOffset, firstChunkSize);
            Array.Copy(firstChunk, 0, result, 0, firstChunk.Length);
            bytesRead += firstChunk.Length;

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

        private static byte[] SerializeCdb(ConfigurationDataBlock cdb)
        {
            var fields = new List<byte>();

            foreach (var field in cdb.Fields)
                SerializeField(field, fields);

            // Build complete CDB: Header(4) + Fields + Checksum(2)
            var bodyLength  = 4          + fields.Count;
            var totalLength = bodyLength + 2;
            var result      = new byte[totalLength];

            // Header: Length[2], Version[1], Flags[1]
            result[0] = (byte) (totalLength      & 0xFF);
            result[1] = (byte) (totalLength >> 8 & 0xFF);
            result[2] = cdb.Header.Version;
            result[3] = cdb.Header.Flags;

            // Fields
            fields.CopyTo(result, 4);

            // Checksum placeholder (will be filled by caller)
            result[^2] = 0;
            result[^1] = 0;

            return result;
        }

        private static void SerializeField(CdbField field, List<byte> buffer)
        {
            var idBytes    = Encoding.ASCII.GetBytes(field.Id);
            var valueBytes = SerializeFieldValue(field.Type, field.Value);

            // Type
            buffer.Add((byte) field.Type);

            // Id Length
            buffer.Add((byte) idBytes.Length);

            // Id
            buffer.AddRange(idBytes);

            // Value Length
            buffer.Add((byte) (valueBytes.Length      & 0xFF));
            buffer.Add((byte) (valueBytes.Length >> 8 & 0xFF));

            // Value
            buffer.AddRange(valueBytes);
        }

        private static byte[] SerializeFieldValue(CdbFieldType type, object? value)
        {
            if (value == null)
                return [];

            return type switch
                   {
                       CdbFieldType.Byte   => [Convert.ToByte(value)],
                       CdbFieldType.Word   => [(byte) (Convert.ToUInt16(value) & 0xFF), (byte) (Convert.ToUInt16(value) >> 8 & 0xFF)],
                       CdbFieldType.DWord  => BitConverter.GetBytes(Convert.ToUInt32(value)),
                       CdbFieldType.String => Encoding.ASCII.GetBytes((string) value),
                       _                   => value is byte[] bytes ? bytes : []
                   };
        }
    }
}
