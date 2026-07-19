using System.Windows.Threading;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// Ospita l'<see cref="OverlayWindow"/> su un thread STA dedicato con il proprio Dispatcher,
/// così il main loop dell'App (console, non-UI) resta libero. Espone un'API thread-safe:
/// <see cref="Start"/>, <see cref="Update"/> (dedup: aggiorna solo se lo stato cambia) e
/// <see cref="Dispose"/>.
///
/// È volutamente robusto: se l'ambiente non ha una UI disponibile (es. build/CI headless), le
/// chiamate falliscono in silenzio e l'App continua col solo indicatore a console.
/// </summary>
public sealed class ModeOverlayController : IDisposable
{
    private Thread? _thread;
    private Dispatcher? _dispatcher;
    private OverlayWindow? _window;
    private CursorIndicatorWindow? _cursorWindow;
    private LegendWindow? _legendWindow;
    private readonly ManualResetEventSlim _ready = new(false);

    private OverlayState _last;
    private bool _hasLast;

    private LegendOverlayState _lastLegend;
    private bool _hasLastLegend;

    private volatile bool _disposed;

    /// <summary>True se l'overlay è stato avviato correttamente.</summary>
    public bool IsRunning => _dispatcher is not null && !_disposed;

    /// <summary>Avvia il thread UI dell'overlay. Non lancia: in caso di errore l'overlay resta inattivo.</summary>
    public void Start()
    {
        if (_thread is not null) return;

        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "cw-overlay",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        // Attende (con timeout) che la finestra sia pronta, così il primo Update non va perso.
        _ready.Wait(3000);
    }

    private void ThreadMain()
    {
        try
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _window = new OverlayWindow();
            // Le finestre accessorie (indicatore cursore, button-legend) vivono sullo stesso thread
            // STA: create nascoste, si mostrano solo quando lo stato lo richiede.
            _cursorWindow = new CursorIndicatorWindow();
            _legendWindow = new LegendWindow();
            _window.Show();
            _ready.Set();
            Dispatcher.Run();
        }
        catch
        {
            // UI non disponibile o errore di rendering: l'App prosegue senza overlay.
            _dispatcher = null;
            _ready.Set();
        }
    }

    /// <summary>
    /// Aggiorna l'overlay col nuovo stato. Puo' essere chiamato ad ogni tick: aggiorna la UI solo
    /// se lo stato è effettivamente cambiato, evitando di inondare il Dispatcher.
    /// </summary>
    public void Update(OverlayState state)
    {
        if (_dispatcher is null || _disposed) return;
        if (_hasLast && _last.Equals(state)) return;

        _last = state;
        _hasLast = true;

        try
        {
            _dispatcher.BeginInvoke(() =>
            {
                _window?.ApplyState(state);
                // L'indicatore evidente della modalità cursore è guidato dallo stesso stato
                // (modalità/pausa) più il flag di configurazione dentro OverlayState.
                _cursorWindow?.ApplyState(state);
            });
        }
        catch { /* dispatcher in shutdown: ignora */ }
    }

    /// <summary>
    /// Aggiorna la button-legend a layer col nuovo stato. Come <see cref="Update"/> deduplica: l'App
    /// dovrebbe chiamarlo solo quando cambia il layer/modalità, ma anche una chiamata ad ogni tick è
    /// sicura (nessun ridisegno se lo stato è invariato → nessun flicker).
    /// </summary>
    public void UpdateLegend(LegendOverlayState state)
    {
        if (_dispatcher is null || _disposed) return;
        if (_hasLastLegend && _lastLegend.Equals(state)) return;

        _lastLegend = state;
        _hasLastLegend = true;

        try
        {
            _dispatcher.BeginInvoke(() => _legendWindow?.ApplyState(state));
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
