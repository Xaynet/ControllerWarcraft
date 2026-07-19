using System.Windows;
using ControllerWarcraft.Gui.ViewModels;

namespace ControllerWarcraft.Gui.Windows;

/// <summary>
/// Finestra del wizard di primo avvio. Ospita il <see cref="WizardViewModel"/> e chiude con
/// <see cref="Window.DialogResult"/> quando il VM lo richiede (Fine o Salta).
/// </summary>
public partial class WizardWindow : Window
{
    private readonly WizardViewModel _vm;

    public WizardWindow(WizardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _vm.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(bool completed)
    {
        _vm.RequestClose -= OnRequestClose;
        DialogResult = completed;
        Close();
    }
}
