using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.WPF.Converters
{
    public class MonitorValueToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush GreenBrush = new(Colors.Green);
        private static readonly SolidColorBrush OrangeBrush = new(Colors.Orange);
        private static readonly SolidColorBrush RedBrush = new(Colors.Red);
        private static readonly SolidColorBrush GrayBrush = new(Colors.Gray);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MonitorValue mv)
            {
                if (mv.HasAlarm) return RedBrush;
                if (mv.HasWarning) return OrangeBrush;
                return GreenBrush;
            }

            return GrayBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
