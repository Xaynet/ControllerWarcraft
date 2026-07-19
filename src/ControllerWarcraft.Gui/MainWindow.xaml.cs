using System.Windows;
using ControllerWarcraft.Gui.ViewModels;
using ControllerWarcraft.Gui.Windows;

namespace ControllerWarcraft.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    /// <summary>Al primo avvio (setup non completato) mostra automaticamente il wizard.</summary>
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm.NeedsSetup) ShowWizard();
    }

    private void OpenWizard_Click(object sender, RoutedEventArgs e) => ShowWizard();

    /// <summary>
    /// Apre il wizard di primo avvio in modale. Al termine ricarica profili e impostazioni nella
    /// finestra principale così l'eventuale nuovo profilo attivo è subito visibile.
    /// </summary>
    private void ShowWizard()
    {
        var wizard = new WizardWindow(new WizardViewModel()) { Owner = this };
        wizard.ShowDialog();
        Vm.ReloadAfterWizard();
    }
}
