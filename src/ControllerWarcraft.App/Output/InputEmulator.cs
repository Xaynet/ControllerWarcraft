using ControllerWarcraft.App.Native;
using ControllerWarcraft.App.Profiles;

namespace ControllerWarcraft.App.Output;

using SC = NativeMethods.ScanCode;

/// <summary>
/// Output Emulator (ANALISI §5): unico punto che inietta tastiera/mouse via SendInput.
/// Tiene traccia dei tasti/pulsanti attualmente premuti per garantire press/release puliti
/// e un <see cref="ReleaseAll"/> affidabile all'uscita o al cambio modalita'.
/// Non contiene alcuna logica di mapping: riceve intenti gia' decisi dal MappingEngine.
/// </summary>
public sealed class InputEmulator
{
    // Tasti mantenuti premuti in modo continuo (es. WASD). Chiave = scancode fisico.
    private readonly HashSet<SC> _heldKeys = new();
    private bool _rmbHeld;
    private bool _lmbHeld;

    // ---------- Tasti continui (movimento) ----------

    /// <summary>Porta un tasto allo stato voluto (premuto/rilasciato) evitando ripetizioni.</summary>
    public void HoldKey(SC sc, bool wanted)
    {
        if (wanted)
        {
            if (_heldKeys.Add(sc)) NativeMethods.KeyDown(sc);
        }
        else if (_heldKeys.Remove(sc))
        {
            NativeMethods.KeyUp(sc);
        }
    }

    // ---------- Abilita' (tap con eventuali modificatori) ----------

    /// <summary>
    /// Esegue un tap di keybind: preme i modificatori, batte il tasto, rilascia i modificatori.
    /// Mapping rigorosamente 1:1 (un tap fisico -> un tap di gioco): nessuna ripetizione/automazione.
    /// </summary>
    public void TapKeybind(in Keybind kb)
    {
        if (kb.IsNone) return;

        if (kb.Shift) NativeMethods.KeyDown(SC.LeftShift);
        if (kb.Ctrl) NativeMethods.KeyDown(SC.LeftControl);
        if (kb.Alt) NativeMethods.KeyDown(SC.LeftAlt);

        NativeMethods.KeyDown(kb.Key);
        NativeMethods.KeyUp(kb.Key);

        if (kb.Alt) NativeMethods.KeyUp(SC.LeftAlt);
        if (kb.Ctrl) NativeMethods.KeyUp(SC.LeftControl);
        if (kb.Shift) NativeMethods.KeyUp(SC.LeftShift);
    }

    // ---------- Mouse: mouselook (RMB tenuto premuto) ----------

    public void SetRightMouseHeld(bool wanted)
    {
        if (wanted && !_rmbHeld) { NativeMethods.RightMouseDown(); _rmbHeld = true; }
        else if (!wanted && _rmbHeld) { NativeMethods.RightMouseUp(); _rmbHeld = false; }
    }

    public void MouseMove(int dx, int dy) => NativeMethods.MouseMoveRelative(dx, dy);

    // ---------- Mouse: click modalita' cursore ----------

    /// <summary>Click sinistro come pressione mantenuta (permette drag & drop, es. talenti/inventario).</summary>
    public void SetLeftMouseHeld(bool wanted)
    {
        if (wanted && !_lmbHeld) { NativeMethods.LeftMouseDown(); _lmbHeld = true; }
        else if (!wanted && _lmbHeld) { NativeMethods.LeftMouseUp(); _lmbHeld = false; }
    }

    /// <summary>Click destro istantaneo (down+up). Usato in modalita' cursore.</summary>
    public void RightClick()
    {
        NativeMethods.RightMouseDown();
        NativeMethods.RightMouseUp();
    }

    // ---------- Pulizia ----------

    /// <summary>Rilascia ogni tasto/pulsante ancora premuto. Idempotente e sicuro a fine loop.</summary>
    public void ReleaseAll()
    {
        foreach (var sc in _heldKeys) NativeMethods.KeyUp(sc);
        _heldKeys.Clear();

        SetRightMouseHeld(false);
        SetLeftMouseHeld(false);
    }
}
