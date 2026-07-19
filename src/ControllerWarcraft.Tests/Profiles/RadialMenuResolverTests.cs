using ControllerWarcraft.Core.Profiles;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Test della geometria pura di selezione del radial menu (<see cref="RadialMenuResolver"/>).
/// Convenzione: voce 0 in alto (12 in punto), poi in senso orario; assi X destra+, Y su+.
/// </summary>
public class RadialMenuResolverTests
{
    // ---------------------------------------------------------------- direzioni cardinali (4 voci)

    [Theory]
    [InlineData(0.0, 1.0, 0)]   // alto     -> voce 0
    [InlineData(1.0, 0.0, 1)]   // destra   -> voce 1
    [InlineData(0.0, -1.0, 2)]  // basso    -> voce 2
    [InlineData(-1.0, 0.0, 3)]  // sinistra -> voce 3
    public void Resolve_FourItems_CardinalDirections(double x, double y, int expected)
    {
        int idx = RadialMenuResolver.Resolve(count: 4, x, y, selectDeadzone: 0.4);
        Assert.Equal(expected, idx);
    }

    // ---------------------------------------------------------------- otto voci

    [Theory]
    [InlineData(0.0, 1.0, 0)]    // alto
    [InlineData(1.0, 1.0, 1)]    // alto-destra (45°)
    [InlineData(1.0, 0.0, 2)]    // destra (90°)
    [InlineData(1.0, -1.0, 3)]   // basso-destra (135°)
    [InlineData(0.0, -1.0, 4)]   // basso (180°)
    [InlineData(-1.0, -1.0, 5)]  // basso-sinistra (225°)
    [InlineData(-1.0, 0.0, 6)]   // sinistra (270°)
    [InlineData(-1.0, 1.0, 7)]   // alto-sinistra (315°)
    public void Resolve_EightItems_DiagonalsAndCardinals(double x, double y, int expected)
    {
        int idx = RadialMenuResolver.Resolve(count: 8, x, y, selectDeadzone: 0.4);
        Assert.Equal(expected, idx);
    }

    // ---------------------------------------------------------------- zona morta centrale

    [Fact]
    public void Resolve_CenterInsideDeadzone_ReturnsMinusOne()
    {
        Assert.Equal(-1, RadialMenuResolver.Resolve(4, 0.0, 0.0, 0.4));
    }

    [Fact]
    public void Resolve_MagnitudeBelowDeadzone_ReturnsMinusOne_EvenWithDirection()
    {
        // Direzione chiara (alto) ma magnitudine 0.2 < deadzone 0.4 => nessuna selezione.
        Assert.Equal(-1, RadialMenuResolver.Resolve(4, 0.0, 0.2, 0.4));
    }

    [Fact]
    public void Resolve_MagnitudeAtOrAboveDeadzone_Selects()
    {
        // Magnitudine 0.5 > deadzone 0.4 => seleziona la voce in alto.
        Assert.Equal(0, RadialMenuResolver.Resolve(4, 0.0, 0.5, 0.4));
    }

    [Fact]
    public void Resolve_ZeroDeadzone_TinyMagnitudeStillSelects()
    {
        Assert.Equal(0, RadialMenuResolver.Resolve(4, 0.0, 0.01, selectDeadzone: 0.0));
    }

    // ---------------------------------------------------------------- count degenere

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_NonPositiveCount_ReturnsMinusOne(int count)
    {
        Assert.Equal(-1, RadialMenuResolver.Resolve(count, 1.0, 0.0, 0.4));
    }

    [Fact]
    public void Resolve_SingleItem_AlwaysSelectsIndexZero()
    {
        Assert.Equal(0, RadialMenuResolver.Resolve(1, 1.0, 0.0, 0.4));
        Assert.Equal(0, RadialMenuResolver.Resolve(1, -1.0, -1.0, 0.4));
    }

    // ---------------------------------------------------------------- arrotondamento al settore vicino

    [Fact]
    public void Resolve_NearTop_RoundsToItemZero()
    {
        // Leggermente a destra dell'alto ma ben dentro il settore di voce 0.
        Assert.Equal(0, RadialMenuResolver.Resolve(4, 0.2, 1.0, 0.4));
        // Leggermente a sinistra dell'alto.
        Assert.Equal(0, RadialMenuResolver.Resolve(4, -0.2, 1.0, 0.4));
    }

    [Fact]
    public void Resolve_IndexAlwaysInRange()
    {
        // Campiona molti angoli: l'indice deve sempre stare in [0..count-1].
        for (int deg = 0; deg < 360; deg += 5)
        {
            double rad = deg * Math.PI / 180.0;
            double x = Math.Sin(rad); // convenzione oraria dall'alto
            double y = Math.Cos(rad);
            int idx = RadialMenuResolver.Resolve(6, x, y, 0.4);
            Assert.InRange(idx, 0, 5);
        }
    }

    // ---------------------------------------------------------------- deadzone fuori range viene clampata

    [Fact]
    public void Resolve_DeadzoneAboveClamp_StillAllowsFullStick()
    {
        // deadzone 2.0 viene clampata a 0.99: uno stick pieno (magnitudine 1.0) supera comunque.
        Assert.Equal(0, RadialMenuResolver.Resolve(4, 0.0, 1.0, selectDeadzone: 2.0));
    }

    // ---------------------------------------------------------------- SectorCenterAngle

    [Fact]
    public void SectorCenterAngle_ItemZero_IsTop()
    {
        Assert.Equal(0.0, RadialMenuResolver.SectorCenterAngle(0, 4), 6);
    }

    [Fact]
    public void SectorCenterAngle_ProgressesClockwise()
    {
        Assert.Equal(Math.PI / 2, RadialMenuResolver.SectorCenterAngle(1, 4), 6);
        Assert.Equal(Math.PI, RadialMenuResolver.SectorCenterAngle(2, 4), 6);
        Assert.Equal(3 * Math.PI / 2, RadialMenuResolver.SectorCenterAngle(3, 4), 6);
    }

    [Fact]
    public void SectorCenterAngle_NonPositiveCount_ReturnsZero()
    {
        Assert.Equal(0.0, RadialMenuResolver.SectorCenterAngle(2, 0));
    }

    // ---------------------------------------------------------------- coerenza resolver <-> center angle

    [Fact]
    public void Resolve_AtSectorCenter_ReturnsThatIndex()
    {
        const int count = 6;
        for (int i = 0; i < count; i++)
        {
            double a = RadialMenuResolver.SectorCenterAngle(i, count);
            double x = Math.Sin(a);
            double y = Math.Cos(a);
            Assert.Equal(i, RadialMenuResolver.Resolve(count, x, y, 0.4));
        }
    }
}
