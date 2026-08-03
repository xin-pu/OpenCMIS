using OpenCMIS.Protocol.Abstractions;

namespace OpenCMIS.Protocol.Core
{
    /// <summary>
    ///     Implements flat 256-byte addressing mode.
    /// </summary>
    public class FlatAddressingStrategy : IAddressingStrategy
    {
        /// <inheritdoc />
        public (byte Page, byte Address) GetPageAndAddress(int absoluteAddress, byte defaultPage = 0)
        {
            // In flat mode, we treat everything as one continuous space.
            // The actual paging is handled under the hood or disabled if the device supports it.
            // Here we assume it maps to page 0 if it's a 256-byte flat device.
            return (0, (byte) (absoluteAddress & 0xFF));
        }

        /// <inheritdoc />
        public bool Validate(byte page, byte address)
        {
            return address <= 0xFF;
        }

        /// <inheritdoc />
        public int MaxPageAddress => 0xFF;
    }
}
