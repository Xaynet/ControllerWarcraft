using ControllerWarcraft.Core.Input;

namespace ControllerWarcraft.Core.Profiles.Presets;

/// <summary>
/// Preset di profilo definiti in codice. Sono:
///   1) la <b>fonte di verita'</b> dei file JSON versionati in <c>profiles/</c>
///      (generati con <c>cwapp --export-presets</c>, quindi codice e JSON coincidono);
///   2) il <b>fallback</b> a runtime se nessun file di profilo e' disponibile.
///
/// Il preset <see cref="Ascension"/> replica <b>esattamente</b> il comportamento hardcoded
/// della Fase 1 (vedi la vecchia <c>AscensionProfile</c>): garantisce continuita' di gioco.
/// </summary>
public static class BuiltInProfiles
{
    /// <summary>Tutti i preset built-in, nell'ordine di priorita' versioni (ANALISI §Priorita').</summary>
    public static IReadOnlyList<ControllerProfile> All => new[] { Ascension(), Classic(), Retail() };

    /// <summary>
    /// Preset Ascension — replica 1:1 della Fase 1.
    /// Action bar assunta lato client: 1-9 (base), Shift+1..9 (+LB), Ctrl+1..9 (+RB).
    /// </summary>
    public static ControllerProfile Ascension()
    {
        var p = new ControllerProfile
        {
            Name = "Ascension",
            GameVersion = "Ascension",
            Description =
                "Preset Ascension WoW — replica esatta della Fase 1 (MVP). " +
                "Assunzioni action bar in gioco: barra principale su 1-9, barra secondaria su Shift+1..9 (LB), " +
                "terza barra su Ctrl+1..9 (RB). A=Salto, L3=Tab-target, R3=toggle cursore.",
            Movement = new MovementSettings(),
            Mouselook = new MouselookSettings { SensitivityX = 18.0, SensitivityY = 14.0, InvertY = false },
            Cursor = new CursorSettings { Speed = 16.0 },
            System = new SystemBindings
            {
                Jump = new Keybind(ScanCode.Space),
                TabTarget = new Keybind(ScanCode.Tab),
                CursorCancel = new Keybind(ScanCode.Escape),
            },
        };
        AddActionBarLayers(p);
        return p;
    }

    /// <summary>
    /// Preset Classic — stesso schema d'azione di Ascension (le barre in Classic si
    /// configurano allo stesso modo). Differenza documentata: Classic <b>non</b> ha il
    /// soft-target, quindi il Tab-target su L3 e' il metodo primario di selezione.
    /// Camera leggermente piu' lenta di Retail (feeling "vanilla").
    /// </summary>
    public static ControllerProfile Classic()
    {
        var p = new ControllerProfile
        {
            Name = "Classic",
            GameVersion = "Classic",
            Description =
                "Preset Classic WoW. Stesso schema action bar di Ascension (1-9 / Shift+1..9 / Ctrl+1..9). " +
                "Assunzione: nessun soft-target in Classic → il Tab-target (L3) e' la selezione primaria. " +
                "Sensibilita' camera come da default vanilla.",
            Movement = new MovementSettings(),
            Mouselook = new MouselookSettings { SensitivityX = 18.0, SensitivityY = 14.0, InvertY = false },
            Cursor = new CursorSettings { Speed = 16.0 },
            System = new SystemBindings
            {
                Jump = new Keybind(ScanCode.Space),
                TabTarget = new Keybind(ScanCode.Tab),
                CursorCancel = new Keybind(ScanCode.Escape),
            },
        };
        AddActionBarLayers(p);
        return p;
    }

    /// <summary>
    /// Preset Retail — stesso schema action bar, ma con camera di default leggermente
    /// piu' reattiva (la UI Retail e' pensata per un mouselook piu' veloce). Differenza
    /// documentata: in Retail esiste il soft-target, quindi il Tab-target resta utile ma
    /// non e' l'unico mezzo di selezione.
    /// </summary>
    public static ControllerProfile Retail()
    {
        var p = new ControllerProfile
        {
            Name = "Retail",
            GameVersion = "Retail",
            Description =
                "Preset Retail WoW. Stesso schema action bar (1-9 / Shift+1..9 / Ctrl+1..9 / Shift+Ctrl+1..9). " +
                "Camera di default piu' reattiva (SensX=20, SensY=15) con curva Power (controllo fine al centro). " +
                "Nota: Retail ha il soft-target; il Tab-target (L3) resta disponibile come alternativa.",
            Movement = new MovementSettings(),
            Mouselook = new MouselookSettings
            {
                SensitivityX = 20.0,
                SensitivityY = 15.0,
                InvertY = false,
                // Fase 3: curva non lineare di esempio — piu' precisione ai piccoli spostamenti,
                // piena velocita' a fondo corsa. Ascension/Classic restano lineari (compat Fase 1).
                Curve = new ResponseCurve { Type = CurveType.Power, Exponent = 1.5 },
            },
            Cursor = new CursorSettings { Speed = 18.0 },
            System = new SystemBindings
            {
                Jump = new Keybind(ScanCode.Space),
                TabTarget = new Keybind(ScanCode.Tab),
                CursorCancel = new Keybind(ScanCode.Escape),
            },
        };
        AddActionBarLayers(p);
        return p;
    }

    /// <summary>
    /// Popola i quattro layer mappando i pulsanti frontali / D-pad / grilletti sugli slot 1-9:
    ///   Base = 1..9, +LB = Shift+1..9, +RB = Ctrl+1..9, +LB+RB = Shift+Ctrl+1..9 (Fase 3).
    /// Schema condiviso dai preset; i primi tre layer sono identici alla Fase 1.
    /// </summary>
    private static void AddActionBarLayers(ControllerProfile p)
    {
        // Ordine dei pulsanti = ordine degli slot 1..9.
        ReadOnlySpan<ActionButton> buttons =
        [
            ActionButton.X,            // 1
            ActionButton.B,            // 2
            ActionButton.Y,            // 3
            ActionButton.RightTrigger, // 4
            ActionButton.LeftTrigger,  // 5
            ActionButton.DPadUp,       // 6
            ActionButton.DPadRight,    // 7
            ActionButton.DPadDown,     // 8
            ActionButton.DPadLeft,     // 9
        ];
        ReadOnlySpan<ScanCode> slots =
        [
            ScanCode.D1, ScanCode.D2, ScanCode.D3, ScanCode.D4, ScanCode.D5,
            ScanCode.D6, ScanCode.D7, ScanCode.D8, ScanCode.D9,
        ];

        for (int i = 0; i < buttons.Length; i++)
        {
            var slot = slots[i];
            p.Abilities.Add(new AbilityBinding(buttons[i], AbilityLayer.Base, new Keybind(slot)));
            p.Abilities.Add(new AbilityBinding(buttons[i], AbilityLayer.Shoulder_LB, new Keybind(slot, Shift: true)));
            p.Abilities.Add(new AbilityBinding(buttons[i], AbilityLayer.Shoulder_RB, new Keybind(slot, Ctrl: true)));
            p.Abilities.Add(new AbilityBinding(buttons[i], AbilityLayer.Shoulder_LBRB, new Keybind(slot, Shift: true, Ctrl: true)));
        }
    }
}
