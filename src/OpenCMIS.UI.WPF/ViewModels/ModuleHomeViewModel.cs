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

        [ObservableProperty]
        private bool _isIdentityPanelVisible = true;

        [ObservableProperty]
        private ObservableCollection<IdentityPropertyItem> _identityProperties = [];

        [ObservableProperty]
        private ObservableCollection<LaneMonitorItem> _perLaneMonitorItems = [];

        [ObservableProperty]
        private int _activeInterruptCount;

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
                UpdateIdentityProperties();
                UpdatePerLaneMonitors(DashData.Monitors, DashData.Lanes);
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
                                                                      UpdatePerLaneMonitors(monitors, lanes);
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
        private void ToggleIdentityPanel()
        {
            IsIdentityPanelVisible = !IsIdentityPanelVisible;
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
            ActiveInterruptCount = InterruptFlagItems.Count(i => i.IsActive);
        }

        private void UpdateIdentityProperties()
        {
            if (DashData?.Identity == null)
            {
                IdentityProperties = [];
                return;
            }
            var id = DashData.Identity;
            IdentityProperties = new ObservableCollection<IdentityPropertyItem>
            {
                new("Vendor Name",      id.VendorName),
                new("Vendor OUI",       id.VendorOUI),
                new("Part Number",      id.PartNumber),
                new("Serial Number",    id.SerialNumber),
                new("Hardware Rev",     id.HardwareRevision),
                new("Firmware Rev",     id.FirmwareRevision),
                new("Date Code",        id.DateCode),
                new("Module Type",      id.ModuleType),
                new("Connector Type",   id.ConnectorType),
                new("CMIS Version",     id.CmisVersion),
                new("CLEI Code",        id.CLEICode),
            };
        }

        private void UpdatePerLaneMonitors(ModuleMonitors? monitors, List<LaneStatus>? lanes)
        {
            if (monitors == null || lanes == null)
            {
                PerLaneMonitorItems = [];
                return;
            }

            var items = new List<LaneMonitorItem>();
            for (var i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                items.Add(new LaneMonitorItem(
                    lane.LaneNumber,
                    lane.IsEnabled,
                    lane.HasFault,
                    i < monitors.TxPowerPerLane.Count ? monitors.TxPowerPerLane[i].Value : 0,
                    i < monitors.RxPowerPerLane.Count ? monitors.RxPowerPerLane[i].Value : 0,
                    i < monitors.TxBiasPerLane.Count   ? monitors.TxBiasPerLane[i].Value   : 0,
                    lane.TxLos,
                    lane.RxLos
                ));
            }
            PerLaneMonitorItems = new ObservableCollection<LaneMonitorItem>(items);
        }
    }

    public record InterruptFlagItem(string Name, bool IsActive);
    public record IdentityPropertyItem(string Label, string Value);
    public record LaneMonitorItem(int LaneNumber, bool IsEnabled, bool HasFault, double TxPower, double RxPower, double TxBias, bool TxLos, bool RxLos);
}
