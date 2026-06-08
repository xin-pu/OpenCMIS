using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.App.Core;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class DeviceConnectionViewModel : ObservableObject
    {
        private readonly IDeviceManager _deviceManager;

        [ObservableProperty]
        private string _selectedPort = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availablePorts = [];

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private string _vendorName = string.Empty;

        [ObservableProperty]
        private string _partNumber = string.Empty;

        [ObservableProperty]
        private string _serialNumber = string.Empty;

        public ICmisDevice? CurrentDevice { get; private set; }
        public event Action<bool, string>? ConnectionChanged;

        public DeviceConnectionViewModel(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        [RelayCommand]
        private async Task ScanPortsAsync()
        {
            IsScanning = true;
            StatusMessage = "Scanning...";

            try
            {
                var devices = await _deviceManager.EnumerateDevicesAsync();
                AvailablePorts.Clear();
                foreach (var device in devices)
                {
                    var portName = device.ConnectionParameters.GetValueOrDefault("PortName", device.Id);
                    AvailablePorts.Add(portName);
                }

                if (AvailablePorts.Count == 0)
                    StatusMessage = "No devices found.";
                else
                    StatusMessage = $"Found {AvailablePorts.Count} device(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan error: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        [RelayCommand]
        private async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(SelectedPort))
            {
                StatusMessage = "Select a port first.";
                return;
            }

            StatusMessage = $"Connecting to {SelectedPort}...";

            try
            {
                var deviceInfo = new DeviceInfo
                {
                    Id = SelectedPort,
                    Name = $"CMIS Module on {SelectedPort}",
                    ConnectionType = ConnectionType.I2C,
                    ConnectionParameters = new Dictionary<string, string>
                    {
                        ["PortName"] = SelectedPort,
                        ["BaudRate"] = "115200",
                        ["SlaveAddress"] = "0xA0"
                    }
                };

                CurrentDevice = await _deviceManager.OpenDeviceAsync(deviceInfo);
                IsConnected = true;

                // Read basic info
                var info = await CurrentDevice.GetModuleInfoAsync();
                VendorName = info.VendorName;
                PartNumber = info.PartNumber;
                SerialNumber = info.SerialNumber;

                StatusMessage = "Connected.";
                ConnectionChanged?.Invoke(true, info.VendorName);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = $"Connection failed: {ex.Message}";
                ConnectionChanged?.Invoke(false, "");
            }
        }

        [RelayCommand]
        private async Task DisconnectAsync()
        {
            if (CurrentDevice != null)
            {
                await CurrentDevice.CloseAsync();
                CurrentDevice = null;
            }

            IsConnected = false;
            StatusMessage = "Disconnected.";
            VendorName = string.Empty;
            PartNumber = string.Empty;
            SerialNumber = string.Empty;
            ConnectionChanged?.Invoke(false, "");
        }
    }
}
