using System.Text.Json.Serialization;

namespace ControllerWarcraft.Core.Profiles;

/// <summary>Forma della curva di risposta applicata all'ampiezza di un asse analogico.</summary>
public enum CurveType
{
    /// <summary>Risposta 1:1 (nessuna curva). Comportamento storico (Fase 1/2).</summary>
    Linear,

    /// <summary>Potenza: <c>y = x^exponent</c>. Con exponent &gt; 1 dà controllo fine al centro e piena velocità a fondo corsa.</summary>
    Power,

    /// <summary>Esponenziale normalizzata: accelerazione più marcata verso fondo corsa (curva "aim assist").</summary>
    Exponential,
}

/// <summary>
/// Curva di sensibilità (ANALISI §5 "Curve deadzone/sensibilità", Fase 3): trasforma l'ampiezza
/// normalizzata di un asse (0..1) in un fattore di velocità (0..1) <b>prima</b> di moltiplicarlo
/// per la sensibilità. Serve a rendere il mouselook più preciso ai piccoli spostamenti dello stick
/// e più rapido a fondo corsa, senza cambiare la velocità massima.
///
/// È solo una rimappatura matematica dell'input analogico dell'utente in quel tick: non introduce
/// alcuna automazione, ripetizione o memoria di stato — resta rigorosamente 1:1 (ANALISI §8).
///
/// Retro-compatibile: <see cref="CurveType.Linear"/> (default) riproduce esattamente il
/// comportamento lineare precedente, quindi un profilo JSON senza il campo <c>curve</c> è identico
/// a prima.
/// </summary>
public sealed class ResponseCurve : ObservableModel
{
    private CurveType _type = CurveType.Linear;
    private double _exponent = 1.5;

    /// <summary>Tipo di curva. Default <see cref="CurveType.Linear"/> (nessuna alterazione).</summary>
    public CurveType Type { get => _type; set => SetField(ref _type, value); }

    /// <summary>
    /// Parametro di intensità della curva:
    /// per <see cref="CurveType.Power"/> è l'esponente (1 = lineare, &gt;1 = più preciso al centro);
    /// per <see cref="CurveType.Exponential"/> è la "durezza" k (più alto = accelerazione più marcata).
    /// Ignorato da <see cref="CurveType.Linear"/>.
    /// </summary>
    public double Exponent { get => _exponent; set => SetField(ref _exponent, value); }

    /// <summary>
    /// Rimappa l'ampiezza <paramref name="magnitude"/> (0..1) secondo la curva, restituendo un
    /// fattore in 0..1. <c>Shape(0)=0</c> e <c>Shape(1)=1</c> per qualunque curva: la velocità
    /// massima resta invariata, cambia solo la progressione.
    /// </summary>
    public double Shape(double magnitude)
    {
        double m = Math.Clamp(Math.Abs(magnitude), 0.0, 1.0);
        return Type switch
        {
            CurveType.Power => Math.Pow(m, Exponent <= 0 ? 1.0 : Exponent),
            CurveType.Exponential => Expo(m, Exponent),
            _ => m,
        };
    }

    /// <summary>Applica la curva a un asse con segno in [-1..1] preservandone la direzione.</summary>
    public double Apply(double axis) => Math.Sign(axis) * Shape(axis);

    // Esponenziale normalizzata: (e^{k m} - 1) / (e^k - 1). Tende alla lineare per k→0.
    private static double Expo(double m, double k)
    {
        if (k <= 0.0001) return m;
        return (Math.Exp(k * m) - 1.0) / (Math.Exp(k) - 1.0);
    }

    /// <summary>Copia profonda (utile alla GUI per l'editing senza toccare l'originale).</summary>
    [JsonIgnore]
    public ResponseCurve Clone => new() { Type = Type, Exponent = Exponent };
}
