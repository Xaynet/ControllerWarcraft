using System.Text.Json.Serialization;
using ControllerWarcraft.Core.Input;

namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Un keybind di gioco: un tasto principale piu' eventuali modificatori (Shift/Ctrl/Alt).
/// Rappresenta il binding "dall'altra parte": es. <c>Shift+1</c> per uno slot dell'action bar.
///
/// E' un <c>record struct</c> con costruttore parametrico: System.Text.Json lo (de)serializza
/// mappando le proprieta' JSON ai parametri per nome. Con <c>JsonStringEnumConverter</c> il
/// campo <see cref="Key"/> viene scritto come stringa leggibile (es. <c>"D1"</c>).
/// </summary>
public readonly record struct Keybind(ScanCode Key, bool Shift = false, bool Ctrl = false, bool Alt = false)
{
    [JsonIgnore]
    public bool IsNone => Key == ScanCode.None;

    public static readonly Keybind None = new(ScanCode.None);

    public override string ToString()
    {
        if (IsNone) return "-";
        var mods = string.Concat(Shift ? "Shift+" : "", Ctrl ? "Ctrl+" : "", Alt ? "Alt+" : "");
        return mods + KeyLabel(Key);
    }

    private static string KeyLabel(ScanCode k) => k switch
    {
        ScanCode.D1 => "1", ScanCode.D2 => "2", ScanCode.D3 => "3", ScanCode.D4 => "4", ScanCode.D5 => "5",
        ScanCode.D6 => "6", ScanCode.D7 => "7", ScanCode.D8 => "8", ScanCode.D9 => "9", ScanCode.D0 => "0",
        ScanCode.Minus => "-", ScanCode.Equals => "=", ScanCode.Space => "Space", ScanCode.Escape => "Esc",
        ScanCode.Tab => "Tab",
        _ => k.ToString(),
    };
}
