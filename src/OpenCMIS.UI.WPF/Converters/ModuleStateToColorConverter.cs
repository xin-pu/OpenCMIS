using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using OpenCMIS.Shared;

namespace OpenCMIS.UI.WPF.Converters
{
    public class ModuleStateToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var app = Application.Current;
            if (value is string stateStr && Enum.TryParse<ModuleState>(stateStr, out var state))
            {
                var key = state switch
                          {
                              ModuleState.Ready          => "OpenCmisSuccessBrush",
                              ModuleState.LowPwr         => "OpenCmisWarningBrush",
                              ModuleState.PwrUp          => "OpenCmisAccentBrush",
                              ModuleState.PwrDn          => "OpenCmisMutedTextBrush",
                              ModuleState.Initialization => "OpenCmisWarningBrush",
                              ModuleState.Fault          => "OpenCmisDangerBrush",
                              _                          => "OpenCmisMutedTextBrush"
                          };
                return app?.TryFindResource(key) ?? new SolidColorBrush(Colors.Gray);
            }

            if (value is ModuleState modState)
            {
                var key = modState switch
                          {
                              ModuleState.Ready          => "OpenCmisSuccessBrush",
                              ModuleState.LowPwr         => "OpenCmisWarningBrush",
                              ModuleState.PwrUp          => "OpenCmisAccentBrush",
                              ModuleState.PwrDn          => "OpenCmisMutedTextBrush",
                              ModuleState.Initialization => "OpenCmisWarningBrush",
                              ModuleState.Fault          => "OpenCmisDangerBrush",
                              _                          => "OpenCmisMutedTextBrush"
                          };
                return app?.TryFindResource(key) ?? new SolidColorBrush(Colors.Gray);
            }

            return app?.TryFindResource("OpenCmisMutedTextBrush") ?? new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
