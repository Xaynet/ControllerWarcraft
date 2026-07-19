using ControllerWarcraft.Core.Profiles;
using Xunit;

namespace ControllerWarcraft.Tests.Profiles;

/// <summary>
/// Test delle curve di sensibilità (<see cref="ResponseCurve"/>): estremi (0→0, 1→1), monotonicità,
/// clamp fuori [0..1], preservazione del segno, e i tre tipi di curva (Linear/Power/Exponential).
/// </summary>
public class ResponseCurveTests
{
    private static ResponseCurve Linear() => new() { Type = CurveType.Linear };
    private static ResponseCurve Power(double e) => new() { Type = CurveType.Power, Exponent = e };
    private static ResponseCurve Expo(double k) => new() { Type = CurveType.Exponential, Exponent = k };

    // ---------------------------------------------------------------- estremi comuni a tutte le curve

    [Theory]
    [InlineData(CurveType.Linear, 1.0)]
    [InlineData(CurveType.Power, 2.0)]
    [InlineData(CurveType.Power, 0.5)]
    [InlineData(CurveType.Exponential, 3.0)]
    public void Shape_Endpoints_AreZeroAndOne(CurveType type, double exponent)
    {
        var c = new ResponseCurve { Type = type, Exponent = exponent };
        Assert.Equal(0.0, c.Shape(0.0), 9);
        Assert.Equal(1.0, c.Shape(1.0), 9);
    }

    // ---------------------------------------------------------------- Linear = identità

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void Linear_Shape_IsIdentity(double m)
    {
        Assert.Equal(m, Linear().Shape(m), 9);
    }

    // ---------------------------------------------------------------- clamp fuori [0..1]

    [Fact]
    public void Shape_ClampsAboveOne()
    {
        Assert.Equal(1.0, Linear().Shape(1.5), 9);
        Assert.Equal(1.0, Power(2).Shape(3.0), 9);
    }

    [Fact]
    public void Shape_UsesAbsoluteValue_ForNegativeMagnitude()
    {
        Assert.Equal(0.5, Linear().Shape(-0.5), 9);
        Assert.Equal(Power(2).Shape(0.5), Power(2).Shape(-0.5), 9);
    }

    // ---------------------------------------------------------------- Power

    [Fact]
    public void Power_ExponentGreaterThanOne_GivesFinerControlAtCenter()
    {
        // Con exponent 2, il valore a metà corsa è più basso del lineare (più precisione al centro).
        double v = Power(2).Shape(0.5);
        Assert.Equal(0.25, v, 9);
        Assert.True(v < 0.5);
    }

    [Fact]
    public void Power_ExponentZeroOrNegative_FallsBackToLinear()
    {
        Assert.Equal(0.5, Power(0).Shape(0.5), 9);
        Assert.Equal(0.5, Power(-3).Shape(0.5), 9);
    }

    // ---------------------------------------------------------------- Exponential

    [Fact]
    public void Exponential_IsBelowLinear_InInterior()
    {
        // La curva esponenziale normalizzata (aim assist) sta sotto la diagonale per k>0.
        double v = Expo(3).Shape(0.5);
        Assert.True(v < 0.5, $"atteso < 0.5, ottenuto {v}");
        Assert.True(v > 0.0);
    }

    [Fact]
    public void Exponential_SmallK_ApproximatesLinear()
    {
        // Per k→0 tende alla lineare.
        Assert.Equal(0.5, Expo(0.00001).Shape(0.5), 6);
    }

    // ---------------------------------------------------------------- monotonicità

    [Theory]
    [InlineData(CurveType.Linear, 1.0)]
    [InlineData(CurveType.Power, 2.0)]
    [InlineData(CurveType.Power, 0.5)]
    [InlineData(CurveType.Exponential, 4.0)]
    public void Shape_IsMonotonicNonDecreasing(CurveType type, double exponent)
    {
        var c = new ResponseCurve { Type = type, Exponent = exponent };
        double prev = c.Shape(0.0);
        for (int i = 1; i <= 100; i++)
        {
            double m = i / 100.0;
            double cur = c.Shape(m);
            Assert.True(cur >= prev - 1e-12, $"non monotona a m={m}: {cur} < {prev}");
            prev = cur;
        }
    }

    [Theory]
    [InlineData(CurveType.Linear, 1.0)]
    [InlineData(CurveType.Power, 2.0)]
    [InlineData(CurveType.Exponential, 4.0)]
    public void Shape_StaysWithinUnitRange(CurveType type, double exponent)
    {
        var c = new ResponseCurve { Type = type, Exponent = exponent };
        for (int i = 0; i <= 100; i++)
        {
            double v = c.Shape(i / 100.0);
            Assert.InRange(v, 0.0, 1.0);
        }
    }

    // ---------------------------------------------------------------- Apply preserva il segno

    [Fact]
    public void Apply_PreservesSign()
    {
        var c = Power(2);
        Assert.True(c.Apply(0.5) > 0);
        Assert.True(c.Apply(-0.5) < 0);
        Assert.Equal(0.0, c.Apply(0.0), 9);
    }

    [Fact]
    public void Apply_IsOddSymmetric()
    {
        var c = Power(2);
        Assert.Equal(-c.Apply(0.7), c.Apply(-0.7), 9);
    }

    // ---------------------------------------------------------------- Clone

    [Fact]
    public void Clone_IsDeepCopy_WithSameValues()
    {
        var original = Power(2.5);
        var clone = original.Clone;

        Assert.NotSame(original, clone);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.Exponent, clone.Exponent);

        clone.Exponent = 9.0;
        Assert.Equal(2.5, original.Exponent); // modificare il clone non tocca l'originale
    }
}
