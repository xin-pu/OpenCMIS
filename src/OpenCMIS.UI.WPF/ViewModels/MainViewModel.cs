using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCMIS.Protocol.Abstractions;

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

        [ObservableProperty]
        private bool _isNavigationPaneExpanded = true;

        public MainViewModel(DeviceConnectionViewModel  deviceConnectionVM,
                             ControlPanelViewModel      controlPanelVM,
                             CdbEditorViewModel         cdbEditorVM,
                             ApplicationSwitchViewModel applicationSwitchVM,
                             PageEditorViewModel        pageEditorVM,
                             ModuleHomeViewModel        moduleHomeVM)
        {
            DeviceConnectionVM  = deviceConnectionVM;
            ControlPanelVM      = controlPanelVM;
            CdbEditorVM         = cdbEditorVM;
            ApplicationSwitchVM = applicationSwitchVM;
            PageEditorVM        = pageEditorVM;
            ModuleHomeVM        = moduleHomeVM;

            ActiveView = DeviceConnectionVM;
        }

        public DeviceConnectionViewModel  DeviceConnectionVM  { get; }
        public ControlPanelViewModel      ControlPanelVM      { get; }
        public CdbEditorViewModel         CdbEditorVM         { get; }
        public ApplicationSwitchViewModel ApplicationSwitchVM { get; }
        public PageEditorViewModel        PageEditorVM        { get; }
        public ModuleHomeViewModel        ModuleHomeVM        { get; }

        public void SetDevice(ICmisDevice? device)
        {
            ControlPanelVM.SetDevice(device);
            CdbEditorVM.SetDevice(device);
            ApplicationSwitchVM.SetDevice(device);
            PageEditorVM.SetDevice(device);
            ModuleHomeVM.SetDevice(device);
        }

        public void UpdateConnectionStatus(bool connected, string deviceName)
        {
            ConnectionStatus = connected ? "Connected" : "Disconnected";
            DeviceName       = deviceName;
        }

        [RelayCommand]
        private void NavigateTo(string viewName)
        {
            SelectedViewName = viewName;
            ActiveView = viewName switch
                         {
                             "ControlPanel"      => ControlPanelVM,
                             "CdbEditor"         => CdbEditorVM,
                             "ApplicationSwitch" => ApplicationSwitchVM,
                             "PageEditor"        => PageEditorVM,
                             "ModuleHome"        => ModuleHomeVM,
                             _                   => DeviceConnectionVM
                         };
        }

        [RelayCommand]
        private void ToggleNavigation()
        {
            IsNavigationPaneExpanded = !IsNavigationPaneExpanded;
        }
    }
}
