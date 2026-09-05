using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Protocol.Core
{
    /// <summary>
    ///     Provides implementation for register access operations.
    ///     This implementation supports CMIS protocol page-based register access with swappable strategies.
    /// </summary>
    public class RegisterAccess : IRegisterAccess
    {
        private readonly IRegisterTransport? _registerTransport;
        private readonly IAddressingStrategy _addressingStrategy;
        private readonly IPageManager?       _pageManager;
        private readonly IMsaMemoryAccessor? _msaMemory;
        private readonly I2cDeviceAddress    _deviceAddress;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterAccess" /> class.
        /// </summary>
        /// <param name="registerTransport">The register transport interface.</param>
        /// <param name="pageManager">The page manager.</param>
        /// <param name="addressingStrategy">The addressing strategy (defaults to standard CMIS).</param>
        [Obsolete(
                "Use RegisterAccess(IMsaMemoryAccessor, I2cDeviceAddress, ...) " +
                "for atomic MSA access.")]
        public RegisterAccess(IRegisterTransport   registerTransport,
                              IPageManager         pageManager,
                              IAddressingStrategy? addressingStrategy = null)
        {
            _registerTransport  = registerTransport  ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(registerTransport));
            _pageManager        = pageManager        ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(pageManager));
            _addressingStrategy = addressingStrategy ?? new StandardAddressingStrategy();
        }

        public RegisterAccess(IMsaMemoryAccessor   msaMemory,
                              I2cDeviceAddress     deviceAddress,
                              IAddressingStrategy? addressingStrategy = null)
        {
            _msaMemory = msaMemory ??
                         throw new CmisException(
                                 CmisErrorCode.InvalidParameterValue,
                                 nameof(msaMemory));
            _deviceAddress = deviceAddress;
            _addressingStrategy =
                    addressingStrategy ?? new StandardAddressingStrategy();
        }

        /// <inheritdoc />
        public async Task<byte> ReadByteAsync(byte page, byte address)
        {
            ValidateAddress(page, address);

            if (_msaMemory is not null)
            {
                var data = await _msaMemory.ReadAsync(
                                   _deviceAddress,
                                   new (page),
                                   new (address),
                                   1);
                return data[0];
            }

            EnsureLegacyConnected();
            await _pageManager!.SwitchPageAsync(page);
            return await _registerTransport!.ReadRegisterAsync(address);
        }

        /// <inheritdoc />
        public async Task WriteByteAsync(byte page, byte address, byte value)
        {
            ValidateWritablePage(page);
            ValidateAddress(page, address);

            if (_msaMemory is not null)
            {
                await _msaMemory.WriteAsync(
                        _deviceAddress,
                        new (page),
                        new (address),
                        new[] {value});
                return;
            }

            EnsureLegacyConnected();
            await _pageManager!.SwitchPageAsync(page);
            await _registerTransport!.WriteRegisterAsync(address, value);
        }

        /// <inheritdoc />
        public Task<byte[]> ReadBlockAsync(byte page,
                                           byte startAddress,
                                           int  length)
        {
            return ReadBlockAsync(0, page, startAddress, length);
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadBlockAsync(byte bank,
                                                 byte page,
                                                 byte startAddress,
                                                 int  length)
        {
            if (length <= 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(length));

            if (startAddress + length > 256)
                throw new CmisException(CmisErrorCode.InvalidRegister, startAddress, page);

            if (_msaMemory is not null)
            {
                return await _msaMemory.ReadAsync(
                               _deviceAddress,
                               new (bank, page),
                               new (startAddress),
                               length);
            }

            if (bank != 0)
            {
                throw new NotSupportedException(
                        "Legacy register access supports bank zero only.");
            }

            EnsureLegacyConnected();
            await _pageManager!.SwitchPageAsync(page);
            return await _registerTransport!.ReadRegisterBlockAsync(startAddress, length);
        }

        /// <inheritdoc />
        public Task WriteBlockAsync(byte   page,
                                    byte   startAddress,
                                    byte[] data)
        {
            return WriteBlockAsync(0, page, startAddress, data);
        }

        /// <inheritdoc />
        public async Task WriteBlockAsync(byte   bank,
                                          byte   page,
                                          byte   startAddress,
                                          byte[] data)
        {
            ValidateWritablePage(page);
            if (data == null || data.Length == 0)
                throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(data));

            if (startAddress + data.Length > 256)
                throw new CmisException(CmisErrorCode.InvalidRegister, startAddress, page);

            if (_msaMemory is not null)
            {
                await _msaMemory.WriteAsync(
                        _deviceAddress,
                        new (bank, page),
                        new (startAddress),
                        data);
                return;
            }

            if (bank != 0)
            {
                throw new NotSupportedException(
                        "Legacy register access supports bank zero only.");
            }

            EnsureLegacyConnected();
            await _pageManager!.SwitchPageAsync(page);
            await _registerTransport!.WriteRegisterBlockAsync(startAddress, data);
        }

        private static void ValidateWritablePage(byte page)
        {
            if (page is >= CmisConstants.VdmDescriptorPageStart and <= CmisConstants.VdmDescriptorPageEnd)
                throw new NotSupportedException("CMIS VDM descriptor pages 20h-23h are read-only.");
        }

        private void EnsureLegacyConnected()
        {
            CmisException.ThrowIf(
                    _registerTransport?.IsConnected != true,
                    CmisErrorCode.DeviceNotConnected);
        }

        private void ValidateAddress(byte page, byte address)
        {
            if (!_addressingStrategy.Validate(page, address))
                throw new CmisException(CmisErrorCode.InvalidRegister, address, page);
        }
    }
}
