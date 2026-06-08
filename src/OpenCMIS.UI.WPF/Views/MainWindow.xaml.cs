using System.Windows;
using System.Windows.Controls;
using OpenCMIS.UI.WPF.ViewModels;

namespace OpenCMIS.UI.WPF.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            viewModel.DeviceConnectionVM.ConnectionChanged += (_, _) =>
            {
                _viewModel.SetDevice(viewModel.DeviceConnectionVM.CurrentDevice);
                _viewModel.UpdateConnectionStatus(
                    viewModel.DeviceConnectionVM.IsConnected,
                    viewModel.DeviceConnectionVM.VendorName);
            };
        }

        private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavListBox.SelectedItem is ListBoxItem item && item.Tag is string viewName)
            {
                _viewModel.NavigateToCommand.Execute(viewName);
            }
        }
    }
}
