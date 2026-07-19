using ControllerWarcraft.Core.Input;
using Xunit;

namespace ControllerWarcraft.Tests.Input;

/// <summary>
/// Test del debounce "hold minimo" (<see cref="HoldGate"/>): sotto/sopra soglia, soglia 0 =
/// comportamento storico (edge-detection classico), rilasci qualificati vs scartati, timing.
/// </summary>
public class HoldGateTests
{
    // ---------------------------------------------------------------- soglia 0 = comportamento storico

    [Fact]
    public void ThresholdZero_PressedEdge_OnFirstHeldTick()
    {
        var gate = new HoldGate();
        gate.Update(rawHeld: true, dtMs: 16, minHoldMs: 0);

        Assert.True(gate.PressedEdge);
        Assert.True(gate.Held);
        Assert.False(gate.ReleasedEdge);
    }

    [Fact]
    public void ThresholdZero_PressedEdge_OnlyOnce_WhileHeld()
    {
        var gate = new HoldGate();
        gate.Update(true, 16, 0);
        Assert.True(gate.PressedEdge);

        // Tick successivi con pulsante ancora premuto: nessun nuovo fronte di salita.
        gate.Update(true, 16, 0);
        Assert.False(gate.PressedEdge);
        Assert.True(gate.Held);

        gate.Update(true, 16, 0);
        Assert.False(gate.PressedEdge);
        Assert.True(gate.Held);
    }

    [Fact]
    public void ThresholdZero_ReleasedEdge_OnRelease()
    {
        var gate = new HoldGate();
        gate.Update(true, 16, 0);
        gate.Update(false, 16, 0);

        Assert.True(gate.ReleasedEdge);
        Assert.False(gate.Held);
        Assert.False(gate.PressedEdge);
    }

    [Fact]
    public void NegativeThreshold_BehavesLikeZero()
    {
        var gate = new HoldGate();
        gate.Update(true, 1, minHoldMs: -50);
        Assert.True(gate.PressedEdge);
        Assert.True(gate.Held);
    }

    // ---------------------------------------------------------------- sotto soglia

    [Fact]
    public void UnderThreshold_NotYetQualified()
    {
        var gate = new HoldGate();
        gate.Update(true, dtMs: 30, minHoldMs: 100);

        Assert.False(gate.PressedEdge);
        Assert.False(gate.Held);
        Assert.False(gate.ReleasedEdge);
    }

    [Fact]
    public void ShortPress_ReleasedBeforeThreshold_ProducesNoEdges()
    {
        var gate = new HoldGate();
        // Premuto 30ms poi 30ms = 60ms totali, sotto la soglia di 100ms.
        gate.Update(true, 30, 100);
        gate.Update(true, 30, 100);
        Assert.False(gate.Held);

        // Rilascio: la pressione era troppo breve, quindi NON conta come "accaduta".
        gate.Update(false, 30, 100);
        Assert.False(gate.ReleasedEdge);
        Assert.False(gate.PressedEdge);
        Assert.False(gate.Held);
    }

    // ---------------------------------------------------------------- sopra soglia

    [Fact]
    public void HeldPastThreshold_QualifiesOnceThenHolds()
    {
        var gate = new HoldGate();
        gate.Update(true, 40, 100); // 40  -> non qualificato
        Assert.False(gate.PressedEdge);
        gate.Update(true, 40, 100); // 80  -> non qualificato
        Assert.False(gate.PressedEdge);
        gate.Update(true, 40, 100); // 120 -> raggiunge la soglia: fronte di salita
        Assert.True(gate.PressedEdge);
        Assert.True(gate.Held);
        gate.Update(true, 40, 100); // 160 -> ancora premuto, nessun nuovo fronte
        Assert.False(gate.PressedEdge);
        Assert.True(gate.Held);
    }

    [Fact]
    public void ExactThreshold_Qualifies()
    {
        var gate = new HoldGate();
        gate.Update(true, 100, 100); // _heldMs (100) >= minHoldMs (100)
        Assert.True(gate.PressedEdge);
        Assert.True(gate.Held);
    }

    [Fact]
    public void QualifiedPress_ThenRelease_ProducesReleasedEdge()
    {
        var gate = new HoldGate();
        gate.Update(true, 120, 100); // qualificato
        Assert.True(gate.Held);

        gate.Update(false, 16, 100);
        Assert.True(gate.ReleasedEdge);
        Assert.False(gate.Held);
    }

    // ---------------------------------------------------------------- riqualificazione dopo rilascio

    [Fact]
    public void HeldMs_ResetsAfterRelease_RequiringFullHoldAgain()
    {
        var gate = new HoldGate();
        gate.Update(true, 120, 100);  // qualificato
        gate.Update(false, 16, 100);  // rilascio -> reset del contatore

        // Nuova pressione breve: deve ripartire da zero, non riqualificare subito.
        gate.Update(true, 40, 100);
        Assert.False(gate.PressedEdge);
        Assert.False(gate.Held);

        gate.Update(true, 80, 100); // 120 totali nella nuova pressione
        Assert.True(gate.PressedEdge);
    }

    // ---------------------------------------------------------------- reset

    [Fact]
    public void Reset_ClearsAllState()
    {
        var gate = new HoldGate();
        gate.Update(true, 200, 100);
        Assert.True(gate.Held);

        gate.Reset();
        Assert.False(gate.Held);
        Assert.False(gate.PressedEdge);
        Assert.False(gate.ReleasedEdge);

        // Dopo il reset serve di nuovo l'intero hold.
        gate.Update(true, 40, 100);
        Assert.False(gate.PressedEdge);
    }

    // ---------------------------------------------------------------- release senza pressione precedente

    [Fact]
    public void ReleaseWithoutPriorPress_NoReleasedEdge()
    {
        var gate = new HoldGate();
        gate.Update(false, 16, 100);
        Assert.False(gate.ReleasedEdge);
        Assert.False(gate.Held);
        Assert.False(gate.PressedEdge);
    }
}
