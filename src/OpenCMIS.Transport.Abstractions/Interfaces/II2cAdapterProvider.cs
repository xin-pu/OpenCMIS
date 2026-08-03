namespace OpenCMIS.Transport.Abstractions
{
    /// <summary>
    ///     Discovers and opens one family of I2C adapters.
    /// </summary>
    public interface II2cAdapterProvider
    {
        string AdapterId { get; }

        ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(CancellationToken cancellationToken = default);

        ValueTask<II2cRegisterBus> OpenAsync(I2cConnectionProfile profile,
                                             CancellationToken    cancellationToken = default);
    }
}
