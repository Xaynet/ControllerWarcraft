using System.Windows;
using System.Windows.Controls;
using ControllerWarcraft.Gui.ViewModels;

namespace ControllerWarcraft.Gui.Controls;

/// <summary>
/// Pannello di test del controller (live). Avvia il polling del <see cref="ControllerTestViewModel"/>
/// quando diventa visibile e lo ferma quando viene nascosto/chiuso, così il timer non resta attivo
/// inutilmente. Riusato sia nel tab della finestra principale sia nel primo passo del wizard.
/// </summary>
public partial class ControllerTestView : UserControl
{
    public ControllerTestView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ControllerTestViewModel vm) vm.StartPolling();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ControllerTestViewModel vm) vm.StopPolling();
    }
}
