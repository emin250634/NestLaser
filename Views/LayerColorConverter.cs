using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NestLaserDesktop.Models;
using NestLaserDesktop.ViewModels;

namespace NestLaserDesktop.Views;

public class LayerColorConverter : IValueConverter
{
    private static MainViewModel? _vm;

    public static void SetViewModel(MainViewModel? vm) => _vm = vm;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string layerName || string.IsNullOrWhiteSpace(layerName))
            return new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));

        try
        {
            if (_vm != null)
            {
                var layer = _vm.Layers.FirstOrDefault(l =>
                    string.Equals(l.Name, layerName, StringComparison.CurrentCultureIgnoreCase));
                if (layer != null && !string.IsNullOrWhiteSpace(layer.Color))
                {
                    var converted = ColorConverter.ConvertFromString(layer.Color);
                    if (converted is Color color)
                        return new SolidColorBrush(color);
                }
            }
        }
        catch { }
        return new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
