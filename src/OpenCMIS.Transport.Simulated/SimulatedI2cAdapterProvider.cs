using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.Simulated
{
    /// <summary>
    ///     Discovers and opens simulated I2C adapter endpoints.
    /// </summary>
    public sealed class SimulatedI2cAdapterProvider : II2cAdapterProvider
    {
        public string AdapterId => "sim";

        public ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            var descriptors = new List<I2cAdapterDescriptor>
                              {
                                  CreateDescriptor("sim-800g-qsfpdd",
                                                   "Simulated 800G CMIS Module",
                                                   "800g-qsfpdd"),
                                  CreateDescriptor("sim-1p6t-osfp",
                                                   "Simulated 1.6T CMIS Module",
                                                   "1p6t-osfp")
                              };

            return ValueTask.FromResult<IReadOnlyList<I2cAdapterDescriptor>>(
                    descriptors);
        }

        public ValueTask<II2cRegisterBus> OpenAsync(I2cConnectionProfile profile,
                                                    CancellationToken    cancellationToken = default)
        {
            if (profile is not SimulatedI2cConnectionProfile simProfile
             || !profile.AdapterId.Equals(
                        AdapterId,
                        StringComparison.OrdinalIgnoreCase))
            {
                throw new CmisException(
                        CmisErrorCode.InvalidParameterValue,
                        nameof(profile));
            }

            var seed = simProfile.Seed > 0
                               ? simProfile.Seed
                               : Random.Shared.Next();

            var bus = new SimulatedI2cRegisterBus(
                    simProfile.ModuleProfile,
                    seed,
                    simProfile.NoiseEnabled);

            return ValueTask.FromResult<II2cRegisterBus>(bus);
        }

        private I2cAdapterDescriptor CreateDescriptor(string deviceId,
                                                      string displayName,
                                                      string moduleProfile)
        {
            var profile = new SimulatedI2cConnectionProfile(
                    AdapterId,
                    new (CmisConstants.DefaultI2cAddress),
                    moduleProfile);

            return new (
                    AdapterId,
                    deviceId,
                    displayName,
                    profile);
        }
    }
}
