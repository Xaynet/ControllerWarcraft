using System.Collections.Generic;
using System.Linq;

namespace ControllerWarcraft.Overlay;

/// <summary>
/// Stato del radial menu da mostrare nell'overlay (Fase 4, punto 1). DTO immutabile: l'App lo
/// costruisce ad ogni cambiamento (apertura, evidenziazione settore, chiusura) e lo passa al
/// <see cref="RadialMenuController"/>, che aggiorna la finestra solo se lo stato è cambiato.
/// Nessun accoppiamento con App/Core: solo dati semplici.
/// </summary>
/// <param name="Visible">Il menu è aperto (trigger tenuto premuto).</param>
/// <param name="Labels">Etichette delle voci, in senso orario a partire dall'alto.</param>
/// <param name="SelectedIndex">Indice della voce evidenziata, o -1 se nessuna (rilascio = annulla).</param>
public readonly record struct RadialOverlayState(
    bool Visible,
    IReadOnlyList<string> Labels,
    int SelectedIndex)
{
    // Uguaglianza per valore: confronta anche il contenuto delle etichette (non il riferimento).
    public bool Equals(RadialOverlayState other) =>
        Visible == other.Visible
        && SelectedIndex == other.SelectedIndex
        && (Labels is null ? other.Labels is null
            : other.Labels is not null && Labels.SequenceEqual(other.Labels));

    public override int GetHashCode() => System.HashCode.Combine(Visible, SelectedIndex, Labels?.Count ?? 0);
}
