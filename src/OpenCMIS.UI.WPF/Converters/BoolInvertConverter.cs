using System.Globalization;
using System.Windows.Data;

namespace OpenCMIS.UI.WPF.Converters
{
    /// <summary>
    ///     Inverts a boolean value. Returns !value.
    /// </summary>
    public class BoolInvertConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : Binding.DoNothing;
        }
        
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : Binding.DoNothing;
        }
    }
}
