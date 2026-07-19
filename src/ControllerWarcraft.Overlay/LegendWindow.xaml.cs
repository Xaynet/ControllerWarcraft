using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// Finestra della <b>button-legend a layer</b>: un pannello discreto, semi-trasparente, always-on-top,
/// click-through e non-attivabile (come l'overlay di modalità), che elenca cosa fa ogni pulsante
/// mappabile nel layer corrente (es. <c>X → Shift+1</c>). Si aggiorna quando cambia il layer.
///
/// È solo un <b>indicatore visivo</b>: non intercetta il mouse e non invia nulla. Le righe arrivano
/// già pronte dal loop dell'App (logica pura nel Core).
/// </summary>
public partial class LegendWindow : Window
{
    private const double EdgeMargin = 18;

    private static readonly Brush ButtonBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0));
    private static readonly Brush KeyBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
    private static readonly Brush KeyUnmappedBrush = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF));

    private LegendCorner _corner = LegendCorner.BottomRight;

    static LegendWindow()
    {
        ButtonBrush.Freeze();
        KeyBrush.Freeze();
        KeyUnmappedBrush.Freeze();
    }

    public LegendWindow()
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

    /// <summary>
    /// Aggiorna la legenda in base allo stato. Se non visibile o senza righe nasconde la finestra.
    /// Va chiamato sul thread UI dell'overlay.
    /// </summary>
    public void ApplyState(LegendOverlayState s)
    {
        var rows = s.Rows;
        if (!s.Visible || rows is null || rows.Count == 0)
        {
            Hide();
            return;
        }

        _corner = s.Corner;
        HeaderText.Text = s.LayerText;
        Rebuild(rows);

        if (!IsVisible)
        {
            Show();
            Reposition();
        }
    }

    private void Rebuild(IReadOnlyList<LegendRow> rows)
    {
        RowsGrid.Children.Clear();
        RowsGrid.RowDefinitions.Clear();

        for (int i = 0; i < rows.Count; i++)
        {
            RowsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var row = rows[i];
            bool unmapped = string.IsNullOrEmpty(row.Keybind) || row.Keybind == "-";

            var btn = new TextBlock
            {
                Text = row.Button,
                Foreground = ButtonBrush,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Margin = new Thickness(0, 1, 14, 1),
            };
            Grid.SetRow(btn, i);
            Grid.SetColumn(btn, 0);

            var key = new TextBlock
            {
                Text = string.IsNullOrEmpty(row.Keybind) ? "-" : row.Keybind,
                Foreground = unmapped ? KeyUnmappedBrush : KeyBrush,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = unmapped ? FontWeights.Normal : FontWeights.SemiBold,
                FontSize = 12,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 1, 0, 1),
            };
            Grid.SetRow(key, i);
            Grid.SetColumn(key, 1);

            RowsGrid.Children.Add(btn);
            RowsGrid.Children.Add(key);
        }
    }

    /// <summary>Ancora la finestra all'angolo configurato dell'area di lavoro dello schermo primario.</summary>
    private void Reposition()
    {
        var wa = SystemParameters.WorkArea;
        bool right = _corner is LegendCorner.TopRight or LegendCorner.BottomRight;
        bool bottom = _corner is LegendCorner.BottomLeft or LegendCorner.BottomRight;

        Left = right ? wa.Right - ActualWidth - EdgeMargin : wa.Left + EdgeMargin;
        Top = bottom ? wa.Bottom - ActualHeight - EdgeMargin : wa.Top + EdgeMargin;
    }
}
