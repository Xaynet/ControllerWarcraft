namespace ControllerWarcraft.App.Input;

/// <summary>
/// Stato del gamepad in un singolo istante, gia' normalizzato e pulito dalle deadzone.
/// E' un DTO immutabile: il <see cref="GamepadPoller"/> lo produce, il MappingEngine lo consuma.
/// Gli assi sono in [-1..1]; i pulsanti sono booleani; i grilletti sono gia' soglia-ti a bool.
/// </summary>
public readonly record struct GamepadSnapshot
{
    public bool Connected { get; init; }

    // Assi analogici normalizzati [-1..1] (Y: positivo = su).
    public double LeftX { get; init; }
    public double LeftY { get; init; }
    public double RightX { get; init; }
    public double RightY { get; init; }

    // Pulsanti frontali.
    public bool A { get; init; }
    public bool B { get; init; }
    public bool X { get; init; }
    public bool Y { get; init; }

    // Dorsali e grilletti (grilletti soglia-ti a bool: analogico->digitale).
    public bool LeftShoulder { get; init; }
    public bool RightShoulder { get; init; }
    public bool LeftTrigger { get; init; }
    public bool RightTrigger { get; init; }

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

    /// <summary>Snapshot "vuoto" a controller disconnesso: tutto a zero/false.</summary>
    public static GamepadSnapshot Disconnected => new() { Connected = false };
}
