using ControllerWarcraft.App.Input;
using ControllerWarcraft.App.Output;
using ControllerWarcraft.Core.Input;
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

    // ---- Hardening input: gate con "hold minimo" per i click-stick e Start ----
    // Debounce le pressioni accidentali/troppo brevi di L3/R3 (e Start quando usato come trigger
    // cursore). Con soglia 0 (default) coincidono col classico fronte di pressione.
    private HoldGate _l3;
    private HoldGate _r3;
    private HoldGate _startGate;

    // Timing per il gate: ms trascorsi tra un tick e l'altro (l'overload senza dt li misura).
    private long _lastTickMs;
    private bool _hasLastTick;

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

    /// <summary>
    /// Elabora un tick misurando da solo il tempo trascorso (per il gate di hold minimo).
    /// Va chiamato SOLO con uno snapshot connesso.
    /// </summary>
    public void Update(in GamepadSnapshot s)
    {
        long now = Environment.TickCount64;
        double dt = _hasLastTick ? (now - _lastTickMs) : 0.0;
        _hasLastTick = true;
        _lastTickMs = now;
        Update(s, dt);
    }

    /// <summary>
    /// Elabora un tick con il delta-tempo esplicito <paramref name="dtMs"/> (ms dal tick precedente),
    /// usato dai gate di hold minimo. Overload deterministico e testabile.
    /// </summary>
    public void Update(in GamepadSnapshot s, double dtMs)
    {
        // Hardening input: aggiorna i gate dei pulsanti "modali" (click-stick + Start) con la soglia
        // di hold minimo del profilo. Va fatto ogni tick perché il tempo di pressione si accumula.
        double minHold = _profile.InputHardening?.ThumbClickMinHoldMs ?? 0;
        _l3.Update(s.LeftThumbClick, dtMs, minHold);
        _r3.Update(s.RightThumbClick, dtMs, minHold);
        _startGate.Update(s.Start, dtMs, minHold);

        // 0) Movimento: stick sinistro -> WASD. Attivo sempre (anche col radial aperto).
        UpdateMovement(s);

        // 1) Radial menu (Fase 4): se configurato, è modale e consuma stick destro + trigger.
        //    Mentre è aperto sospende mouselook/cursore/abilità; al rilascio invia UN SOLO keybind.
        //    L'apertura rispetta l'hold minimo: un tocco troppo breve non fa comparire il menu.
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

        // 2) Attivazione della modalità cursore (configurabile: pulsante + Toggle/Hold/None).
        //    Precedenza: se il pulsante scelto è anche il trigger del radial, vince il radial
        //    (già gestito sopra) e qui l'attivazione viene saltata.
        UpdateCursorActivation();

        // 3) Logica specifica della modalita'.
        if (Mode == ControllerMode.MovementCombat)
            UpdateMovementCombat(s);
        else
            UpdateCursor(s);

        _prev = s;
    }

    // ------------------------------------------------------ attivazione modalità cursore
    // Riferimenti ai gate (già aggiornati questo tick) per pulsante cursore.
    private bool CursorGatePressedEdge() => _profile.Cursor.ActivationButton switch
    {
        CursorActivationButton.RightThumb => _r3.PressedEdge,
        CursorActivationButton.LeftThumb => _l3.PressedEdge,
        CursorActivationButton.Start => _startGate.PressedEdge,
        _ => false,
    };

    private bool CursorGateHeld() => _profile.Cursor.ActivationButton switch
    {
        CursorActivationButton.RightThumb => _r3.Held,
        CursorActivationButton.LeftThumb => _l3.Held,
        CursorActivationButton.Start => _startGate.Held,
        _ => false,
    };

    // True se il pulsante di attivazione cursore coincide col trigger del radial: in tal caso il
    // radial ha la precedenza e l'attivazione cursore su quel pulsante è ignorata.
    private bool CursorButtonTakenByRadial()
    {
        if (!_radial.IsUsable) return false;
        return (_profile.Cursor.ActivationButton, _radial.Trigger) switch
        {
            (CursorActivationButton.RightThumb, RadialTrigger.RightThumb) => true,
            (CursorActivationButton.LeftThumb, RadialTrigger.LeftThumb) => true,
            _ => false,
        };
    }

    private void UpdateCursorActivation()
    {
        var button = _profile.Cursor.ActivationButton;
        if (button == CursorActivationButton.None) return; // modalità cursore disattivata
        if (CursorButtonTakenByRadial()) return;           // precedenza al radial

        if (_profile.Cursor.ActivationMode == CursorActivationMode.Hold)
        {
            // Momentaneo: cursore attivo solo mentre il pulsante (qualificato) è tenuto premuto.
            SetMode(CursorGateHeld() ? ControllerMode.Cursor : ControllerMode.MovementCombat);
        }
        else
        {
            // Toggle (storico): ogni pressione qualificata inverte la modalità.
            if (CursorGatePressedEdge()) ToggleMode();
        }
    }

    // ---------------------------------------------------------------- radial menu
    // True se il radial è attivo e usa quel pulsante come trigger.
    private bool TriggerIs(RadialTrigger t) => _radial.IsUsable && _radial.Trigger == t;

    // Trigger tenuto premuto secondo il gate (rispetta l'hold minimo).
    private bool TriggerHeld() => _radial.Trigger switch
    {
        RadialTrigger.LeftThumb => _l3.Held,
        RadialTrigger.RightThumb => _r3.Held,
        _ => false,
    };

    private void UpdateRadial(in GamepadSnapshot s)
    {
        bool held = TriggerHeld();

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

        // L3 = Tab-target (tap su edge, con hold minimo) — salvo che L3 sia consumato dal radial
        // o usato come pulsante di attivazione cursore (in tal caso L3 non fa più Tab-target).
        bool l3Taken = TriggerIs(RadialTrigger.LeftThumb)
                     || (_profile.Cursor.ActivationButton == CursorActivationButton.LeftThumb);
        if (!l3Taken && _l3.PressedEdge)
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
    private void ToggleMode() => SetMode(
        Mode == ControllerMode.MovementCombat ? ControllerMode.Cursor : ControllerMode.MovementCombat);

    /// <summary>Passa alla modalità <paramref name="target"/> (no-op se già attiva) con pulizia del mouse.</summary>
    private void SetMode(ControllerMode target)
    {
        if (Mode == target) return;

        // Pulisce lo stato del mouse legato alla modalita' uscente (il WASD si auto-corregge ogni tick).
        _out.SetRightMouseHeld(false);
        _out.SetLeftMouseHeld(false);

        Mode = target;
        OnStatus?.Invoke($"Modalita': {ModeLabel(Mode)}");
    }

    /// <summary>Rilascia tutto e azzera l'edge-detection (a disconnessione o uscita).</summary>
    public void Reset()
    {
        _out.ReleaseAll();
        _prev = GamepadSnapshot.Disconnected;
        Layer = AbilityLayer.Base;

        // Azzera i gate e il timing dell'hold minimo: dopo un reset una pressione riparte da zero.
        _l3.Reset();
        _r3.Reset();
        _startGate.Reset();
        _hasLastTick = false;

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
