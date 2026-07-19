namespace ControllerWarcraft.Core.Input;

/// <summary>
/// Stato del controller normalizzato per la <b>visualizzazione</b> (pannello di test della Gui):
/// assi in [-1..1], grilletti in [0..1], pulsanti booleani. A differenza del
/// <c>GamepadSnapshot</c> di gioco dell'App, qui <b>non</b> si applica alcuna deadzone: il pannello
/// di test mostra il valore reale dello stick così l'utente vede esattamente cosa legge l'app
/// (utile per il troubleshooting: drift, stick che non torna a zero, grilletto sporco, ecc.).
///
/// <see cref="FromRaw"/> è una funzione <b>pura</b> (raw byte/short → valori normalizzati),
/// quindi testabile senza hardware.
/// </summary>
public readonly record struct ControllerReading
{
    public bool Connected { get; init; }

    // Assi analogici normalizzati [-1..1] (Y: positivo = su, come XInput).
    public double LeftX { get; init; }
    public double LeftY { get; init; }
    public double RightX { get; init; }
    public double RightY { get; init; }

    // Grilletti analogici normalizzati [0..1].
    public double LeftTrigger { get; init; }
    public double RightTrigger { get; init; }

    // Pulsanti frontali.
    public bool A { get; init; }
    public bool B { get; init; }
    public bool X { get; init; }
    public bool Y { get; init; }

    // Dorsali.
    public bool LeftShoulder { get; init; }
    public bool RightShoulder { get; init; }

    // D-pad.
    public bool DPadUp { get; init; }
    public bool DPadDown { get; init; }
    public bool DPadLeft { get; init; }
    public bool DPadRight { get; init; }

    // Click stick e pulsanti di sistema.
    public bool LeftThumbClick { get; init; }
    public bool RightThumbClick { get; init; }
    public bool Start { get; init; }
    public bool Back { get; init; }

    /// <summary>Lettura a controller disconnesso: tutto a zero/false.</summary>
    public static ControllerReading Disconnected => new() { Connected = false };

    /// <summary>
    /// Converte lo stato grezzo XInput in valori normalizzati. Puro e deterministico: la logica di
    /// normalizzazione vive nel Core ed è condivisa/testabile. Gli assi si dividono per 32767 e si
    /// clampano a [-1..1] (32768 negativo diventa esattamente -1), i grilletti per 255.
    /// </summary>
    public static ControllerReading FromRaw(in XInputGamepadRaw g)
    {
        var b = (GamepadButton)g.Buttons;
        return new ControllerReading
        {
            Connected = true,

            LeftX = Axis(g.ThumbLX),
            LeftY = Axis(g.ThumbLY),
            RightX = Axis(g.ThumbRX),
            RightY = Axis(g.ThumbRY),

            LeftTrigger = g.LeftTrigger / 255.0,
            RightTrigger = g.RightTrigger / 255.0,

            A = b.HasFlag(GamepadButton.A),
            B = b.HasFlag(GamepadButton.B),
            X = b.HasFlag(GamepadButton.X),
            Y = b.HasFlag(GamepadButton.Y),

            LeftShoulder = b.HasFlag(GamepadButton.LeftShoulder),
            RightShoulder = b.HasFlag(GamepadButton.RightShoulder),

            DPadUp = b.HasFlag(GamepadButton.DPadUp),
            DPadDown = b.HasFlag(GamepadButton.DPadDown),
            DPadLeft = b.HasFlag(GamepadButton.DPadLeft),
            DPadRight = b.HasFlag(GamepadButton.DPadRight),

            LeftThumbClick = b.HasFlag(GamepadButton.LeftThumb),
            RightThumbClick = b.HasFlag(GamepadButton.RightThumb),
            Start = b.HasFlag(GamepadButton.Start),
            Back = b.HasFlag(GamepadButton.Back),
        };
    }

    // XInput usa il range [-32768..32767]. Dividere per 32767 e clampare dà -1 esatto sul fondo scala.
    private static double Axis(short value) => Math.Clamp(value / 32767.0, -1.0, 1.0);
}
