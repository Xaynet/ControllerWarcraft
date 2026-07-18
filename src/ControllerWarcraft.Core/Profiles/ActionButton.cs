namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// I pulsanti fisici del controller che, combinati con un <see cref="AbilityLayer"/>,
/// producono un keybind di action bar. Non includono i pulsanti "di sistema"
/// (A=salto, LB/RB=modificatori, L3/R3=tab-target/toggle) gestiti a parte dall'engine.
/// In JSON viene serializzato per nome (es. <c>"DPadUp"</c>).
/// </summary>
public enum ActionButton
{
    X,
    B,
    Y,
    RightTrigger,
    LeftTrigger,
    DPadUp,
    DPadRight,
    DPadDown,
    DPadLeft,
}

/// <summary>
/// Il layer di abilita' attivo, deciso dai modificatori LB/RB (ANALISI §4).
/// Fase 1/2: tre stati (Base, +LB, +RB). Fase 3: aggiunto il quarto stato
/// <see cref="Shoulder_LBRB"/> (LB+RB premuti insieme) per gli slot azione aggiuntivi.
/// Priorita' di selezione: LB+RB &gt; LB &gt; RB &gt; Base.
/// In JSON viene serializzato per nome; i profili senza voci per <see cref="Shoulder_LBRB"/>
/// restano validi (quel layer risolve a No-op).
/// </summary>
public enum AbilityLayer
{
    Base,
    Shoulder_LB,
    Shoulder_RB,

    /// <summary>Quarto layer (Fase 3): LB e RB tenuti insieme. Preset: Shift+Ctrl+1..9.</summary>
    Shoulder_LBRB,
}
