using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ControllerWarcraft.Core.Profiles.Presets;

namespace ControllerWarcraft.Core.Profiles;

/// <summary>Origine di un profilo scoperto sul disco.</summary>
public enum ProfileSource
{
    /// <summary>File nella cartella preset accanto all'eseguibile (sola lettura, versionato nel repo).</summary>
    Preset,

    /// <summary>File nella cartella utente (%APPDATA%), modificabile dalla GUI.</summary>
    User,

    /// <summary>Nessun file: profilo built-in in codice (fallback).</summary>
    BuiltIn,
}

/// <summary>Metadati di un profilo scoperto, senza caricarne l'intero contenuto.</summary>
public sealed record ProfileInfo(string Name, string FileName, string FilePath, ProfileSource Source, string GameVersion);

/// <summary>
/// Profile Manager (ANALISI §5): carica e salva i profili JSON e tiene traccia del profilo
/// attivo. Due posizioni:
/// <list type="bullet">
///   <item><b>Preset</b> — <c>&lt;exe&gt;/profiles/*.json</c>: i preset versionati nel repo, sola lettura.</item>
///   <item><b>Utente</b> — <c>%APPDATA%/ControllerWarcraft/profiles/*.json</c>: i profili creati/modificati dall'utente.</item>
/// </list>
/// I profili sono identificati dal <b>file stem</b> (es. <c>ascension</c>). Se un profilo utente
/// e un preset hanno lo stesso stem, vince quello utente (permette di sovrascrivere un preset).
/// Se non si trova alcun file, si ricade sul built-in in codice (<see cref="BuiltInProfiles.Ascension"/>).
/// </summary>
public sealed class ProfileManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // Output leggibile: niente escape \uXXXX per '+', '—', accenti nei file di profilo.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public string AppDataDir { get; }
    public string UserProfilesDir { get; }
    public string PresetProfilesDir { get; }
    public string SettingsPath { get; }

    /// <param name="appDataDir">Override della cartella dati utente (default %APPDATA%/ControllerWarcraft). Utile ai test.</param>
    /// <param name="presetProfilesDir">Override della cartella preset (default &lt;exe&gt;/profiles).</param>
    public ProfileManager(string? appDataDir = null, string? presetProfilesDir = null)
    {
        AppDataDir = appDataDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ControllerWarcraft");
        UserProfilesDir = Path.Combine(AppDataDir, "profiles");
        PresetProfilesDir = presetProfilesDir ?? Path.Combine(AppContext.BaseDirectory, "profiles");
        SettingsPath = Path.Combine(AppDataDir, "settings.json");
    }

    // -------------------------------------------------------------- scoperta profili

    /// <summary>
    /// Elenca i profili disponibili (preset + utente). A parita' di stem, la versione utente
    /// sostituisce il preset. Ordina per nome.
    /// </summary>
    public IReadOnlyList<ProfileInfo> ListProfiles()
    {
        var byStem = new Dictionary<string, ProfileInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var info in Scan(PresetProfilesDir, ProfileSource.Preset))
            byStem[info.FileName] = info;

        // Gli utente vincono sui preset con lo stesso stem.
        foreach (var info in Scan(UserProfilesDir, ProfileSource.User))
            byStem[info.FileName] = info;

        return byStem.Values.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private IEnumerable<ProfileInfo> Scan(string dir, ProfileSource source)
    {
        if (!Directory.Exists(dir)) yield break;

        foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            if (stem.Equals("settings", StringComparison.OrdinalIgnoreCase)) continue;

            string name = stem, gameVersion = "";
            // Legge solo i metadati (best-effort): un file corrotto non blocca la lista.
            try
            {
                var p = ReadFile(path);
                if (p is not null)
                {
                    name = string.IsNullOrWhiteSpace(p.Name) ? stem : p.Name;
                    gameVersion = p.GameVersion;
                }
            }
            catch { /* file illeggibile: si mostra comunque con lo stem */ }

            yield return new ProfileInfo(name, stem, path, source, gameVersion);
        }
    }

    // -------------------------------------------------------------- caricamento

    /// <summary>Carica un profilo per stem (utente prima, poi preset). <c>null</c> se assente.</summary>
    public ControllerProfile? Load(string fileStem)
    {
        var userPath = Path.Combine(UserProfilesDir, fileStem + ".json");
        if (File.Exists(userPath)) return ReadFile(userPath);

        var presetPath = Path.Combine(PresetProfilesDir, fileStem + ".json");
        if (File.Exists(presetPath)) return ReadFile(presetPath);

        return null;
    }

    /// <summary>
    /// Carica il profilo attivo (da <c>settings.json</c>) con fallback robusto:
    /// attivo → preset "ascension" → built-in in codice. Non lancia mai: restituisce sempre
    /// un profilo utilizzabile e comunica la scelta via <paramref name="report"/>.
    /// </summary>
    public ControllerProfile LoadActiveOrDefault(Action<string>? report = null)
    {
        var settings = LoadSettings();

        var active = Load(settings.ActiveProfile);
        if (active is not null)
        {
            report?.Invoke($"Profilo attivo: {active.Name} ({settings.ActiveProfile})");
            return active;
        }

        report?.Invoke($"Profilo '{settings.ActiveProfile}' non trovato; provo il preset 'ascension'.");
        var ascension = Load("ascension");
        if (ascension is not null)
        {
            report?.Invoke($"Uso il preset: {ascension.Name}");
            return ascension;
        }

        report?.Invoke("Nessun file di profilo trovato; uso il fallback built-in (Ascension).");
        return BuiltInProfiles.Ascension();
    }

    private ControllerProfile? ReadFile(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<ControllerProfile>(stream, JsonOptions);
    }

    // -------------------------------------------------------------- salvataggio

    /// <summary>
    /// Salva un profilo nella cartella utente. Il file stem e' derivato da
    /// <paramref name="fileStem"/> se fornito, altrimenti dal <see cref="ControllerProfile.Name"/>.
    /// Restituisce il percorso scritto.
    /// </summary>
    public string Save(ControllerProfile profile, string? fileStem = null)
    {
        Directory.CreateDirectory(UserProfilesDir);
        var stem = Slugify(fileStem ?? profile.Name);
        if (string.IsNullOrEmpty(stem)) stem = "profilo";

        var path = Path.Combine(UserProfilesDir, stem + ".json");
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(path, json);
        return path;
    }

    /// <summary>Serializza un profilo in una stringa JSON (usato dall'export dei preset).</summary>
    public static string Serialize(ControllerProfile profile) => JsonSerializer.Serialize(profile, JsonOptions);

    /// <summary>Scrive un profilo in un percorso arbitrario (usato da <c>--export-presets</c>).</summary>
    public static void WriteTo(ControllerProfile profile, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Serialize(profile));
    }

    // -------------------------------------------------------------- settings (profilo attivo)

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (s is not null) return s;
            }
        }
        catch { /* settings corrotto: si riparte dai default */ }
        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataDir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    /// <summary>Imposta e persiste il profilo attivo (per stem).</summary>
    public void SetActiveProfile(string fileStem)
    {
        var s = LoadSettings();
        s.ActiveProfile = fileStem;
        SaveSettings(s);
    }

    // -------------------------------------------------------------- helper

    /// <summary>Converte un nome leggibile in un file stem sicuro (minuscolo, senza spazi/simboli).</summary>
    public static string Slugify(string name)
    {
        var chars = name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : (c is ' ' or '-' or '_' ? '-' : '\0'))
            .Where(c => c != '\0')
            .ToArray();
        return new string(chars).Trim('-');
    }
}
