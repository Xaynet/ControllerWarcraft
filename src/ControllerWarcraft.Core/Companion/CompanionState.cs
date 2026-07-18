namespace ControllerWarcraft.Core.Companion;

/// <summary>
/// Stato di gioco esposto dal <b>companion addon opzionale</b> (Fase 4, punto 3). È materiale di
/// <b>sola lettura</b>: l'addon scrive questi campi nei propri SavedVariables (quando la versione di
/// WoW lo supporta) e l'App, <b>se abilitata dall'utente</b>, può leggerli per migliorare il
/// <i>contesto</i> mostrato (es. nome del bersaglio nell'overlay).
///
/// PRINCIPIO NON NEGOZIABILE (ANALISI §8): questo stato non guida MAI l'input. Non entra nella
/// catena di decisione del <c>MappingEngine</c>, non sceglie tasti, non innesca azioni. Serve solo a
/// visualizzare informazioni. L'App funziona al 100% anche senza companion: è puramente additivo.
/// </summary>
public sealed class CompanionState
{
    /// <summary>Esiste un bersaglio selezionato.</summary>
    public bool TargetExists { get; set; }

    /// <summary>Nome del bersaglio (vuoto se nessuno).</summary>
    public string TargetName { get; set; } = "";

    /// <summary>Il bersaglio è ostile/attaccabile.</summary>
    public bool TargetIsEnemy { get; set; }

    /// <summary>Percentuale di vita del bersaglio (0..100).</summary>
    public double TargetHealthPct { get; set; }

    /// <summary>Il giocatore è in combattimento.</summary>
    public bool InCombat { get; set; }

    /// <summary>Percentuale di vita del giocatore (0..100).</summary>
    public double PlayerHealthPct { get; set; }

    /// <summary>Percentuale di risorsa primaria del giocatore (mana/rabbia/energia, 0..100).</summary>
    public double PlayerPowerPct { get; set; }

    /// <summary>Versione di gioco riportata dall'addon (es. "Classic", "Retail", "Ascension").</summary>
    public string GameVersion { get; set; } = "";

    /// <summary>Versione dell'addon companion.</summary>
    public string AddonVersion { get; set; } = "";

    /// <summary>Timestamp Unix dell'ultimo aggiornamento scritto dall'addon (secondi).</summary>
    public double Updated { get; set; }

    /// <summary>Riga breve per overlay/console (vuota se nessun bersaglio).</summary>
    public string ShortLabel =>
        TargetExists && !string.IsNullOrWhiteSpace(TargetName)
            ? $"Target: {TargetName} ({TargetHealthPct:0}%)"
            : "";
}
