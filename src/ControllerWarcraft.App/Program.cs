using ControllerWarcraft.App.Engine;
using ControllerWarcraft.App.Input;
using ControllerWarcraft.App.Output;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Core.Profiles.Presets;

// ============================================================================
//  ControllerWarcraft — App (Fase 2: profili & config)
//  Main loop volutamente sottile: poll → MappingEngine → indicatore di stato.
//  Novita' Fase 2: il mapping non e' piu' hardcoded ma caricato da un profilo
//  JSON tramite il ProfileManager (Core). Il profilo attivo si sceglie da
//  %APPDATA%/ControllerWarcraft/settings.json (campo "activeProfile"), oppure
//  temporaneamente con --profile <nome> da riga di comando o dalla GUI.
//
//  Mapping (default: preset Ascension) — riepilogo:
//    Stick sx        -> WASD (movimento)
//    Stick dx        -> camera (mouselook) / cursore (in modalita' Cursore)
//    LB / RB         -> modificatori di layer (Base / +LB=Shift / +RB=Ctrl)
//    A               -> Salto (movimento) | Click sinistro (cursore)
//    X               -> abilita' | Click destro (cursore)
//    B / Y / D-pad / grilletti -> abilita' secondo il layer
//    L3              -> Tab-target
//    R3              -> Toggle Modalita' (Movimento <-> Cursore)
//    Back            -> Uscita pulita
//  Mapping rigorosamente 1:1, nessuna automazione (ANALISI §8).
// ============================================================================

var manager = new ProfileManager();

// ---- Sotto-comandi non interattivi (non inviano alcun input) -------------------
if (args.Length > 0)
{
    switch (args[0].ToLowerInvariant())
    {
        case "--export-presets":
            ExportPresets(args.Length > 1 ? args[1] : "profiles");
            return;

        case "--list":
            ListProfiles(manager);
            return;

        case "--help" or "-h" or "/?":
            PrintHelp();
            return;
    }
}

// ---- Selezione profilo: --profile <nome> ha priorita' sul settings.json --------
string? overrideStem = null;
for (int i = 0; i < args.Length - 1; i++)
    if (args[i].Equals("--profile", StringComparison.OrdinalIgnoreCase))
        overrideStem = args[i + 1];

ControllerProfile profile;
if (overrideStem is not null)
{
    profile = manager.Load(overrideStem)
        ?? throw new InvalidOperationException(
            $"Profilo '{overrideStem}' non trovato in {manager.UserProfilesDir} ne' {manager.PresetProfilesDir}.");
    Console.WriteLine($"Profilo (override CLI): {profile.Name} ({overrideStem})");
}
else
{
    profile = manager.LoadActiveOrDefault(msg => Console.WriteLine($"  {msg}"));
}

const int TickHz = 125;
const int TickMs = 1000 / TickHz;

var poller = new GamepadPoller(
    userIndex: 0,
    leftDeadzone: profile.Movement.Deadzone,
    rightDeadzone: profile.Mouselook.Deadzone);
var emulator = new InputEmulator();
var engine = new MappingEngine(profile, emulator)
{
    OnStatus = msg => Console.WriteLine($"  [{msg}]"),
};

Console.WriteLine("ControllerWarcraft — App (Fase 2: profili & config)");
Console.WriteLine($"Profilo: {profile.Name}  [versione gioco: {profile.GameVersion}]");
Console.WriteLine("Collega un controller Xbox. Apri WoW (o Blocco note per un test sicuro).");
Console.WriteLine("R3=cambia modalita' · L3=Tab-target · LB/RB=layer abilita' · BACK=esci.");
Console.WriteLine($"Modalita' iniziale: {MappingEngine.ModeLabel(engine.Mode)}");
Console.WriteLine("In attesa del controller...");

// Rilascio pulito su Ctrl+C.
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    engine.Reset();
    Console.WriteLine("\nUscita (Ctrl+C). Input rilasciati.");
    Environment.Exit(0);
};

bool wasConnected = false;

while (true)
{
    var snapshot = poller.Poll();

    if (!snapshot.Connected)
    {
        if (wasConnected)
        {
            engine.Reset();
            Console.WriteLine("Controller disconnesso. In attesa...");
            wasConnected = false;
        }
        Thread.Sleep(200);
        continue;
    }

    if (!wasConnected)
    {
        Console.WriteLine("Controller connesso. Vai.");
        wasConnected = true;
    }

    // Uscita esplicita su Back.
    if (snapshot.Back) break;

    engine.Update(snapshot);

    Thread.Sleep(TickMs);
}

engine.Reset();
Console.WriteLine("Uscita pulita. A presto.");
return;

// ============================================================================
//  Sotto-comandi (definiti come funzioni locali)
// ============================================================================

// Serializza i preset built-in nei file JSON versionati. Non invia alcun input:
// e' un'operazione di solo I/O su file, sicura da eseguire.
static void ExportPresets(string dir)
{
    Directory.CreateDirectory(dir);
    foreach (var preset in BuiltInProfiles.All)
    {
        var stem = ProfileManager.Slugify(preset.Name);
        var path = Path.Combine(dir, stem + ".json");
        ProfileManager.WriteTo(preset, path);
        Console.WriteLine($"Scritto {path}");
    }
    Console.WriteLine($"Esportati {BuiltInProfiles.All.Count} preset in '{Path.GetFullPath(dir)}'.");
}

static void ListProfiles(ProfileManager manager)
{
    var settings = manager.LoadSettings();
    Console.WriteLine($"Profilo attivo (settings.json): {settings.ActiveProfile}");
    Console.WriteLine($"Cartella preset : {manager.PresetProfilesDir}");
    Console.WriteLine($"Cartella utente : {manager.UserProfilesDir}");
    Console.WriteLine("Profili disponibili:");
    foreach (var p in manager.ListProfiles())
        Console.WriteLine($"  - {p.FileName,-16} [{p.Source}] {p.Name}  ({p.GameVersion})");
}

static void PrintHelp()
{
    Console.WriteLine("ControllerWarcraft — App (Fase 2)");
    Console.WriteLine("Uso: cwapp [opzioni]");
    Console.WriteLine("  (nessuna opzione)         Avvia il loop col profilo attivo (settings.json).");
    Console.WriteLine("  --profile <nome>          Usa il profilo <nome> solo per questa esecuzione.");
    Console.WriteLine("  --list                    Elenca i profili disponibili ed esce.");
    Console.WriteLine("  --export-presets [dir]    Scrive i preset JSON in <dir> (default: profiles/) ed esce.");
    Console.WriteLine("  --help                    Mostra questo aiuto.");
}
