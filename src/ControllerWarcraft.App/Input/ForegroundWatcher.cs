using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ControllerWarcraft.App.Input;

/// <summary>
/// Rileva il nome dell'eseguibile della finestra in primo piano (Fase 3, punto 4 — auto-switch).
/// Usa <c>GetForegroundWindow</c> + <c>GetWindowThreadProcessId</c> e risolve il PID in nome
/// processo. È l'unica parte "Win32" dell'auto-switch: la <b>mappatura</b> processo→profilo vive
/// nel Core (<c>AutoSwitchResolver</c>), qui c'è solo la lettura del sistema.
/// </summary>
public static class ForegroundWatcher
{
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Nome del processo (senza <c>.exe</c>) della finestra in primo piano, o <c>null</c> se non
    /// determinabile (nessuna finestra, processo terminato, accesso negato). Non lancia mai.
    /// </summary>
    public static string? GetForegroundProcessName()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == 0) return null;

        _ = GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0) return null;

        try
        {
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName; // già senza estensione
        }
        catch
        {
            return null;
        }
    }
}
