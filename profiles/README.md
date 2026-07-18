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

### `settings.json` (impostazioni globali)

Oltre al profilo attivo, `settings.json` contiene le opzioni UX di Fase 3 (tutte con default
retro-compatibili — un file che ha solo `activeProfile` continua a funzionare):

```jsonc
{
  "activeProfile": "ascension",
  "showOverlay": true,                 // overlay indicatore di modalità
  "autoSwitchEnabled": false,          // carica il profilo dell'app in primo piano
  "pauseWhenGameNotForeground": false, // sospende l'emulazione fuori dal gioco
  "processProfileMap": {               // nome eseguibile (senza .exe) → file stem profilo
    "ascension": "ascension",
    "wow": "retail",
    "wowclassic": "classic"
  },
  "companionEnabled": false,           // Fase 4: legge (sola lettura) lo stato dal companion addon
  "companionSavedVariablesPath": ""    // percorso del file SavedVariables del companion
}
```

Modificabili dalla [GUI](../src/ControllerWarcraft.Gui/README.md) (pannello "Impostazioni globali").

## Selezionare il profilo

- **GUI** — apri `cwgui`, scegli il profilo dal menu a tendina, premi **Imposta come attivo**.
- **A mano** — modifica `activeProfile` in `settings.json`.
- **CLI (solo per questa esecuzione)** — `cwapp --profile classic`.
- **Elenco** — `cwapp --list`.

## Schema del profilo (v1.2)

> **v1.2 (Fase 4):** aggiunto `radialMenu` (radial menu overlay). I file **v1.0/v1.1** restano
> validi: se il campo manca, il radial è semplicemente disattivato (default `enabled:false`,
> `trigger:"None"`), quindi il comportamento è identico a prima.
>
> **v1.1 (Fase 3):** aggiunti `mouselook.curve` (curva di sensibilità) e il quarto layer
> `Shoulder_LBRB`. I file **v1.0** restano validi: i campi nuovi hanno default retro-compatibili
> (`curve` assente → `Linear`, cioè identico a prima; nessuna voce `Shoulder_LBRB` → quel layer è
> un No-op sicuro).

```jsonc
{
  "schemaVersion": "1.1",          // versione dello schema, per migrazioni future
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
    "deadzone": 0.2652,            // deadzone radiale stick dx normalizzata (0..1)
    "curve": {                     // curva di risposta (Fase 3), applicata prima della sensibilità
      "type": "Linear",            // Linear | Power | Exponential
      "exponent": 1.5             // Power: esponente (>1 = più preciso al centro); Exponential: durezza
    }
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
    { "button": "X", "layer": "Base",          "bind": { "key": "D1", "shift": false, "ctrl": false, "alt": false } },
    { "button": "X", "layer": "Shoulder_LB",   "bind": { "key": "D1", "shift": true,  "ctrl": false, "alt": false } },
    { "button": "X", "layer": "Shoulder_RB",   "bind": { "key": "D1", "shift": false, "ctrl": true,  "alt": false } },
    { "button": "X", "layer": "Shoulder_LBRB", "bind": { "key": "D1", "shift": true,  "ctrl": true,  "alt": false } }
    // …
  ],

  "radialMenu": {                  // radial menu overlay (Fase 4). Disattivo per default.
    "enabled": false,              // true per attivarlo
    "trigger": "None",             // None | LeftThumb (L3) | RightThumb (R3): pulsante da tenere premuto
    "selectDeadzone": 0.4,         // ampiezza min. stick per selezionare un settore (sotto = annulla)
    "items": [                     // voci in senso orario dall'alto; ognuna invia UN SOLO keybind
      { "label": "Cavalcatura", "bind": { "key": "F1", "shift": false, "ctrl": false, "alt": false } }
      // …
    ]
  }
}
```

**Valori ammessi**

- `button` (pulsante fisico mappabile): `X`, `B`, `Y`, `RightTrigger`, `LeftTrigger`,
  `DPadUp`, `DPadRight`, `DPadDown`, `DPadLeft`.
  (I pulsanti "di sistema" A=salto, LB/RB=modificatori, L3=tab-target, R3=toggle, Back=uscita
  sono gestiti dall'engine e non compaiono in `abilities`.)
- `layer` (stato modificatori): `Base`, `Shoulder_LB` (LB tenuto), `Shoulder_RB` (RB tenuto),
  `Shoulder_LBRB` (LB+RB tenuti insieme — 4° layer, Fase 3). Priorità: LB+RB > LB > RB > Base.
- `key` (scancode di tastiera, per **nome**): `D1`..`D0`, `A`..`Z`, `F1`..`F12`, `Space`, `Tab`,
  `Escape`, `Minus`, `Equals`, `LeftShift`, `LeftControl`, `LeftAlt`, `None` (= nessun binding).
  (`F1`-`F12` aggiunti in Fase 4, comodi per le utility del radial.)
- `shift`/`ctrl`/`alt`: modificatori premuti insieme al tasto.
- `mouselook.curve.type`: `Linear` (default, = comportamento storico), `Power` (`y=x^exp`),
  `Exponential` (accelerazione normalizzata). `Linear` ignora `exponent`.
- `radialMenu.trigger`: `None` (default, radial off), `LeftThumb` (L3), `RightThumb` (R3).

> **Mapping rigorosamente 1:1** (ANALISI §8): un keybind = un tasto (più modificatori). Lo schema
> non prevede sequenze, macro, ripetizioni o timer. Nessuna automazione.

## Assunzioni dei preset

Tutti e tre condividono lo **stesso schema di action bar**, perché in tutte le versioni le barre
si configurano allo stesso modo lato client:

- Barra principale → `1`-`9` (layer **Base**)
- Barra secondaria → `Shift+1`..`Shift+9` (layer **+LB**)
- Terza barra → `Ctrl+1`..`Ctrl+9` (layer **+RB**)
- Quarta barra → `Shift+Ctrl+1`..`Shift+Ctrl+9` (layer **+LB+RB**, Fase 3)

Queste combinazioni vanno impostate **in gioco** (Menu → Tasti / Keybindings). Adattale se le
tue differiscono, oppure modifica il profilo.

Differenze **documentate** tra i preset (le uniche che dipendono davvero dalla versione):

| Preset | Camera (SensX/Y) | Curva | Cursore | Note |
|---|---|---|---|---|
| **Ascension** | 18 / 14 | Linear | 16 | Replica esatta della Fase 1. |
| **Classic** | 18 / 14 | Linear | 16 | Nessun soft-target in Classic → il Tab-target (L3) è la selezione primaria. |
| **Retail** | 20 / 15 | Power (1.5) | 18 | Camera più reattiva con curva non lineare (controllo fine al centro); esiste il soft-target. |

I keybind di action bar sono identici tra i preset: la differenza di targeting tra versioni si
riflette nel **flusso di gioco**, non in tasti diversi. Personalizza pure ogni profilo dalla GUI.

## Preset di classe (Fase 4) — `classes/`

I preset di classe (`profiles/classes/*.json`, es. `warrior.json`, `mage.json`, `hunter.json`) sono
**override** applicabili *sopra* un profilo di versione. Non sono profili completi: contengono solo
`abilityOverrides` (sostituzioni/aggiunte alla tabella `abilities`, per `button × layer`) ed
eventualmente un `radialMenu` tarato sulla classe. Si applicano **dalla GUI** (menu *Preset di
classe* → *Applica*): il risultato è un normale profilo utente da salvare. A runtime non esiste
alcuna logica speciale — resta tutto 1:1.

```jsonc
{
  "schemaVersion": "1.0",
  "name": "Warrior",
  "className": "Warrior",
  "gameVersion": "",               // "" = qualsiasi versione (informativo/di filtro)
  "description": "…assunzioni sui keybind…",
  "abilityOverrides": [            // sostituisce/aggiunge voci della tabella abilities
    { "button": "DPadUp", "layer": "Shoulder_LBRB", "bind": { "key": "F7" } }
  ],
  "radialMenu": {                  // opzionale: se presente, sostituisce il radial del profilo base
    "enabled": true, "trigger": "LeftThumb", "selectDeadzone": 0.4,
    "items": [ { "label": "Cavalcatura", "bind": { "key": "F1" } } ]
  }
}
```

> **Assunzioni sui keybind.** Nel modello external-only emettiamo solo tasti: quali abilità stiano
> dietro gli slot/tasti si imposta **in gioco**. Ogni preset di classe documenta nella `description`
> quale tasto lega a quale utility (es. `F1=Cavalcatura`). Sono suggerimenti: modificali dalla GUI.

## Companion addon (Fase 4) — opzionale

Il file SavedVariables del [companion addon](../addon/ControllerWarcraftCompanion/README.md) è letto
dall'App **solo se** `companionEnabled:true` in `settings.json` (con `companionSavedVariablesPath`).
È **sola lettura** e serve solo come contesto (overlay): non guida mai l'input. Vedi il README
dell'addon per installazione, campi esposti e limiti (i SavedVariables non sono real-time).
