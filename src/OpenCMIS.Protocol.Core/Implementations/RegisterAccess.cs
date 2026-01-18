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
        private readonly IRegisterTransport  _registerTransport;
        private readonly IAddressingStrategy _addressingStrategy;
        private readonly IPageManager        _pageManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterAccess"/> class.
        /// </summary>
        /// <param name="registerTransport">The register transport interface.</param>
        /// <param name="pageManager">The page manager.</param>
        /// <param name="addressingStrategy">The addressing strategy (defaults to standard CMIS).</param>
        public RegisterAccess(
            IRegisterTransport registerTransport, 
            IPageManager pageManager,
            IAddressingStrategy? addressingStrategy = null)
        {
            _registerTransport  = registerTransport  ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(registerTransport));
            _pageManager        = pageManager        ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(pageManager));
            _addressingStrategy = addressingStrategy ?? new StandardAddressingStrategy();
        }

        /// <inheritdoc />
        public async Task<byte> ReadByteAsync(byte page, byte address)
        {
            EnsureConnected();
            ValidateAddress(page, address);

            await _pageManager.SwitchPageAsync(page);
            return await _registerTransport.ReadRegisterAsync(address);
        }

        /// <inheritdoc />
        public async Task WriteByteAsync(byte page, byte address, byte value)
        {
            EnsureConnected();
            ValidateAddress(page, address);

            await _pageManager.SwitchPageAsync(page);
            await _registerTransport.WriteRegisterAsync(address, value);
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length)
        {
            EnsureConnected();
            if (length <= 0) throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(length));
            
            if (startAddress + length > 256) throw new CmisException(CmisErrorCode.InvalidRegister, startAddress, page);

            await _pageManager.SwitchPageAsync(page);
            return await _registerTransport.ReadRegisterBlockAsync(startAddress, length);
        }

        /// <inheritdoc />
        public async Task WriteBlockAsync(byte page, byte startAddress, byte[] data)
        {
            EnsureConnected();
            if (data == null || data.Length == 0) throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(data));
            
            if (startAddress + data.Length > 256) throw new CmisException(CmisErrorCode.InvalidRegister, startAddress, page);

            await _pageManager.SwitchPageAsync(page);
            await _registerTransport.WriteRegisterBlockAsync(startAddress, data);
        }

        private void EnsureConnected()
        {
            CmisException.ThrowIf(!_registerTransport.IsConnected, CmisErrorCode.DeviceNotConnected);
        }

        private void ValidateAddress(byte page, byte address)
        {
            if (!_addressingStrategy.Validate(page, address))
            {
                throw new CmisException(CmisErrorCode.InvalidRegister, address, page);
            }
        }
    }
}
