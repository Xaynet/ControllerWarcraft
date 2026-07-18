# ControllerWarcraft

Gioca a **World of Warcraft con un controller Xbox** — su **Ascension, Classic e
Retail** — senza dipendere da addon specifici per versione.

L'idea chiave: invece di un addon (che vive dentro il client e dipende dall'API Lua
di *quella* versione), ControllerWarcraft è un'**app esterna** che legge il
controller via XInput ed **emula tastiera+mouse** via SendInput. Il gioco vede solo
KB/mouse, quindi lo stesso motore funziona su ogni versione; le differenze diventano
semplici **profili JSON**. Il mapping è rigorosamente **1:1**, senza automazione.

```
[Controller Xbox] --XInput--> [ControllerWarcraft] --SendInput--> [Client WoW]
```

## Documentazione

- **[QUICKSTART.md](QUICKSTART.md)** — guida al primo avvio per l'utente finale.
- **[ANALISI.md](ANALISI.md)** — analisi, architettura e roadmap.
- **[RELEASING.md](RELEASING.md)** — processo di rilascio (CI/CD, tag).

## Funzionalità

- **Movimento + camera** — stick sx → WASD, stick dx → mouselook, con curve di
  sensibilità configurabili.
- **Layer di abilità** — LB/RB come *shift*: 4 stati per pulsante (Base, +LB, +RB,
  +LB+RB) → copre le action bar (1-9, Shift+1-9, Ctrl+1-9, Shift+Ctrl+1-9).
- **Modalità cursore** — toggle che trasforma lo stick destro in cursore mouse per
  loot / vendor / talenti, con indicatore di modalità via **overlay** trasparente.
- **Radial menu** — menu radiale on-screen per abilità/mount extra (selezione 1:1).
- **Profili JSON per versione** — preset Ascension / Classic / Retail + **preset di
  classe**, editabili da una **GUI** di remap.
- **Auto-switch profilo** — carica il profilo giusto in base al gioco in primo piano.
- **Companion addon opzionale** — espone stato di gioco (sola lettura); mai richiesto.

## Struttura del progetto

| Percorso | Descrizione |
|---|---|
| `src/ControllerWarcraft.Core` | Libreria condivisa: schema profili, ProfileManager, preset, curve, radial |
| `src/ControllerWarcraft.App` | Runtime: XInput → mapping → SendInput (`cwapp.exe`) |
| `src/ControllerWarcraft.Gui` | GUI WPF di remap/selezione profili (`cwgui.exe`) |
| `src/ControllerWarcraft.Overlay` | Libreria WPF: overlay modalità + radial menu |
| `src/ControllerWarcraft.Spike` | Spike Fase 0 (proof-of-concept) |
| `profiles/` | Preset JSON per versione + `classes/` |
| `addon/` | Companion addon WoW opzionale (Lua) |

## Build

Richiede l'**SDK .NET 10** (target `net10.0-windows`).

```powershell
dotnet build ControllerWarcraft.slnx -c Release
```

Per eseguire il runtime dal sorgente:

```powershell
dotnet run -c Release --project src/ControllerWarcraft.App
```

> ⚠️ L'app invia input reali di tastiera/mouse. Provala prima con il **Blocco note**
> in primo piano (vedi [QUICKSTART.md](QUICKSTART.md)).

## Piattaforma

Windows. Controller compatibile Xbox (XInput).

## Nota sull'uso (ToS)

Il mapping è **1:1**, senza automazione: è l'uso tollerato dei remapper di input.
**Ascension** è un server privato con client custom — verifica le regole del server
sugli strumenti di input esterni prima di usarlo. La responsabilità dell'uso è
dell'utente finale.

## Licenza

Distribuito sotto licenza **MIT**. Vedi [LICENSE](LICENSE).
