using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WomierDriver.App.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (Invert)
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is Visibility.Visible) != Invert;
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    /// <summary>When false (default): null is collapsed. When true: null is visible.</summary>
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value is null || (value is string s && s.Length == 0);
        return isNull == Invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ZeroToVisibilityConverter : IValueConverter
{
    /// <summary>When false (default): zero is visible. When true: non-zero is visible.</summary>
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isZero = value switch
        {
            int i => i == 0,
            long l => l == 0,
            double d => d == 0,
            null => true,
            _ => false,
        };
        return isZero != Invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
