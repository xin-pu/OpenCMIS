using Microsoft.Extensions.DependencyInjection;
using OpenCMIS.App.Core;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using Xunit;

namespace OpenCMIS.Transport.Simulated.Tests
{
    public sealed class AppIntegrationTests
    {
        [Fact]
        public async Task DeviceManager_discovers_simulated_device()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSimulatedAdapters();
            var sp = services.BuildServiceProvider();
            var dm = sp.GetRequiredService<IDeviceManager>();

            var devices = (await dm.EnumerateDevicesAsync()).ToList();

            Assert.Contains(devices, d => d.Id == "sim-800g-qsfpdd");
            Assert.Contains(devices, d => d.Id == "sim-1p6t-osfp");
        }

        [Fact]
        public async Task DeviceManager_opens_simulated_device()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSimulatedAdapters();
            var sp        = services.BuildServiceProvider();
            var dm        = sp.GetRequiredService<IDeviceManager>();
            var devices   = (await dm.EnumerateDevicesAsync()).ToList();
            var simDevice = devices.First(d => d.Id == "sim-800g-qsfpdd");

            var device = await dm.OpenDeviceAsync(simDevice);

            Assert.True(device.IsConnected);
            await device.CloseAsync();
        }

        [Fact]
        public async Task Simulated_device_reads_module_info()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSimulatedAdapters();
            var sp        = services.BuildServiceProvider();
            var dm        = sp.GetRequiredService<IDeviceManager>();
            var devices   = (await dm.EnumerateDevicesAsync()).ToList();
            var simDevice = devices.First(d => d.Id == "sim-800g-qsfpdd");
            var device    = await dm.OpenDeviceAsync(simDevice);

            var info = await device.GetModuleInfoAsync();

            Assert.NotNull(info);
            Assert.Equal("OpenCMIS-Sim", info.VendorName?.TrimEnd());
            Assert.Contains("800G", info.PartNumber);
            Assert.Equal("5.2", info.CmisVersion);

            await device.CloseAsync();
        }

        [Fact]
        public async Task Simulated_device_reads_status()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSimulatedAdapters();
            var sp        = services.BuildServiceProvider();
            var dm        = sp.GetRequiredService<IDeviceManager>();
            var devices   = (await dm.EnumerateDevicesAsync()).ToList();
            var simDevice = devices.First(d => d.Id == "sim-800g-qsfpdd");
            var device    = await dm.OpenDeviceAsync(simDevice);

            var status = await device.GetStatusAsync();

            Assert.NotNull(status);
            Assert.True(status.IsReady);
            Assert.Equal(ModuleState.Ready,
                         status.CurrentState);

            await device.CloseAsync();
        }

        [Fact]
        public async Task ReadModuleDashDataAsync_with_8_lanes_succeeds()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSimulatedAdapters();
            var sp        = services.BuildServiceProvider();
            var dm        = sp.GetRequiredService<IDeviceManager>();
            var devices   = (await dm.EnumerateDevicesAsync()).ToList();
            var simDevice = devices.First(d => d.Id == "sim-800g-qsfpdd");
            var device    = await dm.OpenDeviceAsync(simDevice);

            var dash = await device.ReadModuleDashDataAsync(8);

            Assert.NotNull(dash);
            Assert.NotNull(dash.Identity);
            Assert.NotNull(dash.Monitors);
            Assert.Equal(8, dash.Lanes.Count);
            Assert.True(dash.Monitors.Temperature.Value > 0);
            Assert.True(dash.Monitors.VCC.Value         > 0);

            await device.CloseAsync();
        }

        [Fact]
        public async Task RegisterAccess_can_read_and_write_msa_page()
        {
            var services = new ServiceCollection();
            services.AddOpenCmisCore();
            services.AddOpenCmisSimulatedAdapters();
            var sp        = services.BuildServiceProvider();
            var dm        = sp.GetRequiredService<IDeviceManager>();
            var devices   = (await dm.EnumerateDevicesAsync()).ToList();
            var simDevice = devices.First(d => d.Id == "sim-800g-qsfpdd");
            var device    = await dm.OpenDeviceAsync(simDevice);

            var ra = device.RegisterAccess;

            // Write to bank 0, page 0x05 upper memory
            var writeData = new byte[] {0x12, 0x34, 0x56};
            await ra.WriteBlockAsync(0, 0x05, 0x80, writeData);

            // Read back
            var readData = await ra.ReadBlockAsync(0, 0x05, 0x80, 3);
            Assert.Equal(writeData, readData);

            await device.CloseAsync();
        }
    }
}
