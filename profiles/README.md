# Profili (preset) — ControllerWarcraft

Preset di mapping controller→tastiera/mouse in formato **JSON**, uno per versione di gioco.
Sono **versionati nel repo** e copiati accanto all'eseguibile alla build (`<exe>/profiles/`),
dove il `ProfileManager` li trova come profili di sola lettura.

- `ascension.json` — replica **esatta** della Fase 1 (MVP).
- `classic.json` — WoW Classic.
- `retail.json` — WoW Retail.

I file sono **generati dal codice** (fonte di verità: `BuiltInProfiles` nel Core) con:

```powershell
dotnet run --project src/ControllerWarcraft.App -c Release -- --export-presets profiles
```

Così codice di fallback e file JSON restano allineati. Puoi anche modificarli a mano: sono
pensati per essere leggibili e commit-abili.

## Dove vivono i profili a runtime

| Posizione | Percorso | Uso |
|---|---|---|
| **Preset** | `<exe>/profiles/*.json` | Sola lettura, versionati nel repo |
| **Utente** | `%APPDATA%/ControllerWarcraft/profiles/*.json` | Creati/modificati dalla GUI |
| **Attivo** | `%APPDATA%/ControllerWarcraft/settings.json` | `{"activeProfile": "<stem>"}` |

A parità di *file stem* (es. `ascension`), un profilo nella cartella **utente** sostituisce il
preset: è così che la GUI ti permette di personalizzare un preset senza toccare il repo. Se non
si trova alcun file, l'App ricade sul preset `ascension`, e in ultima istanza sul built-in in
codice.

## Selezionare il profilo

- **GUI** — apri `cwgui`, scegli il profilo dal menu a tendina, premi **Imposta come attivo**.
- **A mano** — modifica `activeProfile` in `settings.json`.
- **CLI (solo per questa esecuzione)** — `cwapp --profile classic`.
- **Elenco** — `cwapp --list`.

## Schema del profilo (v1.0)

```jsonc
{
  "schemaVersion": "1.0",          // versione dello schema, per migrazioni future
  "name": "Ascension",             // nome leggibile (mostrato nella GUI)
  "gameVersion": "Ascension",      // Ascension | Classic | Retail (informativo)
  "description": "…",              // note/assunzioni

  "movement": {                    // stick sinistro → WASD digitale
    "forward": "W", "back": "S", "left": "A", "right": "D",
    "threshold": 0.5,              // ampiezza asse (0..1) per attivare il tasto
    "deadzone": 0.2395             // deadzone radiale stick sx normalizzata (0..1)
  },
  "mouselook": {                   // stick destro → RMB tenuto + delta mouse
    "sensitivityX": 18,            // pixel/tick a stick pieno (orizzontale)
    "sensitivityY": 14,            // pixel/tick a stick pieno (verticale)
    "invertY": false,
    "deadzone": 0.2652             // deadzone radiale stick dx normalizzata (0..1)
  },
  "cursor": {                      // modalità cursore (stick destro → cursore virtuale)
    "speed": 16,                   // pixel/tick a stick pieno
    "invertY": false
  },
  "system": {                      // binding gestiti direttamente dall'engine
    "jump":        { "key": "Space",  "shift": false, "ctrl": false, "alt": false },
    "tabTarget":   { "key": "Tab",    "shift": false, "ctrl": false, "alt": false },
    "cursorCancel":{ "key": "Escape", "shift": false, "ctrl": false, "alt": false }
  },

  "abilities": [                   // tabella (pulsante × layer) → keybind di gioco
    { "button": "X", "layer": "Base",        "bind": { "key": "D1", "shift": false, "ctrl": false, "alt": false } },
    { "button": "X", "layer": "Shoulder_LB", "bind": { "key": "D1", "shift": true,  "ctrl": false, "alt": false } },
    { "button": "X", "layer": "Shoulder_RB", "bind": { "key": "D1", "shift": false, "ctrl": true,  "alt": false } }
    // …
  ]
}
```

**Valori ammessi**

- `button` (pulsante fisico mappabile): `X`, `B`, `Y`, `RightTrigger`, `LeftTrigger`,
  `DPadUp`, `DPadRight`, `DPadDown`, `DPadLeft`.
  (I pulsanti "di sistema" A=salto, LB/RB=modificatori, L3=tab-target, R3=toggle, Back=uscita
  sono gestiti dall'engine e non compaiono in `abilities`.)
- `layer` (stato modificatori): `Base`, `Shoulder_LB` (LB tenuto), `Shoulder_RB` (RB tenuto).
- `key` (scancode di tastiera, per **nome**): `D1`..`D0`, `A`..`Z`, `Space`, `Tab`, `Escape`,
  `Minus`, `Equals`, `LeftShift`, `LeftControl`, `LeftAlt`, `None` (= nessun binding).
- `shift`/`ctrl`/`alt`: modificatori premuti insieme al tasto.

> **Mapping rigorosamente 1:1** (ANALISI §8): un keybind = un tasto (più modificatori). Lo schema
> non prevede sequenze, macro, ripetizioni o timer. Nessuna automazione.

## Assunzioni dei preset

Tutti e tre condividono lo **stesso schema di action bar**, perché in tutte le versioni le barre
si configurano allo stesso modo lato client:

- Barra principale → `1`-`9` (layer **Base**)
- Barra secondaria → `Shift+1`..`Shift+9` (layer **+LB**)
- Terza barra → `Ctrl+1`..`Ctrl+9` (layer **+RB**)

Queste combinazioni vanno impostate **in gioco** (Menu → Tasti / Keybindings). Adattale se le
tue differiscono, oppure modifica il profilo.

Differenze **documentate** tra i preset (le uniche che dipendono davvero dalla versione):

| Preset | Camera (SensX/Y) | Cursore | Note |
|---|---|---|---|
| **Ascension** | 18 / 14 | 16 | Replica esatta della Fase 1. |
| **Classic** | 18 / 14 | 16 | Nessun soft-target in Classic → il Tab-target (L3) è la selezione primaria. |
| **Retail** | 20 / 15 | 18 | Camera di default più reattiva; esiste il soft-target, il Tab-target resta un'alternativa. |

I keybind di action bar sono identici tra i preset: la differenza di targeting tra versioni si
riflette nel **flusso di gioco**, non in tasti diversi. Personalizza pure ogni profilo dalla GUI.
