using System.Runtime.InteropServices;
using ControllerWarcraft.Core.Input;

namespace ControllerWarcraft.App.Native;

/// <summary>
/// P/Invoke minimale per <b>SendInput</b> (emulazione KB/mouse). Evoluzione del <c>Native.cs</c>
/// dello Spike Fase 0.
///
/// Fase 2: l'enum <see cref="ScanCode"/> è stato spostato in
/// <c>ControllerWarcraft.Core.Input</c> perché fa parte dello schema di profilo serializzato.
///
/// Onboarding wizard: la lettura XInput (che era qui accanto a SendInput) è stata estratta nel
/// Core come <see cref="XInputReader"/> di sola lettura, condivisa con la Gui. Qui resta
/// <b>solo</b> l'emulazione dell'output: SendInput è e resta esclusivo dell'App.
/// </summary>
public static class NativeMethods
{
    // ======================= SendInput =======================

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

    public static void KeyDown(ScanCode sc) => SendKey(sc, false);
    public static void KeyUp(ScanCode sc) => SendKey(sc, true);

    private static void SendKey(ScanCode sc, bool up)
    {
        if (sc == ScanCode.None) return;
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
