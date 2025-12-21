namespace OpenCMIS.Core
{
    /// <summary>
    ///     Provides data for the StatusChanged event.
    /// </summary>
    public class StatusChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Gets or sets the previous status.
        /// </summary>
        public ModuleStatus? PreviousStatus { get; set; }

        /// <summary>
        ///     Gets or sets the current status.
        /// </summary>
        public ModuleStatus CurrentStatus { get; set; } = new();
    }
}

