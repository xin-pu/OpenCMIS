namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Configures retries for transient adapter I/O failures.
    /// </summary>
    public sealed record I2cRetryOptions
    {
        public I2cRetryOptions(int maxAttempts, TimeSpan delay)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                        nameof(delay),
                        delay,
                        "Retry delay cannot be negative.");
            }

            MaxAttempts = maxAttempts;
            Delay       = delay;
        }

        public int MaxAttempts { get; }

        public TimeSpan Delay { get; }

        public static I2cRetryOptions Default { get; } =
            new (3, TimeSpan.FromMilliseconds(20));
    }
}
