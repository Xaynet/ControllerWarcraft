using System.Linq;
using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Test della logica <b>pura</b> della button-legend (<see cref="ButtonLegend"/>): derivazione delle
/// righe (pulsante → keybind) per ogni layer, pulsanti non mappati, ordine/etichette stabili e la
/// decisione di visibilità (<see cref="ButtonLegend.ShouldShow"/>). L'overlay resta pura presentazione:
/// tutta la logica testabile vive qui.
/// </summary>
public class ButtonLegendTests
{
    // Profilo minimo: alcuni pulsanti mappati su vari layer, altri deliberatamente non mappati.
    private static ControllerProfile MakeProfile()
    {
        var p = new ControllerProfile { Name = "Test" };
        p.Abilities.Add(new AbilityBinding(ActionButton.X, AbilityLayer.Base, new Keybind(ScanCode.D1)));
        p.Abilities.Add(new AbilityBinding(ActionButton.B, AbilityLayer.Base, new Keybind(ScanCode.D2)));
        p.Abilities.Add(new AbilityBinding(ActionButton.X, AbilityLayer.Shoulder_LB, new Keybind(ScanCode.D1, Shift: true)));
        p.Abilities.Add(new AbilityBinding(ActionButton.RightTrigger, AbilityLayer.Shoulder_RB, new Keybind(ScanCode.D5, Ctrl: true)));
        // Y, LT, D-pad non mappati su Base -> devono comparire come "-".
        return p;
    }

    // ---------------------------------------------------------------- copertura pulsanti / layer

    [Fact]
    public void Build_IncludesEveryMappableButton_InStableOrder()
    {
        var rows = ButtonLegend.Build(MakeProfile(), AbilityLayer.Base);

        Assert.Equal(ButtonLegend.ButtonOrder.Count, rows.Count);
        Assert.Equal(ButtonLegend.ButtonOrder.ToArray(), rows.Select(r => r.Button).ToArray());
    }

    [Theory]
    [InlineData(AbilityLayer.Base)]
    [InlineData(AbilityLayer.Shoulder_LB)]
    [InlineData(AbilityLayer.Shoulder_RB)]
    [InlineData(AbilityLayer.Shoulder_LBRB)]
    public void Build_CoversAllButtons_ForEveryLayer(AbilityLayer layer)
    {
        var rows = ButtonLegend.Build(MakeProfile(), layer);
        // Ogni pulsante mappabile è presente esattamente una volta, in ogni layer.
        foreach (var b in ButtonLegend.ButtonOrder)
            Assert.Single(rows, r => r.Button == b);
    }

    [Fact]
    public void Build_MappedButton_ReflectsKeybind_PerLayer()
    {
        var profile = MakeProfile();

        var baseX = ButtonLegend.Build(profile, AbilityLayer.Base).Single(r => r.Button == ActionButton.X);
        Assert.True(baseX.IsMapped);
        Assert.Equal("1", baseX.KeybindLabel);

        var lbX = ButtonLegend.Build(profile, AbilityLayer.Shoulder_LB).Single(r => r.Button == ActionButton.X);
        Assert.Equal("Shift+1", lbX.KeybindLabel);

        var rbRt = ButtonLegend.Build(profile, AbilityLayer.Shoulder_RB).Single(r => r.Button == ActionButton.RightTrigger);
        Assert.Equal("Ctrl+5", rbRt.KeybindLabel);
    }

    // ---------------------------------------------------------------- pulsanti non mappati

    [Fact]
    public void Build_UnmappedButton_ShownAsDash_WhenIncluded()
    {
        var rows = ButtonLegend.Build(MakeProfile(), AbilityLayer.Base);
        var y = rows.Single(r => r.Button == ActionButton.Y);

        Assert.False(y.IsMapped);
        Assert.Equal("-", y.KeybindLabel);
        Assert.Equal("-", y.Display);
    }

    [Fact]
    public void Build_UnmappedButton_OmittedWhenIncludeUnmappedFalse()
    {
        var rows = ButtonLegend.Build(MakeProfile(), AbilityLayer.Base, includeUnmapped: false);

        Assert.All(rows, r => Assert.True(r.IsMapped));
        // Solo X e B sono mappati su Base.
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Button == ActionButton.X);
        Assert.Contains(rows, r => r.Button == ActionButton.B);
    }

    [Fact]
    public void Build_EmptyProfile_AllRowsUnmapped()
    {
        var rows = ButtonLegend.Build(new ControllerProfile(), AbilityLayer.Base);
        Assert.All(rows, r => Assert.False(r.IsMapped));
        Assert.Empty(ButtonLegend.Build(new ControllerProfile(), AbilityLayer.Base, includeUnmapped: false));
    }

    // ---------------------------------------------------------------- etichette

    [Fact]
    public void ButtonLabels_AreDistinctAndNonEmpty()
    {
        var labels = ButtonLegend.ButtonOrder.Select(ButtonLegend.ButtonLabel).ToArray();
        Assert.All(labels, l => Assert.False(string.IsNullOrWhiteSpace(l)));
        Assert.Equal(labels.Length, labels.Distinct().Count());
    }

    [Fact]
    public void Display_PrefersAbilityLabel_WhenPresent()
    {
        var row = new ButtonLegendRow(ActionButton.X, "X", new Keybind(ScanCode.D1), AbilityLabel: "Colpo Eroico");
        Assert.Equal("Colpo Eroico", row.Display);
        Assert.Equal("1", row.KeybindLabel); // il keybind resta disponibile
    }

    // ---------------------------------------------------------------- visibilità

    [Fact]
    public void ShouldShow_False_WhenDisabled()
    {
        Assert.False(ButtonLegend.ShouldShow(
            enabled: false, LegendVisibilityMode.AlwaysVisible, AbilityLayer.Shoulder_LB, cursorMode: false, paused: false));
    }

    [Fact]
    public void ShouldShow_False_WhenPausedOrCursor()
    {
        Assert.False(ButtonLegend.ShouldShow(true, LegendVisibilityMode.AlwaysVisible, AbilityLayer.Base, cursorMode: false, paused: true));
        Assert.False(ButtonLegend.ShouldShow(true, LegendVisibilityMode.AlwaysVisible, AbilityLayer.Base, cursorMode: true, paused: false));
    }

    [Fact]
    public void ShouldShow_AlwaysVisible_ShowsEvenOnBase()
    {
        Assert.True(ButtonLegend.ShouldShow(true, LegendVisibilityMode.AlwaysVisible, AbilityLayer.Base, cursorMode: false, paused: false));
    }

    [Theory]
    [InlineData(AbilityLayer.Base, false)]
    [InlineData(AbilityLayer.Shoulder_LB, true)]
    [InlineData(AbilityLayer.Shoulder_RB, true)]
    [InlineData(AbilityLayer.Shoulder_LBRB, true)]
    public void ShouldShow_WhileModifierHeld_OnlyWhenLayerNotBase(AbilityLayer layer, bool expected)
    {
        Assert.Equal(expected, ButtonLegend.ShouldShow(
            true, LegendVisibilityMode.WhileModifierHeld, layer, cursorMode: false, paused: false));
    }
}
