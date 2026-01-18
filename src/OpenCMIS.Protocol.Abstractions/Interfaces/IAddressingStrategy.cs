using OpenCMIS.Shared;

namespace OpenCMIS.Protocol.Abstractions
{
    /// <summary>
    ///     Defines the strategy for register addressing in CMIS protocol.
    ///     This allows switching between standard 128-byte paging and other modes like 256-byte flat addressing.
    /// </summary>
    public interface IAddressingStrategy
    {
        /// <summary>
        ///     Gets the page and local address for a given absolute address.
        /// </summary>
        /// <param name="absoluteAddress">The absolute address (e.g., 0-255 for flat mode, or paged address).</param>
        /// <param name="targetPage">The default page if not specified in absoluteAddress.</param>
        /// <returns>A tuple containing the target page and the register address within that page.</returns>
        (byte Page, byte Address) GetPageAndAddress(int absoluteAddress, byte defaultPage = 0);

        /// <summary>
        ///     Validates if the given page and address are valid for this strategy.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="address">The register address.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        bool Validate(byte page, byte address);

        /// <summary>
        ///     Gets the maximum addressable range for a single page.
        /// </summary>
        int MaxPageAddress { get; }
    }
}
