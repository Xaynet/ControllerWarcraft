using System.Text.Json.Serialization;
using ControllerWarcraft.Core.Input;

namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Pulsante fisico che, <b>tenuto premuto</b>, apre il radial menu (Fase 4, punto 1).
/// Solo i due click-stick sono ammessi come trigger: sono gli unici pulsanti "modali"
/// (mentre li tieni premuti lo stick destro seleziona un settore invece di guidare
/// camera/cursore) senza rubare uno dei tasti azione frontali/D-pad.
///
/// <see cref="None"/> = radial disattivato (default): il comportamento resta identico
/// alle fasi precedenti (L3 = Tab-target, R3 = toggle modalità).
/// </summary>
public enum RadialTrigger
{
    /// <summary>Radial disattivato. Retro-compatibile: L3/R3 mantengono la funzione storica.</summary>
    None,

    /// <summary>Tieni premuto L3 (click stick sinistro) per aprire il radial.</summary>
    LeftThumb,

    /// <summary>Tieni premuto R3 (click stick destro) per aprire il radial.</summary>
    RightThumb,
}

/// <summary>
/// Una voce del radial menu: un'etichetta leggibile + <b>un solo</b> keybind inviato alla
/// selezione. Mapping rigorosamente 1:1 (ANALISI §8): niente sequenze, macro, ripetizioni o
/// timer — esattamente come una qualunque abilità della tabella layer. La selezione di una voce
/// equivale a un singolo tap di quel keybind.
/// </summary>
public sealed class RadialMenuItem : ObservableModel
{
    private string _label = "";
    private Keybind _bind = Keybind.None;

    /// <summary>Testo mostrato nel settore dell'overlay (es. "Mount", "Cucina", "Marca").</summary>
    public string Label { get => _label; set => SetField(ref _label, value ?? ""); }

    /// <summary>L'unico keybind inviato quando questa voce viene selezionata (1:1).</summary>
    public Keybind Bind { get => _bind; set => SetField(ref _bind, value); }

    public RadialMenuItem() { }

    public RadialMenuItem(string label, Keybind bind)
    {
        _label = label;
        _bind = bind;
    }
}

/// <summary>
/// Impostazioni del radial menu (Fase 4, punto 1). Tenendo premuto il <see cref="Trigger"/>
/// compare un menu radiale on-screen (overlay WPF click-through): muovendo lo stick destro verso
/// un settore lo si evidenzia, e al <b>rilascio</b> del trigger si invia il keybind della voce
/// selezionata. Nessuna voce può eseguire più di un keybind: resta tutto 1:1.
///
/// Retro-compatibile: <see cref="Enabled"/> = false e <see cref="Trigger"/> = <see cref="RadialTrigger.None"/>
/// per default. Un profilo v1.1 (senza il campo <c>radialMenu</c>) si comporta esattamente come prima.
/// </summary>
public sealed class RadialMenuSettings : ObservableModel
{
    private bool _enabled;
    private RadialTrigger _trigger = RadialTrigger.None;
    private double _selectDeadzone = 0.4;

    /// <summary>Se true (e con un <see cref="Trigger"/> valido e almeno una voce) il radial è attivo.</summary>
    public bool Enabled { get => _enabled; set => SetField(ref _enabled, value); }

    /// <summary>Pulsante da tenere premuto per aprire il radial (L3/R3). <see cref="RadialTrigger.None"/> = off.</summary>
    public RadialTrigger Trigger { get => _trigger; set => SetField(ref _trigger, value); }

    /// <summary>
    /// Ampiezza minima dello stick destro (0..1) per considerare selezionato un settore: sotto
    /// questa soglia il rilascio non invia nulla (permette di annullare tornando al centro).
    /// </summary>
    public double SelectDeadzone { get => _selectDeadzone; set => SetField(ref _selectDeadzone, value); }

    /// <summary>Le voci del menu, in senso orario a partire dall'alto (12 in punto).</summary>
    public List<RadialMenuItem> Items { get; set; } = new();

    /// <summary>True se il radial è configurato per essere realmente utilizzabile.</summary>
    [JsonIgnore]
    public bool IsUsable => Enabled && Trigger != RadialTrigger.None && Items.Count > 0;
}
