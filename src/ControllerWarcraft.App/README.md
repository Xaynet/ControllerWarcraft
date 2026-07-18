# ControllerWarcraft — App (Fase 1: MVP giocabile)

MVP dell'applicazione **esterna** che permette di giocare a WoW con un controller Xbox
emulando tastiera + mouse. Nessun addon, mapping rigorosamente **1:1** (nessuna
automazione — vedi [ANALISI.md §8](../../ANALISI.md)).

Evoluzione dello [Spike Fase 0](../ControllerWarcraft.Spike/README.md), riorganizzata
in un'architettura modulare in vista delle fasi successive.

## Cosa fa (Fase 1)

- **Movimento + camera** — stick sx → WASD, stick dx → mouselook (RMB tenuto + delta mouse).
- **Layer di abilità** — LB/RB come *shift*: ogni pulsante frontale/D-pad/grilletto ha 3 stati
  (Base, +LB, +RB), mappati agli slot dell'action bar (1-9, Shift+1-9, Ctrl+1-9).
- **Tab-target** — su L3 (click stick sinistro).
- **Modalità cursore** — toggle su R3: lo stick destro diventa cursore mouse virtuale,
  A = click sinistro, X = click destro, B = Escape. Per loot/vendor/talenti.
- **Macchina a stati delle modalità** — Movimento/Combattimento ↔ Cursore, con indicatore
  a console ad ogni cambio di modalità/layer.
- **Profilo Ascension hardcoded** — i keybind sono costanti in codice (il sistema profili
  JSON è Fase 2).

## Mapping completo (profilo Ascension)

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
Native/    NativeMethods       P/Invoke XInput + SendInput (evoluzione dello spike)
Input/     GamepadPoller       XInput → GamepadSnapshot (normalizzato, deadzone)
           GamepadSnapshot     DTO immutabile dello stato gamepad
Output/    InputEmulator       SendInput: hold/tap tasti, mouselook, click; ReleaseAll
Profiles/  Keybind             tasto + modificatori (Shift/Ctrl/Alt)
           ActionButton        pulsanti mappabili + AbilityLayer (Base/+LB/+RB)
           AscensionProfile    (pulsante × layer) → keybind, hardcoded
Engine/    ControllerMode      enum modalità
           MappingEngine       cuore: state machine modalità + layer, guida l'emulatore
Program.cs                     main loop sottile: poll → engine.Update → sleep
```

Il **MappingEngine** e i profili sono separati dal main loop e dall'I/O: sono il punto di
estensione per la Fase 2 (profili JSON), la Fase 3 (curve di sensibilità, overlay) e i test.
