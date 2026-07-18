namespace ControllerWarcraft.Overlay;

/// <summary>Modalità corrente della macchina a stati, come la vede l'overlay (colori/etichette).</summary>
public enum OverlayMode
{
    MovementCombat,
    Cursor,
}

/// <summary>
/// Stato da mostrare nell'overlay indicatore (Fase 3, punto 1). È un DTO immutabile: l'App lo
/// costruisce ad ogni cambiamento e lo passa al <see cref="ModeOverlayController"/>, che aggiorna
/// la finestra solo se lo stato è cambiato. Nessun accoppiamento con App/Core: solo dati.
/// </summary>
/// <param name="Mode">Modalità corrente (decide il colore dell'overlay).</param>
/// <param name="ModeText">Etichetta leggibile della modalità (es. "MOVIMENTO/COMBATTIMENTO").</param>
/// <param name="LayerText">Etichetta del layer attivo (es. "+LB (Shift)"); ignorata in modalità cursore.</param>
/// <param name="Paused">Se l'emulazione è in pausa (gioco non in primo piano): overlay attenuato + "PAUSA".</param>
/// <param name="ProfileName">Nome del profilo attivo (piccolo, in fondo).</param>
/// <param name="CompanionText">Contesto opzionale dal companion addon (es. "Target: Hogger (87%)"); vuoto se assente/disattivo.</param>
public readonly record struct OverlayState(
    OverlayMode Mode,
    string ModeText,
    string LayerText,
    bool Paused,
    string ProfileName,
    string CompanionText = "");
