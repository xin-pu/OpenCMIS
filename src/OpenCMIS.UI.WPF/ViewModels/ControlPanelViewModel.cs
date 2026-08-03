using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Shared;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public class ControlPanelViewModel : ObservableObject
    {
        private ICmisDevice? _device;

        [ObservableProperty]
        private string _currentState = "Unknown";

        [ObservableProperty]
        private bool _canSetLowPwr;

        [ObservableProperty]
        private bool _canSetPwrUp;

        [ObservableProperty]
        private bool _canSetReady;

        [ObservableProperty]
        private bool _canSetPwrDn;

        // Register read/write
        [ObservableProperty]
        private string _regPage = "0";

        [ObservableProperty]
        private string _regAddress = "0";

        [ObservableProperty]
        private string _regValue = "0";

        [ObservableProperty]
        private string _regReadResult = string.Empty;

        public void SetDevice(ICmisDevice? device)
        {
            _device = device;
            if (device != null)
                _ = RefreshStateAsync();
        }

        [RelayCommand]
        private async Task SetStateAsync(string stateName)
        {
            if (_device == null)
                return;

            if (!Enum.TryParse<ModuleState>(stateName, true, out var targetState))
                return;

            try
            {
                await _device.SetStateAsync(targetState);
                await RefreshStateAsync();
            }
            catch (Exception ex)
            {
                RegReadResult = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ReadRegisterAsync()
        {
            if (_device == null)
                return;

            try
            {
                var page  = byte.Parse(RegPage);
                var addr  = byte.Parse(RegAddress);
                var value = await _device.RegisterAccess.ReadByteAsync(page, addr);
                RegReadResult = $"0x{value:X2} ({value})";
            }
            catch (Exception ex)
            {
                RegReadResult = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task WriteRegisterAsync()
        {
            if (_device == null)
                return;

            try
            {
                var page  = byte.Parse(RegPage);
                var addr  = byte.Parse(RegAddress);
                var value = byte.Parse(RegValue);
                await _device.RegisterAccess.WriteByteAsync(page, addr, value);
                RegReadResult = $"Written 0x{value:X2} to Page 0x{page:X2}, Reg 0x{addr:X2}";
            }
            catch (Exception ex)
            {
                RegReadResult = $"Error: {ex.Message}";
            }
        }

        private async Task RefreshStateAsync()
        {
            if (_device == null)
                return;

            try
            {
                var status = await _device.GetStatusAsync();
                CurrentState = status.CurrentState.ToString();

                CanSetLowPwr = status.CurrentState == ModuleState.Initialization;
                CanSetPwrUp  = status.CurrentState is ModuleState.LowPwr or ModuleState.Ready;
                CanSetReady  = status.CurrentState == ModuleState.PwrUp;
                CanSetPwrDn  = status.CurrentState == ModuleState.Ready;
            }
            catch
            {
                CurrentState = "Error";
            }
        }
    }
}
