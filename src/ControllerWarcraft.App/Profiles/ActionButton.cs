namespace ControllerWarcraft.App.Profiles;

/// <summary>
/// I pulsanti fisici del controller che, combinati con un <see cref="AbilityLayer"/>,
/// producono un keybind di action bar. Non includono i pulsanti "di sistema"
/// (A=salto, LB/RB=modificatori, L3/R3=tab-target/toggle) gestiti a parte dall'engine.
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
/// Fase 1: tre stati (Base, +LB, +RB). LB ha priorita' se entrambi premuti.
/// </summary>
public enum AbilityLayer
{
    Base,
    Shoulder_LB,
    Shoulder_RB,
}
