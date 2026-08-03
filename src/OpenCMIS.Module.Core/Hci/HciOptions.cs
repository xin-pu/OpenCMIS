namespace OpenCMIS.Module.Core.Hci
{
    /// <summary>
    ///     Configures vendor HCI ready-state polling.
    /// </summary>
    public sealed record HciOptions
    {
        public IReadOnlySet<byte> ReadyValues { get; init; } =
            new HashSet<byte> {0x00};

        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

        public TimeSpan InitialPollDelay { get; init; } =
            TimeSpan.FromMilliseconds(10);

        public TimeSpan MaximumPollDelay { get; init; } =
            TimeSpan.FromSeconds(1);
    }
}
