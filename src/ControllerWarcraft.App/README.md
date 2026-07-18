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

## Cosa fa

- **Movimento + camera** — stick sx → WASD, stick dx → mouselook (RMB tenuto + delta mouse).
- **Layer di abilità** — LB/RB come *shift*: ogni pulsante frontale/D-pad/grilletto ha 3 stati
  (Base, +LB, +RB), mappati agli slot dell'action bar (1-9, Shift+1-9, Ctrl+1-9).
- **Tab-target** — su L3 (click stick sinistro).
- **Modalità cursore** — toggle su R3: lo stick destro diventa cursore mouse virtuale,
  A = click sinistro, X = click destro, B = Escape. Per loot/vendor/talenti.
- **Macchina a stati delle modalità** — Movimento/Combattimento ↔ Cursore, con indicatore
  a console ad ogni cambio di modalità/layer.
- **Profilo da JSON** — i keybind, le curve di sensibilità e le deadzone arrivano dal profilo
  attivo (default: preset Ascension, identico alla Fase 1).

## Selezione del profilo

Il profilo attivo è in `%APPDATA%/ControllerWarcraft/settings.json` (`activeProfile`), impostabile
dalla [GUI](../ControllerWarcraft.Gui/README.md) o a mano. Utility da riga di comando:

```powershell
cwapp --list                 # elenca i profili disponibili (preset + utente)
cwapp --profile classic      # usa 'classic' solo per questa esecuzione
cwapp --export-presets dir   # rigenera i preset JSON (nessun input inviato)
cwapp --help
```

Dettagli su schema, posizioni e assunzioni: [`profiles/README.md`](../../profiles/README.md).

## Mapping completo (preset Ascension)

| Input controller | Modalità Movimento/Combattimento | Modalità Cursore |
|---|---|---|
| Stick sinistro | W / A / S / D (movimento) | W / A / S / D (movimento) |
| Stick destro | Mouselook (RMB + delta mouse) | Cursore mouse virtuale |
| **LB** (tenuto) | Modificatore layer → **+LB** (Shift) | — |
| **RB** (tenuto) | Modificatore layer → **+RB** (Ctrl) | — |
| A | Salto (Space) | Click sinistro (tenuto → drag) |
| X | Abilità (1 / Shift+1 / Ctrl+1) | Click destro |
| B | Abilità (2 / Shift+2 / Ctrl+2) | Escape |
| Y | Abilità (3 / Shift+3 / Ctrl+3) | — |
| Grilletto destro (RT) | Abilità (4 / Shift+4 / Ctrl+4) | — |
| Grilletto sinistro (LT) | Abilità (5 / Shift+5 / Ctrl+5) | — |
| D-pad Su | Abilità (6 / Shift+6 / Ctrl+6) | — |
| D-pad Destra | Abilità (7 / Shift+7 / Ctrl+7) | — |
| D-pad Giù | Abilità (8 / Shift+8 / Ctrl+8) | — |
| D-pad Sinistra | Abilità (9 / Shift+9 / Ctrl+9) | — |
| **L3** (click stick sx) | Tab-target | — |
| **R3** (click stick dx) | Toggle → Cursore | Toggle → Movimento |
| Back | Uscita pulita | Uscita pulita |

> Il layer si sceglie **tenendo premuto** LB o RB mentre si preme il pulsante abilità.
> Se entrambi sono premuti, LB ha priorità.

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
  Output/    InputEmulator     SendInput: hold/tap tasti, mouselook, click; ReleaseAll
  Engine/    ControllerMode    enum modalità
             MappingEngine     cuore: state machine + layer, legge i parametri dal profilo
  Program.cs                   main loop + sotto-comandi (--list/--profile/--export-presets)
```

Il **MappingEngine** legge tutti i parametri (mappature, sensibilità, deadzone, soglie) dal
`ControllerProfile`: resta separato dal main loop e dall'I/O ed è pronto per la Fase 3 (curve di
sensibilità non lineari, overlay) e per i test.
