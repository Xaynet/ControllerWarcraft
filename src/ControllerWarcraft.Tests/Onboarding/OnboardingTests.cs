using System.Text.Json;
using ControllerWarcraft.Core.Onboarding;
using ControllerWarcraft.Core.Profiles;
using Xunit;

namespace ControllerWarcraft.Tests.Onboarding;

/// <summary>
/// Test della logica <b>pura</b> del wizard di primo avvio e del flag <see cref="AppSettings.SetupCompleted"/>:
/// retro-compatibilità (settings "vecchio stile" senza il flag ⇒ wizard mostrato una volta),
/// persistenza di <see cref="ProfileManager.MarkSetupCompleted"/> e contenuti informativi (tabella
/// keybinding WoW, versioni suggerite).
/// </summary>
public sealed class OnboardingTests : IDisposable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    private readonly string _appData;

    public OnboardingTests()
    {
        _appData = Path.Combine(Path.GetTempPath(), "cwc_onb_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_appData);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_appData)) Directory.Delete(_appData, recursive: true); }
        catch { /* best-effort */ }
    }

    // ---------------------------------------------------------------- retro-compatibilità del flag

    [Fact]
    public void OldStyleSettings_WithoutFlag_DefaultsToNotCompleted_AndNeedsSetup()
    {
        // settings.json in stile Fase 2/3: nessun campo setupCompleted.
        const string json = """
            { "activeProfile": "ascension", "showOverlay": true, "autoSwitchEnabled": false }
            """;
        var s = JsonSerializer.Deserialize<AppSettings>(json, Options)!;

        Assert.False(s.SetupCompleted);           // default retro-compatibile
        Assert.True(OnboardingInfo.NeedsSetup(s)); // ⇒ il wizard viene mostrato
        Assert.Equal("ascension", s.ActiveProfile); // il resto resta invariato
    }

    [Fact]
    public void DefaultSettings_NeedSetup()
    {
        Assert.True(OnboardingInfo.NeedsSetup(new AppSettings()));
    }

    [Fact]
    public void CompletedSettings_DoNotNeedSetup()
    {
        Assert.False(OnboardingInfo.NeedsSetup(new AppSettings { SetupCompleted = true }));
    }

    // ---------------------------------------------------------------- persistenza

    [Fact]
    public void MarkSetupCompleted_Persists_AndPreservesOtherSettings()
    {
        var pm = new ProfileManager(_appData);
        pm.SaveSettings(new AppSettings { ActiveProfile = "retail", ShowOverlay = false });

        pm.MarkSetupCompleted();

        var reloaded = new ProfileManager(_appData).LoadSettings();
        Assert.True(reloaded.SetupCompleted);
        Assert.False(OnboardingInfo.NeedsSetup(reloaded));
        // Il resto delle impostazioni non è stato toccato.
        Assert.Equal("retail", reloaded.ActiveProfile);
        Assert.False(reloaded.ShowOverlay);
    }

    [Fact]
    public void Settings_RoundTrip_IncludesSetupCompleted()
    {
        var pm = new ProfileManager(_appData);
        pm.SaveSettings(new AppSettings { SetupCompleted = true });
        Assert.True(pm.LoadSettings().SetupCompleted);
    }

    // ---------------------------------------------------------------- contenuti informativi

    [Fact]
    public void WowKeybindings_CoverTheFourLayers()
    {
        var rows = OnboardingInfo.WowKeybindings;
        Assert.Equal(4, rows.Count);
        Assert.All(rows, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Bar));
            Assert.False(string.IsNullOrWhiteSpace(r.Keys));
        });
    }

    [Fact]
    public void GameVersions_IncludeTheThreeSupportedStems()
    {
        var stems = OnboardingInfo.GameVersions.Select(v => v.ProfileStem).ToArray();
        Assert.Contains("ascension", stems);
        Assert.Contains("classic", stems);
        Assert.Contains("retail", stems);
    }
}
