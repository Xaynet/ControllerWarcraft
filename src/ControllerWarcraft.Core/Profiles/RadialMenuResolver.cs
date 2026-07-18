namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Logica pura di selezione del settore nel radial menu (Fase 4, punto 1): dati il numero di voci
/// e la posizione dello stick destro, decide quale voce è evidenziata. Nessuno stato, nessuna
/// P/Invoke: così la geometria è testabile e condivisa tra engine (App) e overlay (rendering).
///
/// Convenzione: voce 0 centrata in alto (12 in punto), poi in senso orario. Gli assi seguono lo
/// <c>GamepadSnapshot</c> (X: destra positiva, Y: su positiva).
/// </summary>
public static class RadialMenuResolver
{
    /// <summary>
    /// Restituisce l'indice della voce selezionata in [0..count-1], oppure <c>-1</c> se lo stick è
    /// entro <paramref name="selectDeadzone"/> (nessuna selezione: il rilascio annulla) o se
    /// <paramref name="count"/> è 0.
    /// </summary>
    public static int Resolve(int count, double stickX, double stickY, double selectDeadzone)
    {
        if (count <= 0) return -1;

        double magnitude = Math.Sqrt(stickX * stickX + stickY * stickY);
        if (magnitude < Math.Clamp(selectDeadzone, 0.0, 0.99)) return -1;

        // Angolo dal +Y (alto), in senso orario: atan2(x, y). 0 = alto, +π/2 = destra.
        double angle = Math.Atan2(stickX, stickY);
        if (angle < 0) angle += 2 * Math.PI;

        double sector = 2 * Math.PI / count;
        // Arrotonda al settore più vicino così la voce 0 resta centrata sull'alto.
        int index = (int)Math.Round(angle / sector) % count;
        return index;
    }

    /// <summary>
    /// Angolo (radianti) del <b>centro</b> del settore <paramref name="index"/> su <paramref name="count"/>
    /// voci, misurato dall'alto in senso orario. Utile al rendering dell'overlay per posizionare le
    /// etichette con la stessa convenzione del resolver.
    /// </summary>
    public static double SectorCenterAngle(int index, int count)
    {
        if (count <= 0) return 0;
        return (2 * Math.PI / count) * index;
    }
}
