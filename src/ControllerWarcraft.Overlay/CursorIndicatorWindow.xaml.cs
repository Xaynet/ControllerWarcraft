using System.Windows;
using System.Windows.Interop;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// Indicatore <b>evidente</b> della modalità cursore (punto 2): una sottile cornice colorata ai bordi
/// dello schermo + un badge in alto al centro, così è impossibile non accorgersi di essere in modalità
/// cursore (attrito segnalato da un tester: si era confuso perché era in cursore senza saperlo).
///
/// Come gli altri overlay è trasparente, always-on-top, click-through e non-attivabile: la cornice ha
/// solo il bordo (nessun riempimento), quindi non copre il gioco e non intercetta il mouse.
/// Compare solo quando lo stato è in modalità cursore (e non in pausa).
/// </summary>
public partial class CursorIndicatorWindow : Window
{
    public CursorIndicatorWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => CoverPrimaryScreen();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeOverlay.MakeClickThrough(hwnd);
    }

    /// <summary>Dimensiona e posiziona la finestra per coprire l'intero schermo primario.</summary>
    private void CoverPrimaryScreen()
    {
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
    }

    /// <summary>
    /// Mostra/nasconde l'indicatore in base allo stato. Visibile solo in modalità cursore, con il
    /// flag di configurazione attivo e fuori pausa. Va chiamato sul thread UI dell'overlay.
    /// </summary>
    public void ApplyState(OverlayState s)
    {
        bool show = s.CursorIndicator && !s.Paused && s.Mode == OverlayMode.Cursor;

        if (show)
        {
            if (!IsVisible)
            {
                Show();
                CoverPrimaryScreen();
            }
        }
        else if (IsVisible)
        {
            Hide();
        }
    }
}
