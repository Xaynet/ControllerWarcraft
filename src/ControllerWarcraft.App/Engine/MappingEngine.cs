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

    /// <summary>Notifica testuale per l'indicatore (console/tray). Es. cambio modalita' o layer.</summary>
    public Action<string>? OnStatus { get; set; }

    public MappingEngine(ControllerProfile profile, InputEmulator emulator)
    {
        _profile = profile;
        _out = emulator;
    }

    /// <summary>Elabora un tick. Va chiamato SOLO con uno snapshot connesso.</summary>
    public void Update(in GamepadSnapshot s)
    {
        // 1) Toggle modalita' su R3 (edge). Reset stato residuo al cambio.
        if (Pressed(s.RightThumbClick, _prev.RightThumbClick))
            ToggleMode();

        // 2) Movimento: stick sinistro -> WASD. Attivo in entrambe le modalita'.
        UpdateMovement(s);

        // 3) Logica specifica della modalita'.
        if (Mode == ControllerMode.MovementCombat)
            UpdateMovementCombat(s);
        else
            UpdateCursor(s);

        _prev = s;
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

        // L3 = Tab-target (tap su edge).
        if (Pressed(s.LeftThumbClick, _prev.LeftThumbClick)) _out.TapKeybind(_profile.System.TabTarget);

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
