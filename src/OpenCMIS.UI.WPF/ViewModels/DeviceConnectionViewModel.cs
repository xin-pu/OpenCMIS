using System.Collections.ObjectModel;
using DevExpress.Mvvm.CodeGenerators;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.UI.WPF.Models;
using OpenCMIS.UI.WPF.Services;

namespace OpenCMIS.UI.WPF.ViewModels;

[GenerateViewModel]
public partial class DeviceConnectionViewModel
{
    private readonly IDeviceManager _deviceManager;
    private readonly DeviceSession _session;
    private IReadOnlyList<DeviceInfo> _discoveredDevices = [];
    private bool _isSynchronizingSelection;

    [GenerateProperty(OnChangedMethod = nameof(OnSelectedAdapterChanged))]
    private AdapterChoice? _selectedAdapter;

    [GenerateProperty(OnChangedMethod = nameof(OnSelectedDeviceChanged))]
    private DeviceInfo? _selectedDevice;

    [GenerateProperty(OnChangedMethod = nameof(OnSelectedPortChanged))]
    private string _selectedPort = string.Empty;

    [GenerateProperty]
    private bool _isRefreshing;

    [GenerateProperty]
    private bool _isScanning;

    [GenerateProperty]
    private bool _isConnected;

    [GenerateProperty]
    private string _statusMessage = "Ready";

    [GenerateProperty]
    private string _vendorName = string.Empty;

    [GenerateProperty]
    private string _partNumber = string.Empty;

    [GenerateProperty]
    private string _serialNumber = string.Empty;

    public DeviceConnectionViewModel(
        IDeviceManager deviceManager,
        DeviceSession session)
    {
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public ObservableCollection<AdapterChoice> AvailableAdapters { get; } = [];
    public ObservableCollection<DeviceInfo> AvailableDevices { get; } = [];

    // Temporary compatibility surface for the current pre-refactor connection view.
    public ObservableCollection<string> AvailablePorts { get; } = [];
    public ICmisDevice? CurrentDevice => _session.CurrentDevice;
    public event Action<bool, string>? ConnectionChanged;

    [GenerateCommand(Name = "ScanPortsCommand")]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        IsScanning = true;
        StatusMessage = "Scanning...";

        try
        {
            _discoveredDevices = (await _deviceManager.EnumerateDevicesAsync()).ToArray();
            ReplaceAdapters();
            SelectedAdapter = AvailableAdapters.FirstOrDefault();
            ApplyAdapterFilter();
            StatusMessage = _discoveredDevices.Count == 0
                ? "No devices found."
                : $"Found {_discoveredDevices.Count} device(s).";
        }
        catch (Exception exception)
        {
            _discoveredDevices = [];
            AvailableAdapters.Clear();
            ApplyAdapterFilter();
            StatusMessage = $"Scan error: {exception.Message}";
        }
        finally
        {
            IsRefreshing = false;
            IsScanning = false;
        }
    }

    [GenerateCommand(Name = "ConnectCommand")]
    public async Task ConnectAsync()
    {
        if (SelectedDevice is null)
        {
            StatusMessage = "Select a device first.";
            return;
        }

        StatusMessage = $"Connecting to {SelectedDevice.Name}...";
        _session.SetConnecting();
        ICmisDevice? openedDevice = null;

        try
        {
            openedDevice = await _deviceManager.OpenDeviceAsync(SelectedDevice);
            var moduleInfo = await openedDevice.GetModuleInfoAsync();
            _session.SetConnected(SelectedDevice, openedDevice);
            VendorName = moduleInfo.VendorName;
            PartNumber = moduleInfo.PartNumber;
            SerialNumber = moduleInfo.SerialNumber;
            IsConnected = true;
            StatusMessage = "Connected.";
            ConnectionChanged?.Invoke(true, VendorName);
        }
        catch (Exception exception)
        {
            if (openedDevice is not null)
            {
                await _deviceManager.CloseDeviceAsync(openedDevice);
            }

            _session.SetConnectionFailed(exception);
            IsConnected = false;
            StatusMessage = $"Connection failed: {exception.Message}";
            ConnectionChanged?.Invoke(false, string.Empty);
        }
    }

    [GenerateCommand(Name = "DisconnectCommand")]
    public async Task DisconnectAsync()
    {
        var device = _session.CurrentDevice;
        if (device is not null)
        {
            _session.SetDisconnecting();
            await _deviceManager.CloseDeviceAsync(device);
        }

        _session.SetDisconnected();
        IsConnected = false;
        VendorName = string.Empty;
        PartNumber = string.Empty;
        SerialNumber = string.Empty;
        StatusMessage = "Disconnected.";
        ConnectionChanged?.Invoke(false, string.Empty);
    }

    private void ReplaceAdapters()
    {
        AvailableAdapters.Clear();
        foreach (var adapterId in _discoveredDevices
                     .Select(device => device.Profile?.AdapterId)
                     .Where(adapterId => !string.IsNullOrWhiteSpace(adapterId))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(adapterId => adapterId, StringComparer.OrdinalIgnoreCase))
        {
            AvailableAdapters.Add(new AdapterChoice(adapterId!, adapterId!));
        }
    }

    private void OnSelectedAdapterChanged() => ApplyAdapterFilter();

    private void ApplyAdapterFilter()
    {
        AvailableDevices.Clear();
        AvailablePorts.Clear();
        var adapterId = SelectedAdapter?.AdapterId;
        foreach (var device in _discoveredDevices.Where(
                     device => adapterId is null || string.Equals(
                         device.Profile?.AdapterId,
                         adapterId,
                         StringComparison.OrdinalIgnoreCase)))
        {
            AvailableDevices.Add(device);
            AvailablePorts.Add(GetDeviceLabel(device));
        }

        if (SelectedDevice is not null && !AvailableDevices.Contains(SelectedDevice))
        {
            SelectedDevice = null;
        }

        if (SelectedDevice is null)
        {
            SelectedDevice = AvailableDevices.FirstOrDefault();
        }
    }

    private void OnSelectedDeviceChanged()
    {
        if (_isSynchronizingSelection)
        {
            return;
        }

        var label = SelectedDevice is null ? string.Empty : GetDeviceLabel(SelectedDevice);
        if (!string.Equals(SelectedPort, label, StringComparison.Ordinal))
        {
            _isSynchronizingSelection = true;
            try
            {
                SelectedPort = label;
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }
    }

    private void OnSelectedPortChanged(string? port)
    {
        if (_isSynchronizingSelection)
        {
            return;
        }

        if (SelectedDevice is not null
            && string.Equals(
                GetDeviceLabel(SelectedDevice),
                port,
                StringComparison.Ordinal))
        {
            return;
        }

        var device = AvailableDevices.FirstOrDefault(
            candidate => string.Equals(GetDeviceLabel(candidate), port, StringComparison.Ordinal));
        if (!ReferenceEquals(SelectedDevice, device))
        {
            _isSynchronizingSelection = true;
            try
            {
                SelectedDevice = device;
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }
    }

    private static string GetDeviceLabel(DeviceInfo device) => device.Profile switch
    {
        SerialI2cConnectionProfile serial => serial.PortName,
        HmMultiChannelConnectionProfile multi => $"{multi.PortName} / CH{multi.Channel}",
        CypressI2cConnectionProfile cypress => $"{cypress.SerialNumber} / Port {cypress.Port}",
        _ => device.Name.Length == 0 ? device.Id : device.Name
    };
}
