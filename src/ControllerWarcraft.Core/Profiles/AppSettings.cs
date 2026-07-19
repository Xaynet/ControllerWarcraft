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

    // ---------------------------------------------------------------- primo avvio (onboarding wizard)

    /// <summary>
    /// <c>true</c> quando l'utente ha completato (o saltato) il wizard di primo avvio della Gui.
    /// La Gui mostra il wizard automaticamente finché questo flag è <c>false</c>.
    ///
    /// Retro-compatibile: assente ⇒ <c>false</c> ⇒ il wizard viene mostrato <b>una volta</b>. Un
    /// <c>settings.json</c> esistente (senza questo campo) fa comparire il wizard al primo avvio
    /// dopo l'aggiornamento; una volta chiuso/completato il flag diventa <c>true</c> e non riappare.
    /// Riapribile in qualsiasi momento da un pulsante nella Gui.
    /// </summary>
    public bool SetupCompleted { get; set; } = false;

    // ---------------------------------------------------------------- overlay (Fase 3, punto 1)

    /// <summary>Mostra l'overlay trasparente always-on-top con la modalità/layer correnti.</summary>
    public bool ShowOverlay { get; set; } = true;

    // ---------------------------------------------------------------- button-legend HUD (overlay)

    /// <summary>
    /// Mostra la <b>button-legend a layer</b>: un pannello discreto, semi-trasparente e click-through
    /// che elenca cosa fa ogni pulsante mappabile <i>nel layer corrente</i>, aggiornandosi quando si
    /// tiene premuto LB/RB. Aiuta a ricordare le abilità dei 4 layer (Base/+LB/+RB/+LB+RB). Default
    /// <c>true</c>. Retro-compatibile: assente ⇒ default.
    /// </summary>
    public bool ShowButtonLegend { get; set; } = true;

    /// <summary>
    /// Modalità di visibilità della button-legend. Default
    /// <see cref="LegendVisibilityMode.WhileModifierHeld"/>: la legenda compare solo mentre si tiene
    /// un modificatore LB/RB (elegante, appare quando serve ricordare un layer). L'alternativa
    /// <see cref="LegendVisibilityMode.AlwaysVisible"/> la tiene sempre a schermo.
    /// </summary>
    public LegendVisibilityMode LegendVisibility { get; set; } = LegendVisibilityMode.WhileModifierHeld;

    /// <summary>Angolo dello schermo in cui ancorare la button-legend. Default in basso a destra.</summary>
    public ScreenCorner LegendCorner { get; set; } = ScreenCorner.BottomRight;

    /// <summary>
    /// Mostra un <b>indicatore evidente della modalità cursore</b>: una sottile cornice colorata ai
    /// bordi dello schermo (overlay click-through) + un badge, così è impossibile non accorgersi di
    /// essere in modalità cursore. Default <c>true</c>. Retro-compatibile: assente ⇒ default.
    /// </summary>
    public bool ShowCursorIndicator { get; set; } = true;

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

    // ---------------------------------------------------------------- companion addon (Fase 4, punto 3)

    /// <summary>
    /// Se true, l'App legge (in sola lettura) lo stato di gioco dai SavedVariables del companion addon
    /// e lo mostra come contesto (es. bersaglio nell'overlay). Default <b>false</b>: il companion è
    /// STRETTAMENTE OPZIONALE e l'App funziona identica senza. Lo stato letto non guida MAI l'input
    /// (ANALISI §8).
    /// </summary>
    public bool CompanionEnabled { get; set; } = false;

    /// <summary>
    /// Percorso del file SavedVariables del companion addon
    /// (<c>…/WTF/Account/&lt;ACCOUNT&gt;/SavedVariables/ControllerWarcraftCompanion.lua</c>).
    /// Vuoto = non configurato. Usato solo se <see cref="CompanionEnabled"/> è true.
    /// </summary>
    public string CompanionSavedVariablesPath { get; set; } = "";
}
