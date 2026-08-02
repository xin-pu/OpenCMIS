using System.Globalization;
using System.Windows.Data;
using OpenCMIS.Protocol.Abstractions.Models;

namespace OpenCMIS.UI.WPF.Converters;

/// <summary>
/// Converts a MonitorValue to the pixel width of a range bar fill,
/// normalized between AlarmLow (0%) and AlarmHigh (100%).
/// ConverterParameter specifies the available pixel width.
/// </summary>
public class ThresholdWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MonitorValue mv)
            return 0.0;

        if (parameter is not string widthStr
            || !double.TryParse(widthStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxWidth))
            return 0.0;

        var range = mv.AlarmHigh - mv.AlarmLow;
        if (range <= 0)
            return 0.0;

        var position = (mv.Value - mv.AlarmLow) / range;
        return Math.Max(0.0, Math.Min(maxWidth, position * maxWidth));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
