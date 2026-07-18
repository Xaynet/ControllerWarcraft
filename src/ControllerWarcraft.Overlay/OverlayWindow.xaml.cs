using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// Finestra dell'overlay indicatore di modalità (Fase 3, punto 1): trasparente, always-on-top,
/// click-through e non-attivabile, così non ruba il focus al gioco né intercetta il mouse.
/// Si posiziona in alto al centro dello schermo primario.
/// </summary>
public partial class OverlayWindow : Window
{
    // Colori per modalità (ARGB con alpha per la semitrasparenza).
    private static readonly Color MovementColor = Color.FromArgb(0xDD, 0x1B, 0x5E, 0x20); // verde scuro
    private static readonly Color CursorColor = Color.FromArgb(0xDD, 0x0D, 0x47, 0xA1);   // blu scuro
    private static readonly Color PausedColor = Color.FromArgb(0xDD, 0x42, 0x42, 0x42);   // grigio

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Reposition();
        SizeChanged += (_, _) => Reposition();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeOverlay.MakeClickThrough(hwnd);
    }

    /// <summary>Centra la finestra in alto sullo schermo primario (area di lavoro).</summary>
    private void Reposition()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Left + (wa.Width - ActualWidth) / 2.0;
        Top = wa.Top + 24;
    }

    /// <summary>Aggiorna testi e colore in base allo stato. Va chiamato sul thread UI dell'overlay.</summary>
    public void ApplyState(OverlayState s)
    {
        ModeText.Text = s.Paused ? "PAUSA" : s.ModeText;

        // Il layer è rilevante solo in Movimento/Combattimento; in Cursore mostra l'hint dei tasti.
        LayerText.Text = s.Paused
            ? "gioco non in primo piano"
            : s.Mode == OverlayMode.Cursor
                ? "A = click · X = click destro · B = Esc"
                : $"Layer: {s.LayerText}";

        ProfileText.Text = string.IsNullOrWhiteSpace(s.ProfileName) ? "" : $"Profilo: {s.ProfileName}";

        // Contesto opzionale dal companion addon (Fase 4): mostrato solo se presente.
        CompanionText.Text = s.CompanionText ?? "";
        CompanionText.Visibility = string.IsNullOrWhiteSpace(s.CompanionText)
            ? Visibility.Collapsed : Visibility.Visible;

        var color = s.Paused ? PausedColor
                  : s.Mode == OverlayMode.Cursor ? CursorColor
                  : MovementColor;
        RootBorder.Background = new SolidColorBrush(color);

        // Riposiziona: il testo può cambiare larghezza (SizeToContent).
        Reposition();
    }
}
