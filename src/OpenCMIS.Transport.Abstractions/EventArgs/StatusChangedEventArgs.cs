namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Provides data for the StatusChanged event.
    /// </summary>
    public class StatusChangedEventArgs : EventArgs
    {
        public StatusChangedEventArgs(ModuleStatus oldStatus, ModuleStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }

        public ModuleStatus OldStatus { get; }
        public ModuleStatus NewStatus { get; }
    }
}