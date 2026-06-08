using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenCMIS.App.Core;
using OpenCMIS.UI.WPF.ViewModels;
using OpenCMIS.UI.WPF.Views;
using Serilog;

namespace OpenCMIS.UI.WPF
{
    public partial class App : Application
    {
        private IHost _host = null!;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            _host = Host.CreateDefaultBuilder()
                .UseSerilog((context, config) =>
                {
                    config.MinimumLevel.Debug()
                          .WriteTo.Console()
                          .WriteTo.File("logs/cmis-wpf-.log", rollingInterval: Serilog.RollingInterval.Day);
                })
                .ConfigureServices(services =>
                {
                    services.AddOpenCmisCore();

                    // ViewModels
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<DeviceConnectionViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ControlPanelViewModel>();
                    services.AddTransient<CdbEditorViewModel>();
                    services.AddTransient<ApplicationSwitchViewModel>();
                    services.AddTransient<PageEditorViewModel>();

                    // Views
                    services.AddTransient<MainWindow>();
                })
                .Build();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
