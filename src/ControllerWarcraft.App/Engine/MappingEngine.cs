using ControllerWarcraft.App.Input;
using ControllerWarcraft.App.Output;
using ControllerWarcraft.Core.Profiles;

namespace ControllerWarcraft.App.Engine;

/// <summary>
/// Mapping Engine (ANALISI §5): il cuore del progetto. Ad ogni tick riceve uno
/// <see cref="GamepadSnapshot"/> e, in base a (profilo + modalita' + layer), guida
/// l'<see cref="InputEmulator"/>. Contiene la macchina a stati delle modalita' e la
/// logica dei layer/modificatori. Nessuna automazione: ogni azione di gioco nasce da
/// un input fisico dell'utente in quel tick (mapping rigorosamente 1:1, ANALISI §8).
///
/// Volutamente separato dal main loop e dall'I/O: e' testabile e pronto per le fasi
/// successive (profili JSON, curve di sensibilita', overlay).
/// </summary>
public sealed class MappingEngine
{
    private readonly ControllerProfile _profile;
    private readonly InputEmulator _out;

    // Snapshot precedente, per l'edge-detection (reagire alla pressione, non a ogni tick).
    private GamepadSnapshot _prev = GamepadSnapshot.Disconnected;

    // ---- Parametri letti dal profilo (Fase 2: configurabili via JSON/GUI) ----
    private double LookSensX => _profile.Mouselook.SensitivityX;
    private double LookSensY => _profile.Mouselook.SensitivityY;
    private bool InvertLookY => _profile.Mouselook.InvertY;
    private double CursorSpeed => _profile.Cursor.Speed;
    private bool InvertCursorY => _profile.Cursor.InvertY;
    private double MoveThreshold => _profile.Movement.Threshold;

    public ControllerMode Mode { get; private set; } = ControllerMode.MovementCombat;
    public AbilityLayer Layer { get; private set; } = AbilityLayer.Base;

    // ---- Radial menu (Fase 4, punto 1) ----
    private readonly RadialMenuSettings _radial;
    private readonly string[] _radialLabels; // etichette in cache (il profilo è fisso per engine)
    private bool _radialOpen;
    private int _radialIndex = -1;

    /// <summary>True se il radial menu è attualmente aperto (trigger tenuto premuto).</summary>
    public bool RadialOpen => _radialOpen;

    /// <summary>Indice della voce evidenziata nel radial (o -1 se nessuna / chiuso).</summary>
    public int RadialIndex => _radialIndex;

    /// <summary>Etichette delle voci del radial, nell'ordine (orario dall'alto). Vuoto se non usato.</summary>
    public IReadOnlyList<string> RadialLabels => _radialLabels;

    /// <summary>True se il profilo ha un radial menu configurato e utilizzabile.</summary>
    public bool RadialUsable => _radial.IsUsable;

    /// <summary>Notifica testuale per l'indicatore (console/tray). Es. cambio modalita' o layer.</summary>
    public Action<string>? OnStatus { get; set; }

    /// <summary>Notifica di cambio stato del radial (apertura/evidenziazione/chiusura) per l'overlay.</summary>
    public Action? OnRadialChanged { get; set; }

    public MappingEngine(ControllerProfile profile, InputEmulator emulator)
    {
        _profile = profile;
        _out = emulator;
        _radial = profile.RadialMenu ?? new RadialMenuSettings();
        _radialLabels = _radial.Items.Select(i => string.IsNullOrWhiteSpace(i.Label) ? i.Bind.ToString() : i.Label).ToArray();
    }

    /// <summary>Elabora un tick. Va chiamato SOLO con uno snapshot connesso.</summary>
    public void Update(in GamepadSnapshot s)
    {
        // 0) Movimento: stick sinistro -> WASD. Attivo sempre (anche col radial aperto).
        UpdateMovement(s);

        // 1) Radial menu (Fase 4): se configurato, è modale e consuma stick destro + trigger.
        //    Mentre è aperto sospende mouselook/cursore/abilità; al rilascio invia UN SOLO keybind.
        if (_radial.IsUsable)
        {
            bool wasOpen = _radialOpen;
            UpdateRadial(s);
            if (wasOpen || _radialOpen)
            {
                _out.SetRightMouseHeld(false); // niente camera mentre il menu è (o era) aperto in questo tick
                _prev = s;
                return;
            }
        }

        // 2) Toggle modalita' su R3 (edge) — salvo che R3 sia il trigger del radial.
        if (!TriggerIs(RadialTrigger.RightThumb) && Pressed(s.RightThumbClick, _prev.RightThumbClick))
            ToggleMode();

        // 3) Logica specifica della modalita'.
        if (Mode == ControllerMode.MovementCombat)
            UpdateMovementCombat(s);
        else
            UpdateCursor(s);

        _prev = s;
    }

    // ---------------------------------------------------------------- radial menu
    // True se il radial è attivo e usa quel pulsante come trigger.
    private bool TriggerIs(RadialTrigger t) => _radial.IsUsable && _radial.Trigger == t;

    private bool TriggerHeld(in GamepadSnapshot s) => _radial.Trigger switch
    {
        RadialTrigger.LeftThumb => s.LeftThumbClick,
        RadialTrigger.RightThumb => s.RightThumbClick,
        _ => false,
    };

    private void UpdateRadial(in GamepadSnapshot s)
    {
        bool held = TriggerHeld(s);

        if (held && !_radialOpen)
        {
            // Apertura: il menu compare, nessuna voce ancora selezionata.
            _radialOpen = true;
            _radialIndex = -1;
            OnStatus?.Invoke("Radial: aperto");
            OnRadialChanged?.Invoke();
        }

        if (_radialOpen && held)
        {
            // Evidenziazione del settore in base allo stick destro (logica pura in Core).
            int idx = RadialMenuResolver.Resolve(_radial.Items.Count, s.RightX, s.RightY, _radial.SelectDeadzone);
            if (idx != _radialIndex)
            {
                _radialIndex = idx;
                OnRadialChanged?.Invoke();
            }
        }

        if (_radialOpen && !held)
        {
            // Rilascio: invia il keybind della voce selezionata — UN SOLO tap (1:1, ANALISI §8).
            int chosen = _radialIndex;
            _radialOpen = false;
            _radialIndex = -1;

            if (chosen >= 0 && chosen < _radial.Items.Count)
            {
                var item = _radial.Items[chosen];
                _out.TapKeybind(item.Bind);
                OnStatus?.Invoke($"Radial: {(_radialLabels.Length > chosen ? _radialLabels[chosen] : item.Bind.ToString())}");
            }
            else
            {
                OnStatus?.Invoke("Radial: annullato");
            }
            OnRadialChanged?.Invoke();
        }
    }

    // ---------------------------------------------------------------- movimento
    private void UpdateMovement(in GamepadSnapshot s)
    {
        var m = _profile.Movement;
        double t = MoveThreshold;
        _out.HoldKey(m.Forward, s.LeftY > t);
        _out.HoldKey(m.Back, s.LeftY < -t);
        _out.HoldKey(m.Right, s.LeftX > t);
        _out.HoldKey(m.Left, s.LeftX < -t);
    }

    // -------------------------------------------------- modalita' Movimento/Combattimento
    private void UpdateMovementCombat(in GamepadSnapshot s)
    {
        // Camera: stick destro -> mouselook (tiene premuto RMB + delta mouse).
        bool look = s.RightX != 0 || s.RightY != 0;
        _out.SetRightMouseHeld(look);
        if (look)
        {
            // Fase 3: curva di sensibilità applicata all'ampiezza dello stick (per-asse, con segno),
            // poi moltiplicata per la sensibilità. Con curva Linear equivale al calcolo precedente.
            var curve = _profile.Mouselook.Curve;
            int dx = (int)Math.Round(curve.Apply(s.RightX) * LookSensX);
            int dy = (int)Math.Round(curve.Apply(s.RightY) * LookSensY) * (InvertLookY ? 1 : -1);
            _out.MouseMove(dx, dy);
        }

        // Layer attivo dai modificatori LB/RB (LB ha priorita').
        UpdateLayer(s);

        // A = Salto (tap su edge).
        if (Pressed(s.A, _prev.A)) _out.TapKeybind(_profile.System.Jump);

        // L3 = Tab-target (tap su edge) — salvo che L3 sia il trigger del radial.
        if (!TriggerIs(RadialTrigger.LeftThumb) && Pressed(s.LeftThumbClick, _prev.LeftThumbClick))
            _out.TapKeybind(_profile.System.TabTarget);

        // Abilita' dei pulsanti frontali/D-pad/grilletti, secondo il layer corrente.
        FireAbility(ActionButton.X, s.X, _prev.X);
        FireAbility(ActionButton.B, s.B, _prev.B);
        FireAbility(ActionButton.Y, s.Y, _prev.Y);
        FireAbility(ActionButton.RightTrigger, s.RightTrigger, _prev.RightTrigger);
        FireAbility(ActionButton.LeftTrigger, s.LeftTrigger, _prev.LeftTrigger);
        FireAbility(ActionButton.DPadUp, s.DPadUp, _prev.DPadUp);
        FireAbility(ActionButton.DPadRight, s.DPadRight, _prev.DPadRight);
        FireAbility(ActionButton.DPadDown, s.DPadDown, _prev.DPadDown);
        FireAbility(ActionButton.DPadLeft, s.DPadLeft, _prev.DPadLeft);
    }

    private void UpdateLayer(in GamepadSnapshot s)
    {
        // Priorità: LB+RB (4° layer, Fase 3) > LB > RB > Base.
        var layer = s.LeftShoulder && s.RightShoulder ? AbilityLayer.Shoulder_LBRB
                  : s.LeftShoulder ? AbilityLayer.Shoulder_LB
                  : s.RightShoulder ? AbilityLayer.Shoulder_RB
                  : AbilityLayer.Base;

        if (layer != Layer)
        {
            Layer = layer;
            OnStatus?.Invoke($"Layer: {LayerLabel(layer)}");
        }
    }

    private void FireAbility(ActionButton btn, bool now, bool before)
    {
        if (Pressed(now, before))
            _out.TapKeybind(_profile.Resolve(btn, Layer));
    }

    // -------------------------------------------------------------- modalita' Cursore
    private void UpdateCursor(in GamepadSnapshot s)
    {
        // Stick destro -> cursore virtuale (nessun RMB tenuto: e' la differenza col mouselook).
        if (s.RightX != 0 || s.RightY != 0)
        {
            int dx = (int)Math.Round(s.RightX * CursorSpeed);
            // schermo: Y cresce verso il basso, quindi di norma si inverte (stick su -> cursore su).
            int dy = (int)Math.Round(s.RightY * CursorSpeed) * (InvertCursorY ? 1 : -1);
            _out.MouseMove(dx, dy);
        }

        // A = click sinistro mantenuto (permette drag & drop: talenti, inventario).
        _out.SetLeftMouseHeld(s.A);

        // X = click destro (tap su edge).
        if (Pressed(s.X, _prev.X)) _out.RightClick();

        // B = Escape (chiude finestre) su edge.
        if (Pressed(s.B, _prev.B)) _out.TapKeybind(_profile.System.CursorCancel);
    }

    // ---------------------------------------------------------------- macchina a stati
    private void ToggleMode()
    {
        // Pulisce lo stato del mouse legato alla modalita' uscente (il WASD si auto-corregge ogni tick).
        _out.SetRightMouseHeld(false);
        _out.SetLeftMouseHeld(false);

        Mode = Mode == ControllerMode.MovementCombat
            ? ControllerMode.Cursor
            : ControllerMode.MovementCombat;

        OnStatus?.Invoke($"Modalita': {ModeLabel(Mode)}");
    }

    /// <summary>Rilascia tutto e azzera l'edge-detection (a disconnessione o uscita).</summary>
    public void Reset()
    {
        _out.ReleaseAll();
        _prev = GamepadSnapshot.Disconnected;
        Layer = AbilityLayer.Base;

        if (_radialOpen)
        {
            // Chiude il radial senza selezionare (nessun input inviato): l'uscita/disconnessione
            // non deve mai far partire un'abilità.
            _radialOpen = false;
            _radialIndex = -1;
            OnRadialChanged?.Invoke();
        }
    }

    // ---------------------------------------------------------------- helper
    // Edge "pressione": ora premuto, prima no.
    private static bool Pressed(bool now, bool before) => now && !before;

    public static string ModeLabel(ControllerMode m) => m switch
    {
        ControllerMode.MovementCombat => "MOVIMENTO/COMBATTIMENTO",
        ControllerMode.Cursor => "CURSORE",
        _ => m.ToString(),
    };

    public static string LayerLabel(AbilityLayer l) => l switch
    {
        AbilityLayer.Base => "BASE (1-9)",
        AbilityLayer.Shoulder_LB => "+LB (Shift)",
        AbilityLayer.Shoulder_RB => "+RB (Ctrl)",
        AbilityLayer.Shoulder_LBRB => "+LB+RB (Shift+Ctrl)",
        _ => l.ToString(),
    };
}
