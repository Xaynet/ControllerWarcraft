using System.Runtime.InteropServices;

namespace ControllerWarcraft.Core.Input;

/// <summary>
/// Stato grezzo del gamepad come restituito da XInput (<c>XINPUT_GAMEPAD</c>), prima di qualunque
/// normalizzazione o deadzone. È il tipo di scambio tra il P/Invoke e i consumatori.
/// </summary>
public struct XInputGamepadRaw
{
    public ushort Buttons;
    public byte LeftTrigger;
    public byte RightTrigger;
    public short ThumbLX;
    public short ThumbLY;
    public short ThumbRX;
    public short ThumbRY;
}

/// <summary>Bit dei pulsanti nel campo <c>wButtons</c> di XInput.</summary>
[Flags]
public enum GamepadButton : ushort
{
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000,
}

/// <summary>
/// Lettore <b>di sola lettura</b> dello stato del gamepad via XInput. Fa P/Invoke di
/// <b>esclusivamente</b> <c>XInputGetState</c>: nessuna funzione di emulazione (SendInput vive
/// solo nell'App). Questo è il vincolo architetturale chiave — vive nel Core così sia l'App
/// (poller di gioco) sia la Gui (pannello di test live) possono leggere il controller
/// condividendo un unico P/Invoke, <b>senza</b> che la Gui acquisisca la capacità di iniettare
/// input: il Core semplicemente non contiene alcun codice di output.
///
/// È la controparte del vecchio blocco XInput di <c>ControllerWarcraft.App.Native.NativeMethods</c>,
/// ora estratto e condiviso (la memoria del progetto notava che lettura XInput e SendInput erano
/// accorpati nell'App: qui la parte di lettura è separata e riusabile).
/// </summary>
public sealed class XInputReader
{
    // Deadzone consigliate da Microsoft (unità grezze 0..32767). Esposte qui perché fanno parte
    // del contratto di lettura ed erano storicamente nell'App.
    public const short LeftThumbDeadzone = 7849;
    public const short RightThumbDeadzone = 8689;

    /// <summary>Soglia grilletto (0..255): oltre questa il grilletto conta come "premuto" (analogico→bool).</summary>
    public const byte TriggerThreshold = 30;

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepadRaw Gamepad;
    }

    // xinput1_4.dll = Windows 8+. Ritorna 0 (ERROR_SUCCESS) se il controller è connesso.
    // NOTA: è l'UNICA funzione nativa del Core, e legge soltanto.
    [DllImport("xinput1_4.dll")]
    private static extern uint XInputGetState(uint dwUserIndex, out XInputState pState);

    /// <summary>
    /// Legge lo stato grezzo dello slot <paramref name="userIndex"/> (0-3). Restituisce
    /// <c>false</c> se il controller non è connesso (in tal caso <paramref name="gamepad"/> è
    /// azzerato). Non lancia eccezioni nel percorso normale.
    /// </summary>
    public bool TryGetState(uint userIndex, out XInputGamepadRaw gamepad)
    {
        if (XInputGetState(userIndex, out var state) != 0)
        {
            gamepad = default;
            return false;
        }
        gamepad = state.Gamepad;
        return true;
    }

    /// <summary>
    /// Legge lo stato dello slot <paramref name="userIndex"/> e lo normalizza in un
    /// <see cref="ControllerReading"/> pronto per la visualizzazione (assi in [-1..1], grilletti in
    /// [0..1], pulsanti bool). Comoda per il pannello di test della Gui.
    /// </summary>
    public ControllerReading Read(uint userIndex = 0)
        => TryGetState(userIndex, out var raw)
            ? ControllerReading.FromRaw(raw)
            : ControllerReading.Disconnected;
}
