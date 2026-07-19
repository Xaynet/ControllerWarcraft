using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Core.Profiles.Presets;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Test del <see cref="ProfileManager"/>: round-trip save→load, fallback ai built-in quando manca
/// il file, precedenza utente sui preset, persistenza di <see cref="AppSettings"/>. Ogni test usa
/// cartelle temporanee isolate (nessun contatto con %APPDATA% reale).
/// </summary>
public sealed class ProfileManagerTests : IDisposable
{
    private readonly string _appData;
    private readonly string _presets;

    public ProfileManagerTests()
    {
        var root = Path.Combine(Path.GetTempPath(), "cwc_pm_" + Guid.NewGuid().ToString("N"));
        _appData = Path.Combine(root, "appdata");
        _presets = Path.Combine(root, "presets");
        Directory.CreateDirectory(_appData);
        Directory.CreateDirectory(_presets);
    }

    public void Dispose()
    {
        try
        {
            var parent = Directory.GetParent(_appData)?.FullName;
            if (parent is not null && Directory.Exists(parent)) Directory.Delete(parent, recursive: true);
        }
        catch { /* best-effort cleanup */ }
    }

    private ProfileManager NewManager() => new(_appData, _presets);

    // ---------------------------------------------------------------- round-trip

    [Fact]
    public void Save_ThenLoad_RoundTripsProfile()
    {
        var pm = NewManager();
        var profile = BuiltInProfiles.Retail();

        var path = pm.Save(profile, "retail");
        Assert.True(File.Exists(path));

        var loaded = pm.Load("retail");
        Assert.NotNull(loaded);
        Assert.Equal(profile.Name, loaded!.Name);
        Assert.Equal(profile.GameVersion, loaded.GameVersion);
        Assert.Equal(profile.SchemaVersion, loaded.SchemaVersion);
        Assert.Equal(profile.Abilities.Count, loaded.Abilities.Count);
        Assert.Equal(profile.Mouselook.SensitivityX, loaded.Mouselook.SensitivityX);
        Assert.Equal(profile.Mouselook.Curve.Type, loaded.Mouselook.Curve.Type);

        // Un binding rappresentativo sopravvive al round-trip.
        Assert.Equal(
            profile.Resolve(ActionButton.DPadLeft, AbilityLayer.Shoulder_LBRB),
            loaded.Resolve(ActionButton.DPadLeft, AbilityLayer.Shoulder_LBRB));
    }

    [Fact]
    public void Save_UsesSlugifiedName_WhenNoStemGiven()
    {
        var pm = NewManager();
        var profile = new ControllerProfile { Name = "My Cool Profile!" };
        var path = pm.Save(profile);
        Assert.EndsWith("my-cool-profile.json", path.Replace('\\', '/'));
        Assert.NotNull(pm.Load("my-cool-profile"));
    }

    // ---------------------------------------------------------------- load assente

    [Fact]
    public void Load_MissingProfile_ReturnsNull()
    {
        var pm = NewManager();
        Assert.Null(pm.Load("nonexistent"));
    }

    // ---------------------------------------------------------------- precedenza utente sui preset

    [Fact]
    public void Load_UserProfile_WinsOverPreset_WithSameStem()
    {
        // Preset "ascension" accanto all'exe.
        ProfileManager.WriteTo(BuiltInProfiles.Ascension(), Path.Combine(_presets, "ascension.json"));

        var pm = NewManager();
        // Profilo utente con lo stesso stem ma nome diverso.
        pm.Save(new ControllerProfile { Name = "Utente Vince" }, "ascension");

        var loaded = pm.Load("ascension");
        Assert.Equal("Utente Vince", loaded!.Name);
    }

    [Fact]
    public void Load_PresetOnly_WhenNoUserFile()
    {
        ProfileManager.WriteTo(BuiltInProfiles.Classic(), Path.Combine(_presets, "classic.json"));
        var pm = NewManager();
        var loaded = pm.Load("classic");
        Assert.Equal("Classic", loaded!.Name);
    }

    // ---------------------------------------------------------------- fallback built-in

    [Fact]
    public void LoadActiveOrDefault_NoFilesAtAll_FallsBackToBuiltInAscension()
    {
        var pm = NewManager(); // nessun preset, nessun profilo utente, nessun settings
        var messages = new List<string>();

        var profile = pm.LoadActiveOrDefault(messages.Add);

        Assert.Equal("Ascension", profile.Name);
        Assert.NotEmpty(profile.Abilities); // il built-in ha i suoi layer
        Assert.Contains(messages, m => m.Contains("built-in", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LoadActiveOrDefault_FallsBackToAscensionPreset_WhenActiveMissing()
    {
        // C'è un preset ascension ma il profilo attivo puntato dai settings non esiste.
        ProfileManager.WriteTo(BuiltInProfiles.Ascension(), Path.Combine(_presets, "ascension.json"));
        var pm = NewManager();
        pm.SaveSettings(new AppSettings { ActiveProfile = "profilo-che-non-esiste" });

        var profile = pm.LoadActiveOrDefault();
        Assert.Equal("Ascension", profile.Name);
    }

    [Fact]
    public void LoadActiveOrDefault_LoadsActive_WhenPresent()
    {
        ProfileManager.WriteTo(BuiltInProfiles.Retail(), Path.Combine(_presets, "retail.json"));
        var pm = NewManager();
        pm.SetActiveProfile("retail");

        var profile = pm.LoadActiveOrDefault();
        Assert.Equal("Retail", profile.Name);
    }

    // ---------------------------------------------------------------- settings

    [Fact]
    public void Settings_RoundTrip()
    {
        var pm = NewManager();
        var s = new AppSettings
        {
            ActiveProfile = "classic",
            ShowOverlay = false,
            AutoSwitchEnabled = true,
            CompanionEnabled = true,
            CompanionSavedVariablesPath = @"C:\wow\saved.lua",
        };
        pm.SaveSettings(s);

        var loaded = pm.LoadSettings();
        Assert.Equal("classic", loaded.ActiveProfile);
        Assert.False(loaded.ShowOverlay);
        Assert.True(loaded.AutoSwitchEnabled);
        Assert.True(loaded.CompanionEnabled);
        Assert.Equal(@"C:\wow\saved.lua", loaded.CompanionSavedVariablesPath);
    }

    [Fact]
    public void LoadSettings_NoFile_ReturnsDefaults()
    {
        var pm = NewManager();
        var s = pm.LoadSettings();
        Assert.Equal("ascension", s.ActiveProfile); // default retro-compatibile
        Assert.True(s.ShowOverlay);
        Assert.False(s.AutoSwitchEnabled);
    }

    [Fact]
    public void SetActiveProfile_PersistsAcrossReload()
    {
        var pm = NewManager();
        pm.SetActiveProfile("classic");
        Assert.Equal("classic", NewManager().LoadSettings().ActiveProfile);
    }

    // ---------------------------------------------------------------- discovery

    [Fact]
    public void ListProfiles_MergesPresetAndUser_UserWins()
    {
        ProfileManager.WriteTo(BuiltInProfiles.Ascension(), Path.Combine(_presets, "ascension.json"));
        ProfileManager.WriteTo(BuiltInProfiles.Classic(), Path.Combine(_presets, "classic.json"));
        var pm = NewManager();
        pm.Save(new ControllerProfile { Name = "Ascension Mio", GameVersion = "Ascension" }, "ascension");

        var list = pm.ListProfiles();
        Assert.Equal(2, list.Count); // ascension (utente) + classic (preset), non 3
        var asc = list.Single(i => i.FileName == "ascension");
        Assert.Equal(ProfileSource.User, asc.Source);
        Assert.Equal("Ascension Mio", asc.Name);
    }

    // ---------------------------------------------------------------- Slugify

    [Theory]
    [InlineData("Ascension", "ascension")]
    [InlineData("My Cool Profile", "my-cool-profile")]
    [InlineData("  Spaces  ", "spaces")]
    [InlineData("Warrior_Arms", "warrior-arms")]
    [InlineData("a/b:c", "abc")] // i simboli non ' ' '-' '_' vengono rimossi
    [InlineData("---edge---", "edge")]
    public void Slugify_ProducesSafeStems(string input, string expected)
    {
        Assert.Equal(expected, ProfileManager.Slugify(input));
    }
}
