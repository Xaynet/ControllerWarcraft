namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Logica riutilizzabile dell'auto-switch (Fase 3, punto 4): dato il nome dell'eseguibile in
/// primo piano, decide quale profilo caricare in base alla <see cref="AppSettings.ProcessProfileMap"/>.
///
/// È puro (nessuna P/Invoke, nessuno stato): l'App fornisce il nome del processo tramite
/// <c>GetForegroundWindow</c>/<c>GetWindowThreadProcessId</c>; qui vive solo la mappatura, così
/// la regola è testabile e condivisibile (anche la GUI la usa per validare la configurazione).
/// </summary>
public static class AutoSwitchResolver
{
    /// <summary>
    /// Restituisce il file stem del profilo associato al processo in primo piano, oppure
    /// <c>null</c> se non mappato. Confronto case-insensitive e tollerante al suffisso <c>.exe</c>.
    /// </summary>
    public static string? ResolveProfileStem(AppSettings settings, string? foregroundProcess)
    {
        if (string.IsNullOrWhiteSpace(foregroundProcess)) return null;

        var target = Normalize(foregroundProcess);
        foreach (var kv in settings.ProcessProfileMap)
        {
            if (Normalize(kv.Key) == target && !string.IsNullOrWhiteSpace(kv.Value))
                return kv.Value;
        }
        return null;
    }

    /// <summary>True se il processo in primo piano è un gioco riconosciuto (ha una voce nella mappa).</summary>
    public static bool IsGameForeground(AppSettings settings, string? foregroundProcess)
        => ResolveProfileStem(settings, foregroundProcess) is not null;

    /// <summary>Normalizza un nome processo/eseguibile: trim, minuscolo, senza estensione <c>.exe</c>.</summary>
    public static string Normalize(string processOrExe)
    {
        var n = processOrExe.Trim().ToLowerInvariant();
        if (n.EndsWith(".exe", StringComparison.Ordinal)) n = n[..^4];
        return n;
    }
}
