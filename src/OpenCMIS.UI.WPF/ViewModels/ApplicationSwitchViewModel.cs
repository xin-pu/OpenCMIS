using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Core;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class ApplicationSwitchViewModel : ObservableObject
    {
        private ICmisDevice? _device;
        private CmisApplicationFactory? _factory;

        [ObservableProperty]
        private string _currentApplication = "Unknown";

        [ObservableProperty]
        private ObservableCollection<CmisApplication> _availableApplications = [];

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public void SetDevice(ICmisDevice? device)
        {
            _device = device;
            if (device != null)
            {
                _factory = new CmisApplicationFactory(device.RegisterAccess);
                _ = RefreshApplicationsAsync();
            }
        }

        [RelayCommand]
        private async Task RefreshApplicationsAsync()
        {
            if (_device == null || _factory == null) return;

            try
            {
                var current = await _factory.GetCurrentApplicationAsync();
                CurrentApplication = current?.ToString() ?? "Unknown";

                var apps = await _factory.GetSupportedApplicationsAsync();
                AvailableApplications = new ObservableCollection<CmisApplication>(apps);

                StatusMessage = $"Found {apps.Count()} supported applications.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SwitchApplicationAsync(byte appCode)
        {
            if (_factory == null) return;

            try
            {
                await _factory.SwitchApplicationAsync(appCode);
                CurrentApplication = $"[0x{appCode:X2}]";
                StatusMessage = $"Switched to application 0x{appCode:X2}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
    }
}
