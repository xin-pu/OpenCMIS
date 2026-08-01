using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class ModuleHomeViewModel : ObservableObject
    {
        private ICmisDevice? _device;
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private ModuleDashData? _dashData;

        [ObservableProperty]
        private int _refreshInterval = 2;

        [ObservableProperty]
        private bool _isMonitoring;

        [ObservableProperty]
        private string _statusText = "Ready to load";

        [ObservableProperty]
        private string _currentStateText = "Disconnected";

        [ObservableProperty]
        private bool _isDeviceAvailable;

        public List<int> RefreshIntervalOptions { get; } = [1, 2, 5, 10];

        public void SetDevice(ICmisDevice? device)
        {
            _ = StopMonitoringAsync();
            _device = device;
            IsDeviceAvailable = device != null;

            if (device != null)
                _ = RefreshAllAsync();
            else
                DashData = null;
        }

        [RelayCommand]
        private async Task RefreshAllAsync()
        {
            if (_device == null)
            {
                StatusText = "No device connected.";
                return;
            }

            try
            {
                StatusText = "Loading...";
                DashData = await _device.ReadModuleDashDataAsync();
                CurrentStateText = DashData.CurrentState.ToString();
                StatusText = $"Last refresh: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RefreshIdentityAsync()
        {
            if (_device == null || DashData == null) return;

            try
            {
                DashData.Identity = await _device.ReadModuleIdentityAsync();
                StatusText = $"Identity refreshed at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task StartMonitoringAsync()
        {
            if (_device == null) return;

            IsMonitoring = true;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var delayMs = RefreshInterval * 1000;

            StatusText = $"Monitoring every {RefreshInterval}s";

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(delayMs, token);

                    if (_device == null) break;

                    try
                    {
                        var monitors = await _device.ReadModuleMonitorsAsync();
                        var lanes = await _device.ReadLaneStatusAsync();
                        var modStatus = await _device.GetStatusAsync();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (DashData == null) return;

                            DashData.Monitors = monitors;
                            DashData.Lanes = lanes;
                            DashData.CurrentState = modStatus.CurrentState;
                            DashData.IsReady = modStatus.IsReady;
                            DashData.StatusTimestamp = DateTime.Now;
                            CurrentStateText = modStatus.CurrentState.ToString();
                            StatusText = $"Monitoring every {RefreshInterval}s — Last: {DateTime.Now:HH:mm:ss}";
                        });
                    }
                    catch
                    {
                        // Skip failed reads during monitoring
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected on stop
            }
            finally
            {
                IsMonitoring = false;
                StatusText = "Monitoring stopped.";
            }
        }

        [RelayCommand]
        private async Task StopMonitoringAsync()
        {
            if (_cts != null)
            {
                await _cts.CancelAsync();
                _cts.Dispose();
                _cts = null;
            }

            IsMonitoring = false;
        }

        partial void OnRefreshIntervalChanged(int value)
        {
            if (IsMonitoring)
            {
                _ = StopMonitoringAsync();
                _ = StartMonitoringAsync();
            }
        }
    }
}
