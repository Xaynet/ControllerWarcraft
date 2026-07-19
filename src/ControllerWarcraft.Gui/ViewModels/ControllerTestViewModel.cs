using System.Windows.Threading;
using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// ViewModel del <b>pannello di test del controller</b> (live). Legge lo stato del gamepad ~60 volte
/// al secondo tramite il <see cref="XInputReader"/> di <b>sola lettura</b> del Core ed espone valori
/// bindabili (posizione stick, grilletti, pulsanti). Serve all'utente per confermare che il
/// controller funziona e per capire la mappatura — è anche uno strumento di troubleshooting.
///
/// VINCOLO ARCHITETTURALE: qui si <b>legge soltanto</b>. Il Core non contiene SendInput, quindi la
/// Gui non può in alcun modo iniettare input reali; l'emulazione resta esclusiva dell'App.
/// </summary>
public sealed class ControllerTestViewModel : ObservableObject, IDisposable
{
    private readonly XInputReader _reader = new();
    private readonly DispatcherTimer _timer;
    private ControllerReading _r = ControllerReading.Disconnected;
    private int _userIndex;

    /// <summary>Lato (px) del riquadro di uno stick nella UI.</summary>
    public const double BoxSize = 130;

    /// <summary>Diametro (px) del pallino che rappresenta la posizione dello stick.</summary>
    public const double DotSize = 18;

    // Proprietà "di visualizzazione" aggiornate ad ogni tick. Le notifichiamo esplicitamente per
    // NON disturbare la selezione dello slot (UserIndex) e le ItemsSource statiche.
    private static readonly string[] DisplayProps =
    {
        nameof(IsConnected), nameof(ConnectionText),
        nameof(LeftStickText), nameof(RightStickText),
        nameof(LeftDotX), nameof(LeftDotY), nameof(RightDotX), nameof(RightDotY),
        nameof(LeftTriggerValue), nameof(RightTriggerValue),
        nameof(LeftTriggerText), nameof(RightTriggerText),
        nameof(A), nameof(B), nameof(X), nameof(Y),
        nameof(LeftShoulder), nameof(RightShoulder),
        nameof(LeftTriggerPressed), nameof(RightTriggerPressed),
        nameof(DPadUp), nameof(DPadDown), nameof(DPadLeft), nameof(DPadRight),
        nameof(LeftThumbClick), nameof(RightThumbClick), nameof(Start), nameof(Back),
    };

    public ControllerTestViewModel()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => Tick();
    }

    /// <summary>Avvia il polling live (chiamato quando il pannello diventa visibile).</summary>
    public void StartPolling()
    {
        Tick();
        _timer.Start();
    }

    /// <summary>Ferma il polling live (chiamato quando il pannello viene nascosto/chiuso).</summary>
    public void StopPolling() => _timer.Stop();

    public void Dispose() => _timer.Stop();

    // -------------------------------------------------------------- slot XInput

    public IReadOnlyList<int> UserIndices { get; } = new[] { 0, 1, 2, 3 };

    /// <summary>Slot XInput da leggere (0-3). Cambiarlo reindirizza semplicemente la lettura.</summary>
    public int UserIndex
    {
        get => _userIndex;
        set { if (SetField(ref _userIndex, value)) Tick(); }
    }

    // -------------------------------------------------------------- stato letto

    private void Tick()
    {
        _r = _reader.Read((uint)_userIndex);
        foreach (var p in DisplayProps) OnPropertyChanged(p);
    }

    public bool IsConnected => _r.Connected;

    public string ConnectionText => _r.Connected
        ? $"Controller connesso (slot {_userIndex})"
        : $"Nessun controller nello slot {_userIndex}. Collega/accoppia un controller Xbox.";

    public string LeftStickText => $"X {_r.LeftX:+0.00;-0.00; 0.00}   Y {_r.LeftY:+0.00;-0.00; 0.00}";
    public string RightStickText => $"X {_r.RightX:+0.00;-0.00; 0.00}   Y {_r.RightY:+0.00;-0.00; 0.00}";

    // Posizione del pallino nel riquadro (px). Y invertita: su stick = su schermo.
    public double LeftDotX => ToDot(_r.LeftX);
    public double LeftDotY => ToDot(-_r.LeftY);
    public double RightDotX => ToDot(_r.RightX);
    public double RightDotY => ToDot(-_r.RightY);

    private static double ToDot(double axis) => (Math.Clamp(axis, -1, 1) * 0.5 + 0.5) * (BoxSize - DotSize);

    // Grilletti (0..1) per le ProgressBar + testo percentuale.
    public double LeftTriggerValue => _r.LeftTrigger;
    public double RightTriggerValue => _r.RightTrigger;
    public string LeftTriggerText => $"{_r.LeftTrigger * 100:0}%";
    public string RightTriggerText => $"{_r.RightTrigger * 100:0}%";

    // Pulsanti (evidenziati quando premuti).
    public bool A => _r.A;
    public bool B => _r.B;
    public bool X => _r.X;
    public bool Y => _r.Y;
    public bool LeftShoulder => _r.LeftShoulder;
    public bool RightShoulder => _r.RightShoulder;
    public bool LeftTriggerPressed => _r.LeftTrigger > 0.12;
    public bool RightTriggerPressed => _r.RightTrigger > 0.12;
    public bool DPadUp => _r.DPadUp;
    public bool DPadDown => _r.DPadDown;
    public bool DPadLeft => _r.DPadLeft;
    public bool DPadRight => _r.DPadRight;
    public bool LeftThumbClick => _r.LeftThumbClick;
    public bool RightThumbClick => _r.RightThumbClick;
    public bool Start => _r.Start;
    public bool Back => _r.Back;
}
