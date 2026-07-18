using ControllerWarcraft.App.Engine;
using ControllerWarcraft.App.Input;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Core.Profiles.Presets;
using ControllerWarcraft.Overlay;

// ============================================================================
//  ControllerWarcraft — App (Fase 3: UX)
//  Main loop volutamente sottile: poll → MappingEngine → indicatore (console + overlay).
//  Novita' Fase 3:
//    • Overlay indicatore di modalità (WPF trasparente, click-through) accanto alla console.
//    • Curve di sensibilità del mouselook (dal profilo).
//    • Quarto layer +LB+RB (Shift+Ctrl) per più slot azione.
//    • Auto-switch del profilo in base alla finestra in primo piano (+ pausa opzionale).
//
//  Mapping (default: preset Ascension) — riepilogo:
//    Stick sx        -> WASD (movimento)
//    Stick dx        -> camera (mouselook, con curva) / cursore (in modalita' Cursore)
//    LB / RB / LB+RB -> modificatori di layer (Base / +LB / +RB / +LB+RB)
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

bool noOverlay = args.Any(a => a.Equals("--no-overlay", StringComparison.OrdinalIgnoreCase));
bool noAutoSwitch = args.Any(a => a.Equals("--no-autoswitch", StringComparison.OrdinalIgnoreCase));

// ---- Selezione profilo: --profile <nome> ha priorita' sul settings.json --------
string? overrideStem = null;
for (int i = 0; i < args.Length - 1; i++)
    if (args[i].Equals("--profile", StringComparison.OrdinalIgnoreCase))
        overrideStem = args[i + 1];

var settings = manager.LoadSettings();

ControllerProfile profile;
string activeStem;
if (overrideStem is not null)
{
    profile = manager.Load(overrideStem)
        ?? throw new InvalidOperationException(
            $"Profilo '{overrideStem}' non trovato in {manager.UserProfilesDir} ne' {manager.PresetProfilesDir}.");
    activeStem = overrideStem;
    Console.WriteLine($"Profilo (override CLI): {profile.Name} ({overrideStem})");
}
else
{
    profile = manager.LoadActiveOrDefault(msg => Console.WriteLine($"  {msg}"));
    activeStem = settings.ActiveProfile;
}

const int TickHz = 125;
const int TickMs = 1000 / TickHz;

// Ogni quanti tick controllare la finestra in primo piano (auto-switch): ~0.5s a 125 Hz.
const int ForegroundCheckEveryTicks = 60;

var host = new EngineHost(manager, profile, activeStem)
{
    OnStatus = msg => Console.WriteLine($"  [{msg}]"),
};

// ---- Overlay indicatore di modalità (opzionale) --------------------------------
ModeOverlayController? overlay = null;
bool overlayEnabled = settings.ShowOverlay && !noOverlay;
if (overlayEnabled)
{
    overlay = new ModeOverlayController();
    overlay.Start();
    if (!overlay.IsRunning)
        Console.WriteLine("  (overlay non disponibile in questo ambiente: uso solo l'indicatore a console)");
}

bool autoSwitch = settings.AutoSwitchEnabled && !noAutoSwitch;

Console.WriteLine("ControllerWarcraft — App (Fase 3: UX)");
Console.WriteLine($"Profilo: {profile.Name}  [versione gioco: {profile.GameVersion}]");
Console.WriteLine("Collega un controller Xbox. Apri WoW (o Blocco note per un test sicuro).");
Console.WriteLine("R3=cambia modalita' · L3=Tab-target · LB/RB/LB+RB=layer abilita' · BACK=esci.");
Console.WriteLine($"Overlay: {(overlay?.IsRunning == true ? "attivo" : "disattivo")} · " +
                  $"Auto-switch: {(autoSwitch ? "attivo" : "disattivo")}");
Console.WriteLine($"Modalita' iniziale: {MappingEngine.ModeLabel(host.Mode)}");
Console.WriteLine("In attesa del controller...");

// Rilascio pulito su Ctrl+C.
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    host.Reset();
    overlay?.Dispose();
    Console.WriteLine("\nUscita (Ctrl+C). Input rilasciati.");
    Environment.Exit(0);
};

bool wasConnected = false;
bool paused = false;
bool pausedAnnounced = false;
int fgCounter = 0;

while (true)
{
    var snapshot = host.Poll();

    if (!snapshot.Connected)
    {
        if (wasConnected)
        {
            host.Reset();
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

    // ---- Auto-switch profilo + pausa (throttled) -------------------------------
    if (autoSwitch && ++fgCounter >= ForegroundCheckEveryTicks)
    {
        fgCounter = 0;
        var proc = ForegroundWatcher.GetForegroundProcessName();

        var target = AutoSwitchResolver.ResolveProfileStem(settings, proc);
        if (target is not null) host.SwapTo(target);

        paused = settings.PauseWhenGameNotForeground && !AutoSwitchResolver.IsGameForeground(settings, proc);
    }

    if (paused)
    {
        if (!pausedAnnounced)
        {
            host.Reset();
            Console.WriteLine("  [PAUSA — gioco non in primo piano; input sospesi]");
            pausedAnnounced = true;
        }
        PushOverlay(overlay, host, paused: true);
        Thread.Sleep(TickMs);
        continue;
    }

    if (pausedAnnounced)
    {
        Console.WriteLine("  [RIPRESA — gioco in primo piano]");
        pausedAnnounced = false;
    }

    host.Update(snapshot);
    PushOverlay(overlay, host, paused: false);

    Thread.Sleep(TickMs);
}

host.Reset();
overlay?.Dispose();
Console.WriteLine("Uscita pulita. A presto.");
return;

// ============================================================================
//  Funzioni locali
// ============================================================================

// Aggiorna l'overlay (se attivo) con lo stato corrente. Il controller deduplica: nessun costo
// se lo stato non è cambiato.
static void PushOverlay(ModeOverlayController? overlay, EngineHost host, bool paused)
{
    if (overlay is null) return;

    var mode = host.Mode == ControllerMode.Cursor ? OverlayMode.Cursor : OverlayMode.MovementCombat;
    overlay.Update(new OverlayState(
        mode,
        MappingEngine.ModeLabel(host.Mode),
        MappingEngine.LayerLabel(host.Layer),
        paused,
        host.ProfileName));
}

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
    Console.WriteLine($"Overlay: {(settings.ShowOverlay ? "on" : "off")} · " +
                      $"Auto-switch: {(settings.AutoSwitchEnabled ? "on" : "off")} " +
                      $"(pausa fuori gioco: {(settings.PauseWhenGameNotForeground ? "on" : "off")})");
    if (settings.AutoSwitchEnabled && settings.ProcessProfileMap.Count > 0)
    {
        Console.WriteLine("Mappa processo → profilo:");
        foreach (var kv in settings.ProcessProfileMap)
            Console.WriteLine($"  {kv.Key} → {kv.Value}");
    }
    Console.WriteLine($"Cartella preset : {manager.PresetProfilesDir}");
    Console.WriteLine($"Cartella utente : {manager.UserProfilesDir}");
    Console.WriteLine("Profili disponibili:");
    foreach (var p in manager.ListProfiles())
        Console.WriteLine($"  - {p.FileName,-16} [{p.Source}] {p.Name}  ({p.GameVersion})");
}

static void PrintHelp()
{
    Console.WriteLine("ControllerWarcraft — App (Fase 3)");
    Console.WriteLine("Uso: cwapp [opzioni]");
    Console.WriteLine("  (nessuna opzione)         Avvia il loop col profilo attivo (settings.json).");
    Console.WriteLine("  --profile <nome>          Usa il profilo <nome> solo per questa esecuzione.");
    Console.WriteLine("  --no-overlay              Disabilita l'overlay indicatore per questa esecuzione.");
    Console.WriteLine("  --no-autoswitch           Disabilita l'auto-switch profilo per questa esecuzione.");
    Console.WriteLine("  --list                    Elenca i profili disponibili ed esce.");
    Console.WriteLine("  --export-presets [dir]    Scrive i preset JSON in <dir> (default: profiles/) ed esce.");
    Console.WriteLine("  --help                    Mostra questo aiuto.");
}
