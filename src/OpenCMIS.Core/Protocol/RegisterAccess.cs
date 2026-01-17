namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides implementation for register access operations.
    ///     This implementation supports CMIS protocol page-based register access.
    /// </summary>
    public class RegisterAccess : IRegisterAccess
    {
        private const byte PageSelectRegister = 0x7F;
        private readonly IRegisterTransport _registerTransport;
        private byte _currentPage = 0xFF;

        /// <summary>
        ///     Initializes a new instance of the RegisterAccess class.
        /// </summary>
        /// <param name="registerTransport">The register transport interface.</param>
        public RegisterAccess(IRegisterTransport registerTransport)
        {
            _registerTransport = registerTransport ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(registerTransport));
        }

        /// <inheritdoc />
        public async Task<byte> ReadByteAsync(byte page, byte address)
        {
            CmisException.ThrowIf(!_registerTransport.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(address > 0x7F && address != PageSelectRegister, CmisErrorCode.InvalidRegister, address, page);

            await EnsurePageAsync(page);

            return await _registerTransport.ReadRegisterAsync(address);
        }

        /// <inheritdoc />
        public async Task WriteByteAsync(byte page, byte address, byte value)
        {
            CmisException.ThrowIf(!_registerTransport.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(address > 0x7F && address != PageSelectRegister, CmisErrorCode.InvalidRegister, address, page);

            await EnsurePageAsync(page);

            await _registerTransport.WriteRegisterAsync(address, value);
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length)
        {
            CmisException.ThrowIf(!_registerTransport.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(length <= 0, CmisErrorCode.InvalidParameterValue, nameof(length));
            CmisException.ThrowIf(startAddress + length > 0x80, CmisErrorCode.InvalidRegister, startAddress, page);

            await EnsurePageAsync(page);

            return await _registerTransport.ReadRegisterBlockAsync(startAddress, length);
        }

        /// <inheritdoc />
        public async Task WriteBlockAsync(byte page, byte startAddress, byte[] data)
        {
            CmisException.ThrowIf(!_registerTransport.IsConnected, CmisErrorCode.DeviceNotConnected);
            CmisException.ThrowIf(data == null || data.Length == 0, CmisErrorCode.InvalidParameterValue, nameof(data));
            CmisException.ThrowIf(startAddress + data.Length > 0x80, CmisErrorCode.InvalidRegister, startAddress, page);

            await EnsurePageAsync(page);

            await _registerTransport.WriteRegisterBlockAsync(startAddress, data);
        }

        /// <summary>
        ///     Ensures the specified page is selected.
        /// </summary>
        /// <param name="page">The target page number.</param>
        private async Task EnsurePageAsync(byte page)
        {
            if (_currentPage == page)
                return;

            await _registerTransport.WriteRegisterAsync(PageSelectRegister, page);

            _currentPage = page;
        }
    }
}