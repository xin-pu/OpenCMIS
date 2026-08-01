using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Adapters;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Providers;

public sealed class HmMultiChannelAdapterProvider : SerialAdapterProviderBase
{
    public HmMultiChannelAdapterProvider(
        ISerialSessionFactory sessionFactory,
        ISerialPortCatalog portCatalog,
        I2cRetryOptions retryOptions,
        TimeProvider timeProvider)
        : base(sessionFactory, portCatalog, retryOptions, timeProvider)
    {
    }

    public override string AdapterId => "hm-multichannel";

    public override ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var address = new I2cDeviceAddress(0x50);
        var descriptors = (
                from port in PortCatalog.GetPortNames()
                from channel in Enumerable.Range(1, 5)
                let profile = new HmMultiChannelConnectionProfile(
                    AdapterId,
                    port,
                    1500000,
                    (byte)channel,
                    address)
                select new I2cAdapterDescriptor(
                    AdapterId,
                    $"{AdapterId}:{port}:{channel}",
                    $"HM I2C {port} channel {channel}",
                    profile))
            .ToArray();
        return ValueTask.FromResult<IReadOnlyList<I2cAdapterDescriptor>>(
            descriptors);
    }

    public override async ValueTask<II2cRegisterBus> OpenAsync(
        I2cConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        var typed = RequireProfile<HmMultiChannelConnectionProfile>(profile);
        var adapter = new HmMultiChannelI2cAdapter(
            SessionFactory,
            typed,
            RetryOptions,
            TimeProvider);
        await adapter.OpenAsync(cancellationToken).ConfigureAwait(false);
        return adapter;
    }
}
