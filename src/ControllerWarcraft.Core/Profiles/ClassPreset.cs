namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Preset per <b>classe</b> (Fase 4, punto 2): un insieme di <i>override</i> applicabili sopra un
/// profilo di versione (Ascension/Classic/Retail). Non è un profilo completo: è un "layer" che
/// ridefinisce solo alcune voci — tipicamente l'arrangiamento della tabella abilità e/o il radial
/// menu — lasciando invariato tutto il resto (movimento, mouselook, cursore, ...).
///
/// L'applicazione avviene <b>a tempo di editing</b> (dalla GUI): si carica un profilo di versione,
/// si applica un preset di classe, si salva il risultato come normale profilo utente. A runtime
/// l'App vede solo un <see cref="ControllerProfile"/> ordinario — nessuna logica speciale, nessuna
/// automazione. Il vincolo 1:1 resta garantito: gli override sono semplici keybind singoli.
///
/// Poiché nel modello "external-only" emettiamo solo tasti 1-9 (le abilità dietro gli slot si
/// impostano <b>in gioco</b>), un preset di classe è soprattutto una <b>convenzione documentata</b>
/// (quale pulsante → quale slot) più un radial menu tarato sulle utility di quella classe.
/// Le assunzioni sui keybind vanno descritte in <see cref="Description"/>.
/// </summary>
public sealed class ClassPreset
{
    /// <summary>Versione dello schema del preset di classe (per migrazioni future).</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>Nome leggibile del preset (mostrato nella GUI), es. "Warrior (Arms)".</summary>
    public string Name { get; set; } = "";

    /// <summary>Nome della classe di riferimento, es. "Warrior", "Mage".</summary>
    public string ClassName { get; set; } = "";

    /// <summary>
    /// Versione di gioco per cui è pensato (<c>Ascension</c> | <c>Classic</c> | <c>Retail</c>), oppure
    /// stringa vuota = applicabile a qualsiasi versione. Puramente informativo/di filtro nella GUI.
    /// </summary>
    public string GameVersion { get; set; } = "";

    /// <summary>Descrizione e <b>assunzioni sui keybind</b> del preset (quale slot = quale abilità).</summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Override della tabella abilità: ogni voce sostituisce (per <c>button × layer</c>) la voce
    /// corrispondente del profilo base, o la aggiunge se assente. Le voci non elencate restano
    /// quelle del profilo base.
    /// </summary>
    public List<AbilityBinding> AbilityOverrides { get; set; } = new();

    /// <summary>
    /// Radial menu della classe. Se <c>null</c> il radial del profilo base resta invariato; se
    /// valorizzato, sostituisce interamente <see cref="ControllerProfile.RadialMenu"/>.
    /// </summary>
    public RadialMenuSettings? RadialMenu { get; set; }

    /// <summary>
    /// Applica gli override di questo preset <b>direttamente</b> sul profilo <paramref name="target"/>
    /// (lo modifica) e lo restituisce. Pensato per essere usato su una copia appena caricata dal
    /// disco (come fa la GUI), così l'originale su file non viene toccato finché non si salva.
    /// </summary>
    public ControllerProfile ApplyTo(ControllerProfile target)
    {
        // Indicizza le voci esistenti per sostituzione rapida.
        var byKey = new Dictionary<(ActionButton, AbilityLayer), int>();
        for (int i = 0; i < target.Abilities.Count; i++)
        {
            var a = target.Abilities[i];
            byKey[(a.Button, a.Layer)] = i;
        }

        foreach (var ov in AbilityOverrides)
        {
            var clone = new AbilityBinding(ov.Button, ov.Layer, ov.Bind);
            if (byKey.TryGetValue((ov.Button, ov.Layer), out var idx))
                target.Abilities[idx] = clone;     // sostituisce
            else
            {
                byKey[(ov.Button, ov.Layer)] = target.Abilities.Count;
                target.Abilities.Add(clone);        // aggiunge
            }
        }
        target.InvalidateIndex();

        if (RadialMenu is not null)
        {
            // Copia difensiva così il preset in memoria non resta legato al profilo salvato.
            target.RadialMenu = new RadialMenuSettings
            {
                Enabled = RadialMenu.Enabled,
                Trigger = RadialMenu.Trigger,
                SelectDeadzone = RadialMenu.SelectDeadzone,
                Items = RadialMenu.Items.Select(i => new RadialMenuItem(i.Label, i.Bind)).ToList(),
            };
        }

        return target;
    }
}

/// <summary>Metadati di un preset di classe scoperto sul disco (senza caricarne il contenuto pieno).</summary>
public sealed record ClassPresetInfo(string Name, string FileName, string FilePath, string ClassName, string GameVersion);
