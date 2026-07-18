using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// Riga editabile di una voce del radial menu (Fase 4): etichetta + un solo keybind. Fa da
/// adattatore tra <see cref="RadialMenuItem"/> e i controlli a due vie della DataGrid, come
/// <see cref="AbilityRowViewModel"/> per la tabella abilità. Ogni voce = un keybind (1:1).
/// </summary>
public sealed class RadialItemRowViewModel : ObservableObject
{
    private string _label;
    private ScanCode _key;
    private bool _shift;
    private bool _ctrl;
    private bool _alt;

    public RadialItemRowViewModel() : this(new RadialMenuItem()) { }

    public RadialItemRowViewModel(RadialMenuItem item)
    {
        _label = item.Label;
        _key = item.Bind.Key;
        _shift = item.Bind.Shift;
        _ctrl = item.Bind.Ctrl;
        _alt = item.Bind.Alt;
    }

    public string Label { get => _label; set => SetField(ref _label, value); }
    public ScanCode Key { get => _key; set => SetField(ref _key, value); }
    public bool Shift { get => _shift; set => SetField(ref _shift, value); }
    public bool Ctrl { get => _ctrl; set => SetField(ref _ctrl, value); }
    public bool Alt { get => _alt; set => SetField(ref _alt, value); }

    /// <summary>Ricostruisce la <see cref="RadialMenuItem"/> dal contenuto editato.</summary>
    public RadialMenuItem ToItem() => new(Label ?? "", new Keybind(Key, Shift, Ctrl, Alt));
}
