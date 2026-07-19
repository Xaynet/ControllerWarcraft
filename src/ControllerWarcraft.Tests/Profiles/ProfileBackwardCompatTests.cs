using System.Text.Json;
using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Test di RETRO-COMPATIBILITÀ dei profili (obiettivo punto 3): un JSON "vecchio stile" privo dei
/// campi aggiunti di recente deve deserializzare producendo i default che riproducono il
/// <b>comportamento storico</b> (cursore su R3 in Toggle, hold minimo 0, radial disattivo, curva
/// lineare). Verifica anche che i preset JSON reali in <c>profiles/</c> deserializzino senza errori.
/// </summary>
public class ProfileBackwardCompatTests
{
    // Le stesse opzioni usate dal ProfileManager, ricreate qui per deserializzare frammenti "a mano".
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private static ControllerProfile Deserialize(string json) =>
        JsonSerializer.Deserialize<ControllerProfile>(json, Options)!;

    // ---------------------------------------------------------------- profilo v1.0 minimale

    /// <summary>
    /// JSON in stile Fase 1/2 (schema 1.0): niente <c>curve</c>, niente <c>radialMenu</c>, niente
    /// <c>inputHardening</c>, niente <c>cursor.activationButton/Mode</c>, nessun layer Shoulder_LBRB.
    /// </summary>
    private const string OldStyleV10 = """
        {
          "schemaVersion": "1.0",
          "name": "Vecchio Profilo",
          "gameVersion": "Ascension",
          "movement": { "forward": "W", "back": "S", "left": "A", "right": "D", "threshold": 0.5, "deadzone": 0.2395 },
          "mouselook": { "sensitivityX": 18, "sensitivityY": 14, "invertY": false, "deadzone": 0.2652 },
          "cursor": { "speed": 16, "invertY": false },
          "system": {
            "jump": { "key": "Space" },
            "tabTarget": { "key": "Tab" },
            "cursorCancel": { "key": "Escape" }
          },
          "abilities": [
            { "button": "X", "layer": "Base", "bind": { "key": "D1" } },
            { "button": "X", "layer": "Shoulder_LB", "bind": { "key": "D1", "shift": true } },
            { "button": "X", "layer": "Shoulder_RB", "bind": { "key": "D1", "ctrl": true } }
          ]
        }
        """;

    [Fact]
    public void OldStyleV10_Deserializes_WithoutError()
    {
        var p = Deserialize(OldStyleV10);
        Assert.Equal("Vecchio Profilo", p.Name);
        Assert.Equal(3, p.Abilities.Count);
    }

    [Fact]
    public void OldStyleV10_Cursor_DefaultsToHistoricBehavior_RightThumbToggle()
    {
        var p = Deserialize(OldStyleV10);
        Assert.Equal(CursorActivationButton.RightThumb, p.Cursor.ActivationButton);
        Assert.Equal(CursorActivationMode.Toggle, p.Cursor.ActivationMode);
    }

    [Fact]
    public void OldStyleV10_InputHardening_DefaultsToZero_NoDebounce()
    {
        var p = Deserialize(OldStyleV10);
        Assert.NotNull(p.InputHardening);
        Assert.Equal(0, p.InputHardening.ThumbClickMinHoldMs);
    }

    [Fact]
    public void OldStyleV10_Mouselook_DefaultsToLinearCurve()
    {
        var p = Deserialize(OldStyleV10);
        Assert.NotNull(p.Mouselook.Curve);
        Assert.Equal(CurveType.Linear, p.Mouselook.Curve.Type);
    }

    [Fact]
    public void OldStyleV10_Modifiers_DefaultToHistoricLB_RB()
    {
        var p = Deserialize(OldStyleV10);
        Assert.NotNull(p.Modifiers);
        // Un profilo privo del campo 'modifiers' usa LB/RB → comportamento layer identico a prima.
        Assert.Equal(ModifierButton.LeftShoulder, p.Modifiers.Modifier1);
        Assert.Equal(ModifierButton.RightShoulder, p.Modifiers.Modifier2);
        // Nessuna abilità disabilitata (i grilletti restano abilità).
        Assert.Empty(LayerModifiers.DisabledAbilities(p.Modifiers));
    }

    [Fact]
    public void ExplicitModifiers_AreHonored()
    {
        const string json = """
            {
              "name": "TriggerMods",
              "modifiers": { "modifier1": "LeftTrigger", "modifier2": "RightTrigger" }
            }
            """;
        var p = Deserialize(json);
        Assert.Equal(ModifierButton.LeftTrigger, p.Modifiers.Modifier1);
        Assert.Equal(ModifierButton.RightTrigger, p.Modifiers.Modifier2);
        Assert.True(LayerModifiers.IsAbilityDisabled(p.Modifiers, ActionButton.LeftTrigger));
        Assert.True(LayerModifiers.IsAbilityDisabled(p.Modifiers, ActionButton.RightTrigger));
    }

    [Fact]
    public void OldStyleV10_RadialMenu_DefaultsToDisabled()
    {
        var p = Deserialize(OldStyleV10);
        Assert.NotNull(p.RadialMenu);
        Assert.False(p.RadialMenu.Enabled);
        Assert.Equal(RadialTrigger.None, p.RadialMenu.Trigger);
        Assert.False(p.RadialMenu.IsUsable);
    }

    [Fact]
    public void OldStyleV10_MissingLBRBLayer_ResolvesToNoOp()
    {
        var p = Deserialize(OldStyleV10);
        // Il layer Shoulder_LBRB non è nel file: deve risolvere a No-op sicuro.
        Assert.Equal(Keybind.None, p.Resolve(ActionButton.X, AbilityLayer.Shoulder_LBRB));
        // Mentre i layer presenti restano corretti.
        Assert.Equal(new Keybind(ScanCode.D1), p.Resolve(ActionButton.X, AbilityLayer.Base));
        Assert.Equal(new Keybind(ScanCode.D1, Shift: true), p.Resolve(ActionButton.X, AbilityLayer.Shoulder_LB));
    }

    // ---------------------------------------------------------------- override esplicito ancora rispettato

    [Fact]
    public void ExplicitCursorSettings_AreHonored_NotOverriddenByDefaults()
    {
        const string json = """
            {
              "name": "Custom",
              "cursor": { "speed": 20, "activationButton": "Start", "activationMode": "Hold" },
              "inputHardening": { "thumbClickMinHoldMs": 90 }
            }
            """;
        var p = Deserialize(json);
        Assert.Equal(CursorActivationButton.Start, p.Cursor.ActivationButton);
        Assert.Equal(CursorActivationMode.Hold, p.Cursor.ActivationMode);
        Assert.Equal(90, p.InputHardening.ThumbClickMinHoldMs);
    }

    [Fact]
    public void CompletelyEmptyObject_ProducesFullyDefaultedProfile()
    {
        var p = Deserialize("{}");
        // Tutti i default retro-compatibili sono presenti anche senza alcun campo.
        Assert.Equal(CursorActivationButton.RightThumb, p.Cursor.ActivationButton);
        Assert.Equal(CursorActivationMode.Toggle, p.Cursor.ActivationMode);
        Assert.Equal(0, p.InputHardening.ThumbClickMinHoldMs);
        Assert.Equal(CurveType.Linear, p.Mouselook.Curve.Type);
        Assert.False(p.RadialMenu.Enabled);
        Assert.Equal(ModifierButton.LeftShoulder, p.Modifiers.Modifier1);
        Assert.Equal(ModifierButton.RightShoulder, p.Modifiers.Modifier2);
        Assert.Empty(p.Abilities);
    }

    // ---------------------------------------------------------------- preset JSON reali del repo

    public static IEnumerable<object[]> RealProfileFiles()
    {
        foreach (var path in Directory.EnumerateFiles(TestPaths.ProfilesDir, "*.json"))
            yield return new object[] { path };
    }

    [Theory]
    [MemberData(nameof(RealProfileFiles))]
    public void RealPresetProfiles_Deserialize_WithoutError(string path)
    {
        var json = File.ReadAllText(path);
        var p = JsonSerializer.Deserialize<ControllerProfile>(json, Options);
        Assert.NotNull(p);
        Assert.False(string.IsNullOrWhiteSpace(p!.Name), $"{Path.GetFileName(path)} ha un nome vuoto");
        Assert.NotEmpty(p.Abilities);
    }

    public static IEnumerable<object[]> RealClassPresetFiles()
    {
        if (!Directory.Exists(TestPaths.ClassesDir)) yield break;
        foreach (var path in Directory.EnumerateFiles(TestPaths.ClassesDir, "*.json"))
            yield return new object[] { path };
    }

    [Theory]
    [MemberData(nameof(RealClassPresetFiles))]
    public void RealClassPresets_Deserialize_WithoutError(string path)
    {
        var json = File.ReadAllText(path);
        var preset = JsonSerializer.Deserialize<ClassPreset>(json, Options);
        Assert.NotNull(preset);
        Assert.False(string.IsNullOrWhiteSpace(preset!.Name), $"{Path.GetFileName(path)} ha un nome vuoto");
    }

    [Fact]
    public void RealProfilesDirectory_ExistsAndHasFiles()
    {
        // Guardia: se questo fallisce, i MemberData sopra sarebbero vuoti e passerebbero a vuoto.
        Assert.True(Directory.Exists(TestPaths.ProfilesDir), TestPaths.ProfilesDir);
        Assert.NotEmpty(Directory.EnumerateFiles(TestPaths.ProfilesDir, "*.json"));
    }
}
