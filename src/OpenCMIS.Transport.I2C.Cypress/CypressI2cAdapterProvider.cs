using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.Transport.I2C.Cypress;

public sealed class CypressI2cAdapterProvider(
    ICypressDeviceApiFactory apiFactory) : II2cAdapterProvider
{
    private readonly ICypressDeviceApiFactory _apiFactory =
        apiFactory ?? throw new ArgumentNullException(nameof(apiFactory));

    public string AdapterId => "cypress";

    public async ValueTask<IReadOnlyList<I2cAdapterDescriptor>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var api = _apiFactory.Create();
        var devices = await Task.Run(api.Discover, CancellationToken.None)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return devices
            .Select(device =>
            {
                var speedKhz = device.Kind == CypressDeviceKind.Fic2Usb
                    ? 100
                    : 90;
                var profile = new CypressI2cConnectionProfile(
                    AdapterId,
                    device.SerialNumber,
                    port: 0,
                    speedKhz,
                    new I2cDeviceAddress(0x50));
                return new I2cAdapterDescriptor(
                    AdapterId,
                    device.SerialNumber,
                    device.DisplayName,
                    profile);
            })
            .ToArray();
    }

    public async ValueTask<II2cRegisterBus> OpenAsync(
        I2cConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (profile is not CypressI2cConnectionProfile cypressProfile ||
            !profile.AdapterId.Equals(
                AdapterId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new CmisException(
                CmisErrorCode.InvalidParameterValue,
                nameof(profile));
        }

        var kind = await FindDeviceKindAsync(
                cypressProfile.SerialNumber,
                cancellationToken)
            .ConfigureAwait(false);
        if (kind is null)
        {
            throw new CmisException(CmisErrorCode.I2cAdapterNotFound);
        }

        var api = _apiFactory.Create();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var opened = await Task.Run(
                    () => api.Open(cypressProfile.SerialNumber),
                    CancellationToken.None)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!opened)
            {
                throw new CmisException(CmisErrorCode.I2cConnectionFailed);
            }

            II2cRegisterBus adapter = kind switch
            {
                CypressDeviceKind.Fic2Usb => new Fic2UsbI2cAdapter(
                    api,
                    cypressProfile.Port,
                    cypressProfile.SpeedKhz),
                CypressDeviceKind.Eui3 => new Eui3I2cAdapter(
                    api,
                    cypressProfile.Port,
                    cypressProfile.SpeedKhz),
                _ => throw new CmisException(CmisErrorCode.I2cAdapterNotFound)
            };
            await adapter.OpenAsync(cancellationToken).ConfigureAwait(false);
            return adapter;
        }
        catch
        {
            api.Close();
            await api.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async ValueTask<CypressDeviceKind?> FindDeviceKindAsync(
        string serialNumber,
        CancellationToken cancellationToken)
    {
        await using var discoveryApi = _apiFactory.Create();
        cancellationToken.ThrowIfCancellationRequested();
        var devices = await Task.Run(
                discoveryApi.Discover,
                CancellationToken.None)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return devices
            .FirstOrDefault(device =>
                device.SerialNumber.Equals(
                    serialNumber,
                    StringComparison.OrdinalIgnoreCase))
            ?.Kind;
    }
}
