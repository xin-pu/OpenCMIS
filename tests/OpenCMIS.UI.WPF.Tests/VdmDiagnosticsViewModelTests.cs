using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.UI.WPF.Tests.Fakes;
using OpenCMIS.UI.WPF.ViewModels;
using Xunit;

namespace OpenCMIS.UI.WPF.Tests;

public sealed class VdmDiagnosticsViewModelTests
{
    [Theory]
    [InlineData(VdmReadStatus.Partial, "partial")]
    [InlineData(VdmReadStatus.Unavailable, "unavailable")]
    public async Task Incomplete_diagnostics_report_status_and_unknown_sample(VdmReadStatus status, string expected)
    {
        var device = new FakeCmisDevice(new DeviceInfo())
        {
            VdmRead = () => Task.FromResult(new VdmDiagnostics
            {
                IsSupported = true, ReadStatus = status,
                ObservableInstances = [new VdmObservable { Instance = 1, Descriptor = [0, 1] }]
            })
        };
        var vm = new VdmDiagnosticsViewModel();
        await vm.SetDeviceAsync(device);
        await vm.RefreshAllCommand.ExecuteAsync(null);
        Assert.Contains(expected, vm.StatusText, StringComparison.OrdinalIgnoreCase);
        var row = Assert.Single(vm.ObservableRows);
        Assert.Equal("unknown", row.SampleHex);
        Assert.Null(row.Flags.HighAlarm);
        Assert.Equal("unknown", row.HighAlarmText);
        Assert.Equal("unknown", row.LowAlarmText);
        Assert.Equal("unknown", row.HighWarningText);
        Assert.Equal("unknown", row.LowWarningText);
        Assert.Equal(0, vm.ActiveFlagCount);
    }

    [Fact]
    public async Task Refresh_preserves_unknown_descriptors_raw_samples_and_flags()
    {
        var device = new FakeCmisDevice(new DeviceInfo())
        {
            VdmRead = () => Task.FromResult(new VdmDiagnostics
            {
                IsSupported = true,
                ObservableInstances = [new VdmObservable
                {
                    Instance = 65, Descriptor = [0xFE, 0xAB], Sample = 0x1234,
                    Flags = new VdmObservableFlags { HighAlarm = true, LowWarning = true, HighWarning = false, LowAlarm = false }
                }]
            })
        };
        var vm = new VdmDiagnosticsViewModel();
        await vm.SetDeviceAsync(device);
        await vm.RefreshAllCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.ObservableRows);
        Assert.Equal(65, row.Instance);
        Assert.Equal("FEAB", row.DescriptorHex);
        Assert.Equal("0x1234", row.SampleHex);
        Assert.True(row.Flags.HighAlarm);
        Assert.False(row.Flags.HighWarning);
        Assert.True(row.Flags.LowWarning);
        Assert.False(row.Flags.LowAlarm);
        Assert.Equal("set", row.HighAlarmText);
        Assert.Equal("clear", row.HighWarningText);
        Assert.Equal(2, vm.ActiveFlagCount);

        await vm.SetDeviceAsync(null);
        Assert.Empty(vm.ObservableRows);
        Assert.False(vm.IsVdmSupported);
        Assert.Equal(0, vm.ActiveFlagCount);
    }

    [Fact]
    public async Task Stop_and_restart_during_pending_read_never_launches_a_second_read()
    {
        var pending = new TaskCompletionSource<VdmDiagnostics>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var device = new FakeCmisDevice(new DeviceInfo()) { VdmRead = () =>
        {
            Interlocked.Increment(ref reads);
            entered.TrySetResult();
            return pending.Task;
        }};
        var vm = new VdmDiagnosticsViewModel();
        await vm.SetDeviceAsync(device);
        await vm.StartMonitoringCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await vm.StopMonitoringCommand.ExecuteAsync(null).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.False(vm.IsMonitoring);
        await vm.StartMonitoringCommand.ExecuteAsync(null);
        await vm.StartMonitoringCommand.ExecuteAsync(null);
        Assert.True(vm.IsMonitoring);
        Assert.Equal(1, Volatile.Read(ref reads));

        await vm.StopMonitoringCommand.ExecuteAsync(null).WaitAsync(TimeSpan.FromSeconds(1));
        pending.SetResult(new VdmDiagnostics { IsSupported = true });
        Assert.Null(vm.Diagnostics);
        Assert.Equal(1, Volatile.Read(ref reads));
    }

    [Fact]
    public async Task Interval_changes_wait_for_the_previous_run_and_do_not_overlap_device_reads()
    {
        var first = new TaskCompletionSource<VdmDiagnostics>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<VdmDiagnostics>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var device = new FakeCmisDevice(new DeviceInfo()) { VdmRead = () =>
        {
            if (Interlocked.Increment(ref reads) == 1) return first.Task;
            secondEntered.TrySetResult();
            return second.Task;
        }};
        var vm = new VdmDiagnosticsViewModel();
        await vm.SetDeviceAsync(device);
        try
        {
            await vm.StartMonitoringCommand.ExecuteAsync(null);
            vm.RefreshInterval = 1;
            vm.RefreshInterval = 5;
            vm.RefreshInterval = 10;
            // This command joins the ordered transition chain after all interval changes.
            await vm.StartMonitoringCommand.ExecuteAsync(null);
            Assert.Equal(1, Volatile.Read(ref reads));
            first.SetResult(new VdmDiagnostics { IsSupported = true });
            await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.True(vm.IsMonitoring);
            Assert.Null(vm.Diagnostics);
            Assert.Equal(2, Volatile.Read(ref reads));
            await vm.StopMonitoringCommand.ExecuteAsync(null).WaitAsync(TimeSpan.FromSeconds(1));
            Assert.False(vm.IsMonitoring);
        }
        finally
        {
            await vm.StopMonitoringCommand.ExecuteAsync(null);
            first.TrySetResult(new VdmDiagnostics());
            second.TrySetResult(new VdmDiagnostics());
        }
    }

    [Fact]
    public async Task Device_replacement_discards_pending_manual_refresh_and_monitor_results()
    {
        var pending = new TaskCompletionSource<VdmDiagnostics>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldDevice = new FakeCmisDevice(new DeviceInfo()) { VdmRead = () => pending.Task };
        var newSnapshot = new VdmDiagnostics
        {
            IsSupported = true,
            ObservableInstances = [new VdmObservable { Instance = 2, Descriptor = [0xFF, 0x91], Sample = 0xBEEF }]
        };
        var newDevice = new FakeCmisDevice(new DeviceInfo()) { VdmRead = () => Task.FromResult(newSnapshot) };
        var vm = new VdmDiagnosticsViewModel();
        await vm.SetDeviceAsync(oldDevice);
        var refresh = vm.RefreshAllCommand.ExecuteAsync(null);
        await vm.StartMonitoringCommand.ExecuteAsync(null);
        try
        {
            await vm.SetDeviceAsync(newDevice).WaitAsync(TimeSpan.FromSeconds(1));
            await refresh.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.False(vm.IsMonitoring);
            Assert.Null(vm.Diagnostics);
            pending.SetResult(new VdmDiagnostics { IsSupported = true });
            await vm.RefreshAllCommand.ExecuteAsync(null).WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Same(newSnapshot, vm.Diagnostics);
            Assert.Equal("0xBEEF", Assert.Single(vm.ObservableRows).SampleHex);
        }
        finally
        {
            pending.TrySetResult(new VdmDiagnostics());
            await vm.SetDeviceAsync(null);
        }
    }
}
