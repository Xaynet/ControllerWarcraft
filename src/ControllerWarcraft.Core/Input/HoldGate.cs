namespace ControllerWarcraft.Core.Input;

/// <summary>
/// Debounce "hold minimo" per un pulsante digitale (hardening input): una pressione conta come
/// valida solo dopo che il pulsante è stato tenuto premuto per almeno <c>minHoldMs</c> millisecondi
/// continui. Serve a scartare le pressioni <b>accidentali</b> e troppo brevi dei click-stick
/// (L3/R3), facili da premere per sbaglio mentre si inclina la levetta.
///
/// È una logica <b>pura e testabile</b> (nessun accesso a orologio o I/O): il tempo trascorso dal
/// tick precedente (<c>dtMs</c>) viene passato dall'esterno, così l'engine resta deterministico.
/// Non introduce alcuna automazione: rimappa soltanto <i>quando</i> l'input dell'utente in quel
/// tick viene considerato — resta rigorosamente 1:1 (ANALISI §8).
///
/// Retro-compatibilità: con <c>minHoldMs = 0</c> (default) il comportamento coincide esattamente
/// con l'edge-detection classico — la pressione qualifica subito, sul primo tick in cui il
/// pulsante risulta premuto.
/// </summary>
public struct HoldGate
{
    private bool _wasQualified; // il pulsante era già "qualificato" al tick precedente
    private double _heldMs;     // ms di pressione continua accumulati

    /// <summary>
    /// true nel <b>singolo</b> tick in cui la pressione raggiunge la soglia (fronte di salita
    /// "premuto qualificato"). Con soglia 0 coincide col classico fronte di pressione.
    /// </summary>
    public bool PressedEdge { get; private set; }

    /// <summary>true finché il pulsante è premuto e ha già superato la soglia di hold minimo.</summary>
    public bool Held { get; private set; }

    /// <summary>
    /// true nel singolo tick in cui una pressione <b>qualificata</b> viene rilasciata. Una
    /// pressione scartata perché troppo breve (rilasciata prima della soglia) non genera questo
    /// fronte: non è mai "accaduta" ai fini logici.
    /// </summary>
    public bool ReleasedEdge { get; private set; }

    /// <summary>
    /// Aggiorna lo stato del gate con lo stato grezzo del pulsante (<paramref name="rawHeld"/>) e
    /// il tempo trascorso dall'ultimo tick (<paramref name="dtMs"/>), data la soglia
    /// <paramref name="minHoldMs"/> (valori &lt;= 0 = disattiva la soglia).
    /// </summary>
    public void Update(bool rawHeld, double dtMs, double minHoldMs)
    {
        if (rawHeld)
        {
            _heldMs += dtMs;
            bool qualified = _heldMs >= minHoldMs; // con minHoldMs<=0 è vero già al primo tick
            PressedEdge = qualified && !_wasQualified;
            ReleasedEdge = false;
            Held = qualified;
            _wasQualified = qualified;
        }
        else
        {
            ReleasedEdge = _wasQualified;
            PressedEdge = false;
            Held = false;
            _heldMs = 0;
            _wasQualified = false;
        }
    }

    /// <summary>Azzera il gate (a disconnessione/uscita o cambio profilo).</summary>
    public void Reset()
    {
        _wasQualified = false;
        _heldMs = 0;
        PressedEdge = false;
        Held = false;
        ReleasedEdge = false;
    }
}
