using ControllerWarcraft.App.Native;

namespace ControllerWarcraft.App.Input;

/// <summary>
/// Legge lo stato del gamepad via XInput e lo converte in un <see cref="GamepadSnapshot"/>
/// normalizzato. Applica le deadzone radiali per-stick e soglia i grilletti.
/// E' l'unico punto che parla direttamente con XInput (Input Poller di ANALISI §5).
/// </summary>
public sealed class GamepadPoller
{
    private readonly uint _userIndex;
    private readonly short _leftDeadzone;
    private readonly short _rightDeadzone;

    /// <param name="userIndex">Slot XInput (0-3).</param>
    /// <param name="leftDeadzone">Deadzone stick sinistro normalizzata (0..1). Se null usa il default XInput.</param>
    /// <param name="rightDeadzone">Deadzone stick destro normalizzata (0..1). Se null usa il default XInput.</param>
    public GamepadPoller(uint userIndex = 0, double? leftDeadzone = null, double? rightDeadzone = null)
    {
        _userIndex = userIndex;
        _leftDeadzone = ToRaw(leftDeadzone, NativeMethods.LeftThumbDeadzone);
        _rightDeadzone = ToRaw(rightDeadzone, NativeMethods.RightThumbDeadzone);
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
        if (NativeMethods.XInputGetState(_userIndex, out var state) != 0)
            return GamepadSnapshot.Disconnected;

        var pad = state.Gamepad;
        var b = (NativeMethods.GamepadButton)pad.wButtons;

        return new GamepadSnapshot
        {
            Connected = true,

            LeftX = Normalize(pad.sThumbLX, _leftDeadzone),
            LeftY = Normalize(pad.sThumbLY, _leftDeadzone),
            RightX = Normalize(pad.sThumbRX, _rightDeadzone),
            RightY = Normalize(pad.sThumbRY, _rightDeadzone),

            A = b.HasFlag(NativeMethods.GamepadButton.A),
            B = b.HasFlag(NativeMethods.GamepadButton.B),
            X = b.HasFlag(NativeMethods.GamepadButton.X),
            Y = b.HasFlag(NativeMethods.GamepadButton.Y),

            LeftShoulder = b.HasFlag(NativeMethods.GamepadButton.LeftShoulder),
            RightShoulder = b.HasFlag(NativeMethods.GamepadButton.RightShoulder),
            LeftTrigger = pad.bLeftTrigger > NativeMethods.TriggerThreshold,
            RightTrigger = pad.bRightTrigger > NativeMethods.TriggerThreshold,

            DPadUp = b.HasFlag(NativeMethods.GamepadButton.DPadUp),
            DPadDown = b.HasFlag(NativeMethods.GamepadButton.DPadDown),
            DPadLeft = b.HasFlag(NativeMethods.GamepadButton.DPadLeft),
            DPadRight = b.HasFlag(NativeMethods.GamepadButton.DPadRight),

            LeftThumbClick = b.HasFlag(NativeMethods.GamepadButton.LeftThumb),
            RightThumbClick = b.HasFlag(NativeMethods.GamepadButton.RightThumb),
            Start = b.HasFlag(NativeMethods.GamepadButton.Start),
            Back = b.HasFlag(NativeMethods.GamepadButton.Back),
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
