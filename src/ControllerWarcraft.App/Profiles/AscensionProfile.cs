using ControllerWarcraft.App.Native;

namespace ControllerWarcraft.App.Profiles;

using SC = NativeMethods.ScanCode;

/// <summary>
/// Profilo Ascension WoW hardcoded (Fase 1). Mappa (pulsante fisico × layer) -> keybind di gioco.
/// In Fase 2 questa classe verra' sostituita da un profilo JSON caricato dal Profile Manager;
/// per ora i binding sono costanti in codice, come previsto dalla roadmap.
///
/// Convenzione action bar assunta lato client WoW (da impostare in gioco, Tasti > Barre azioni):
///   Barra principale : 1 2 3 4 5 6 7 8 9 0
///   Barra "Shift"    : Shift+1 ... Shift+9   (LB)
///   Barra "Ctrl"     : Ctrl+1  ... Ctrl+9    (RB)
///
/// Pulsanti "di sistema" NON in questa tabella (gestiti dal MappingEngine):
///   A = Salto (Space) · LB/RB = modificatori di layer · L3 = Tab-target · R3 = toggle cursore · Back = uscita.
/// </summary>
public sealed class AscensionProfile
{
    public string Name => "Ascension (hardcoded)";

    // (pulsante, layer) -> keybind. Assente = nessun binding (No-op sicuro).
    private readonly Dictionary<(ActionButton, AbilityLayer), Keybind> _map = new();

    public AscensionProfile()
    {
        // --- Layer BASE: slot 1-9 dell'action bar principale ---
        Set(ActionButton.X, AbilityLayer.Base, new Keybind(SC.D1));
        Set(ActionButton.B, AbilityLayer.Base, new Keybind(SC.D2));
        Set(ActionButton.Y, AbilityLayer.Base, new Keybind(SC.D3));
        Set(ActionButton.RightTrigger, AbilityLayer.Base, new Keybind(SC.D4));
        Set(ActionButton.LeftTrigger, AbilityLayer.Base, new Keybind(SC.D5));
        Set(ActionButton.DPadUp, AbilityLayer.Base, new Keybind(SC.D6));
        Set(ActionButton.DPadRight, AbilityLayer.Base, new Keybind(SC.D7));
        Set(ActionButton.DPadDown, AbilityLayer.Base, new Keybind(SC.D8));
        Set(ActionButton.DPadLeft, AbilityLayer.Base, new Keybind(SC.D9));

        // --- Layer +LB: Shift+1..9 ---
        Set(ActionButton.X, AbilityLayer.Shoulder_LB, new Keybind(SC.D1, Shift: true));
        Set(ActionButton.B, AbilityLayer.Shoulder_LB, new Keybind(SC.D2, Shift: true));
        Set(ActionButton.Y, AbilityLayer.Shoulder_LB, new Keybind(SC.D3, Shift: true));
        Set(ActionButton.RightTrigger, AbilityLayer.Shoulder_LB, new Keybind(SC.D4, Shift: true));
        Set(ActionButton.LeftTrigger, AbilityLayer.Shoulder_LB, new Keybind(SC.D5, Shift: true));
        Set(ActionButton.DPadUp, AbilityLayer.Shoulder_LB, new Keybind(SC.D6, Shift: true));
        Set(ActionButton.DPadRight, AbilityLayer.Shoulder_LB, new Keybind(SC.D7, Shift: true));
        Set(ActionButton.DPadDown, AbilityLayer.Shoulder_LB, new Keybind(SC.D8, Shift: true));
        Set(ActionButton.DPadLeft, AbilityLayer.Shoulder_LB, new Keybind(SC.D9, Shift: true));

        // --- Layer +RB: Ctrl+1..9 ---
        Set(ActionButton.X, AbilityLayer.Shoulder_RB, new Keybind(SC.D1, Ctrl: true));
        Set(ActionButton.B, AbilityLayer.Shoulder_RB, new Keybind(SC.D2, Ctrl: true));
        Set(ActionButton.Y, AbilityLayer.Shoulder_RB, new Keybind(SC.D3, Ctrl: true));
        Set(ActionButton.RightTrigger, AbilityLayer.Shoulder_RB, new Keybind(SC.D4, Ctrl: true));
        Set(ActionButton.LeftTrigger, AbilityLayer.Shoulder_RB, new Keybind(SC.D5, Ctrl: true));
        Set(ActionButton.DPadUp, AbilityLayer.Shoulder_RB, new Keybind(SC.D6, Ctrl: true));
        Set(ActionButton.DPadRight, AbilityLayer.Shoulder_RB, new Keybind(SC.D7, Ctrl: true));
        Set(ActionButton.DPadDown, AbilityLayer.Shoulder_RB, new Keybind(SC.D8, Ctrl: true));
        Set(ActionButton.DPadLeft, AbilityLayer.Shoulder_RB, new Keybind(SC.D9, Ctrl: true));
    }

    private void Set(ActionButton btn, AbilityLayer layer, Keybind kb) => _map[(btn, layer)] = kb;

    /// <summary>Keybind per (pulsante, layer). <see cref="Keybind.None"/> se non mappato.</summary>
    public Keybind Resolve(ActionButton btn, AbilityLayer layer)
        => _map.TryGetValue((btn, layer), out var kb) ? kb : Keybind.None;

    // --- Binding "di sistema" del profilo (usati dal MappingEngine) ---
    public Keybind Jump => new(SC.Space);
    public Keybind TabTarget => new(SC.Tab);
    public Keybind CursorCancel => new(SC.Escape);
}
