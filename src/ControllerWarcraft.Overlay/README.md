# ControllerWarcraft — Overlay (Fase 3)

Overlay **indicatore di modalità**: una finestra WPF trasparente, always-on-top e **click-through**
che mostra la modalità corrente (Movimento/Combattimento ↔ Cursore), il **layer** attivo
(Base / +LB / +RB / +LB+RB) e il profilo caricato. È l'anello "Overlay" di
[ANALISI.md §5](../../ANALISI.md).

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
OverlayState.cs          DTO immutabile (modalità/testi/pausa/profilo) + enum OverlayMode
OverlayWindow.xaml(.cs)  finestra trasparente + colori per modalità + posizionamento in alto-centro
NativeOverlay.cs         P/Invoke SetWindowLong: rende l'handle click-through e non-attivabile
ModeOverlayController.cs host STA + Dispatcher, API thread-safe con dedup
```

## Disabilitare l'overlay

- Da `settings.json`: `"showOverlay": false`.
- Solo per un'esecuzione: `cwapp --no-overlay`.
