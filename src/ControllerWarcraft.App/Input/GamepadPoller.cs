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

    public GamepadPoller(uint userIndex = 0) => _userIndex = userIndex;

    public GamepadSnapshot Poll()
    {
        if (NativeMethods.XInputGetState(_userIndex, out var state) != 0)
            return GamepadSnapshot.Disconnected;

        var pad = state.Gamepad;
        var b = (NativeMethods.GamepadButton)pad.wButtons;

        return new GamepadSnapshot
        {
            Connected = true,

            LeftX = Normalize(pad.sThumbLX, NativeMethods.LeftThumbDeadzone),
            LeftY = Normalize(pad.sThumbLY, NativeMethods.LeftThumbDeadzone),
            RightX = Normalize(pad.sThumbRX, NativeMethods.RightThumbDeadzone),
            RightY = Normalize(pad.sThumbRY, NativeMethods.RightThumbDeadzone),

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
