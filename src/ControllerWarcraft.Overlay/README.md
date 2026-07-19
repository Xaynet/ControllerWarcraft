# ControllerWarcraft — Overlay (Fase 3 + 4)

Overlay **indicatore di modalità**: una finestra WPF trasparente, always-on-top e **click-through**
che mostra la modalità corrente (Movimento/Combattimento ↔ Cursore), il **layer** attivo
(Base / +LB / +RB / +LB+RB), il profilo caricato e — se il companion è attivo — una riga di
**contesto** (es. bersaglio). È l'anello "Overlay" di [ANALISI.md §5](../../ANALISI.md).

**Fase 4 — radial menu:** una seconda finestra (`RadialMenuWindow`, stesso stile click-through)
disegna un menu radiale on-screen quando l'utente tiene premuto il trigger (L3/R3). I settori e le
etichette sono resi dinamicamente e il settore selezionato dallo stick destro è evidenziato. È solo
un **indicatore visivo**: la selezione e l'invio del keybind (uno solo, 1:1) restano interamente nel
`MappingEngine` dell'App.

**Button-legend a layer + indicatore cursore:** altre due finestre click-through, ospitate sullo
**stesso thread STA** del `ModeOverlayController`:

- **`LegendWindow`** — un pannello discreto (angolo configurabile) che elenca cosa fa ogni pulsante
  mappabile **nel layer corrente** (es. `X → Shift+1`), aggiornandosi al cambio layer. Le righe
  arrivano già pronte dal loop dell'App: la **logica di derivazione è pura nel Core**
  (`ButtonLegend`), l'overlay è pura presentazione. Compare sempre o **solo mentre tieni un
  modificatore** LB/RB, a seconda della configurazione.
- **`CursorIndicatorWindow`** — quando si è in modalità cursore mostra una **cornice colorata ai
  bordi dello schermo** + un **badge**, così è impossibile non accorgersi della modalità cursore. È
  guidata dallo stesso `OverlayState` (modalità/pausa) più il flag `CursorIndicator`.

> Non ruba il focus al gioco e non intercetta il mouse: gli stili estesi
> `WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` fanno passare i click
> sotto e tengono la finestra fuori da Alt-Tab.

## Perché un progetto separato

È una **libreria WPF** a sé, non parte dell'App né della Gui:

- l'App resta un **loop console sottile**; le dipendenze WPF sono isolate qui;
- l'API è fatta di **soli tipi semplici** (`OverlayState`, `OverlayMode`): nessun riferimento ad
  App o Core, quindi il componente è disaccoppiato e riusabile;
- l'App la ospita su un **thread STA dedicato** con Dispatcher proprio, mentre il polling del
  gamepad continua sul thread principale.

## API

```csharp
using var overlay = new ModeOverlayController();
overlay.Start();                       // avvia il thread UI (no-op se la UI non è disponibile)
overlay.Update(new OverlayState(
    OverlayMode.MovementCombat,
    "MOVIMENTO/COMBATTIMENTO",         // etichetta modalità
    "+LB (Shift)",                     // etichetta layer
    paused: false,
    profileName: "Ascension",
    companionText: "",
    cursorIndicator: true));           // mostra cornice+badge quando Mode == Cursor

// Button-legend a layer (righe pre-calcolate dal Core; l'App chiama solo al cambio layer):
overlay.UpdateLegend(new LegendOverlayState(
    visible: true,
    layerText: "+LB (Shift)",
    corner: LegendCorner.BottomRight,
    rows: new[] { new LegendRow("X", "Shift+1"), new LegendRow("B", "Shift+2") }));
// ...
overlay.Dispose();                     // chiude le finestre e il Dispatcher
```

`Update`/`UpdateLegend` **deduplicano**: possono essere chiamati ad ogni tick, aggiornano la finestra
solo se lo stato è cambiato (nessun flicker). Se la UI non è disponibile (ambiente headless/CI)
`Start` fallisce in silenzio e `IsRunning` resta `false`: l'App prosegue col solo indicatore a
console. L'indicatore cursore e la button-legend vivono sullo stesso thread STA e seguono lo stesso
schema.

## File

```
OverlayState.cs           DTO immutabile (modalità/testi/pausa/profilo/companion/cursorIndicator) + enum OverlayMode
OverlayWindow.xaml(.cs)   finestra trasparente + colori per modalità + posizionamento in alto-centro
CursorIndicatorWindow.xaml(.cs) cornice ai bordi + badge "MODALITÀ CURSORE" (indicatore evidente)
LegendOverlayState.cs     DTO della button-legend (visibile/layer/angolo/righe) + LegendRow + enum LegendCorner
LegendWindow.xaml(.cs)    pannello discreto: righe pulsante → keybind nel layer corrente
NativeOverlay.cs          P/Invoke SetWindowLong: rende l'handle click-through e non-attivabile
ModeOverlayController.cs  host STA + Dispatcher per indicatore/cursore/legenda, API thread-safe con dedup
RadialOverlayState.cs     DTO del radial (visibile/etichette/indice selezionato)  — Fase 4
RadialMenuWindow.xaml(.cs) finestra del radial: disegna settori + etichette, evidenzia la selezione — Fase 4
RadialMenuController.cs   host STA + Dispatcher per il radial (come ModeOverlayController) — Fase 4
```

Il radial overlay ha la stessa API di controllo dell'indicatore di modalità:

```csharp
using var radial = new RadialMenuController();
radial.Start();
radial.Update(new RadialOverlayState(visible: true, labels, selectedIndex: 2)); // dedup + hide se non visibile
radial.Dispose();
```

## Disabilitare l'overlay

- Da `settings.json`: `"showOverlay": false` (indicatore di modalità),
  `"showButtonLegend": false` (button-legend), `"showCursorIndicator": false` (cornice/badge cursore).
- Solo per un'esecuzione: `cwapp --no-overlay` (disabilita l'intero overlay: indicatore, legenda,
  cursore e radial).

## Configurazione della button-legend

In `settings.json` (o dalla GUI):

- `showButtonLegend` (default `true`) — on/off.
- `legendVisibility` — `WhileModifierHeld` (default: compare solo mentre tieni LB/RB) o
  `AlwaysVisible` (sempre in Movimento/Combattimento).
- `legendCorner` — `TopLeft` / `TopRight` / `BottomLeft` / `BottomRight` (default).
