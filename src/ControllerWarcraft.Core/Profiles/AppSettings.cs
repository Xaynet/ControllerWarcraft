namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Impostazioni globali dell'applicazione, persistite in <c>%APPDATA%/ControllerWarcraft/settings.json</c>.
/// Per ora contiene solo il profilo attivo; e' il punto in cui App e Gui si accordano su
/// quale profilo caricare.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Nome (file stem, es. <c>"ascension"</c>) del profilo da caricare all'avvio.</summary>
    public string ActiveProfile { get; set; } = "ascension";
}
