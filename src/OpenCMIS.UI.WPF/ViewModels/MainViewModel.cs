using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenCMIS.UI.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? _activeView;

        [ObservableProperty]
        private string _selectedViewName = "DeviceConnection";

        [ObservableProperty]
        private string _connectionStatus = "Disconnected";

        [ObservableProperty]
        private string _deviceName = "No Device";

        public DeviceConnectionViewModel DeviceConnectionVM { get; }
        public DashboardViewModel DashboardVM { get; }
        public ControlPanelViewModel ControlPanelVM { get; }
        public CdbEditorViewModel CdbEditorVM { get; }
        public ApplicationSwitchViewModel ApplicationSwitchVM { get; }
        public PageEditorViewModel PageEditorVM { get; }

        public MainViewModel(
            DeviceConnectionViewModel deviceConnectionVM,
            DashboardViewModel dashboardVM,
            ControlPanelViewModel controlPanelVM,
            CdbEditorViewModel cdbEditorVM,
            ApplicationSwitchViewModel applicationSwitchVM,
            PageEditorViewModel pageEditorVM)
        {
            DeviceConnectionVM = deviceConnectionVM;
            DashboardVM = dashboardVM;
            ControlPanelVM = controlPanelVM;
            CdbEditorVM = cdbEditorVM;
            ApplicationSwitchVM = applicationSwitchVM;
            PageEditorVM = pageEditorVM;

            ActiveView = DeviceConnectionVM;
        }

        [RelayCommand]
        private void NavigateTo(string viewName)
        {
            SelectedViewName = viewName;
            ActiveView = viewName switch
            {
                "Dashboard"          => DashboardVM,
                "ControlPanel"       => ControlPanelVM,
                "CdbEditor"          => CdbEditorVM,
                "ApplicationSwitch"  => ApplicationSwitchVM,
                "PageEditor"         => PageEditorVM,
                _                    => DeviceConnectionVM
            };
        }

        public void UpdateConnectionStatus(bool connected, string deviceName)
        {
            ConnectionStatus = connected ? "Connected" : "Disconnected";
            DeviceName = deviceName;
        }
    }
}
