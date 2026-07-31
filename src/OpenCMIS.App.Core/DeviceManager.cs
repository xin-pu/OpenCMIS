using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.App.Core;

/// <summary>
/// Coordinates adapter providers without constructing hardware implementations.
/// </summary>
public sealed class DeviceManager : IDeviceManager
{
    private readonly IReadOnlyDictionary<string, II2cAdapterProvider> _providers;
    private readonly IOpticalModuleFactory _moduleFactory;

    public DeviceManager(
        IEnumerable<II2cAdapterProvider> providers,
        IOpticalModuleFactory moduleFactory)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _moduleFactory = moduleFactory ??
                         throw new ArgumentNullException(nameof(moduleFactory));
        _providers = providers.ToDictionary(
            provider => provider.AdapterId,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<I2cProbeFailure> LastProbeFailures { get; private set; } =
        [];

    public async Task<IEnumerable<DeviceInfo>> EnumerateDevicesAsync()
    {
        var devices = new List<DeviceInfo>();
        var failures = new List<I2cProbeFailure>();

        foreach (var provider in _providers.Values)
        {
            try
            {
                var descriptors = await provider.DiscoverAsync()
                    .ConfigureAwait(false);
                devices.AddRange(
                    descriptors.Select(
                        descriptor => new DeviceInfo
                        {
                            Id = descriptor.DeviceId,
                            Name = descriptor.DisplayName,
                            ConnectionType = ConnectionType.I2C,
                            Profile = descriptor.Profile,
                            ConnectionParameters =
                                ToLegacyParameters(descriptor.Profile)
                        }));
            }
            catch (Exception exception)
            {
                failures.Add(
                    new I2cProbeFailure(
                        provider.AdapterId,
                        "*",
                        exception.Message));
            }
        }

        LastProbeFailures = failures;
        return devices;
    }

    public async Task<ICmisDevice> OpenDeviceAsync(DeviceInfo deviceInfo)
    {
        if (deviceInfo is null)
        {
            throw new CmisException(
                CmisErrorCode.InvalidParameterValue,
                nameof(deviceInfo));
        }

        var profile = deviceInfo.Profile ?? ParseLegacyProfile(deviceInfo);
        deviceInfo.Profile = profile;
        if (!_providers.TryGetValue(profile.AdapterId, out var provider))
        {
            throw new CmisException(
                CmisErrorCode.I2cAdapterNotFound,
                profile.AdapterId);
        }

        var bus = await provider.OpenAsync(profile).ConfigureAwait(false);
        try
        {
            return await _moduleFactory.CreateAsync(deviceInfo, bus)
                .ConfigureAwait(false);
        }
        catch
        {
            await bus.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public Task CloseDeviceAsync(ICmisDevice device)
    {
        if (device is null)
        {
            throw new CmisException(
                CmisErrorCode.InvalidParameterValue,
                nameof(device));
        }

        return device.CloseAsync();
    }

    private static I2cConnectionProfile ParseLegacyProfile(DeviceInfo deviceInfo)
    {
        var parameters = deviceInfo.ConnectionParameters;
        var connector = parameters.GetValueOrDefault("ConnectorType", "TypeA");
        var adapterId = connector.Equals(
            "TypeB",
            StringComparison.OrdinalIgnoreCase)
            ? "hm"
            : "linktel";
        var portName = parameters.GetValueOrDefault("PortName", deviceInfo.Id);
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new CmisException(
                CmisErrorCode.InvalidParameterValue,
                "PortName");
        }

        var defaultBaudRate = adapterId == "hm" ? 1500000 : 115200;
        if (!int.TryParse(
                parameters.GetValueOrDefault(
                    "BaudRate",
                    defaultBaudRate.ToString()),
                out var baudRate))
        {
            throw new CmisException(
                CmisErrorCode.InvalidParameterValue,
                "BaudRate");
        }

        var legacyAddressText = parameters.GetValueOrDefault(
            "SlaveAddress",
            "0xA0");
        var address = ParseLegacyAddress(legacyAddressText);
        return new SerialI2cConnectionProfile(
            adapterId,
            portName,
            baudRate,
            address);
    }

    private static I2cDeviceAddress ParseLegacyAddress(string value)
    {
        try
        {
            var legacy = Convert.ToByte(
                value,
                value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? 16
                    : 10);
            return I2cDeviceAddress.FromLegacy8Bit(legacy);
        }
        catch (Exception exception)
            when (exception is FormatException or
                  OverflowException or
                  ArgumentOutOfRangeException)
        {
            throw new CmisException(
                CmisErrorCode.InvalidParameterValue,
                "SlaveAddress",
                exception);
        }
    }

    private static Dictionary<string, string> ToLegacyParameters(
        I2cConnectionProfile profile)
    {
        var parameters = new Dictionary<string, string>
        {
            ["ConnectorType"] = profile.AdapterId,
            ["SlaveAddress"] =
                $"0x{profile.DeviceAddress.ToWriteAddress8Bit():X2}"
        };

        switch (profile)
        {
            case SerialI2cConnectionProfile serial:
                parameters["PortName"] = serial.PortName;
                parameters["BaudRate"] = serial.BaudRate.ToString();
                break;
            case HmMultiChannelConnectionProfile multi:
                parameters["PortName"] = multi.PortName;
                parameters["BaudRate"] = multi.BaudRate.ToString();
                parameters["Channel"] = multi.Channel.ToString();
                break;
            case CypressI2cConnectionProfile cypress:
                parameters["SerialNumber"] = cypress.SerialNumber;
                parameters["Port"] = cypress.Port.ToString();
                parameters["SpeedKhz"] = cypress.SpeedKhz.ToString();
                break;
        }

        return parameters;
    }
}
