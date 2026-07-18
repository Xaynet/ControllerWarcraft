namespace ControllerWarcraft.App.Engine;

/// <summary>
/// Le modalita' della macchina a stati (ANALISI §4). Fase 1 ne implementa due.
/// </summary>
public enum ControllerMode
{
    /// <summary>Default: stick sx = movimento, stick dx = camera (mouselook), pulsanti = abilita'.</summary>
    MovementCombat,

    /// <summary>Cursore: stick dx = cursore mouse virtuale, A = click sx, X = click dx. Per loot/vendor/talenti.</summary>
    Cursor,
}
