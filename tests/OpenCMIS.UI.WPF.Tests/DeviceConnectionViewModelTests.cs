using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.Simulated;
using OpenCMIS.UI.WPF.Services;
using OpenCMIS.UI.WPF.Tests.Fakes;
using OpenCMIS.UI.WPF.ViewModels;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests;

public sealed class DeviceConnectionViewModelTests
{
    [Fact]
    public async Task Connect_passes_the_original_typed_device_to_the_manager()
    {
        var profile = new CypressI2cConnectionProfile(
            "cypress",
            "CY123",
            1,
            400,
            new I2cDeviceAddress(0x50));
        var info = new DeviceInfo { Id = "CY123:1", Name = "EUI3", Profile = profile };
        var manager = new FakeDeviceManager(info);
        var session = new DeviceSession();
        var viewModel = new DeviceConnectionViewModel(manager, session);

        await viewModel.RefreshAsync();
        viewModel.SelectedAdapter = viewModel.AvailableAdapters.Single();
        viewModel.SelectedDevice = viewModel.AvailableDevices.Single();
        await viewModel.ConnectAsync();

        Assert.Same(info, manager.OpenedDeviceInfo);
        Assert.Same(profile, session.CurrentDeviceInfo!.Profile);
        Assert.Equal(DeviceSessionState.Connected, session.State);
    }

    [Fact]
    public async Task Adapter_selection_filters_devices_without_flattening_profiles()
    {
        var linktel = CreateSerial("linktel", "COM7");
        var hm = CreateSerial("hm", "COM8");
        var manager = new FakeDeviceManager(linktel, hm);
        var viewModel = new DeviceConnectionViewModel(manager, new DeviceSession());

        await viewModel.RefreshAsync();
        viewModel.SelectedAdapter = viewModel.AvailableAdapters.Single(x => x.AdapterId == "hm");

        var selected = Assert.Single(viewModel.AvailableDevices);
        Assert.Same(hm, selected);
        Assert.Same(hm.Profile, selected.Profile);
    }

    [Fact]
    public async Task Refresh_selects_first_device_so_simulated_modules_can_connect_immediately()
    {
        var sim800g = new DeviceInfo
        {
            Id = "sim-800g-qsfpdd",
            Name = "Simulated 800G CMIS Module",
            Profile = new SimulatedI2cConnectionProfile(
                "sim",
                new I2cDeviceAddress(0x50),
                "800g-qsfpdd")
        };
        var sim1p6t = new DeviceInfo
        {
            Id = "sim-1p6t-osfp",
            Name = "Simulated 1.6T CMIS Module",
            Profile = new SimulatedI2cConnectionProfile(
                "sim",
                new I2cDeviceAddress(0x50),
                "1p6t-osfp")
        };
        var manager = new FakeDeviceManager(sim800g, sim1p6t);
        var viewModel = new DeviceConnectionViewModel(manager, new DeviceSession());

        await viewModel.RefreshAsync();
        await viewModel.ConnectAsync();

        Assert.Same(sim800g, viewModel.SelectedDevice);
        Assert.Equal("Simulated 800G CMIS Module", viewModel.SelectedPort);
        Assert.Same(sim800g, manager.OpenedDeviceInfo);
    }

    [Fact]
    public async Task Connection_failure_preserves_selection_and_resets_session()
    {
        var info = CreateSerial("linktel", "COM7");
        var manager = new FakeDeviceManager(info) { OpenException = new IOException("open failed") };
        var session = new DeviceSession();
        var viewModel = new DeviceConnectionViewModel(manager, session);
        await viewModel.RefreshAsync();
        viewModel.SelectedAdapter = viewModel.AvailableAdapters.Single();
        viewModel.SelectedDevice = viewModel.AvailableDevices.Single();

        await viewModel.ConnectAsync();

        Assert.Same(info, viewModel.SelectedDevice);
        Assert.Equal(DeviceSessionState.Disconnected, session.State);
        Assert.Contains("open failed", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disconnect_closes_the_device_and_clears_session()
    {
        var info = CreateSerial("linktel", "COM7");
        var manager = new FakeDeviceManager(info);
        var session = new DeviceSession();
        var viewModel = new DeviceConnectionViewModel(manager, session);
        await viewModel.RefreshAsync();
        viewModel.SelectedAdapter = viewModel.AvailableAdapters.Single();
        viewModel.SelectedDevice = viewModel.AvailableDevices.Single();
        await viewModel.ConnectAsync();

        await viewModel.DisconnectAsync();

        Assert.False(manager.OpenedDevice!.IsConnected);
        Assert.Equal(DeviceSessionState.Disconnected, session.State);
        Assert.Null(session.CurrentDevice);
    }

    [Fact]
    public async Task Module_identity_failure_closes_the_opened_device()
    {
        var info = CreateSerial("linktel", "COM7");
        var manager = new FakeDeviceManager(info)
        {
            ModuleInfoException = new IOException("identity failed")
        };
        var session = new DeviceSession();
        var viewModel = new DeviceConnectionViewModel(manager, session);
        await viewModel.RefreshAsync();
        viewModel.SelectedAdapter = viewModel.AvailableAdapters.Single();
        viewModel.SelectedDevice = viewModel.AvailableDevices.Single();

        await viewModel.ConnectAsync();

        Assert.False(manager.OpenedDevice!.IsConnected);
        Assert.Equal(DeviceSessionState.Disconnected, session.State);
    }

    private static DeviceInfo CreateSerial(string adapterId, string portName) =>
        new()
        {
            Id = portName,
            Name = $"{adapterId} on {portName}",
            Profile = new SerialI2cConnectionProfile(
                adapterId,
                portName,
                115200,
                new I2cDeviceAddress(0x50))
        };
}
