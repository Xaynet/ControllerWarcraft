using ControllerWarcraft.App.Input;
using ControllerWarcraft.App.Output;
using ControllerWarcraft.Core.Profiles;

namespace ControllerWarcraft.App.Engine;

/// <summary>
/// Contenitore mutabile di runtime (Fase 3): possiede poller + engine + profilo attivo e permette
/// di <b>sostituire il profilo a caldo</b> (auto-switch, punto 4) senza dover ricostruire il main
/// loop. Il <see cref="InputEmulator"/> è riusato tra i profili così un <c>Reset()</c> rilascia
/// sempre in modo pulito ogni tasto prima del cambio.
///
/// Non contiene logica di gioco: delega tutto al <see cref="MappingEngine"/>. Serve solo a tenere
/// il <c>Program.cs</c> leggibile pur avendo poller/engine variabili.
/// </summary>
public sealed class EngineHost
{
    private readonly ProfileManager _manager;
    private readonly InputEmulator _emulator = new();

    private GamepadPoller _poller;

    public MappingEngine Engine { get; private set; }
    public ControllerProfile Profile { get; private set; }
    public string CurrentStem { get; private set; }

    /// <summary>Callback di stato (console) propagata a ogni engine ricostruito.</summary>
    public Action<string>? OnStatus { get; set; }

    /// <summary>Callback di cambio stato del radial menu, propagata a ogni engine ricostruito.</summary>
    public Action? OnRadialChanged { get; set; }

    public EngineHost(ProfileManager manager, ControllerProfile profile, string stem)
    {
        _manager = manager;
        Profile = profile;
        CurrentStem = stem;
        _poller = BuildPoller(profile);
        Engine = BuildEngine(profile);
    }

    public ControllerMode Mode => Engine.Mode;
    public AbilityLayer Layer => Engine.Layer;
    public string ProfileName => Profile.Name;

    // ---- Radial menu (Fase 4): pass-through verso l'engine corrente ----
    public bool RadialUsable => Engine.RadialUsable;
    public bool RadialOpen => Engine.RadialOpen;
    public int RadialIndex => Engine.RadialIndex;
    public IReadOnlyList<string> RadialLabels => Engine.RadialLabels;

    public GamepadSnapshot Poll() => _poller.Poll();
    public void Update(in GamepadSnapshot s) => Engine.Update(s);
    public void Reset() => Engine.Reset();

    /// <summary>
    /// Carica il profilo <paramref name="stem"/> e ricostruisce poller+engine. Prima rilascia tutti
    /// gli input dell'engine corrente (Reset). Restituisce false se il profilo non esiste (in tal
    /// caso lo stato resta invariato). No-op se <paramref name="stem"/> è già quello attivo.
    /// </summary>
    public bool SwapTo(string stem)
    {
        if (string.Equals(stem, CurrentStem, StringComparison.OrdinalIgnoreCase))
            return true;

        var loaded = _manager.Load(stem);
        if (loaded is null) return false;

        Engine.Reset(); // rilascia ogni tasto/pulsante col profilo uscente

        Profile = loaded;
        CurrentStem = stem;
        _poller = BuildPoller(loaded);
        Engine = BuildEngine(loaded);

        OnStatus?.Invoke($"Profilo cambiato (auto-switch): {loaded.Name} ({stem})");
        return true;
    }

    private GamepadPoller BuildPoller(ControllerProfile p) => new(
        userIndex: 0,
        leftDeadzone: p.Movement.Deadzone,
        rightDeadzone: p.Mouselook.Deadzone);

    private MappingEngine BuildEngine(ControllerProfile p) => new(p, _emulator)
    {
        OnStatus = OnStatus,
        OnRadialChanged = OnRadialChanged,
    };
}
