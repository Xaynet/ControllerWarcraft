using System.Text.Json.Serialization;
using ControllerWarcraft.Core.Input;

namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Schema di profilo serializzabile (System.Text.Json) — Fase 2 "Profili &amp; config".
/// Rappresenta il mapping completo del controller in modo indipendente dalla versione di
/// gioco (ANALISI §5 "Profile Manager"): movimento, mouselook, cursore, binding di sistema
/// e la tabella (pulsante × layer) → keybind. Le differenze tra Ascension/Classic/Retail
/// vivono <b>solo</b> nei dati di questo file, non nel codice.
///
/// Mapping rigorosamente 1:1: lo schema non prevede alcuna forma di automazione, macro o
/// timer (ANALISI §8). Un keybind = un tasto (piu' modificatori) inviato al gioco.
/// </summary>
public sealed class ControllerProfile
{
    /// <summary>Versione dello schema del file (SemVer-ish). Serve alla migrazione futura.
    /// v1.1 (Fase 3): aggiunta <see cref="MouselookSettings.Curve"/> e il layer <c>Shoulder_LBRB</c>.
    /// v1.2 (Fase 4): aggiunto <see cref="RadialMenu"/> (radial menu overlay).
    /// I file v1.0/v1.1 restano leggibili: i campi nuovi hanno default retro-compatibili.</summary>
    public string SchemaVersion { get; set; } = "1.2";

    /// <summary>Nome leggibile del profilo (mostrato nella GUI e nella selezione).</summary>
    public string Name { get; set; } = "";

    /// <summary>Versione di gioco di riferimento: <c>Ascension</c> | <c>Classic</c> | <c>Retail</c> (informativo).</summary>
    public string GameVersion { get; set; } = "";

    /// <summary>Descrizione / note sulle assunzioni del profilo.</summary>
    public string Description { get; set; } = "";

    /// <summary>Movimento: stick sinistro → WASD (con deadzone e soglia digitale).</summary>
    public MovementSettings Movement { get; set; } = new();

    /// <summary>Mouselook: stick destro → RMB tenuto + delta mouse (sensibilita', inversione, deadzone).</summary>
    public MouselookSettings Mouselook { get; set; } = new();

    /// <summary>Modalita' cursore: stick destro → cursore virtuale.</summary>
    public CursorSettings Cursor { get; set; } = new();

    /// <summary>Binding "di sistema" gestiti direttamente dall'engine (salto, tab-target, annulla cursore).</summary>
    public SystemBindings System { get; set; } = new();

    /// <summary>Tabella delle abilita': ogni voce lega (pulsante fisico × layer) a un keybind di gioco.</summary>
    public List<AbilityBinding> Abilities { get; set; } = new();

    /// <summary>
    /// Radial menu overlay (Fase 4, punto 1): tieni premuto un trigger, muovi lo stick destro verso
    /// un settore, rilascia per inviare il keybind (uno solo, 1:1) di quella voce. Disattivo per
    /// default: un profilo senza questo campo si comporta esattamente come nelle fasi precedenti.
    /// </summary>
    public RadialMenuSettings RadialMenu { get; set; } = new();

    // Indice di lookup costruito a partire dalla lista (non serializzato). Popolato pigramente.
    [JsonIgnore]
    private Dictionary<(ActionButton, AbilityLayer), Keybind>? _index;

    /// <summary>Keybind per (pulsante, layer). <see cref="Keybind.None"/> se non mappato (No-op sicuro).</summary>
    public Keybind Resolve(ActionButton button, AbilityLayer layer)
    {
        _index ??= BuildIndex();
        return _index.TryGetValue((button, layer), out var kb) ? kb : Keybind.None;
    }

    /// <summary>Invalida l'indice cache dopo modifiche a <see cref="Abilities"/> (usato dalla GUI).</summary>
    public void InvalidateIndex() => _index = null;

    private Dictionary<(ActionButton, AbilityLayer), Keybind> BuildIndex()
    {
        var map = new Dictionary<(ActionButton, AbilityLayer), Keybind>();
        foreach (var a in Abilities)
            map[(a.Button, a.Layer)] = a.Bind; // in caso di duplicati vince l'ultimo
        return map;
    }
}

/// <summary>Impostazioni del movimento (stick sinistro → WASD digitale).</summary>
public sealed class MovementSettings : ObservableModel
{
    private ScanCode _forward = ScanCode.W;
    private ScanCode _back = ScanCode.S;
    private ScanCode _left = ScanCode.A;
    private ScanCode _right = ScanCode.D;
    private double _threshold = 0.5;
    private double _deadzone = 0.2395;

    public ScanCode Forward { get => _forward; set => SetField(ref _forward, value); }
    public ScanCode Back { get => _back; set => SetField(ref _back, value); }
    public ScanCode Left { get => _left; set => SetField(ref _left, value); }
    public ScanCode Right { get => _right; set => SetField(ref _right, value); }

    /// <summary>Ampiezza dell'asse (0..1) oltre la quale il tasto WASD si considera premuto.</summary>
    public double Threshold { get => _threshold; set => SetField(ref _threshold, value); }

    /// <summary>Deadzone radiale dello stick sinistro, normalizzata (0..1). Default XInput ≈ 0.24.</summary>
    public double Deadzone { get => _deadzone; set => SetField(ref _deadzone, value); }
}

/// <summary>Impostazioni del mouselook (stick destro in modalita' Movimento/Combattimento).</summary>
public sealed class MouselookSettings : ObservableModel
{
    private double _sensitivityX = 18.0;
    private double _sensitivityY = 14.0;
    private bool _invertY = false;
    private double _deadzone = 0.2652;
    private ResponseCurve _curve = new();

    /// <summary>Pixel di movimento mouse orizzontale per tick a stick pieno.</summary>
    public double SensitivityX { get => _sensitivityX; set => SetField(ref _sensitivityX, value); }

    /// <summary>Pixel di movimento mouse verticale per tick a stick pieno.</summary>
    public double SensitivityY { get => _sensitivityY; set => SetField(ref _sensitivityY, value); }

    /// <summary>Inverte l'asse verticale della camera.</summary>
    public bool InvertY { get => _invertY; set => SetField(ref _invertY, value); }

    /// <summary>Deadzone radiale dello stick destro, normalizzata (0..1). Default XInput ≈ 0.265.</summary>
    public double Deadzone { get => _deadzone; set => SetField(ref _deadzone, value); }

    /// <summary>
    /// Curva di risposta (Fase 3) applicata all'ampiezza dello stick destro prima della sensibilità.
    /// Default <see cref="CurveType.Linear"/> = comportamento storico; un profilo senza questo campo
    /// si comporta esattamente come prima.
    /// </summary>
    public ResponseCurve Curve { get => _curve; set => SetField(ref _curve, value ?? new ResponseCurve()); }
}

/// <summary>Impostazioni della modalita' cursore (stick destro → cursore virtuale).</summary>
public sealed class CursorSettings : ObservableModel
{
    private double _speed = 16.0;
    private bool _invertY = false;

    /// <summary>Pixel di spostamento cursore per tick a stick pieno.</summary>
    public double Speed { get => _speed; set => SetField(ref _speed, value); }

    /// <summary>Inverte l'asse verticale del cursore (raro; di norma false).</summary>
    public bool InvertY { get => _invertY; set => SetField(ref _invertY, value); }
}

/// <summary>Binding "di sistema" usati direttamente dal MappingEngine, fuori dalla tabella layer.</summary>
public sealed class SystemBindings
{
    public Keybind Jump { get; set; } = new(ScanCode.Space);
    public Keybind TabTarget { get; set; } = new(ScanCode.Tab);

    /// <summary>Tasto inviato dal pulsante "annulla" in modalita' cursore (di norma Escape).</summary>
    public Keybind CursorCancel { get; set; } = new(ScanCode.Escape);
}

/// <summary>Una voce della tabella abilita': (pulsante × layer) → keybind di gioco.</summary>
public sealed class AbilityBinding
{
    public ActionButton Button { get; set; }
    public AbilityLayer Layer { get; set; }
    public Keybind Bind { get; set; }

    public AbilityBinding() { }

    public AbilityBinding(ActionButton button, AbilityLayer layer, Keybind bind)
    {
        Button = button;
        Layer = layer;
        Bind = bind;
    }
}
