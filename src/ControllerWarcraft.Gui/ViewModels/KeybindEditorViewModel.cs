using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// Adattatore editabile per un <see cref="Keybind"/> "di sistema" (Salto, Tab-target, Annulla).
/// Fase 3: la GUI ora permette di modificarli (in Fase 2 erano sola lettura). Poiché
/// <see cref="Keybind"/> è un <c>record struct</c> immutabile, ogni modifica ricostruisce il
/// keybind e lo riscrive nel profilo tramite i delegati get/set forniti dal <see cref="MainViewModel"/>.
/// </summary>
public sealed class KeybindEditorViewModel : ObservableObject
{
    private readonly Func<Keybind> _get;
    private readonly Action<Keybind> _set;

    public KeybindEditorViewModel(Func<Keybind> get, Action<Keybind> set)
    {
        _get = get;
        _set = set;
    }

    public ScanCode Key
    {
        get => _get().Key;
        set => Replace(_get() with { Key = value });
    }

    public bool Shift
    {
        get => _get().Shift;
        set => Replace(_get() with { Shift = value });
    }

    public bool Ctrl
    {
        get => _get().Ctrl;
        set => Replace(_get() with { Ctrl = value });
    }

    public bool Alt
    {
        get => _get().Alt;
        set => Replace(_get() with { Alt = value });
    }

    private void Replace(Keybind kb)
    {
        _set(kb);
        OnPropertyChanged(nameof(Key));
        OnPropertyChanged(nameof(Shift));
        OnPropertyChanged(nameof(Ctrl));
        OnPropertyChanged(nameof(Alt));
    }
}
