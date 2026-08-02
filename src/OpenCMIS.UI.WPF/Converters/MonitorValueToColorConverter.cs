using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.WPF.Converters
{
    public class MonitorValueToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var app = Application.Current;
            if (value is MonitorValue mv)
            {
                if (mv.HasAlarm)
                    return app?.TryFindResource("OpenCmisDangerBrush") ?? new SolidColorBrush(Colors.Red);
                if (mv.HasWarning)
                    return app?.TryFindResource("OpenCmisWarningBrush") ?? new SolidColorBrush(Colors.Orange);
                return app?.TryFindResource("OpenCmisSuccessBrush") ?? new SolidColorBrush(Colors.Green);
            }

            return app?.TryFindResource("OpenCmisMutedTextBrush") ?? new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
