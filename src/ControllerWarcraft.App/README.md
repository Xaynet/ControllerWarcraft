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

## Cosa fa

- **Movimento + camera** — stick sx → WASD, stick dx → mouselook (RMB tenuto + delta mouse),
  con **curva di sensibilità** configurabile (Linear/Power/Exponential).
- **Layer di abilità** — LB/RB come *shift*: ogni pulsante frontale/D-pad/grilletto ha **4 stati**
  (Base, +LB, +RB, **+LB+RB**), mappati agli slot dell'action bar (1-9, Shift+1-9, Ctrl+1-9,
  Shift+Ctrl+1-9). Priorità: LB+RB > LB > RB > Base.
- **Tab-target** — su L3 (click stick sinistro).
- **Modalità cursore** — toggle su R3: lo stick destro diventa cursore mouse virtuale,
  A = click sinistro, X = click destro, B = Escape. Per loot/vendor/talenti.
- **Macchina a stati delle modalità** — Movimento/Combattimento ↔ Cursore, con indicatore
  a console **e overlay** trasparente always-on-top ad ogni cambio di modalità/layer.
- **Auto-switch profilo** — rileva l'eseguibile in primo piano e carica il profilo associato
  (mappa in `settings.json`); opzionale: mette in pausa l'emulazione fuori dal gioco.
- **Profilo da JSON** — i keybind, le curve di sensibilità e le deadzone arrivano dal profilo
  attivo (default: preset Ascension, identico alla Fase 1).

## Selezione del profilo

Il profilo attivo è in `%APPDATA%/ControllerWarcraft/settings.json` (`activeProfile`), impostabile
dalla [GUI](../ControllerWarcraft.Gui/README.md) o a mano. Utility da riga di comando:

```powershell
cwapp --list                 # elenca i profili disponibili (+ stato overlay/auto-switch)
cwapp --profile classic      # usa 'classic' solo per questa esecuzione
cwapp --no-overlay           # disabilita l'overlay per questa esecuzione
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
| **L3** (click stick sx) | Tab-target | — |
| **R3** (click stick dx) | Toggle → Cursore | Toggle → Movimento |
| Back | Uscita pulita | Uscita pulita |

> Il layer si sceglie **tenendo premuto** LB e/o RB mentre si preme il pulsante abilità.
> Priorità: **LB+RB > LB > RB > Base**.

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

## Architettura del codice

Struttura modulare che rispecchia [ANALISI.md §5](../../ANALISI.md):

```
(Core, condiviso con la Gui)
  Input/     ScanCode          enum scancode di tastiera (parte dello schema JSON)
  Profiles/  Keybind           tasto + modificatori (Shift/Ctrl/Alt)
             ActionButton      pulsanti mappabili + AbilityLayer (Base/+LB/+RB)
             ControllerProfile schema serializzabile: movimento/mouselook/cursore/system/abilities
             ProfileManager    carica/salva profili JSON, profilo attivo, fallback
             AppSettings       profilo attivo persistito
             Presets/BuiltInProfiles  Ascension (=Fase 1) / Classic / Retail in codice

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
  ModeOverlayController         host STA + Dispatcher, API thread-safe con dedup
  OverlayWindow / OverlayState  finestra trasparente click-through + DTO di stato
```

Il **MappingEngine** legge tutti i parametri (mappature, sensibilità, curva, deadzone, soglie) dal
`ControllerProfile`: resta separato dal main loop e dall'I/O. L'**EngineHost** lo incapsula per
permettere l'auto-switch del profilo senza ricostruire il loop; l'**overlay** vive in un progetto
WPF a sé, ospitato su un thread STA.
