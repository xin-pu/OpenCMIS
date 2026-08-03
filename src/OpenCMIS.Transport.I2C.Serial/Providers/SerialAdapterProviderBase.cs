using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial.Serial;

namespace OpenCMIS.Transport.I2C.Serial.Providers
{
    public abstract class SerialAdapterProviderBase : II2cAdapterProvider
    {
        protected SerialAdapterProviderBase(ISerialSessionFactory sessionFactory,
                                            ISerialPortCatalog    portCatalog,
                                            I2cRetryOptions       retryOptions,
                                            TimeProvider          timeProvider)
        {
            SessionFactory = sessionFactory ??
                             throw new ArgumentNullException(nameof(sessionFactory));
            PortCatalog = portCatalog ??
                          throw new ArgumentNullException(nameof(portCatalog));
            RetryOptions = retryOptions ??
                           throw new ArgumentNullException(nameof(retryOptions));
            TimeProvider = timeProvider ??
                           throw new ArgumentNullException(nameof(timeProvider));
        }

        protected ISerialSessionFactory SessionFactory { get; }

        protected ISerialPortCatalog PortCatalog { get; }

        protected I2cRetryOptions RetryOptions { get; }

        protected TimeProvider TimeProvider { get; }

        public abstract string AdapterId { get; }

        public abstract ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(CancellationToken cancellationToken = default);

        public abstract ValueTask<II2cRegisterBus> OpenAsync(I2cConnectionProfile profile,
                                                             CancellationToken    cancellationToken = default);

        protected TProfile RequireProfile<TProfile>(I2cConnectionProfile profile)
        where TProfile : I2cConnectionProfile
        {
            if (profile is not TProfile typed ||
                !profile.AdapterId.Equals(
                        AdapterId,
                        StringComparison.OrdinalIgnoreCase))
            {
                throw new CmisException(
                        CmisErrorCode.InvalidParameterValue,
                        nameof(profile));
            }

            return typed;
        }
    }
}
