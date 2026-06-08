using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OpenCMIS.Shared;

namespace OpenCMIS.UI.WPF.Converters
{
    public class ModuleStateToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stateStr && Enum.TryParse<ModuleState>(stateStr, out var state))
            {
                return state switch
                {
                    ModuleState.Ready         => new SolidColorBrush(Colors.Green),
                    ModuleState.LowPwr        => new SolidColorBrush(Colors.Orange),
                    ModuleState.PwrUp         => new SolidColorBrush(Colors.DodgerBlue),
                    ModuleState.PwrDn         => new SolidColorBrush(Colors.Gray),
                    ModuleState.Initialization => new SolidColorBrush(Colors.Yellow),
                    ModuleState.Fault         => new SolidColorBrush(Colors.Red),
                    _                         => new SolidColorBrush(Colors.Gray)
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
