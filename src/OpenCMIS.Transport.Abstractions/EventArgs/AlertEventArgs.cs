using OpenCMIS.Shared;

namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Provides data for the Alert event.
    /// </summary>
    public class AlertEventArgs : EventArgs
    {
        /// <summary>
        ///     Gets or sets the alert type.
        /// </summary>
        public AlertType AlertType { get; set; }

        /// <summary>
        ///     Gets or sets the alert message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
