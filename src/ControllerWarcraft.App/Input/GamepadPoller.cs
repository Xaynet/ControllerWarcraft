using ControllerWarcraft.Core.Input;

namespace ControllerWarcraft.App.Input;

/// <summary>
/// Legge lo stato del gamepad e lo converte in un <see cref="GamepadSnapshot"/> normalizzato per il
/// gioco. Applica le deadzone radiali per-stick e soglia i grilletti a bool.
/// È l'Input Poller di ANALISI §5. La lettura grezza XInput passa ora per il
/// <see cref="XInputReader"/> condiviso del Core (di sola lettura); il poller resta responsabile
/// solo della normalizzazione specifica per il gioco (deadzone/soglia dal profilo).
/// </summary>
public sealed class GamepadPoller
{
    private readonly XInputReader _reader = new();
    private readonly uint _userIndex;
    private readonly short _leftDeadzone;
    private readonly short _rightDeadzone;

    /// <param name="userIndex">Slot XInput (0-3).</param>
    /// <param name="leftDeadzone">Deadzone stick sinistro normalizzata (0..1). Se null usa il default XInput.</param>
    /// <param name="rightDeadzone">Deadzone stick destro normalizzata (0..1). Se null usa il default XInput.</param>
    public GamepadPoller(uint userIndex = 0, double? leftDeadzone = null, double? rightDeadzone = null)
    {
        _userIndex = userIndex;
        _leftDeadzone = ToRaw(leftDeadzone, XInputReader.LeftThumbDeadzone);
        _rightDeadzone = ToRaw(rightDeadzone, XInputReader.RightThumbDeadzone);
    }

    // Converte una deadzone normalizzata (0..1) nelle unita' grezze XInput (0..32767).
    private static short ToRaw(double? normalized, short fallback)
    {
        if (normalized is not { } n) return fallback;
        n = Math.Clamp(n, 0.0, 0.95);
        return (short)Math.Round(n * 32767);
    }

    public GamepadSnapshot Poll()
    {
        if (!_reader.TryGetState(_userIndex, out var pad))
            return GamepadSnapshot.Disconnected;

        var b = (GamepadButton)pad.Buttons;

        return new GamepadSnapshot
        {
            Connected = true,

            LeftX = Normalize(pad.ThumbLX, _leftDeadzone),
            LeftY = Normalize(pad.ThumbLY, _leftDeadzone),
            RightX = Normalize(pad.ThumbRX, _rightDeadzone),
            RightY = Normalize(pad.ThumbRY, _rightDeadzone),

            A = b.HasFlag(GamepadButton.A),
            B = b.HasFlag(GamepadButton.B),
            X = b.HasFlag(GamepadButton.X),
            Y = b.HasFlag(GamepadButton.Y),

            LeftShoulder = b.HasFlag(GamepadButton.LeftShoulder),
            RightShoulder = b.HasFlag(GamepadButton.RightShoulder),
            LeftTrigger = pad.LeftTrigger > XInputReader.TriggerThreshold,
            RightTrigger = pad.RightTrigger > XInputReader.TriggerThreshold,

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

    /// <summary>Normalizza un asse in [-1..1] applicando una deadzone radiale semplice.</summary>
    private static double Normalize(short value, short deadzone)
    {
        if (value > deadzone) return (value - deadzone) / (double)(32767 - deadzone);
        if (value < -deadzone) return (value + deadzone) / (double)(32768 - deadzone);
        return 0;
    }
}
