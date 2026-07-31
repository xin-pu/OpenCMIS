using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Protocol.Core
{
    /// <summary>
    ///     Implements page management for CMIS protocol.
    /// </summary>
    [Obsolete(
        "Use IMsaMemoryAccessor through OpticalModuleSession so page selection " +
        "and transfer share one atomic gate.")]
    public class PageManager : IPageManager
    {
        private const byte PageSelectRegister = CmisConstants.PageSelectRegister;
        private readonly IRegisterTransport _registerTransport;
        private byte _currentPage = 0xFF; // Start with unknown state

        public PageManager(IRegisterTransport registerTransport)
        {
            _registerTransport = registerTransport ?? throw new CmisException(CmisErrorCode.InvalidParameterValue, nameof(registerTransport));
        }

        /// <inheritdoc />
        public byte CurrentPage => _currentPage;

        /// <inheritdoc />
        public async Task SwitchPageAsync(byte page)
        {
            if (_currentPage == page)
                return;

            CmisException.ThrowIf(!_registerTransport.IsConnected, CmisErrorCode.DeviceNotConnected);

            try
            {
                await _registerTransport.WriteRegisterAsync(PageSelectRegister, page);
                _currentPage = page;
            }
            catch (Exception ex)
            {
                throw new CmisException(CmisErrorCode.RegisterWriteFailed, ex, PageSelectRegister, page);
            }
        }

        /// <inheritdoc />
        public async Task ResetAsync()
        {
            await SwitchPageAsync(0);
        }
    }
}
