# Changelog

Tutte le modifiche rilevanti a questo progetto sono documentate in questo file.

Il formato segue [Keep a Changelog](https://keepachangelog.com/it/1.1.0/) e il
progetto aderisce al [Semantic Versioning](https://semver.org/lang/it/).

## [Unreleased]

### Added
- **Button-legend a layer (HUD)**: pannello overlay discreto, semi-trasparente e click-through che
  mostra cosa fa **ogni pulsante mappabile nel layer corrente** (es. `X → Shift+1`, `RT → Ctrl+4`),
  aggiornandosi quando si tiene premuto LB/RB (Base → +LB → +RB → +LB+RB). Aiuta a ricordare le
  abilità dei 4 layer *in gioco*. La logica di derivazione è **pura nel Core** (`ButtonLegend.Build`,
  profilo + layer → righe); l'overlay è pura presentazione. Configurabile (`settings.json` + GUI):
  on/off (`showButtonLegend`, default on), modalità di visibilità (`legendVisibility`:
  `AlwaysVisible` / `WhileModifierHeld`, default **solo mentre tieni un modificatore**) e angolo dello
  schermo (`legendCorner`, default in basso a destra).
- **Indicatore evidente della modalità cursore**: quando si è in modalità cursore l'overlay mostra una
  sottile **cornice colorata ai bordi dello schermo** + un **badge** ("MODALITÀ CURSORE"), così è
  impossibile non accorgersene (attrito segnalato da un tester). Click-through: non copre il gioco né
  intercetta il mouse. Configurabile (`showCursorIndicator`, default on).
- Test: logica pura della button-legend (`ButtonLegend.Build` su tutti i pulsanti/layer e i pulsanti
  non mappati, etichette, e `ButtonLegend.ShouldShow`) e retro-compatibilità dei nuovi campi di
  `AppSettings` (un `settings.json` senza i campi legenda/indicatore usa i default documentati).
- **Wizard di primo avvio** nella GUI (`cwgui.exe`): mostrato automaticamente al primo lancio
  (quando manca `settings.json` o il flag `setupCompleted` è `false`) e riapribile dal pulsante
  *Wizard di primo avvio*. Passi: benvenuto + test del controller → scelta versione
  (Ascension/Classic/Retail) e preset di classe opzionale (salvati come profilo attivo) → tabella
  dei keybinding da impostare in WoW → avviso sul prompt UAC/admin e spiegazione della modalità
  cursore. Al termine imposta `setupCompleted = true`.
- **Pannello di test del controller (live)** nella GUI: tab *Test controller* (e primo passo del
  wizard) che mostra in tempo reale posizione dei due stick, grilletti analogici, D-pad e pulsanti
  (~60 Hz), con scelta dello slot XInput (0-3). Strumento di verifica e troubleshooting.
- **Lettore XInput condiviso di sola lettura** nel Core (`XInputReader` + `ControllerReading`): fa
  P/Invoke di **esclusivamente** `XInputGetState`, nessun SendInput. Consente a GUI e App di leggere
  il controller condividendo un unico P/Invoke, **senza** che la GUI possa iniettare input
  (l'emulazione SendInput resta esclusiva dell'App). L'App legge ora via questo componente.
- **Flag `setupCompleted`** in `AppSettings` (Core, default `false`). Retro-compatibile: assente ⇒
  `false` ⇒ il wizard viene mostrato **una volta**; gli altri campi restano invariati.
- Test: normalizzazione pura `ControllerReading.FromRaw` (raw XInput → valori normalizzati) e logica
  di onboarding (`OnboardingInfo.NeedsSetup`, retro-compatibilità del flag, `MarkSetupCompleted`,
  contenuti informativi del wizard).
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
- **Suite di test automatici** (`src/ControllerWarcraft.Tests`, xUnit) sui componenti puri del Core:
  `HoldGate`, `RadialMenuResolver`, `ResponseCurve`, `ClassPreset.ApplyTo`,
  `CompanionStateReader.TryParse` e `ProfileManager` (round-trip, fallback). Incluso un test esplicito
  di **retro-compatibilità** dei profili (un JSON "vecchio stile" privo dei campi recenti produce i
  default storici: cursore su R3 in Toggle, hold minimo 0, radial disattivo, curva lineare) e la
  verifica che i preset JSON reali in `profiles/` e `profiles/classes/` deserializzino senza errori.
- **CI**: la pipeline esegue ora `dotnet test` dopo la build su ogni push/PR verso `main`.

### Changed
- Schema profilo a **v1.3**. I file v1.0/v1.1/v1.2 restano validi: i nuovi campi hanno default che
  riproducono **esattamente** il comportamento precedente (cursore su R3 in Toggle, hold minimo 0).
- Preset `ascension`/`classic`/`retail` aggiornati con i nuovi campi impostati al comportamento attuale.
- **Refactor interno (nessun cambiamento di comportamento del runtime)**: la lettura XInput è stata
  estratta dal `NativeMethods` dell'App al `XInputReader` di sola lettura del Core; il `NativeMethods`
  dell'App ora contiene solo l'emulazione SendInput. Il `GamepadPoller` dell'App usa il lettore
  condiviso mantenendo la stessa normalizzazione di gioco.
- La finestra della GUI è ora organizzata in tab (*Editor profili* / *Test controller*) con una
  toolbar che include il pulsante del wizard.

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
