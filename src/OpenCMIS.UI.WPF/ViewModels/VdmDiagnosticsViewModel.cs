using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.WPF.ViewModels;

public partial class VdmDiagnosticsViewModel : ObservableObject
{
    private ICmisDevice? _device;
    private CancellationTokenSource? _cts;
    private Task _monitorTask = Task.CompletedTask;
    private Task _transitionTask = Task.CompletedTask;
    private readonly SemaphoreSlim _readGate = new(1, 1);
    private CancellationTokenSource _deviceCts = new();

    [ObservableProperty] private VdmDiagnostics? _diagnostics;
    [ObservableProperty] private bool _isVdmSupported;
    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private int _refreshInterval = 2;
    [ObservableProperty] private string _statusText = "Ready to load";
    [ObservableProperty] private int _activeFlagCount;
    [ObservableProperty] private ObservableCollection<VdmObservableRow> _observableRows = [];

    public List<int> RefreshIntervalOptions { get; } = [1, 2, 5, 10];

    public Task SetDeviceAsync(ICmisDevice? device) => QueueTransitionAsync(async () =>
    {
        await StopCoreAsync();
        _deviceCts.Cancel();
        _deviceCts.Dispose();
        _deviceCts = new();
        _device = device;
        Diagnostics = null;
        IsVdmSupported = false;
        ObservableRows = [];
        ActiveFlagCount = 0;
        StatusText = device is null ? "No device connected." : "Ready to load VDM diagnostics.";
    });

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        await _transitionTask;
        var device = _device;
        var token = _deviceCts.Token;
        if (device is null) return;
        try
        {
            var diagnostics = await ReadAsync(device, token);
            token.ThrowIfCancellationRequested();
            ApplyDiagnostics(diagnostics);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
    }

    private void ApplyDiagnostics(VdmDiagnostics diagnostics)
    {
        Diagnostics = diagnostics;
        IsVdmSupported = diagnostics.IsSupported;
        ObservableRows = new(diagnostics.ObservableInstances.Select(o => new VdmObservableRow(o)));
        ActiveFlagCount = diagnostics.ObservableInstances.Sum(o =>
            (o.Flags.HighAlarm ? 1 : 0) + (o.Flags.HighWarning ? 1 : 0) +
            (o.Flags.LowWarning ? 1 : 0) + (o.Flags.LowAlarm ? 1 : 0));
        StatusText = diagnostics.IsSupported
            ? $"Last refresh: {DateTime.Now:HH:mm:ss}"
            : "VDM is unsupported or has no advertised observables.";
    }

    [RelayCommand]
    private Task StartMonitoringAsync() => QueueTransitionAsync(() =>
    {
        StartCore();
        return Task.CompletedTask;
    });

    private void StartCore()
    {
        if (_device is null || IsMonitoring) return;
        IsMonitoring = true;
        _cts = new();
        _monitorTask = MonitorAsync(_device, Math.Max(1, RefreshInterval), _cts.Token);
    }

    private async Task MonitorAsync(ICmisDevice device, int interval, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var diagnostics = await ReadAsync(device, token);
                    token.ThrowIfCancellationRequested();
                    ApplyDiagnostics(diagnostics);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
                await Task.Delay(TimeSpan.FromSeconds(interval), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            IsMonitoring = false;
            StatusText = "VDM monitoring stopped.";
        }
    }

    [RelayCommand]
    private Task StopMonitoringAsync() => QueueTransitionAsync(StopCoreAsync);

    private async Task StopCoreAsync()
    {
        var cts = _cts;
        if (cts is not null)
        {
            cts.Cancel();
            await _monitorTask;
            cts.Dispose();
            _cts = null;
        }
        IsMonitoring = false;
    }

    partial void OnRefreshIntervalChanged(int value)
    {
        QueueTransitionAsync(async () =>
        {
            if (!IsMonitoring) return;
            await StopCoreAsync();
            StartCore();
        });
    }

    // UI commands and property changes share one ordered, tracked transition chain.
    private Task QueueTransitionAsync(Func<Task> transition)
    {
        _transitionTask = RunTransitionAsync(_transitionTask, transition);
        return _transitionTask;
    }

    private static async Task RunTransitionAsync(Task previous, Func<Task> transition)
    {
        await previous;
        await transition();
    }

    private async Task<VdmDiagnostics> ReadAsync(ICmisDevice device, CancellationToken token)
    {
        // The device API cannot cancel physical I/O. Stop waiting promptly, while
        // the gate remains held until that I/O ends so a restart cannot overlap it.
        return await ReadSerializedAsync(device, token).WaitAsync(token);
    }

    private async Task<VdmDiagnostics> ReadSerializedAsync(ICmisDevice device, CancellationToken token)
    {
        await _readGate.WaitAsync(token);
        try
        {
            token.ThrowIfCancellationRequested();
            return await device.ReadVdmDiagnosticsAsync();
        }
        finally { _readGate.Release(); }
    }
}

public sealed class VdmObservableRow(VdmObservable observable)
{
    public int Instance => observable.Instance;
    public string DescriptorHex => Convert.ToHexString(observable.Descriptor);
    public string SampleHex => $"0x{observable.Sample:X4}";
    public VdmObservableFlags Flags => observable.Flags;
}
