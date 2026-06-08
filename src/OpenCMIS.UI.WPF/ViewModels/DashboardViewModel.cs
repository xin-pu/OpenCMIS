using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.App.Core;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private DeviceMonitor? _monitor;
        private ICmisDevice? _device;

        [ObservableProperty]
        private string _currentState = "Unknown";

        [ObservableProperty]
        private bool _isReady;

        [ObservableProperty]
        private bool _hasAlerts;

        [ObservableProperty]
        private ObservableCollection<string> _activeAlerts = [];

        [ObservableProperty]
        private bool _isMonitoring;

        [ObservableProperty]
        private string _statusText = "Not monitoring";

        public void SetDevice(ICmisDevice? device)
        {
            _ = StopMonitoringAsync();
            _device = device;

            if (device != null)
                _ = RefreshStatusAsync();
        }

        [RelayCommand]
        private async Task RefreshStatusAsync()
        {
            if (_device == null) return;

            try
            {
                var status = await _device.GetStatusAsync();
                CurrentState = status.CurrentState.ToString();
                IsReady = status.IsReady;
                HasAlerts = status.HasAlerts;
                ActiveAlerts = new ObservableCollection<string>(status.ActiveAlerts);
                StatusText = $"Last update: {DateTime.Now:HH:mm:ss}";
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

            _monitor = new DeviceMonitor(_device);
            _monitor.StatusChanged += (_, args) =>
            {
                CurrentState = args.NewStatus.CurrentState.ToString();
                IsReady = args.NewStatus.IsReady;
                HasAlerts = args.NewStatus.HasAlerts;
                ActiveAlerts = new ObservableCollection<string>(args.NewStatus.ActiveAlerts);
                StatusText = $"Last update: {DateTime.Now:HH:mm:ss}";
            };
            _monitor.Alert += (_, args) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveAlerts.Add(args.Message);
                });
            };

            await _monitor.StartMonitoringAsync(TimeSpan.FromSeconds(1));
            IsMonitoring = true;
            StatusText = "Monitoring active...";
        }

        [RelayCommand]
        private async Task StopMonitoringAsync()
        {
            if (_monitor != null)
            {
                await _monitor.StopMonitoringAsync();
                _monitor = null;
            }

            IsMonitoring = false;
            StatusText = "Monitoring stopped.";
        }
    }
}
