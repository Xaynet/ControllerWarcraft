using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// Riga editabile della tabella abilita' nella DataGrid. Fa da adattatore tra la
/// <see cref="AbilityBinding"/> immutabile (Keybind e' un record struct) e i controlli
/// a due vie della griglia.
/// </summary>
public sealed class AbilityRowViewModel : ObservableObject
{
    private ActionButton _button;
    private AbilityLayer _layer;
    private ScanCode _key;
    private bool _shift;
    private bool _ctrl;
    private bool _alt;

    public AbilityRowViewModel(AbilityBinding b)
    {
        _button = b.Button;
        _layer = b.Layer;
        _key = b.Bind.Key;
        _shift = b.Bind.Shift;
        _ctrl = b.Bind.Ctrl;
        _alt = b.Bind.Alt;
    }

    public ActionButton Button { get => _button; set => SetField(ref _button, value); }
    public AbilityLayer Layer { get => _layer; set => SetField(ref _layer, value); }
    public ScanCode Key { get => _key; set => SetField(ref _key, value); }
    public bool Shift { get => _shift; set => SetField(ref _shift, value); }
    public bool Ctrl { get => _ctrl; set => SetField(ref _ctrl, value); }
    public bool Alt { get => _alt; set => SetField(ref _alt, value); }

    /// <summary>Ricostruisce la <see cref="AbilityBinding"/> dal contenuto editato.</summary>
    public AbilityBinding ToBinding() => new(Button, Layer, new Keybind(Key, Shift, Ctrl, Alt));
}
