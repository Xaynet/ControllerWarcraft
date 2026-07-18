using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// Finestra del radial menu (Fase 4, punto 1): trasparente, always-on-top, click-through e
/// non-attivabile (come l'overlay di modalità). Disegna N settori attorno a un cerchio con le
/// etichette delle voci ed evidenzia il settore attualmente selezionato dallo stick destro.
///
/// È solo un <b>indicatore visivo</b>: non intercetta il mouse e non invia nulla. La selezione e
/// l'invio del keybind (uno solo, 1:1) restano interamente nel <c>MappingEngine</c> dell'App.
/// </summary>
public partial class RadialMenuWindow : Window
{
    private const double Size = 380;
    private const double Center = Size / 2.0;
    private const double OuterRadius = 168;
    private const double InnerRadius = 66;
    private const double LabelRadius = (OuterRadius + InnerRadius) / 2.0;

    private static readonly Brush WedgeFill = new SolidColorBrush(Color.FromArgb(0xCC, 0x1B, 0x24, 0x2E));
    private static readonly Brush WedgeSelectedFill = new SolidColorBrush(Color.FromArgb(0xEE, 0x1B, 0x5E, 0x20));
    private static readonly Brush WedgeStroke = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));
    private static readonly Brush LabelBrush = Brushes.White;

    static RadialMenuWindow()
    {
        WedgeFill.Freeze();
        WedgeSelectedFill.Freeze();
        WedgeStroke.Freeze();
    }

    public RadialMenuWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Reposition();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeOverlay.MakeClickThrough(hwnd);
    }

    /// <summary>Centra la finestra sullo schermo primario (area di lavoro).</summary>
    private void Reposition()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Left + (wa.Width - Width) / 2.0;
        Top = wa.Top + (wa.Height - Height) / 2.0;
    }

    /// <summary>
    /// Ridisegna il menu in base allo stato. Se non visibile o senza voci nasconde la finestra.
    /// Va chiamato sul thread UI dell'overlay.
    /// </summary>
    public void ApplyState(RadialOverlayState s)
    {
        var labels = s.Labels;
        if (!s.Visible || labels is null || labels.Count == 0)
        {
            Hide();
            return;
        }

        Draw(labels, s.SelectedIndex);

        if (!IsVisible)
        {
            Show();
            Reposition();
        }
    }

    private void Draw(IReadOnlyList<string> labels, int selected)
    {
        RadialCanvas.Children.Clear();
        int count = labels.Count;
        double sector = 2 * Math.PI / count;
        double half = sector / 2.0;

        for (int i = 0; i < count; i++)
        {
            // Centro del settore i: dall'alto (12 in punto), in senso orario — stessa convenzione
            // di RadialMenuResolver (Core).
            double centerAngle = sector * i;
            double start = centerAngle - half;
            double end = centerAngle + half;

            var wedge = BuildWedge(start, end, i == selected);
            RadialCanvas.Children.Add(wedge);

            var label = BuildLabel(labels[i], centerAngle, i == selected);
            RadialCanvas.Children.Add(label);
        }
    }

    // Costruisce un settore ad anello (donut wedge) tra InnerRadius e OuterRadius.
    private Path BuildWedge(double startAngle, double endAngle, bool isSelected)
    {
        Point pInnerStart = Polar(InnerRadius, startAngle);
        Point pOuterStart = Polar(OuterRadius, startAngle);
        Point pOuterEnd = Polar(OuterRadius, endAngle);
        Point pInnerEnd = Polar(InnerRadius, endAngle);

        var figure = new PathFigure { StartPoint = pInnerStart, IsClosed = true };
        figure.Segments.Add(new LineSegment(pOuterStart, true));
        figure.Segments.Add(new ArcSegment(pOuterEnd, new Size(OuterRadius, OuterRadius),
            0, false, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(pInnerEnd, true));
        figure.Segments.Add(new ArcSegment(pInnerStart, new Size(InnerRadius, InnerRadius),
            0, false, SweepDirection.Counterclockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return new Path
        {
            Data = geometry,
            Fill = isSelected ? WedgeSelectedFill : WedgeFill,
            Stroke = WedgeStroke,
            StrokeThickness = 1.5,
        };
    }

    private TextBlock BuildLabel(string text, double centerAngle, bool isSelected)
    {
        var tb = new TextBlock
        {
            Text = text,
            Foreground = LabelBrush,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = isSelected ? 15 : 13,
            FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 96,
            TextWrapping = TextWrapping.Wrap,
        };

        // Misura per centrare l'etichetta sul punto radiale.
        tb.Measure(new Size(96, 60));
        var desired = tb.DesiredSize;

        Point at = Polar(LabelRadius, centerAngle);
        Canvas.SetLeft(tb, at.X - desired.Width / 2.0);
        Canvas.SetTop(tb, at.Y - desired.Height / 2.0);
        return tb;
    }

    // Converte (raggio, angolo-dall'alto-orario) in coordinate canvas.
    private static Point Polar(double radius, double angleFromTop)
    {
        double x = Center + radius * Math.Sin(angleFromTop);
        double y = Center - radius * Math.Cos(angleFromTop);
        return new Point(x, y);
    }
}
