using OpenCMIS.Protocol.Abstractions;

namespace OpenCMIS.Protocol.Core
{
    /// <summary>
    ///     Implements standard CMIS 128-byte page-based addressing.
    /// </summary>
    public class StandardAddressingStrategy : IAddressingStrategy
    {
        private const byte PageSelectRegister = 0x7F;

        /// <inheritdoc />
        public (byte Page, byte Address) GetPageAndAddress(int absoluteAddress, byte defaultPage = 0)
        {
            // Standard CMIS logic: 
            // 0x00 - 0x7F: Lower Page (Common)
            // 0x80 - 0xFF: Upper Page (Paged)
            
            if (absoluteAddress < 0x80)
            {
                return (0, (byte)absoluteAddress); // Lower page is effectively page 0 or common
            }
            
            return (defaultPage, (byte)absoluteAddress);
        }

        /// <inheritdoc />
        public bool Validate(byte page, byte address)
        {
            // Address must be between 0 and 255.
            // Page select register is special but usually valid.
            return address <= 0xFF;
        }

        /// <inheritdoc />
        public int MaxPageAddress => 0xFF;
    }
}
