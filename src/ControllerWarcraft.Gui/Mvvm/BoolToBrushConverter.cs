using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ControllerWarcraft.Gui.Mvvm;

/// <summary>
/// Converte un <c>bool</c> in un pennello: pulsante premuto → colore acceso, altrimenti neutro.
/// Usato dal pannello di test del controller per evidenziare i pulsanti attivi in tempo reale.
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    private static readonly Brush On = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));   // verde
    private static readonly Brush Off = new SolidColorBrush(Color.FromRgb(0xDD, 0xDF, 0xE2));   // grigio chiaro

    static BoolToBrushConverter()
    {
        On.Freeze();
        Off.Freeze();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? On : Off;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
