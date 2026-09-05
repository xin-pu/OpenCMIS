using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Accordion;
using OpenCMIS.UI.WPF.ViewModels;

namespace OpenCMIS.UI.WPF.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel  = viewModel;
            DataContext = viewModel;

            viewModel.DeviceConnectionVM.ConnectionChanged += async (_, _) =>
                                                                  {
                                                                      await _viewModel.SetDeviceAsync(viewModel.DeviceConnectionVM.CurrentDevice);
                                                                      _viewModel.UpdateConnectionStatus(
                                                                              viewModel.DeviceConnectionVM.IsConnected,
                                                                              viewModel.DeviceConnectionVM.VendorName);
                                                                  };
        }

        private void NavAccordionItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is AccordionItem item && item.Tag is string viewName)
                _viewModel.NavigateToCommand.Execute(viewName);
        }
    }
}
