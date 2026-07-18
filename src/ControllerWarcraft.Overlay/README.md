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
    profileName: "Ascension"));
// ...
overlay.Dispose();                     // chiude la finestra e il Dispatcher
```

`Update` **deduplica**: può essere chiamato ad ogni tick, aggiorna la finestra solo se lo stato è
cambiato. Se la UI non è disponibile (ambiente headless/CI) `Start` fallisce in silenzio e
`IsRunning` resta `false`: l'App prosegue col solo indicatore a console.

## File

```
OverlayState.cs           DTO immutabile (modalità/testi/pausa/profilo/companion) + enum OverlayMode
OverlayWindow.xaml(.cs)   finestra trasparente + colori per modalità + posizionamento in alto-centro
NativeOverlay.cs          P/Invoke SetWindowLong: rende l'handle click-through e non-attivabile
ModeOverlayController.cs  host STA + Dispatcher, API thread-safe con dedup
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

- Da `settings.json`: `"showOverlay": false`.
- Solo per un'esecuzione: `cwapp --no-overlay`.
