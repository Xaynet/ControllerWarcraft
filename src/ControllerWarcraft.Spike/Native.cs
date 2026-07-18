using System.Runtime.InteropServices;

namespace ControllerWarcraft.Spike;

/// <summary>
/// P/Invoke minimale per XInput (lettura gamepad) e SendInput (emulazione KB/mouse).
/// Nessuna dipendenza esterna: lo spike deve compilare stand-alone.
/// </summary>
internal static class Native
{
    // ---------- XInput ----------

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    // xinput1_4.dll = Windows 8+. Ritorna 0 (ERROR_SUCCESS) se il controller e' connesso.
    [DllImport("xinput1_4.dll")]
    public static extern uint XInputGetState(uint dwUserIndex, out XInputState pState);

    [Flags]
    public enum GamepadButton : ushort
    {
        DPadUp = 0x0001,
        DPadDown = 0x0002,
        DPadLeft = 0x0004,
        DPadRight = 0x0008,
        Start = 0x0010,
        Back = 0x0020,
        LeftThumb = 0x0040,
        RightThumb = 0x0080,
        LeftShoulder = 0x0100,
        RightShoulder = 0x0200,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000,
    }

    // Deadzone consigliate da Microsoft.
    public const short LeftThumbDeadzone = 7849;
    public const short RightThumbDeadzone = 8689;

    // ---------- SendInput ----------

    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput mi;
        [FieldOffset(0)] public KeyboardInput ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public InputUnion u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    /// <summary>Codici scancode (set 1) — i giochi che leggono DirectInput preferiscono gli scancode ai virtual-key.</summary>
    public enum ScanCode : ushort
    {
        W = 0x11,
        A = 0x1E,
        S = 0x1F,
        D = 0x20,
        Space = 0x39,
        Tab = 0x0F,
        E = 0x12,
    }

    public static void KeyDown(ScanCode sc) => SendKey(sc, false);
    public static void KeyUp(ScanCode sc) => SendKey(sc, true);

    private static void SendKey(ScanCode sc, bool up)
    {
        var input = new Input
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KeyboardInput
                {
                    wScan = (ushort)sc,
                    dwFlags = KEYEVENTF_SCANCODE | (up ? KEYEVENTF_KEYUP : 0),
                }
            }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }

    public static void MouseMoveRelative(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;
        var input = new Input
        {
            type = INPUT_MOUSE,
            u = new InputUnion { mi = new MouseInput { dx = dx, dy = dy, dwFlags = MOUSEEVENTF_MOVE } }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }

    public static void RightMouseDown() => SendMouseButton(MOUSEEVENTF_RIGHTDOWN);
    public static void RightMouseUp() => SendMouseButton(MOUSEEVENTF_RIGHTUP);
    public static void LeftMouseDown() => SendMouseButton(MOUSEEVENTF_LEFTDOWN);
    public static void LeftMouseUp() => SendMouseButton(MOUSEEVENTF_LEFTUP);

    private static void SendMouseButton(uint flag)
    {
        var input = new Input
        {
            type = INPUT_MOUSE,
            u = new InputUnion { mi = new MouseInput { dwFlags = flag } }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }
}
