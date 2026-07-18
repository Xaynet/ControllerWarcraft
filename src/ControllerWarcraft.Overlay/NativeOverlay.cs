using System.Runtime.InteropServices;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// P/Invoke minimale per rendere la finestra dell'overlay <b>click-through</b> e non-attivabile:
/// aggiunge gli stili estesi WS_EX_TRANSPARENT (i click passano sotto), WS_EX_LAYERED,
/// WS_EX_NOACTIVATE (non ruba il focus al gioco) e WS_EX_TOOLWINDOW (fuori da Alt-Tab).
/// </summary>
internal static class NativeOverlay
{
    private const int GWL_EXSTYLE = -20;

    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    /// <summary>Applica gli stili estesi che rendono l'handle click-through e non-attivabile.</summary>
    public static void MakeClickThrough(nint hwnd)
    {
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);
    }
}
