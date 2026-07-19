using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Core.Profiles.Presets;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Test del merge/override dei preset di classe (<see cref="ClassPreset.ApplyTo"/>): override
/// applicati, voci non specificate invariate, aggiunta di nuove voci, sostituzione del radial e
/// idempotenza.
/// </summary>
public class ClassPresetTests
{
    // ---------------------------------------------------------------- override di una voce esistente

    [Fact]
    public void ApplyTo_OverridesExistingBinding()
    {
        var profile = BuiltInProfiles.Ascension();
        // Ascension: X/Base -> D1. Sostituiamolo con F5.
        Assert.Equal(new Keybind(ScanCode.D1), profile.Resolve(ActionButton.X, AbilityLayer.Base));

        var preset = new ClassPreset
        {
            AbilityOverrides =
            {
                new AbilityBinding(ActionButton.X, AbilityLayer.Base, new Keybind(ScanCode.F5)),
            },
        };

        preset.ApplyTo(profile);

        Assert.Equal(new Keybind(ScanCode.F5), profile.Resolve(ActionButton.X, AbilityLayer.Base));
    }

    [Fact]
    public void ApplyTo_DoesNotGrowAbilityList_WhenReplacing()
    {
        var profile = BuiltInProfiles.Ascension();
        int before = profile.Abilities.Count;

        var preset = new ClassPreset
        {
            AbilityOverrides =
            {
                new AbilityBinding(ActionButton.B, AbilityLayer.Shoulder_LB, new Keybind(ScanCode.F1)),
            },
        };
        preset.ApplyTo(profile);

        Assert.Equal(before, profile.Abilities.Count); // sostituzione, non aggiunta
    }

    // ---------------------------------------------------------------- aggiunta di una voce assente

    [Fact]
    public void ApplyTo_AddsBinding_WhenNotPresent()
    {
        // Profilo minimale senza alcuna voce per (Y, Base).
        var profile = new ControllerProfile();
        Assert.Equal(Keybind.None, profile.Resolve(ActionButton.Y, AbilityLayer.Base));

        var preset = new ClassPreset
        {
            AbilityOverrides =
            {
                new AbilityBinding(ActionButton.Y, AbilityLayer.Base, new Keybind(ScanCode.F3)),
            },
        };
        preset.ApplyTo(profile);

        Assert.Single(profile.Abilities);
        Assert.Equal(new Keybind(ScanCode.F3), profile.Resolve(ActionButton.Y, AbilityLayer.Base));
    }

    // ---------------------------------------------------------------- campi non specificati invariati

    [Fact]
    public void ApplyTo_LeavesUnrelatedSettingsUntouched()
    {
        var profile = BuiltInProfiles.Retail();
        double sensX = profile.Mouselook.SensitivityX;
        double cursorSpeed = profile.Cursor.Speed;
        var forward = profile.Movement.Forward;
        var jump = profile.System.Jump;

        var preset = new ClassPreset
        {
            AbilityOverrides =
            {
                new AbilityBinding(ActionButton.X, AbilityLayer.Base, new Keybind(ScanCode.F5)),
            },
        };
        preset.ApplyTo(profile);

        Assert.Equal(sensX, profile.Mouselook.SensitivityX);
        Assert.Equal(cursorSpeed, profile.Cursor.Speed);
        Assert.Equal(forward, profile.Movement.Forward);
        Assert.Equal(jump, profile.System.Jump);
    }

    [Fact]
    public void ApplyTo_OtherBindings_Unchanged()
    {
        var profile = BuiltInProfiles.Ascension();
        var yBase = profile.Resolve(ActionButton.Y, AbilityLayer.Base);

        var preset = new ClassPreset
        {
            AbilityOverrides =
            {
                new AbilityBinding(ActionButton.X, AbilityLayer.Base, new Keybind(ScanCode.F5)),
            },
        };
        preset.ApplyTo(profile);

        // La voce Y/Base non è toccata dall'override su X/Base.
        Assert.Equal(yBase, profile.Resolve(ActionButton.Y, AbilityLayer.Base));
    }

    // ---------------------------------------------------------------- radial menu

    [Fact]
    public void ApplyTo_NullRadial_LeavesBaseRadialUntouched()
    {
        var profile = BuiltInProfiles.Ascension();
        profile.RadialMenu.Enabled = true;
        profile.RadialMenu.Trigger = RadialTrigger.LeftThumb;
        profile.RadialMenu.Items.Add(new RadialMenuItem("Base", new Keybind(ScanCode.F1)));
        var original = profile.RadialMenu;

        var preset = new ClassPreset { RadialMenu = null };
        preset.ApplyTo(profile);

        Assert.Same(original, profile.RadialMenu);
        Assert.True(profile.RadialMenu.Enabled);
        Assert.Single(profile.RadialMenu.Items);
    }

    [Fact]
    public void ApplyTo_NonNullRadial_ReplacesEntirely_WithDefensiveCopy()
    {
        var profile = BuiltInProfiles.Ascension();

        var presetRadial = new RadialMenuSettings
        {
            Enabled = true,
            Trigger = RadialTrigger.RightThumb,
            SelectDeadzone = 0.5,
            Items = { new RadialMenuItem("Mount", new Keybind(ScanCode.F1)) },
        };
        var preset = new ClassPreset { RadialMenu = presetRadial };
        preset.ApplyTo(profile);

        Assert.True(profile.RadialMenu.Enabled);
        Assert.Equal(RadialTrigger.RightThumb, profile.RadialMenu.Trigger);
        Assert.Equal(0.5, profile.RadialMenu.SelectDeadzone);
        Assert.Single(profile.RadialMenu.Items);

        // Copia difensiva: il radial del profilo non deve essere la stessa istanza del preset.
        Assert.NotSame(presetRadial, profile.RadialMenu);
        Assert.NotSame(presetRadial.Items[0], profile.RadialMenu.Items[0]);

        // Mutare il preset dopo l'applicazione non altera il profilo.
        presetRadial.Items.Add(new RadialMenuItem("Extra", new Keybind(ScanCode.F2)));
        Assert.Single(profile.RadialMenu.Items);
    }

    // ---------------------------------------------------------------- idempotenza

    [Fact]
    public void ApplyTo_IsIdempotent_ForBindings()
    {
        var preset = new ClassPreset
        {
            AbilityOverrides =
            {
                new AbilityBinding(ActionButton.X, AbilityLayer.Base, new Keybind(ScanCode.F5)),
                new AbilityBinding(ActionButton.DPadUp, AbilityLayer.Shoulder_LBRB, new Keybind(ScanCode.F6)),
            },
        };

        var once = BuiltInProfiles.Ascension();
        preset.ApplyTo(once);
        int countOnce = once.Abilities.Count;

        var twice = BuiltInProfiles.Ascension();
        preset.ApplyTo(twice);
        preset.ApplyTo(twice);

        Assert.Equal(countOnce, twice.Abilities.Count);
        Assert.Equal(new Keybind(ScanCode.F5), twice.Resolve(ActionButton.X, AbilityLayer.Base));
    }

    [Fact]
    public void ApplyTo_ReturnsSameInstance()
    {
        var profile = BuiltInProfiles.Ascension();
        var preset = new ClassPreset();
        var result = preset.ApplyTo(profile);
        Assert.Same(profile, result);
    }
}
