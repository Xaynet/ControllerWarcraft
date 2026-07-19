using System.Linq;
using ControllerWarcraft.Core.Input;
using ControllerWarcraft.Core.Profiles;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Test della logica <b>pura</b> dei modificatori di layer configurabili (<see cref="LayerModifiers"/>
/// / <see cref="ModifierSettings"/>): risoluzione del layer con priorità, default = comportamento
/// storico (LB/RB), conflitto quando un grilletto è usato come modificatore, ed etichette che
/// riflettono i pulsanti configurati.
/// </summary>
public class ModifiersTests
{
    // ---------------------------------------------------------------- default = comportamento storico

    [Fact]
    public void Default_Modifiers_AreLeftAndRightShoulder()
    {
        var mods = new ModifierSettings();
        Assert.Equal(ModifierButton.LeftShoulder, mods.Modifier1);
        Assert.Equal(ModifierButton.RightShoulder, mods.Modifier2);
    }

    [Fact]
    public void NewProfile_HasDefaultModifiers_LB_RB()
    {
        var p = new ControllerProfile();
        Assert.NotNull(p.Modifiers);
        Assert.Equal(ModifierButton.LeftShoulder, p.Modifiers.Modifier1);
        Assert.Equal(ModifierButton.RightShoulder, p.Modifiers.Modifier2);
    }

    // ---------------------------------------------------------------- risoluzione del layer (verità storica)

    [Theory]
    [InlineData(false, false, AbilityLayer.Base)]
    [InlineData(true, false, AbilityLayer.Shoulder_LB)]   // solo mod1
    [InlineData(false, true, AbilityLayer.Shoulder_RB)]   // solo mod2
    [InlineData(true, true, AbilityLayer.Shoulder_LBRB)]  // entrambi
    public void ResolveLayer_ReproducesHistoricTruthTable(bool m1, bool m2, AbilityLayer expected)
    {
        Assert.Equal(expected, LayerModifiers.ResolveLayer(m1, m2));
    }

    [Fact]
    public void ResolveLayer_Modifier1_HasPriorityOverModifier2()
    {
        // Con un solo modificatore alla volta la priorità non si nota; il caso "entrambi" dà LBRB
        // (che è coerente con mod1 > mod2 > Base). Verifichiamo che mod1 da solo non dia mai RB.
        Assert.Equal(AbilityLayer.Shoulder_LB, LayerModifiers.ResolveLayer(true, false));
        Assert.NotEqual(AbilityLayer.Shoulder_RB, LayerModifiers.ResolveLayer(true, false));
    }

    // ---------------------------------------------------------------- conflitto trigger-come-modificatore

    [Theory]
    [InlineData(ModifierButton.LeftShoulder, null)]
    [InlineData(ModifierButton.RightShoulder, null)]
    [InlineData(ModifierButton.LeftTrigger, ActionButton.LeftTrigger)]
    [InlineData(ModifierButton.RightTrigger, ActionButton.RightTrigger)]
    public void ConflictingAbility_MapsTriggersOnly(ModifierButton mod, ActionButton? expected)
    {
        Assert.Equal(expected, LayerModifiers.ConflictingAbility(mod));
    }

    [Fact]
    public void DefaultModifiers_DisableNoAbility()
    {
        var mods = new ModifierSettings(); // LB/RB
        Assert.Empty(LayerModifiers.DisabledAbilities(mods));
        Assert.False(LayerModifiers.IsAbilityDisabled(mods, ActionButton.LeftTrigger));
        Assert.False(LayerModifiers.IsAbilityDisabled(mods, ActionButton.RightTrigger));
        Assert.False(LayerModifiers.IsAbilityDisabled(mods, ActionButton.X));
    }

    [Fact]
    public void TriggerAsModifier_DisablesThatTriggerAbility()
    {
        var mods = new ModifierSettings { Modifier1 = ModifierButton.LeftTrigger, Modifier2 = ModifierButton.RightTrigger };

        Assert.True(LayerModifiers.IsAbilityDisabled(mods, ActionButton.LeftTrigger));
        Assert.True(LayerModifiers.IsAbilityDisabled(mods, ActionButton.RightTrigger));
        // I pulsanti frontali restano abilità normali.
        Assert.False(LayerModifiers.IsAbilityDisabled(mods, ActionButton.X));

        var disabled = LayerModifiers.DisabledAbilities(mods);
        Assert.Equal(2, disabled.Count);
        Assert.Contains(ActionButton.LeftTrigger, disabled);
        Assert.Contains(ActionButton.RightTrigger, disabled);
    }

    [Fact]
    public void OneTriggerModifier_DisablesOnlyThatTrigger()
    {
        // Config mista: LT come modificatore 1, RB come modificatore 2. Solo LT è disabilitato.
        var mods = new ModifierSettings { Modifier1 = ModifierButton.LeftTrigger, Modifier2 = ModifierButton.RightShoulder };

        Assert.True(LayerModifiers.IsAbilityDisabled(mods, ActionButton.LeftTrigger));
        Assert.False(LayerModifiers.IsAbilityDisabled(mods, ActionButton.RightTrigger));
        Assert.Single(LayerModifiers.DisabledAbilities(mods));
    }

    [Fact]
    public void SameTriggerOnBothModifiers_DisabledOnce()
    {
        var mods = new ModifierSettings { Modifier1 = ModifierButton.LeftTrigger, Modifier2 = ModifierButton.LeftTrigger };
        var disabled = LayerModifiers.DisabledAbilities(mods);
        Assert.Single(disabled);
        Assert.Equal(ActionButton.LeftTrigger, disabled[0]);
    }

    // ---------------------------------------------------------------- config ambigua

    [Fact]
    public void AreAmbiguous_TrueWhenBothModifiersEqual()
    {
        Assert.True(LayerModifiers.AreAmbiguous(new ModifierSettings { Modifier1 = ModifierButton.LeftShoulder, Modifier2 = ModifierButton.LeftShoulder }));
        Assert.False(LayerModifiers.AreAmbiguous(new ModifierSettings())); // default LB/RB
    }

    // ---------------------------------------------------------------- etichette coerenti coi pulsanti

    [Fact]
    public void LayerLabel_DefaultModifiers_MatchHistoricLabels()
    {
        var mods = new ModifierSettings(); // LB/RB
        Assert.Equal("BASE (1-9)", LayerModifiers.LayerLabel(AbilityLayer.Base, mods));
        Assert.Equal("+LB (Shift)", LayerModifiers.LayerLabel(AbilityLayer.Shoulder_LB, mods));
        Assert.Equal("+RB (Ctrl)", LayerModifiers.LayerLabel(AbilityLayer.Shoulder_RB, mods));
        Assert.Equal("+LB+RB (Shift+Ctrl)", LayerModifiers.LayerLabel(AbilityLayer.Shoulder_LBRB, mods));
    }

    [Fact]
    public void LayerLabel_TriggerModifiers_ReflectConfiguredButtons()
    {
        var mods = new ModifierSettings { Modifier1 = ModifierButton.LeftTrigger, Modifier2 = ModifierButton.RightTrigger };
        Assert.Equal("+LT (Shift)", LayerModifiers.LayerLabel(AbilityLayer.Shoulder_LB, mods));
        Assert.Equal("+RT (Ctrl)", LayerModifiers.LayerLabel(AbilityLayer.Shoulder_RB, mods));
        Assert.Equal("+LT+RT (Shift+Ctrl)", LayerModifiers.LayerLabel(AbilityLayer.Shoulder_LBRB, mods));
    }

    [Theory]
    [InlineData(ModifierButton.LeftShoulder, "LB")]
    [InlineData(ModifierButton.RightShoulder, "RB")]
    [InlineData(ModifierButton.LeftTrigger, "LT")]
    [InlineData(ModifierButton.RightTrigger, "RT")]
    public void ShortLabel_IsStable(ModifierButton mod, string expected)
    {
        Assert.Equal(expected, LayerModifiers.ShortLabel(mod));
    }

    // ---------------------------------------------------------------- interazione con la button-legend

    [Fact]
    public void ButtonLegend_OmitsTrigger_WhenUsedAsModifier()
    {
        var p = new ControllerProfile { Name = "T" };
        p.Modifiers.Modifier1 = ModifierButton.LeftTrigger; // LT diventa modificatore
        // Anche se il profilo avesse un binding su LT, non deve comparire in legenda.
        p.Abilities.Add(new AbilityBinding(ActionButton.LeftTrigger, AbilityLayer.Base, new Keybind(ScanCode.D3)));
        p.Abilities.Add(new AbilityBinding(ActionButton.RightTrigger, AbilityLayer.Base, new Keybind(ScanCode.D4)));

        var rows = ButtonLegend.Build(p, AbilityLayer.Base);

        Assert.DoesNotContain(rows, r => r.Button == ActionButton.LeftTrigger);
        // RT non è modificatore qui: resta una normale abilità in legenda.
        Assert.Contains(rows, r => r.Button == ActionButton.RightTrigger);
    }

    [Fact]
    public void ButtonLegend_DefaultModifiers_KeepAllButtons()
    {
        // Col default LB/RB nessun grilletto è consumato: la legenda copre tutti i pulsanti.
        var rows = ButtonLegend.Build(new ControllerProfile(), AbilityLayer.Base);
        Assert.Equal(ButtonLegend.ButtonOrder.Count, rows.Count);
    }
}
