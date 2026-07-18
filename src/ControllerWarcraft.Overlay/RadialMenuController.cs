using System;
using System.Threading;
using System.Windows.Threading;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// Ospita la <see cref="RadialMenuWindow"/> su un thread STA dedicato con il proprio Dispatcher
/// (come <see cref="ModeOverlayController"/>), così il main loop console dell'App resta libero.
/// Espone un'API thread-safe: <see cref="Start"/>, <see cref="Update"/> (dedup: aggiorna solo se lo
/// stato cambia) e <see cref="Dispose"/>.
///
/// Robusto: se l'ambiente non ha una UI (build/CI headless) le chiamate falliscono in silenzio e
/// l'App prosegue senza radial overlay (il radial resta comunque un elemento opzionale).
/// </summary>
public sealed class RadialMenuController : IDisposable
{
    private Thread? _thread;
    private Dispatcher? _dispatcher;
    private RadialMenuWindow? _window;
    private readonly ManualResetEventSlim _ready = new(false);

    private RadialOverlayState _last;
    private bool _hasLast;
    private volatile bool _disposed;

    /// <summary>True se l'overlay radiale è stato avviato correttamente.</summary>
    public bool IsRunning => _dispatcher is not null && !_disposed;

    /// <summary>Avvia il thread UI dell'overlay radiale. Non lancia: in caso di errore resta inattivo.</summary>
    public void Start()
    {
        if (_thread is not null) return;

        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "cw-radial",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait(3000);
    }

    private void ThreadMain()
    {
        try
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _window = new RadialMenuWindow();
            // Creata nascosta: si mostra solo quando il menu è aperto.
            _ready.Set();
            Dispatcher.Run();
        }
        catch
        {
            _dispatcher = null;
            _ready.Set();
        }
    }

    /// <summary>Aggiorna il radial overlay col nuovo stato (dedup se invariato).</summary>
    public void Update(RadialOverlayState state)
    {
        if (_dispatcher is null || _disposed) return;
        if (_hasLast && _last.Equals(state)) return;

        _last = state;
        _hasLast = true;

        try
        {
            _dispatcher.BeginInvoke(() => _window?.ApplyState(state));
        }
        catch { /* dispatcher in shutdown: ignora */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _dispatcher?.InvokeShutdown(); }
        catch { /* già in shutdown */ }

        _ready.Dispose();
    }
}
