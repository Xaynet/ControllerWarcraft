using System.Globalization;
using System.Text.RegularExpressions;

namespace ControllerWarcraft.Core.Companion;

/// <summary>
/// Canale di comunicazione <b>addon → App</b> (Fase 4, punto 3): l'addon companion scrive lo stato
/// di gioco nei propri SavedVariables (un file Lua), e questo lettore lo interpreta lato App.
///
/// Perché SavedVariables e non un socket/named pipe? L'ambiente Lua di WoW è <b>sandboxed</b>: un
/// addon non può aprire socket né scrivere file arbitrari. L'unico canale "ufficiale" e conforme
/// alla ToS è la variabile salvata (scritta dal client). È di sola lettura per noi e non richiede
/// alcuna automazione dentro il gioco.
///
/// LIMITE NOTO (documentato): il client di norma scrive i SavedVariables su disco solo a
/// <b>logout / reload UI</b>, non in tempo reale. Quindi lo stato è uno <i>snapshot</i>, non un feed
/// live. Va benissimo per informazioni di contesto; NON è (e non deve essere) usato per guidare
/// l'input. Alcune build/versioni possono differire — vedi il README dell'addon.
///
/// Il parser è volutamente minimale e tollerante: estrae coppie <c>["chiave"] = valore</c> a livello
/// piatto dalla tabella globale dell'addon. Qualsiasi errore ⇒ <c>null</c> (l'App prosegue senza).
/// </summary>
public static class CompanionStateReader
{
    // Cattura ["chiave"] = valore  (valore = stringa "…", numero, oppure true/false).
    private static readonly Regex Pair = new(
        "\\[\"(?<k>[^\"]+)\"\\]\\s*=\\s*(?<v>\"(?:[^\"\\\\]|\\\\.)*\"|true|false|-?[0-9]+(?:\\.[0-9]+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Prova a leggere e interpretare lo stato dal file SavedVariables in <paramref name="path"/>.
    /// Restituisce <c>false</c> (con <paramref name="state"/> = null) se il file non esiste, è
    /// illeggibile o non contiene dati riconoscibili. Non lancia mai.
    /// </summary>
    public static bool TryRead(string? path, out CompanionState? state)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            if (!File.Exists(path)) return false;
            var text = File.ReadAllText(path);
            return TryParse(text, out state);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Interpreta il contenuto Lua dei SavedVariables (esposto per i test).</summary>
    public static bool TryParse(string luaContent, out CompanionState? state)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(luaContent)) return false;

        var matches = Pair.Matches(luaContent);
        if (matches.Count == 0) return false;

        var s = new CompanionState();
        bool any = false;

        foreach (Match m in matches)
        {
            var key = m.Groups["k"].Value;
            var raw = m.Groups["v"].Value;
            any = true;

            switch (key.ToLowerInvariant())
            {
                case "targetexists": s.TargetExists = ParseBool(raw); break;
                case "targetname": s.TargetName = ParseString(raw); break;
                case "targetisenemy": s.TargetIsEnemy = ParseBool(raw); break;
                case "targethealthpct": s.TargetHealthPct = ParseNum(raw); break;
                case "incombat": s.InCombat = ParseBool(raw); break;
                case "playerhealthpct": s.PlayerHealthPct = ParseNum(raw); break;
                case "playerpowerpct": s.PlayerPowerPct = ParseNum(raw); break;
                case "gameversion": s.GameVersion = ParseString(raw); break;
                case "addonversion": s.AddonVersion = ParseString(raw); break;
                case "updated": s.Updated = ParseNum(raw); break;
                // chiavi sconosciute: ignorate (tolleranza in avanti)
            }
        }

        if (!any) return false;
        state = s;
        return true;
    }

    private static bool ParseBool(string raw) => raw == "true";

    private static double ParseNum(string raw) =>
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;

    private static string ParseString(string raw)
    {
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            raw = raw[1..^1];
        return raw.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
