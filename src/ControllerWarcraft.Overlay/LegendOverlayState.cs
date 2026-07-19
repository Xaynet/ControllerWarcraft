using System.Collections.Generic;
using System.Linq;

namespace ControllerWarcraft.Overlay;

/// <summary>Angolo dello schermo in cui ancorare la button-legend (mirror dell'enum di configurazione).</summary>
public enum LegendCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>
/// Una riga della button-legend, come la mostra l'overlay: solo dati di presentazione (nessun tipo
/// di App/Core, coerente con <see cref="OverlayState"/>). L'App mappa le righe pure del Core in
/// queste prima di passarle all'overlay.
/// </summary>
/// <param name="Button">Etichetta breve del pulsante (es. "X", "RT", "D-Pad ↑").</param>
/// <param name="Keybind">Testo di destinazione (keybind o, in futuro, nome abilità); "-" se non mappato.</param>
public readonly record struct LegendRow(string Button, string Keybind);

/// <summary>
/// Stato della button-legend a layer da mostrare nell'overlay. DTO immutabile: l'App lo costruisce
/// solo quando cambia il layer/modalità (non a ogni tick) e lo passa al
/// <see cref="ModeOverlayController"/>, che aggiorna la finestra solo se lo stato è cambiato.
/// Nessun accoppiamento con App/Core: solo dati semplici.
/// </summary>
/// <param name="Visible">La legenda deve essere mostrata ora.</param>
/// <param name="LayerText">Etichetta del layer corrente, come intestazione (es. "+LB (Shift)").</param>
/// <param name="Corner">Angolo dello schermo in cui ancorare il pannello.</param>
/// <param name="Rows">Righe (pulsante → keybind/etichetta), in ordine di presentazione.</param>
public readonly record struct LegendOverlayState(
    bool Visible,
    string LayerText,
    LegendCorner Corner,
    IReadOnlyList<LegendRow> Rows)
{
    // Uguaglianza per valore: confronta anche il contenuto delle righe (non il riferimento), così la
    // dedup del controller evita ridisegni/flicker quando lo stato non cambia davvero.
    public bool Equals(LegendOverlayState other) =>
        Visible == other.Visible
        && LayerText == other.LayerText
        && Corner == other.Corner
        && (Rows is null ? other.Rows is null
            : other.Rows is not null && Rows.SequenceEqual(other.Rows));

    public override int GetHashCode() =>
        System.HashCode.Combine(Visible, LayerText, Corner, Rows?.Count ?? 0);
}
