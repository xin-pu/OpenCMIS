using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.Simulated;

/// <summary>
/// Describes a simulated I2C module profile with configurable noise.
/// </summary>
public sealed record SimulatedI2cConnectionProfile : I2cConnectionProfile
{
    public SimulatedI2cConnectionProfile(
        string adapterId,
        I2cDeviceAddress deviceAddress,
        string moduleProfile,
        int seed = 42,
        bool noiseEnabled = true)
        : base(adapterId, deviceAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleProfile);
        ModuleProfile = moduleProfile;
        Seed = seed;
        NoiseEnabled = noiseEnabled;
    }

    /// <summary>Module profile name (e.g. "800g-qsfpdd", "1p6t-osfp").</summary>
    public string ModuleProfile { get; }

    /// <summary>Deterministic seed for monitor noise.</summary>
    public int Seed { get; }

    /// <summary>Whether monitor noise is applied on reads.</summary>
    public bool NoiseEnabled { get; }
}
