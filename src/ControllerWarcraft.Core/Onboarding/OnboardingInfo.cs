using ControllerWarcraft.Core.Profiles;

namespace ControllerWarcraft.Core.Onboarding;

/// <summary>Una riga della tabella dei keybinding da impostare dentro WoW (barra → tasti).</summary>
public sealed record KeybindingRow(string Bar, string Keys, string Note);

/// <summary>Una versione di gioco proposta nel wizard, con il file stem del preset da attivare.</summary>
public sealed record GameVersionChoice(string DisplayName, string ProfileStem, string Description);

/// <summary>
/// Contenuti <b>puri</b> (dati) del wizard di primo avvio, indipendenti dalla UI: la logica di
/// "serve il setup?", la tabella dei keybinding da impostare in WoW e l'elenco delle versioni
/// suggerite. Vivono nel Core così sono testabili senza WPF e riusabili (Gui + documentazione).
/// </summary>
public static class OnboardingInfo
{
    /// <summary>
    /// Il wizard va mostrato automaticamente finché il setup non è completato. Coincide con
    /// <c>!settings.SetupCompleted</c>: un <c>settings.json</c> mancante produce
    /// <see cref="AppSettings"/> di default con <see cref="AppSettings.SetupCompleted"/> = false,
    /// quindi anche il primo avvio assoluto mostra il wizard.
    /// </summary>
    public static bool NeedsSetup(AppSettings settings) => !settings.SetupCompleted;

    /// <summary>
    /// Keybinding da impostare <b>dentro WoW</b> (Menu → Tasti). L'app invia questi tasti; il gioco
    /// deve avere le abilità legate ad essi. Sono i default dei preset; adattabili in gioco o nel
    /// profilo con la Gui.
    /// </summary>
    public static IReadOnlyList<KeybindingRow> WowKeybindings { get; } = new[]
    {
        new KeybindingRow("Barra principale (slot 1-9)", "1 … 9", "Layer Base (nessun grilletto dorsale)"),
        new KeybindingRow("Seconda barra", "Shift+1 … Shift+9", "Layer +LB"),
        new KeybindingRow("Terza barra", "Ctrl+1 … Ctrl+9", "Layer +RB"),
        new KeybindingRow("Quarta barra", "Shift+Ctrl+1 … Shift+Ctrl+9", "Layer +LB+RB"),
    };

    /// <summary>
    /// Versioni di gioco proposte nel wizard. Gli stem corrispondono ai preset versionati
    /// (<c>ascension</c>/<c>classic</c>/<c>retail</c>). La Gui può comunque scoprire i profili reali
    /// dal <see cref="ProfileManager"/>; questa lista fornisce ordine e descrizioni suggerite.
    /// </summary>
    public static IReadOnlyList<GameVersionChoice> GameVersions { get; } = new[]
    {
        new GameVersionChoice("Ascension", "ascension", "Server privato con client custom (priorità 1)."),
        new GameVersionChoice("Classic", "classic", "WoW Classic / Era."),
        new GameVersionChoice("Retail", "retail", "WoW Retail (Blizzard live)."),
    };
}
