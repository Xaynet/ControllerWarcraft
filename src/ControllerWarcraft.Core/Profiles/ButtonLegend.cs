namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Modalità di visibilità della button-legend (HUD dei pulsanti). Configurabile in
/// <see cref="AppSettings.LegendVisibility"/>.
/// </summary>
public enum LegendVisibilityMode
{
    /// <summary>La legenda è sempre visibile (anche nel layer Base), finché si è in Movimento/Combattimento.</summary>
    AlwaysVisible,

    /// <summary>
    /// La legenda compare solo <b>mentre si tiene premuto un modificatore</b> LB/RB (cioè quando il
    /// layer attivo non è Base). Default: elegante, appare quando serve ricordare un layer.
    /// </summary>
    WhileModifierHeld,
}

/// <summary>Angolo dello schermo in cui ancorare un pannello dell'overlay (es. la button-legend).</summary>
public enum ScreenCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>
/// Una riga della button-legend: cosa fa un pulsante fisico nel layer corrente.
/// </summary>
/// <param name="Button">Il pulsante fisico mappabile.</param>
/// <param name="ButtonLabel">Etichetta breve e leggibile del pulsante (es. <c>"X"</c>, <c>"RT"</c>, <c>"D-Pad ↑"</c>).</param>
/// <param name="Bind">Il keybind di gioco associato (o <see cref="Keybind.None"/> se non mappato).</param>
/// <param name="AbilityLabel">
/// Etichetta leggibile dell'abilità (predisposta per il futuro: se un giorno il profilo avrà nomi
/// di abilità, qui comparirà es. "Colpo Eroico"). Vuota per ora.
/// </param>
public readonly record struct ButtonLegendRow(
    ActionButton Button,
    string ButtonLabel,
    Keybind Bind,
    string AbilityLabel = "")
{
    /// <summary>True se il pulsante ha un keybind assegnato nel layer corrente.</summary>
    public bool IsMapped => !Bind.IsNone;

    /// <summary>Etichetta del keybind di destinazione (es. <c>"1"</c>, <c>"Shift+1"</c>, <c>"-"</c> se non mappato).</summary>
    public string KeybindLabel => Bind.ToString();

    /// <summary>
    /// Testo da mostrare a destra della riga: preferisce l'etichetta leggibile dell'abilità se
    /// presente, altrimenti il keybind di destinazione. Oggi coincide col keybind (nessuna etichetta).
    /// </summary>
    public string Display => string.IsNullOrWhiteSpace(AbilityLabel) ? KeybindLabel : AbilityLabel;
}

/// <summary>
/// Logica <b>pura</b> (testabile, senza dipendenze WPF) della button-legend a layer: data una
/// coppia <see cref="ControllerProfile"/> + <see cref="AbilityLayer"/>, produce le righe
/// (pulsante → keybind/etichetta) da mostrare nell'overlay. L'overlay resta pura presentazione:
/// riceve queste righe dal loop dell'App.
///
/// La sfida centrale del controller su WoW è ricordare cosa fa ogni pulsante nei 4 layer
/// (Base/+LB/+RB/+LB+RB): questa legenda lo rinforza <i>in gioco</i>, aggiornandosi al cambio layer.
/// </summary>
public static class ButtonLegend
{
    /// <summary>
    /// Ordine di presentazione stabile dei pulsanti mappabili: prima i frontali (Y/X/B), poi i
    /// grilletti (LT/RT), infine il D-pad. Riproduce grosso modo la disposizione fisica sul pad.
    /// </summary>
    public static readonly IReadOnlyList<ActionButton> ButtonOrder = new[]
    {
        ActionButton.Y,
        ActionButton.X,
        ActionButton.B,
        ActionButton.LeftTrigger,
        ActionButton.RightTrigger,
        ActionButton.DPadUp,
        ActionButton.DPadRight,
        ActionButton.DPadDown,
        ActionButton.DPadLeft,
    };

    /// <summary>Etichetta breve e leggibile del pulsante fisico per la legenda.</summary>
    public static string ButtonLabel(ActionButton button) => button switch
    {
        ActionButton.X => "X",
        ActionButton.B => "B",
        ActionButton.Y => "Y",
        ActionButton.RightTrigger => "RT",
        ActionButton.LeftTrigger => "LT",
        ActionButton.DPadUp => "D-Pad ↑",
        ActionButton.DPadRight => "D-Pad →",
        ActionButton.DPadDown => "D-Pad ↓",
        ActionButton.DPadLeft => "D-Pad ←",
        _ => button.ToString(),
    };

    /// <summary>
    /// Costruisce le righe della legenda per il layer corrente, in <see cref="ButtonOrder"/>.
    /// </summary>
    /// <param name="profile">Profilo attivo (fonte dei keybind).</param>
    /// <param name="layer">Layer corrente (Base / +LB / +RB / +LB+RB).</param>
    /// <param name="includeUnmapped">
    /// Se true (default) include anche i pulsanti senza keybind nel layer (mostrati come <c>"-"</c>);
    /// se false li omette, per una legenda più compatta.
    /// </param>
    public static IReadOnlyList<ButtonLegendRow> Build(
        ControllerProfile profile,
        AbilityLayer layer,
        bool includeUnmapped = true)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var rows = new List<ButtonLegendRow>(ButtonOrder.Count);
        foreach (var button in ButtonOrder)
        {
            // Un grilletto usato come modificatore di layer non è più un pulsante di abilità: non
            // spara nulla, quindi non compare nella legenda (riflette i pulsanti configurati).
            if (LayerModifiers.IsAbilityDisabled(profile.Modifiers, button)) continue;

            var bind = profile.Resolve(button, layer);
            if (!includeUnmapped && bind.IsNone) continue;
            rows.Add(new ButtonLegendRow(button, ButtonLabel(button), bind));
        }
        return rows;
    }

    /// <summary>
    /// Decide, in modo <b>puro</b>, se la button-legend debba essere visibile in questo momento.
    /// La legenda è rilevante solo in Movimento/Combattimento (i layer non si applicano al cursore)
    /// e non mentre l'emulazione è in pausa.
    /// </summary>
    /// <param name="enabled">La legenda è abilitata (<see cref="AppSettings.ShowButtonLegend"/>).</param>
    /// <param name="mode">Modalità di visibilità configurata.</param>
    /// <param name="layer">Layer corrente.</param>
    /// <param name="cursorMode">True se si è in modalità cursore.</param>
    /// <param name="paused">True se l'emulazione è in pausa (gioco non in primo piano).</param>
    public static bool ShouldShow(
        bool enabled,
        LegendVisibilityMode mode,
        AbilityLayer layer,
        bool cursorMode,
        bool paused)
    {
        if (!enabled || paused || cursorMode) return false;
        return mode == LegendVisibilityMode.AlwaysVisible || layer != AbilityLayer.Base;
    }
}
