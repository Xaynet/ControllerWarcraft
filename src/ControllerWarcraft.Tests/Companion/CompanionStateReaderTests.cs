using ControllerWarcraft.Core.Companion;
using Xunit;

namespace ControllerWarcraft.Tests.Companion;

/// <summary>
/// Test del parser tollerante dei SavedVariables Lua (<see cref="CompanionStateReader.TryParse"/>):
/// parsing valido di numeri/bool/stringhe, input malformato/vuoto ⇒ null senza eccezioni, chiavi
/// sconosciute ignorate.
/// </summary>
public class CompanionStateReaderTests
{
    private const string ValidSaved = """
        ControllerWarcraftCompanionDB = {
            ["targetExists"] = true,
            ["targetName"] = "Hogger",
            ["targetIsEnemy"] = true,
            ["targetHealthPct"] = 73.5,
            ["inCombat"] = false,
            ["playerHealthPct"] = 100,
            ["playerPowerPct"] = 42,
            ["gameVersion"] = "Classic",
            ["addonVersion"] = "1.0.3",
            ["updated"] = 1721000000,
        }
        """;

    // ---------------------------------------------------------------- parsing valido

    [Fact]
    public void TryParse_ValidContent_ReturnsTrue_AndPopulatesAllFields()
    {
        bool ok = CompanionStateReader.TryParse(ValidSaved, out var state);

        Assert.True(ok);
        Assert.NotNull(state);
        Assert.True(state!.TargetExists);
        Assert.Equal("Hogger", state.TargetName);
        Assert.True(state.TargetIsEnemy);
        Assert.Equal(73.5, state.TargetHealthPct);
        Assert.False(state.InCombat);
        Assert.Equal(100, state.PlayerHealthPct);
        Assert.Equal(42, state.PlayerPowerPct);
        Assert.Equal("Classic", state.GameVersion);
        Assert.Equal("1.0.3", state.AddonVersion);
        Assert.Equal(1721000000, state.Updated);
    }

    [Fact]
    public void TryParse_ShortLabel_ReflectsTarget()
    {
        CompanionStateReader.TryParse(ValidSaved, out var state);
        Assert.Equal("Target: Hogger (74%)", state!.ShortLabel);
    }

    // ---------------------------------------------------------------- tipi

    [Fact]
    public void TryParse_NegativeAndDecimalNumbers()
    {
        const string lua = """
            db = {
                ["targetHealthPct"] = -12.25,
                ["updated"] = 0,
            }
            """;
        bool ok = CompanionStateReader.TryParse(lua, out var state);
        Assert.True(ok);
        Assert.Equal(-12.25, state!.TargetHealthPct);
        Assert.Equal(0, state.Updated);
    }

    [Fact]
    public void TryParse_BoolFalse_ParsedCorrectly()
    {
        CompanionStateReader.TryParse("""t = { ["targetExists"] = false }""", out var state);
        Assert.False(state!.TargetExists);
    }

    [Fact]
    public void TryParse_EscapedQuotesInString()
    {
        const string lua = """t = { ["targetName"] = "Say \"Hi\"" }""";
        bool ok = CompanionStateReader.TryParse(lua, out var state);
        Assert.True(ok);
        Assert.Equal("Say \"Hi\"", state!.TargetName);
    }

    // ---------------------------------------------------------------- chiavi sconosciute ignorate

    [Fact]
    public void TryParse_UnknownKeys_AreIgnored_ButKnownOnesParsed()
    {
        const string lua = """
            db = {
                ["someFutureField"] = 999,
                ["targetName"] = "Ragnaros",
                ["nestedGarbage"] = "ignored",
            }
            """;
        bool ok = CompanionStateReader.TryParse(lua, out var state);
        Assert.True(ok);
        Assert.Equal("Ragnaros", state!.TargetName);
    }

    // ---------------------------------------------------------------- input malformato/vuoto ⇒ null

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void TryParse_EmptyOrWhitespace_ReturnsFalse_AndNull(string content)
    {
        bool ok = CompanionStateReader.TryParse(content, out var state);
        Assert.False(ok);
        Assert.Null(state);
    }

    [Fact]
    public void TryParse_NoRecognizablePairs_ReturnsFalse()
    {
        // Lua sintatticamente "roba" ma senza coppie ["k"] = v.
        bool ok = CompanionStateReader.TryParse("this is not a saved variables file {} = 42", out var state);
        Assert.False(ok);
        Assert.Null(state);
    }

    [Fact]
    public void TryParse_MalformedButHasOnePair_ParsesTolerantly()
    {
        // Parentesi non bilanciate ecc.: il parser è tollerante, estrae ciò che riconosce.
        const string lua = """garbage ["targetName"] = "Onyxia" more garbage {{{ """;
        bool ok = CompanionStateReader.TryParse(lua, out var state);
        Assert.True(ok);
        Assert.Equal("Onyxia", state!.TargetName);
    }

    [Fact]
    public void TryParse_DoesNotThrow_OnArbitraryInput()
    {
        // Non deve mai lanciare, qualunque input.
        var inputs = new[]
        {
            "]]]][[[[",
            "[\"k\"] =",           // valore mancante
            "[\"\"] = 1",          // chiave vuota (non tra le note)
            new string('x', 10000),
        };
        foreach (var input in inputs)
        {
            var ex = Record.Exception(() => CompanionStateReader.TryParse(input, out _));
            Assert.Null(ex);
        }
    }

    // ---------------------------------------------------------------- TryRead da file

    [Fact]
    public void TryRead_NonExistentPath_ReturnsFalse()
    {
        bool ok = CompanionStateReader.TryRead(@"Z:\does\not\exist\companion.lua", out var state);
        Assert.False(ok);
        Assert.Null(state);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryRead_NullOrEmptyPath_ReturnsFalse(string? path)
    {
        bool ok = CompanionStateReader.TryRead(path, out var state);
        Assert.False(ok);
        Assert.Null(state);
    }

    [Fact]
    public void TryRead_ValidFile_RoundTrips()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "cwc_companion_" + Guid.NewGuid().ToString("N") + ".lua");
        try
        {
            File.WriteAllText(tmp, ValidSaved);
            bool ok = CompanionStateReader.TryRead(tmp, out var state);
            Assert.True(ok);
            Assert.Equal("Hogger", state!.TargetName);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
