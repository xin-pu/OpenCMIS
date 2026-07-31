using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Adapters;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Providers;

public sealed class LinktelSerialAdapterProvider : SerialAdapterProviderBase
{
    public LinktelSerialAdapterProvider(
        ISerialSessionFactory sessionFactory,
        ISerialPortCatalog portCatalog,
        I2cRetryOptions retryOptions,
        TimeProvider timeProvider)
        : base(sessionFactory, portCatalog, retryOptions, timeProvider)
    {
    }

    public override string AdapterId => "linktel";

    public override ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var address = new I2cDeviceAddress(0x50);
        var descriptors = PortCatalog.GetPortNames()
            .Select(
                port =>
                {
                    var profile = new SerialI2cConnectionProfile(
                        AdapterId,
                        port,
                        115200,
                        address);
                    return new I2cAdapterDescriptor(
                        AdapterId,
                        $"{AdapterId}:{port}",
                        $"Linktel I2C on {port}",
                        profile);
                })
            .ToArray();
        return ValueTask.FromResult<IReadOnlyList<I2cAdapterDescriptor>>(
            descriptors);
    }

    public override async ValueTask<II2cRegisterBus> OpenAsync(
        I2cConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        var typed = RequireProfile<SerialI2cConnectionProfile>(profile);
        var adapter = new LinktelSerialI2cAdapter(
            SessionFactory,
            typed,
            RetryOptions,
            TimeProvider);
        await adapter.OpenAsync(cancellationToken).ConfigureAwait(false);
        return adapter;
    }
}
