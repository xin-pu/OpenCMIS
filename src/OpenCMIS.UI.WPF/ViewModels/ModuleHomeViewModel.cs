using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class ModuleHomeViewModel : ObservableObject
    {
        private ICmisDevice?             _device;
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

        [ObservableProperty]
        private bool _hasAlerts;

        [ObservableProperty]
        private ObservableCollection<string> _activeAlerts = [];

        [ObservableProperty]
        private CmisInterruptFlags? _interruptFlags;

        [ObservableProperty]
        private ObservableCollection<InterruptFlagItem> _interruptFlagItems = [];

        public List<int> RefreshIntervalOptions { get; } = [1, 2, 5, 10];

        public void SetDevice(ICmisDevice? device)
        {
            _                 = StopMonitoringAsync();
            _device           = device;
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
                StatusText       = "Loading...";
                DashData         = await _device.ReadModuleDashDataAsync();
                CurrentStateText = DashData.CurrentState.ToString();
                UpdateAlertsAndFlags(DashData.Status);
                StatusText       = $"Last refresh: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RefreshIdentityAsync()
        {
            if (_device == null || DashData == null)
                return;

            try
            {
                DashData.Identity = await _device.ReadModuleIdentityAsync();
                StatusText        = $"Identity refreshed at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task StartMonitoringAsync()
        {
            if (_device == null)
                return;

            IsMonitoring = true;
            _cts         = new ();
            var token   = _cts.Token;
            var delayMs = RefreshInterval * 1000;

            StatusText = $"Monitoring every {RefreshInterval}s";

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(delayMs, token);

                    if (_device == null)
                        break;

                    try
                    {
                        var monitors  = await _device.ReadModuleMonitorsAsync();
                        var lanes     = await _device.ReadLaneStatusAsync();
                        var modStatus = await _device.GetStatusAsync();

                        Application.Current.Dispatcher.Invoke(() =>
                                                                  {
                                                                      if (DashData == null)
                                                                          return;

                                                                      // Replace the whole DashData object: ModuleDashData and its nested
                                                                      // monitor models are plain POCOs without INotifyPropertyChanged, so
                                                                      // mutating nested properties would never refresh the bound gauges.
                                                                      DashData = new ModuleDashData
                                                                                 {
                                                                                     Identity        = DashData.Identity,
                                                                                     Monitors        = monitors,
                                                                                     Lanes           = lanes,
                                                                                     CurrentState    = modStatus.CurrentState,
                                                                                     IsReady         = modStatus.IsReady,
                                                                                     Status          = modStatus,
                                                                                     StatusTimestamp = DateTime.Now
                                                                                 };
                                                                      CurrentStateText = modStatus.CurrentState.ToString();
                                                                      UpdateAlertsAndFlags(modStatus);
                                                                      StatusText =
                                                                              $"Monitoring every {RefreshInterval}s — Last: {DateTime.Now:HH:mm:ss}";
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
                StatusText   = "Monitoring stopped.";
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

        private void UpdateAlertsAndFlags(ModuleStatus? status)
        {
            if (status == null)
            {
                HasAlerts         = false;
                ActiveAlerts      = [];
                InterruptFlags    = null;
                InterruptFlagItems = [];
                return;
            }

            HasAlerts      = status.HasAlerts;
            ActiveAlerts   = new ObservableCollection<string>(status.ActiveAlerts);
            InterruptFlags = status.InterruptFlags;
            InterruptFlagItems = new ObservableCollection<InterruptFlagItem>
            {
                new("Temp H",   status.InterruptFlags.TempHighAlarm),
                new("Temp L",   status.InterruptFlags.TempLowAlarm),
                new("VCC H",    status.InterruptFlags.VccHighAlarm),
                new("VCC L",    status.InterruptFlags.VccLowAlarm),
                new("TX Pwr H", status.InterruptFlags.TxPowerHighAlarm),
                new("TX Pwr L", status.InterruptFlags.TxPowerLowAlarm),
                new("RX Pwr H", status.InterruptFlags.RxPowerHighAlarm),
                new("RX Pwr L", status.InterruptFlags.RxPowerLowAlarm),
                new("TX Bias H",status.InterruptFlags.TxBiasHighAlarm),
                new("TX Bias L",status.InterruptFlags.TxBiasLowAlarm),
                new("TX Fault", status.InterruptFlags.TxFault),
                new("RX LOS",   status.InterruptFlags.RxLOS)
            };
        }
    }

    public record InterruptFlagItem(string Name, bool IsActive);
}
