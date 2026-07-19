# ControllerWarcraft — App (runtime)

Applicazione **esterna** che permette di giocare a WoW con un controller Xbox emulando
tastiera + mouse. Nessun addon, mapping rigorosamente **1:1** (nessuna automazione — vedi
[ANALISI.md §8](../../ANALISI.md)).

Evoluzione dello [Spike Fase 0](../ControllerWarcraft.Spike/README.md), riorganizzata
in un'architettura modulare.

> **Fase 2:** il mapping non è più hardcoded. L'App carica un **profilo JSON** tramite il
> `ProfileManager` condiviso ([`ControllerWarcraft.Core`](../ControllerWarcraft.Core/)); i preset
> per Ascension/Classic/Retail sono in [`profiles/`](../../profiles/README.md) e si modificano
> dalla [GUI](../ControllerWarcraft.Gui/README.md). Il preset Ascension replica esattamente il
> comportamento della Fase 1; se nessun file è presente si ricade sul built-in Ascension in codice.
>
> **Fase 3 (UX):** overlay indicatore di modalità
> ([`ControllerWarcraft.Overlay`](../ControllerWarcraft.Overlay/README.md)) accanto alla console;
> curve di sensibilità del mouselook (dal profilo); **quarto layer +LB+RB** (Shift+Ctrl); e
> **auto-switch** del profilo in base alla finestra in primo piano, con pausa opzionale fuori gioco.
>
> **Fase 4 (Polish):** **radial menu** overlay (tieni premuto L3/R3 → menu radiale; muovi lo stick
> destro verso un settore, rilascia per inviare **un solo** keybind — 1:1, nessuna sequenza);
> **preset per classe** applicabili sul profilo dalla GUI; e **companion addon opzionale** (sola
> lettura) il cui stato è mostrato come contesto nell'overlay (mai usato per l'input).

## Cosa fa

- **Movimento + camera** — stick sx → WASD, stick dx → mouselook (RMB tenuto + delta mouse),
  con **curva di sensibilità** configurabile (Linear/Power/Exponential).
- **Layer di abilità** — LB/RB come *shift*: ogni pulsante frontale/D-pad/grilletto ha **4 stati**
  (Base, +LB, +RB, **+LB+RB**), mappati agli slot dell'action bar (1-9, Shift+1-9, Ctrl+1-9,
  Shift+Ctrl+1-9). Priorità: LB+RB > LB > RB > Base.
- **Tab-target** — su L3 (click stick sinistro).
- **Modalità cursore** — attivazione **configurabile** (default: toggle su R3, come da storico).
  Pulsante rimappabile (R3 / L3 / Start / *None* per disattivarla) e modalità **Toggle** o **Hold**
  (momentaneo, attivo solo mentre tieni premuto). In modalità cursore: stick destro = cursore mouse
  virtuale, A = click sinistro, X = click destro, B = Escape. Per loot/vendor/talenti.
- **Hardening input** — soglia opzionale di *hold minimo* (ms) sui click-stick L3/R3 (e Start):
  scarta le pressioni accidentali troppo brevi. Default 0 = comportamento storico.
- **Macchina a stati delle modalità** — Movimento/Combattimento ↔ Cursore, con indicatore
  a console **e overlay** trasparente always-on-top ad ogni cambio di modalità/layer.
- **Button-legend a layer (HUD)** — pannello overlay discreto che ricorda cosa fa ogni pulsante
  **nel layer corrente** (es. `X → Shift+1`), aggiornato al cambio layer. Sempre visibile o **solo
  mentre tieni un modificatore** LB/RB (configurabile). Le righe sono derivate da una funzione
  **pura del Core** (`ButtonLegend`); l'App le calcola solo al cambio layer (niente flicker).
- **Indicatore evidente della modalità cursore** — cornice colorata ai bordi + badge quando si è in
  modalità cursore, così è impossibile non accorgersene (click-through, non copre il gioco).
- **Auto-switch profilo** — rileva l'eseguibile in primo piano e carica il profilo associato
  (mappa in `settings.json`); opzionale: mette in pausa l'emulazione fuori dal gioco.
- **Profilo da JSON** — i keybind, le curve di sensibilità e le deadzone arrivano dal profilo
  attivo (default: preset Ascension, identico alla Fase 1).

## Selezione del profilo

Il profilo attivo è in `%APPDATA%/ControllerWarcraft/settings.json` (`activeProfile`), impostabile
dalla [GUI](../ControllerWarcraft.Gui/README.md) o a mano. Utility da riga di comando:

```powershell
cwapp --list                 # elenca i profili disponibili (+ stato overlay/auto-switch/companion)
cwapp --profile classic      # usa 'classic' solo per questa esecuzione
cwapp --no-overlay           # disabilita l'overlay (indicatore + radial) per questa esecuzione
cwapp --no-autoswitch        # disabilita l'auto-switch per questa esecuzione
cwapp --export-presets dir   # rigenera i preset JSON (nessun input inviato)
cwapp --help
```

### Auto-switch profilo (Fase 3)

Con `"autoSwitchEnabled": true` in `settings.json`, l'App controlla ~2 volte al secondo quale
eseguibile è in primo piano e carica il profilo mappato in `processProfileMap`
(`nome_processo_senza_exe → file_stem`). Con `"pauseWhenGameNotForeground": true` l'emulazione è
sospesa (input rilasciati) quando in primo piano non c'è un gioco riconosciuto. Default suggeriti:
`ascension→ascension`, `wow→retail`, `wowclassic→classic`. Configurabile anche dalla GUI.

Dettagli su schema, posizioni e assunzioni: [`profiles/README.md`](../../profiles/README.md).

## Mapping completo (preset Ascension)

| Input controller | Modalità Movimento/Combattimento | Modalità Cursore |
|---|---|---|
| Stick sinistro | W / A / S / D (movimento) | W / A / S / D (movimento) |
| Stick destro | Mouselook (RMB + delta mouse) | Cursore mouse virtuale |
| **LB** (tenuto) | Modificatore layer → **+LB** (Shift) | — |
| **RB** (tenuto) | Modificatore layer → **+RB** (Ctrl) | — |
| **LB+RB** (tenuti) | Modificatore layer → **+LB+RB** (Shift+Ctrl) | — |
| A | Salto (Space) | Click sinistro (tenuto → drag) |
| X | Abilità (1 / Shift+1 / Ctrl+1 / Shift+Ctrl+1) | Click destro |
| B | Abilità (2 / …) | Escape |
| Y | Abilità (3 / …) | — |
| Grilletto destro (RT) | Abilità (4 / …) | — |
| Grilletto sinistro (LT) | Abilità (5 / …) | — |
| D-pad Su | Abilità (6 / …) | — |
| D-pad Destra | Abilità (7 / …) | — |
| D-pad Giù | Abilità (8 / …) | — |
| D-pad Sinistra | Abilità (9 / …) | — |
| **L3** (click stick sx) | Tab-target *(o trigger radial / attivazione cursore, se configurato)* | — |
| **R3** (click stick dx) | Attiva Cursore *(pulsante e modalità configurabili — o trigger radial)* | Torna a Movimento |
| Back | Uscita pulita | Uscita pulita |

> Il layer si sceglie **tenendo premuto** LB e/o RB mentre si preme il pulsante abilità.
> Priorità: **LB+RB > LB > RB > Base**.

### Hardening input (attivazione cursore & pressioni accidentali)

L'attivazione della modalità cursore è configurabile nel profilo (`cursor.activationButton` +
`cursor.activationMode`) e dalla GUI:

- **Pulsante** (`activationButton`): `RightThumb` (R3, default), `LeftThumb` (L3), `Start`, o
  `None` (modalità cursore disattivata). Se è `LeftThumb`, L3 non fa più Tab-target.
- **Modalità** (`activationMode`): `Toggle` (default storico) o `Hold` (cursore attivo solo mentre
  tieni premuto il pulsante).
- **Hold minimo** (`inputHardening.thumbClickMinHoldMs`, ms): un click-stick più breve della soglia
  è ignorato (scarta le pressioni accidentali). Default 0 = comportamento storico. Si applica a
  toggle/hold cursore, Tab-target e apertura del radial.

**Precedenza:** se un pulsante è sia il trigger del radial sia l'attivazione cursore, vince il
radial. La logica di debounce vive nel Core (`HoldGate`, pura e testabile); il `MappingEngine`
legge tutto dal profilo. Retro-compatibile: i profili senza questi campi si comportano come prima.

### Radial menu (Fase 4)

Se il profilo ha un radial menu attivo (`radialMenu.enabled` + un `trigger` L3/R3 + almeno una voce),
**tenendo premuto** il trigger compare un menu radiale on-screen. Muovi lo stick destro verso un
settore per evidenziarlo e **rilascia** il trigger per inviare il keybind di quella voce. Il rilascio
al centro (sotto la soglia) **annulla** senza inviare nulla. Ogni selezione invia **un solo** keybind
(1:1, ANALISI §8): nessuna sequenza, nessun timer. Quando il radial usa L3/R3, quel pulsante non fa
più Tab-target / toggle mentre il radial è attivo. Le voci si configurano nel profilo o dalla GUI.

### Companion addon (Fase 4, opzionale)

Con `"companionEnabled": true` e `"companionSavedVariablesPath": "…"` in `settings.json`, l'App legge
(in **sola lettura**, ~1×/s) lo stato esposto dal [companion addon](../../addon/ControllerWarcraftCompanion/README.md)
e lo mostra come contesto nell'overlay (es. bersaglio). **Non guida mai l'input.** Disattivo per
default: l'App funziona identica senza.

## Configurazione lato WoW (una tantum)

Perché il profilo funzioni, in gioco (Menu → Tasti / Keybindings) gli slot dell'action bar
devono corrispondere:

- Barra principale (slot 1-9) → tasti `1`-`9`
- Barra secondaria → `Shift+1` … `Shift+9`
- Un'altra barra → `Ctrl+1` … `Ctrl+9`

Queste combinazioni sono i default di molte configurazioni WoW; adattale se le tue differiscono.

## Come eseguire

```powershell
dotnet run -c Release --project src/ControllerWarcraft.App
```

oppure l'eseguibile compilato:

```powershell
./src/ControllerWarcraft.App/bin/Release/net10.0-windows/cwapp.exe
```

⚠️ **L'app invia input reali di tastiera/mouse.** Testala prima con il **Blocco note** in
foreground (stick sx → `wasd`, X → `1`, LB+X → non stampa ma invia Shift+1, ecc.), poi in gioco.
Premi **BACK** per fermarti; l'app rilascia sempre ogni tasto all'uscita.

> 🔐 **Elevazione automatica.** `cwapp` include un manifest con
> `requestedExecutionLevel = requireAdministrator` ([`app.manifest`](app.manifest)):
> all'avvio richiede i permessi da amministratore (prompt UAC). È **necessario** perché il
> gioco gira spesso da admin e Windows (UIPI) blocca l'input da un processo non elevato verso
> una finestra elevata. Con `dotnet run` in fase di sviluppo, avvia il terminale come admin.

## Architettura del codice

Struttura modulare che rispecchia [ANALISI.md §5](../../ANALISI.md):

```
(Core, condiviso con la Gui)
  Input/     ScanCode          enum scancode di tastiera (parte dello schema JSON; +F1-F12 in Fase 4)
  Profiles/  Keybind           tasto + modificatori (Shift/Ctrl/Alt)
             ActionButton      pulsanti mappabili + AbilityLayer (Base/+LB/+RB/+LB+RB)
             ControllerProfile schema serializzabile: movimento/mouselook/cursore/system/abilities/radialMenu
             RadialMenu        RadialMenuSettings/Item + RadialTrigger (Fase 4)
             RadialMenuResolver selezione settore pura e testabile (Fase 4)
             ClassPreset       override per classe + merge ApplyTo (Fase 4)
             ButtonLegend      derivazione pura delle righe legenda (profilo + layer → pulsante/keybind) + visibilità
             ProfileManager    carica/salva profili e preset di classe, profilo attivo, fallback
             AppSettings       profilo attivo + opzioni overlay/button-legend/cursore/auto-switch/companion
             Presets/BuiltInProfiles  Ascension (=Fase 1) / Classic / Retail in codice
  Companion/ CompanionState / CompanionStateReader  lettura opzionale dei SavedVariables (Fase 4)

(App, runtime)
  Native/    NativeMethods     P/Invoke XInput + SendInput (usa Core.ScanCode)
  Input/     GamepadPoller     XInput → GamepadSnapshot (deadzone dal profilo)
             GamepadSnapshot   DTO immutabile dello stato gamepad
             ForegroundWatcher GetForegroundWindow → nome processo (auto-switch, Fase 3)
  Output/    InputEmulator     SendInput: hold/tap tasti, mouselook, click; ReleaseAll
  Engine/    ControllerMode    enum modalità
             MappingEngine     cuore: state machine + 4 layer, curva mouselook dal profilo
             EngineHost        possiede poller+engine+profilo; swap del profilo a caldo
  Program.cs                   main loop + overlay + auto-switch + sotto-comandi

(Overlay, WPF — vedi ControllerWarcraft.Overlay/README.md)
  ModeOverlayController         host STA + Dispatcher (modalità + cursore + legenda), API thread-safe con dedup
  OverlayWindow / OverlayState  finestra trasparente click-through + DTO di stato
  CursorIndicatorWindow         cornice ai bordi + badge "MODALITÀ CURSORE"
  LegendWindow / LegendOverlayState  button-legend a layer (righe pulsante → keybind) + DTO
```

Il **MappingEngine** legge tutti i parametri (mappature, sensibilità, curva, deadzone, soglie) dal
`ControllerProfile`: resta separato dal main loop e dall'I/O. L'**EngineHost** lo incapsula per
permettere l'auto-switch del profilo senza ricostruire il loop; l'**overlay** vive in un progetto
WPF a sé, ospitato su un thread STA.
