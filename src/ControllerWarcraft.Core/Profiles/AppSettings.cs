namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Impostazioni globali dell'applicazione, persistite in <c>%APPDATA%/ControllerWarcraft/settings.json</c>.
/// Oltre al profilo attivo (punto di accordo tra App e Gui), la Fase 3 aggiunge le opzioni UX:
/// overlay indicatore di modalità e auto-switch del profilo in base alla finestra in primo piano.
///
/// Tutti i campi nuovi hanno default retro-compatibili: un <c>settings.json</c> della Fase 2
/// (solo <c>activeProfile</c>) continua a funzionare senza modifiche.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Nome (file stem, es. <c>"ascension"</c>) del profilo da caricare all'avvio.</summary>
    public string ActiveProfile { get; set; } = "ascension";

    // ---------------------------------------------------------------- overlay (Fase 3, punto 1)

    /// <summary>Mostra l'overlay trasparente always-on-top con la modalità/layer correnti.</summary>
    public bool ShowOverlay { get; set; } = true;

    // ---------------------------------------------------------------- auto-switch (Fase 3, punto 4)

    /// <summary>
    /// Se true, l'App rileva l'eseguibile in primo piano e carica automaticamente il profilo
    /// associato in <see cref="ProcessProfileMap"/>. Default false (comportamento Fase 2 invariato).
    /// </summary>
    public bool AutoSwitchEnabled { get; set; } = false;

    /// <summary>
    /// Se true (e con <see cref="AutoSwitchEnabled"/>), l'emulazione è messa in pausa quando in
    /// primo piano non c'è un gioco riconosciuto (nessuna voce di <see cref="ProcessProfileMap"/>):
    /// utile per non inviare input mentre si è su desktop/browser.
    /// </summary>
    public bool PauseWhenGameNotForeground { get; set; } = false;

    /// <summary>
    /// Mappa <c>nome processo → file stem del profilo</c> (chiave senza <c>.exe</c>, case-insensitive).
    /// Es. <c>{ "ascension": "ascension", "wow": "retail", "wowclassic": "classic" }</c>.
    /// I default suggeriti coprono i client WoW più comuni; sono usati solo se
    /// <see cref="AutoSwitchEnabled"/> è true.
    /// </summary>
    public Dictionary<string, string> ProcessProfileMap { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ascension"] = "ascension",
        ["wow"] = "retail",
        ["wowclassic"] = "classic",
    };
}
