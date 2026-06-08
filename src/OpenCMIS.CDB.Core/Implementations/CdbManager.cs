using OpenCMIS.CDB.Abstractions;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.CDB.Core
{
    /// <summary>
    ///     Manages CDB operations including read, write, validate, and apply.
    /// </summary>
    public class CdbManager
    {
        private readonly ICdbReader    _reader;
        private readonly ICdbWriter    _writer;
        private readonly ICdbValidator _validator;

        /// <summary>
        ///     Initializes a new instance of the CdbManager class.
        /// </summary>
        /// <param name="reader">The CDB reader.</param>
        /// <param name="writer">The CDB writer.</param>
        /// <param name="validator">The CDB validator.</param>
        public CdbManager(ICdbReader reader, ICdbWriter writer, ICdbValidator validator)
        {
            _reader    = reader    ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(reader));
            _writer    = writer    ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(writer));
            _validator = validator ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(validator));
        }

        /// <summary>
        ///     Reads the CDB from the specified device.
        /// </summary>
        /// <param name="device">The CMIS device.</param>
        /// <returns>The configuration data block.</returns>
        public async Task<ConfigurationDataBlock> ReadCdbAsync(ICmisDevice device)
        {
            var cdb = await _reader.ReadAsync(device);

            if (!_validator.Validate(cdb))
                throw new CmisException(CmisErrorCode.CdbValidationFailed);

            return cdb;
        }

        /// <summary>
        ///     Writes the CDB to the specified device after validation.
        /// </summary>
        /// <param name="device">The CMIS device.</param>
        /// <param name="cdb">The configuration data block to write.</param>
        public async Task WriteCdbAsync(ICmisDevice device, ConfigurationDataBlock cdb)
        {
            if (!_validator.Validate(cdb))
                throw new CmisException(CmisErrorCode.CdbValidationFailed);

            await _writer.WriteAsync(device, cdb);
        }

        /// <summary>
        ///     Validates the CDB without reading from or writing to a device.
        /// </summary>
        /// <param name="cdb">The configuration data block to validate.</param>
        /// <returns>True if the CDB is valid; otherwise, false.</returns>
        public bool ValidateCdb(ConfigurationDataBlock cdb)
        {
            return _validator.Validate(cdb);
        }

        /// <summary>
        ///     Reads, applies modifications, validates, and writes the CDB in a single operation.
        /// </summary>
        /// <param name="device">The CMIS device.</param>
        /// <param name="modifications">A function that modifies the CDB.</param>
        public async Task ApplyCdbAsync(ICmisDevice device, Action<ConfigurationDataBlock> modifications)
        {
            if (modifications == null)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(modifications));

            var cdb = await ReadCdbAsync(device);
            modifications(cdb);
            await WriteCdbAsync(device, cdb);
        }
    }
}
