# Changelog

Tutte le modifiche rilevanti a questo progetto sono documentate in questo file.

Il formato segue [Keep a Changelog](https://keepachangelog.com/it/1.1.0/) e il
progetto aderisce al [Semantic Versioning](https://semver.org/lang/it/).

## [Unreleased]

### Added
- **Attivazione modalità cursore configurabile** (hardening input). Nuovi campi profilo
  `cursor.activationButton` (`None` / `RightThumb` / `LeftThumb` / `Start`) e
  `cursor.activationMode` (`Toggle` / `Hold`):
  - **Toggle** (default): una pressione entra in modalità cursore, un'altra esce (comportamento storico).
  - **Hold** (momentaneo): la modalità cursore è attiva solo *mentre* il pulsante è tenuto premuto.
  - Il pulsante di attivazione è **rimappabile** (non più solo R3) e la modalità cursore si può
    **disattivare** del tutto (`activationButton: "None"`).
- **Mitigazione delle pressioni accidentali** di L3/R3 (e Start): nuova soglia opzionale di **hold
  minimo** (`inputHardening.thumbClickMinHoldMs`, ms). Un click troppo breve non attiva più
  cursore / Tab-target / radial. Default `0` = comportamento storico invariato.
- GUI: nuove opzioni nel pannello *Cursore* (pulsante e modalità di attivazione) e nuovo pannello
  *Hardening input* (hold minimo).

### Changed
- Schema profilo a **v1.3**. I file v1.0/v1.1/v1.2 restano validi: i nuovi campi hanno default che
  riproducono **esattamente** il comportamento precedente (cursore su R3 in Toggle, hold minimo 0).
- Preset `ascension`/`classic`/`retail` aggiornati con i nuovi campi impostati al comportamento attuale.

### Precedenza (documentata)
- Se un pulsante è sia il trigger del **radial menu** sia quello di attivazione **cursore**, vince
  il radial. Se **L3** è usato per il cursore, non fa più Tab-target. L'hold minimo si applica a
  toggle/hold cursore, Tab-target e apertura del radial.

## [0.1.1] - 2026-07-19

### Fixed
- In gioco non veniva recepito alcun input quando il client girava come
  amministratore (comune su Ascension): `cwapp` ora include un manifest
  `requireAdministrator` e si auto-eleva all'avvio, così l'input iniettato
  raggiunge la finestra elevata (UIPI). La GUI resta non elevata.

## [0.1.0] - 2026-07-19

Prima release pubblica. Giocare a World of Warcraft con controller Xbox su
**Ascension, Classic e Retail** tramite un'app esterna che emula tastiera+mouse
(mapping 1:1, nessuna automazione).

### Added

#### Core / runtime
- Approccio **esterno**: lettura controller via XInput ed emulazione tastiera+mouse
  via SendInput, indipendente dalla versione del gioco.
- **Movimento** (stick sinistro → WASD) e **camera/mouselook** (stick destro → RMB
  tenuto + delta mouse) con **curve di sensibilità** configurabili (Linear/Power/
  Exponential) e deadzone dal profilo.
- **Layer di abilità** con LB/RB come *shift*: 4 stati per pulsante (Base, +LB, +RB,
  +LB+RB) mappati agli slot dell'action bar (1-9, Shift+1-9, Ctrl+1-9, Shift+Ctrl+1-9).
- **Tab-target** (L3) e **modalità cursore** (toggle R3) per loot/vendor/talenti.
- **Macchina a stati** delle modalità (Movimento/Combattimento ↔ Cursore).

#### Profili & configurazione
- **Sistema profili JSON** con `ProfileManager` (carica/salva, profilo attivo,
  fallback ai preset built-in).
- **Preset per versione**: Ascension, Classic, Retail.
- **Preset per classe** (warrior/mage/hunter) applicabili come override sul profilo.
- **GUI di remap** (WPF) per selezionare profilo/classe, editare mappature, curve di
  sensibilità e voci del radial menu.

#### UX
- **Overlay** trasparente always-on-top con indicatore di modalità e layer correnti.
- **Radial menu** on-screen (tieni il trigger, inclina lo stick, rilascia per inviare
  **un solo** keybind — 1:1, nessuna sequenza).
- **Auto-switch profilo** in base all'eseguibile in primo piano, con pausa opzionale
  dell'emulazione fuori dal gioco.

#### Companion (opzionale)
- **Companion addon** WoW (Lua) in sola lettura che espone stato di gioco
  (bersaglio, combattimento, vita/risorsa) come contesto per l'overlay. Mai richiesto:
  l'app funziona al 100% senza.

#### Distribuzione & documentazione
- Solution `ControllerWarcraft.slnx` che raggruppa tutti i progetti.
- **CI** GitHub Actions (build della solution su push/PR verso `main`).
- **Release automatica** su tag `v*.*.*`: pubblica `cwapp.exe` + `cwgui.exe`
  (self-contained win-x64 single-file), la cartella `profiles/` (con `classes/`) e il
  companion addon, allegati a una GitHub Release.
- Documentazione: [README](README.md), [QUICKSTART](QUICKSTART.md) (utente finale),
  [ANALISI](ANALISI.md) (design), [RELEASING](RELEASING.md) (processo di rilascio).
- Licenza **MIT**.

[Unreleased]: https://github.com/Xaynet/ControllerWarcraft/compare/v0.1.1...HEAD
[0.1.1]: https://github.com/Xaynet/ControllerWarcraft/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/Xaynet/ControllerWarcraft/releases/tag/v0.1.0
