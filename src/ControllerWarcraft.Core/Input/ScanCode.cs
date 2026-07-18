namespace ControllerWarcraft.Core.Input;

/// <summary>
/// Codici scancode di tastiera (set 1). I giochi che leggono DirectInput preferiscono
/// gli scancode ai virtual-key: usiamo sempre gli scancode per massima compatibilita'
/// col client WoW.
///
/// Vive nel Core (non piu' annidato in <c>NativeMethods</c>) perche' e' parte dello
/// schema di profilo serializzato: sia l'App (che emette input) sia la Gui (che modifica
/// i profili) devono conoscere questi valori. In JSON viene serializzato per <b>nome</b>
/// (es. <c>"D1"</c>, <c>"Space"</c>), non per valore numerico.
/// </summary>
public enum ScanCode : ushort
{
    None = 0x00,
    Escape = 0x01,
    D1 = 0x02,
    D2 = 0x03,
    D3 = 0x04,
    D4 = 0x05,
    D5 = 0x06,
    D6 = 0x07,
    D7 = 0x08,
    D8 = 0x09,
    D9 = 0x0A,
    D0 = 0x0B,
    Minus = 0x0C,
    Equals = 0x0D,
    Tab = 0x0F,
    Q = 0x10,
    W = 0x11,
    E = 0x12,
    R = 0x13,
    T = 0x14,
    F = 0x21,
    G = 0x22,
    LeftControl = 0x1D,
    A = 0x1E,
    S = 0x1F,
    D = 0x20,
    Z = 0x2C,
    X = 0x2D,
    C = 0x2E,
    V = 0x2F,
    LeftShift = 0x2A,
    LeftAlt = 0x38,
    Space = 0x39,
}
