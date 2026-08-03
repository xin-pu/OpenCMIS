using OpenCMIS.Module.Core;
using OpenCMIS.Module.Core.Hci;
using OpenCMIS.Module.Core.Msa;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core
{
    public sealed class OpticalModuleFactory(TimeProvider timeProvider) : IOpticalModuleFactory
    {
        public async ValueTask<ICmisDevice> CreateAsync(DeviceInfo        deviceInfo,
                                                        II2cRegisterBus   bus,
                                                        CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(deviceInfo);
            ArgumentNullException.ThrowIfNull(bus);
            var profile = deviceInfo.Profile ??
                          throw new ArgumentException(
                                  "A typed I2C profile is required.",
                                  nameof(deviceInfo));
            var session = new OpticalModuleSession(bus);

            try
            {
                if (!session.IsOpen)
                    await session.OpenAsync(cancellationToken).ConfigureAwait(false);

                var msa = new MsaMemoryAccessor(session);
                var hci = new HciMemoryAccessor(
                        session,
                        new (),
                        timeProvider);
                return new CmisDevice(
                        deviceInfo,
                        session,
                        profile.DeviceAddress,
                        msa,
                        hci);
            }
            catch
            {
                await session.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
