using OpenCMIS.Transport.Abstractions;
using OpenCMIS.UI.WPF.Services;
using OpenCMIS.UI.WPF.Tests.Fakes;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests;

public sealed class DeviceSessionTests
{
    [Fact]
    public void Connected_session_publishes_the_same_device_and_profile()
    {
        var profile = new CypressI2cConnectionProfile(
            "cypress",
            "CY123",
            0,
            400,
            new I2cDeviceAddress(0x50));
        var info = new DeviceInfo { Id = "CY123:0", Name = "EUI3", Profile = profile };
        var device = new FakeCmisDevice(info);
        var session = new DeviceSession();

        session.SetConnecting();
        session.SetConnected(info, device);

        Assert.Equal(DeviceSessionState.Connected, session.State);
        Assert.Same(info, session.CurrentDeviceInfo);
        Assert.Same(profile, session.CurrentDeviceInfo!.Profile);
        Assert.Same(device, session.CurrentDevice);
    }

    [Fact]
    public void Disconnect_clears_the_active_device()
    {
        var info = new DeviceInfo { Id = "COM7", Name = "Linktel" };
        var session = new DeviceSession();
        session.SetConnecting();
        session.SetConnected(info, new FakeCmisDevice(info));

        session.SetDisconnecting();
        session.SetDisconnected();

        Assert.Equal(DeviceSessionState.Disconnected, session.State);
        Assert.Null(session.CurrentDeviceInfo);
        Assert.Null(session.CurrentDevice);
    }
}
