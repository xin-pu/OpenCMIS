using OpenCMIS.Shared;

namespace OpenCMIS.Protocol.Abstractions
{
    /// <summary>
    ///     Provides interface for page management in CMIS protocol.
    /// </summary>
    public interface IPageManager
    {
        /// <summary>
        ///     Gets the current active page.
        /// </summary>
        byte CurrentPage { get; }

        /// <summary>
        ///     Switches to the specified page asynchronously.
        /// </summary>
        /// <param name="page">The target page number.</param>
        /// <returns>A task representing the switch operation.</returns>
        Task SwitchPageAsync(byte page);

        /// <summary>
        ///     Resets the page state to default (usually page 0).
        /// </summary>
        Task ResetAsync();
    }
}
