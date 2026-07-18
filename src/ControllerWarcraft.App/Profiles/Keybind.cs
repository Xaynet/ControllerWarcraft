using ControllerWarcraft.App.Native;

namespace ControllerWarcraft.App.Profiles;

using SC = NativeMethods.ScanCode;

/// <summary>
/// Un keybind di gioco: un tasto principale piu' eventuali modificatori (Shift/Ctrl/Alt).
/// Rappresenta il binding "dall'altra parte": es. <c>Shift+1</c> per uno slot dell'action bar.
/// In Fase 2 questi valori arriveranno da un profilo JSON; qui sono costanti (Fase 1).
/// </summary>
public readonly record struct Keybind(SC Key, bool Shift = false, bool Ctrl = false, bool Alt = false)
{
    public bool IsNone => Key == SC.None;

    public static readonly Keybind None = new(SC.None);

    public override string ToString()
    {
        if (IsNone) return "-";
        var mods = string.Concat(Shift ? "Shift+" : "", Ctrl ? "Ctrl+" : "", Alt ? "Alt+" : "");
        return mods + KeyLabel(Key);
    }

    private static string KeyLabel(SC k) => k switch
    {
        SC.D1 => "1", SC.D2 => "2", SC.D3 => "3", SC.D4 => "4", SC.D5 => "5",
        SC.D6 => "6", SC.D7 => "7", SC.D8 => "8", SC.D9 => "9", SC.D0 => "0",
        SC.Minus => "-", SC.Equals => "=", SC.Space => "Space", SC.Escape => "Esc",
        SC.Tab => "Tab", SC.E => "E",
        _ => k.ToString(),
    };
}
