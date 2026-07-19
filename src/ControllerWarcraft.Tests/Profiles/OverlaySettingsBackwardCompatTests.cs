using System.Text.Json;
using System.Text.Json.Serialization;
using ControllerWarcraft.Core.Profiles;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Retro-compatibilità dei nuovi campi di <see cref="AppSettings"/> per la button-legend e
/// l'indicatore cursore: un <c>settings.json</c> "vecchio stile" (senza questi campi) deve
/// riprodurre i default documentati (legenda attiva, "solo mentre tieni un modificatore", angolo in
/// basso a destra, indicatore cursore attivo).
/// </summary>
public class OverlaySettingsBackwardCompatTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public void OldStyleSettings_WithoutNewFields_UseSensibleDefaults()
    {
        // settings.json in stile Fase 3/4: nessun campo legenda/indicatore cursore.
        const string json = """
            { "activeProfile": "ascension", "showOverlay": true, "autoSwitchEnabled": false }
            """;
        var s = JsonSerializer.Deserialize<AppSettings>(json, Options)!;

        Assert.True(s.ShowButtonLegend);
        Assert.Equal(LegendVisibilityMode.WhileModifierHeld, s.LegendVisibility);
        Assert.Equal(ScreenCorner.BottomRight, s.LegendCorner);
        Assert.True(s.ShowCursorIndicator);
        // Il resto resta invariato.
        Assert.Equal("ascension", s.ActiveProfile);
        Assert.True(s.ShowOverlay);
    }

    [Fact]
    public void NewFields_RoundTrip_ByName()
    {
        var original = new AppSettings
        {
            ShowButtonLegend = false,
            LegendVisibility = LegendVisibilityMode.AlwaysVisible,
            LegendCorner = ScreenCorner.TopLeft,
            ShowCursorIndicator = false,
        };

        var json = JsonSerializer.Serialize(original, Options);
        // Enum serializzati per nome (leggibili nel file), coerente con il resto dello schema.
        Assert.Contains("AlwaysVisible", json);
        Assert.Contains("TopLeft", json);

        var back = JsonSerializer.Deserialize<AppSettings>(json, Options)!;
        Assert.False(back.ShowButtonLegend);
        Assert.Equal(LegendVisibilityMode.AlwaysVisible, back.LegendVisibility);
        Assert.Equal(ScreenCorner.TopLeft, back.LegendCorner);
        Assert.False(back.ShowCursorIndicator);
    }
}
