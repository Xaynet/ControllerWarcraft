namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Il pulsante fisico usato come <b>modificatore di layer</b> (il "shift" del pad, ANALISI §4).
/// Storicamente il layer era deciso da LB/RB in modo cablato; ora è configurabile <i>quali</i> due
/// pulsanti fisici fungono da modificatori. Sono ammessi i due dorsali e i due grilletti: alcuni
/// giocatori preferiscono LT/RT (più comodi da tenere) a LB/RB.
///
/// In JSON viene serializzato per nome (es. <c>"LeftShoulder"</c>).
///
/// <b>Conflitto trigger-usato-come-modificatore</b>: se un grilletto (LT/RT) è scelto come
/// modificatore, non può più agire come pulsante di abilità (<see cref="ActionButton.LeftTrigger"/> /
/// <see cref="ActionButton.RightTrigger"/>). La precedenza è: <i>ruolo di modificatore &gt; ruolo di
/// abilità</i>. LB/RB non corrispondono ad alcun <see cref="ActionButton"/>, quindi non generano mai
/// conflitto. Vedi <see cref="LayerModifiers.ConflictingAbility"/>.
/// </summary>
public enum ModifierButton
{
    /// <summary>LB (dorsale sinistro) — default per il modificatore 1.</summary>
    LeftShoulder,

    /// <summary>RB (dorsale destro) — default per il modificatore 2.</summary>
    RightShoulder,

    /// <summary>LT (grilletto sinistro). Se scelto, LT non spara più l'abilità <see cref="ActionButton.LeftTrigger"/>.</summary>
    LeftTrigger,

    /// <summary>RT (grilletto destro). Se scelto, RT non spara più l'abilità <see cref="ActionButton.RightTrigger"/>.</summary>
    RightTrigger,
}

/// <summary>
/// Quali due pulsanti fisici fungono da modificatori di layer.
/// <see cref="Modifier1"/> attiva il layer <see cref="AbilityLayer.Shoulder_LB"/> (keybind Shift) e
/// <see cref="Modifier2"/> il layer <see cref="AbilityLayer.Shoulder_RB"/> (keybind Ctrl); tenuti
/// insieme danno <see cref="AbilityLayer.Shoulder_LBRB"/> (Shift+Ctrl).
///
/// <b>Default: Modifier1 = LeftShoulder (LB), Modifier2 = RightShoulder (RB)</b> → comportamento
/// identico alle fasi precedenti. Un profilo privo di questo campo (JSON v1.0–v1.3) usa i default,
/// quindi resta invariato. I <b>valori dell'enum</b> <see cref="AbilityLayer"/> non cambiano nome:
/// restano identificatori interni per non rompere la serializzazione dei profili esistenti.
/// </summary>
public sealed class ModifierSettings : ObservableModel
{
    private ModifierButton _modifier1 = ModifierButton.LeftShoulder;
    private ModifierButton _modifier2 = ModifierButton.RightShoulder;

    /// <summary>
    /// Modificatore 1 → layer <see cref="AbilityLayer.Shoulder_LB"/> (keybind Shift). Default LB.
    /// </summary>
    public ModifierButton Modifier1 { get => _modifier1; set => SetField(ref _modifier1, value); }

    /// <summary>
    /// Modificatore 2 → layer <see cref="AbilityLayer.Shoulder_RB"/> (keybind Ctrl). Default RB.
    /// </summary>
    public ModifierButton Modifier2 { get => _modifier2; set => SetField(ref _modifier2, value); }
}

/// <summary>
/// Logica <b>pura</b> (testabile, senza dipendenze da App/WPF/XInput) dei modificatori di layer:
/// risoluzione del layer dai due modificatori premuti, etichette leggibili che riflettono i
/// <i>pulsanti configurati</i> (non più "LB"/"RB" fissi) e gestione del conflitto quando un
/// grilletto è usato come modificatore.
///
/// Il <c>MappingEngine</c> (App) si limita a leggere lo stato fisico dei due pulsanti configurati e
/// a delegare qui la decisione, mantenendo l'unica fonte di verità testabile nel Core.
/// </summary>
public static class LayerModifiers
{
    /// <summary>Etichetta breve del pulsante modificatore per le legende/HUD (<c>"LB"</c>/<c>"RB"</c>/<c>"LT"</c>/<c>"RT"</c>).</summary>
    public static string ShortLabel(ModifierButton button) => button switch
    {
        ModifierButton.LeftShoulder => "LB",
        ModifierButton.RightShoulder => "RB",
        ModifierButton.LeftTrigger => "LT",
        ModifierButton.RightTrigger => "RT",
        _ => button.ToString(),
    };

    /// <summary>
    /// Risolve il layer di abilità dallo stato dei due modificatori configurati.
    /// Semantica invariata rispetto al cablato LB/RB: mod1 → <see cref="AbilityLayer.Shoulder_LB"/>,
    /// mod2 → <see cref="AbilityLayer.Shoulder_RB"/>, entrambi → <see cref="AbilityLayer.Shoulder_LBRB"/>.
    /// <b>Priorità: mod1 &gt; mod2 &gt; Base</b> (quando è premuto solo uno dei due).
    /// </summary>
    public static AbilityLayer ResolveLayer(bool modifier1Held, bool modifier2Held)
        => modifier1Held && modifier2Held ? AbilityLayer.Shoulder_LBRB
         : modifier1Held ? AbilityLayer.Shoulder_LB
         : modifier2Held ? AbilityLayer.Shoulder_RB
         : AbilityLayer.Base;

    /// <summary>
    /// Se il modificatore è un grilletto, l'<see cref="ActionButton"/> che viene <b>consumato</b> (non
    /// spara più come abilità); <c>null</c> per LB/RB (che non hanno un corrispettivo ActionButton).
    /// </summary>
    public static ActionButton? ConflictingAbility(ModifierButton button) => button switch
    {
        ModifierButton.LeftTrigger => ActionButton.LeftTrigger,
        ModifierButton.RightTrigger => ActionButton.RightTrigger,
        _ => null,
    };

    /// <summary>
    /// True se il pulsante di abilità <paramref name="ability"/> è disabilitato perché uno dei due
    /// modificatori configurati lo sta usando come "shift". In tal caso l'engine non deve farlo
    /// sparare e la GUI lo segnala all'utente.
    /// </summary>
    public static bool IsAbilityDisabled(ModifierSettings modifiers, ActionButton ability)
    {
        ArgumentNullException.ThrowIfNull(modifiers);
        return ConflictingAbility(modifiers.Modifier1) == ability
            || ConflictingAbility(modifiers.Modifier2) == ability;
    }

    /// <summary>
    /// Insieme dei pulsanti di abilità disabilitati perché usati come modificatori (0, 1 o 2 voci).
    /// Utile all'engine per precomputare una volta lo stato al caricamento del profilo.
    /// </summary>
    public static IReadOnlyList<ActionButton> DisabledAbilities(ModifierSettings modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);
        var list = new List<ActionButton>(2);
        if (ConflictingAbility(modifiers.Modifier1) is { } a1) list.Add(a1);
        if (ConflictingAbility(modifiers.Modifier2) is { } a2 && !list.Contains(a2)) list.Add(a2);
        return list;
    }

    /// <summary>
    /// True se i due modificatori configurati coincidono: configurazione degenere (non si possono più
    /// ottenere i layer intermedi +mod1/+mod2, solo Base o entrambi). La GUI la segnala.
    /// </summary>
    public static bool AreAmbiguous(ModifierSettings modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);
        return modifiers.Modifier1 == modifiers.Modifier2;
    }

    /// <summary>
    /// Etichetta leggibile del layer che <b>riflette i pulsanti configurati</b> (non più "LB"/"RB"
    /// fissi). Es. con Modifier1=LT, Modifier2=RT: <c>"+LT (Shift)"</c>, <c>"+RT (Ctrl)"</c>,
    /// <c>"+LT+RT (Shift+Ctrl)"</c>. Il layer Base resta <c>"BASE (1-9)"</c>.
    /// </summary>
    public static string LayerLabel(AbilityLayer layer, ModifierSettings modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);
        var m1 = ShortLabel(modifiers.Modifier1);
        var m2 = ShortLabel(modifiers.Modifier2);
        return layer switch
        {
            AbilityLayer.Base => "BASE (1-9)",
            AbilityLayer.Shoulder_LB => $"+{m1} (Shift)",
            AbilityLayer.Shoulder_RB => $"+{m2} (Ctrl)",
            AbilityLayer.Shoulder_LBRB => $"+{m1}+{m2} (Shift+Ctrl)",
            _ => layer.ToString(),
        };
    }
}
