# ControllerWarcraft — Analisi & Design

Giocare a World of Warcraft con controller Xbox invece di mouse e tastiera,
su più versioni del gioco.

**Priorità versioni:** 1) Ascension WoW · 2) Classic WoW · 3) Retail WoW

---

## 1. Obiettivo

Permettere di giocare a WoW **interamente da controller Xbox**, con un'esperienza
fluida (movimento, camera, combattimento, gestione UI/inventario), su tutte le
versioni principali del gioco.

## 2. Il vero problema e la scelta architetturale

Gli addon di controller (es. ConsolePort) vivono **dentro** il client e dipendono
dall'API Lua di quella specifica versione. Ascension, Classic e Retail hanno API
diverse; Ascension usa un client custom su server privato. Da qui l'incompatibilità.

**Decisione chiave: NON costruire un addon, ma un'applicazione ESTERNA.**

```
[Controller Xbox] --XInput--> [ControllerWarcraft] --SendInput--> [Client WoW]
                                (legge gamepad)      (emula KB+mouse)
```

Il client vede solo tastiera e mouse. Non sa (e non gli importa) che versione è:
il motore di input è lo stesso ovunque. **Le differenze tra versioni diventano
semplici profili di keybinding (JSON), non codice.** Questo disaccoppia il progetto
dal problema dell'API degli addon e garantisce la compatibilità cross-versione
richiesta.

## 3. Le sfide specifiche di WoW con controller

WoW è progettato per mouse+tastiera. I nodi difficili sono:

| Sfida | Descrizione | Soluzione |
|---|---|---|
| **Camera** | Guardarsi intorno = muovere il mouse con tasto destro premuto (mouselook) | Stick destro → tieni premuto RMB e traduci lo stick in delta-mouse relativi |
| **Movimento** | WoW usa WASD digitale, non analogico | Stick sinistro → WASD con deadzone; avanti/indietro + strafe |
| **Poche buttons vs molte abilità** | ~16 tasti fisici contro 12+ slot azione × più barre | **Layer/modificatori**: LB e RB come "shift" → ogni tasto ha 3-4 stati |
| **Targeting** | Selezionare nemici senza mouse | Tab-target su un tasto + "target nearest"; su Retail sfruttare il soft-target |
| **UI / loot / vendor / mappa** | Serve un puntatore per cliccare | **Modalità cursore**: toggle che trasforma lo stick destro in cursore virtuale |

## 4. Modello di mapping (cuore del progetto)

**Budget tasti Xbox:** A/B/X/Y, LB/RB, LT/RT (analogici), L3/R3 (click stick),
D-pad ×4, Start, View. → ~16 discreti + 2 stick analogici.

**Layer con modificatori (modello ConsolePort-like):**
LB e RB usati come shift. 4 tasti frontali × 4 combinazioni (normale, +LB, +RB,
+LB+RB) = 16 abilità solo dai tasti frontali, più D-pad × layer, ecc. → copre
tutte le action bar.

**Macchina a stati (modalità):**
- **Movimento/Combattimento** (default): stick sx = movimento, stick dx = camera (mouselook)
- **Cursore**: stick dx = cursore virtuale, A = click sx, X = click dx (loot, vendor, talenti, mappa)
- **Menu** (opzionale): D-pad naviga la UI
- Indicatore di modalità sempre visibile (overlay/tray).

## 5. Architettura software

Motore modulare, indipendente dalla versione:

```
┌─────────────────────────────────────────────┐
│  GUI Configuratore  (remap, profili, curve)  │
├─────────────────────────────────────────────┤
│  Profile Manager  (JSON per versione/classe) │
├─────────────────────────────────────────────┤
│  Mapping Engine                              │
│   • State machine (modalità)                 │
│   • Layer/modificatori                       │
│   • Curve deadzone/sensibilità               │
├──────────────┬──────────────┬────────────────┤
│ Input Poller │ Mouselook Mgr│ Output Emulator │
│ (XInput)     │ (RMB+delta)  │ (SendInput KB+M)│
├──────────────┴──────────────┴────────────────┤
│  Overlay (opzionale: modalità, radial menu)  │
└─────────────────────────────────────────────┘
```

- **Input Poller** — legge lo stato del gamepad ad alta frequenza.
- **Mapping Engine** — applica profilo + stato + layer → produce "intenti".
- **Output Emulator** — inietta tastiera/mouse via SendInput.
- **Mouselook Manager** — mantiene RMB premuto e converte lo stick destro in movimento mouse relativo, con curva di sensibilità/accelerazione.
- **Profile Manager** — un profilo JSON per versione e per classe/build.
- **Overlay** — finestra trasparente always-on-top per indicatore modalità e (fase avanzata) radial menu.
- **Auto-switch profilo** — opzionale: rileva la finestra/eseguibile attivo per caricare il profilo giusto.

### Compatibilità cross-versione
Poiché emettiamo **solo** tastiera+mouse, il core è agnostico alla versione. Le
differenze (quali keybind esistono, targeting, soft-target) si gestiscono **solo
nei profili**. Path "external-only" funziona **sempre**, anche dove gli addon sono
bloccati o l'API differisce (Ascension, Classic Era).

### Companion addon opzionale (Fase 4 — fatto)
Dove l'addon è supportato, un piccolo companion espone in **sola lettura** lo stato di gioco
(target, vita/risorsa, combattimento) per dare *contesto* migliore. **Resta strettamente opzionale**:
mai un requisito. Canale = **SavedVariables** (l'unico conforme alla sandbox Lua): l'addon scrive lo
stato, l'App lo legge se abilitato. Limite noto: i SavedVariables sono scritti dal client a
logout/reload, quindi sono uno **snapshot**, non un feed live — motivo in più per non usarli mai per
guidare l'input. Vedi [`addon/ControllerWarcraftCompanion`](addon/ControllerWarcraftCompanion/README.md).

## 6. Stack tecnologico consigliato

Piattaforma target: **Windows**.

| Componente | Scelta consigliata | Alternative |
|---|---|---|
| Linguaggio/Runtime | **C# / .NET 8** | C++ (latenza minima), Rust, AutoHotkey (solo prototipo) |
| Lettura gamepad | **Vortice.XInput** o Windows.Gaming.Input | SharpDX (deprecato) |
| Output KB/mouse | **SendInput** via P/Invoke | Interception driver (kernel, più robusto ma install complessa) |
| GUI | **WPF** | WinUI 3, Avalonia |
| Config | **JSON** | TOML |

Motivazione C#/.NET: sviluppo rapido su Windows, ottimo accesso a XInput e SendInput,
packaging semplice, latenza più che sufficiente per WoW (non è un FPS competitivo).

## 7. Prior art (da studiare / differenziare)

- **ConsolePort** — addon ufficiale, UX ottima ma **dipende dalla versione** (il nostro problema).
- **WoWmapper** — approccio esterno identico al nostro (legge controller, emula KB/M) + companion addon. Riferimento principale.
- **Steam Input / reWASD / JoyToKey / Xpadder** — remapper generici. **Fanno già ~70% del lavoro.**

**Nota di onestà ingegneristica:** Steam Input + una buona config coprono molto.
Il nostro valore aggiunto deve essere: mouselook tarato per WoW, action bar a layer,
modalità cursore, macchina a stati delle modalità e **profili pronti per versione**
(Ascension/Classic/Retail) out-of-the-box. Se non offriamo questo, reWASD basta.

## 8. Rischi e vincoli

- **ToS / Anti-cheat (Retail & Classic — Blizzard Warden).** Un remap **1:1**
  (un input fisico → un input di gioco, nessuna automazione) è ampiamente usato e
  tollerato (ConsolePort, Steam esistono). **Da evitare assolutamente**: macro che
  eseguono più azioni/rotazioni automatiche da un solo tasto, timer, automazione.
  Restare rigorosamente 1:1. Nessuna garanzia: le policy possono cambiare.
- **Ascension** — server privato con client custom: **verificare le loro regole**
  sugli input tool esterni prima di distribuire.
- **Latenza** — polling ad alta frequenza + elaborazione minima nel loop.
- **Mouselook stabilità** — la gestione RMB-tenuto-premuto è la parte più delicata; edge case su alt-tab, cursore che scappa, cambio finestra.

## 9. Roadmap a fasi (MVP-first)

- **Fase 0 — Spike tecnico:** ✅ *Fatto.* Leggere XInput + iniettare WASD e mouselook.
  Prova di fattibilità dell'approccio esterno. → [`src/ControllerWarcraft.Spike`](src/ControllerWarcraft.Spike/README.md).
- **Fase 1 — MVP giocabile:** ✅ *Fatto.* Movimento + camera + layer di abilità (LB/RB come
  shift: Base/+LB/+RB) + tab-target (L3) + modalità cursore (toggle R3) + macchina a stati
  delle modalità con indicatore a console. Profilo Ascension hardcoded. Architettura modulare
  (Poller / Emulator / Profile / MappingEngine separati dal main loop).
  → [`src/ControllerWarcraft.App`](src/ControllerWarcraft.App/README.md).
- **Fase 2 — Profili & config:** ✅ *Fatto.* Sistema profili JSON (System.Text.Json) con
  `ControllerProfile` serializzabile + `ProfileManager` (Core condiviso), preset versionati per
  Ascension/Classic/Retail in [`profiles/`](profiles/README.md), caricamento del profilo attivo
  al posto dell'hardcoded (fallback built-in Ascension) e GUI WPF di remap
  ([`src/ControllerWarcraft.Gui`](src/ControllerWarcraft.Gui/README.md)) per selezione profilo,
  editing mappature/curve e salvataggio.
- **Fase 3 — UX:** ✅ *Fatto.* Overlay indicatore di modalità (WPF trasparente, click-through,
  non ruba il focus → [`src/ControllerWarcraft.Overlay`](src/ControllerWarcraft.Overlay/README.md)),
  curve di sensibilità del mouselook (Linear/Power/Exponential in `ResponseCurve`, Core), quarto
  layer +LB+RB (Shift+Ctrl) come 4° stato dei modificatori, auto-switch profilo in base alla
  finestra in primo piano (mappa processo→profilo in `settings.json`, con pausa opzionale fuori
  gioco) ed editing dei binding di sistema e delle curve nella GUI. Schema profilo a `v1.1`,
  retro-compatibile con i file `v1.0`.
- **Fase 4 — Polish:** ✅ *Fatto.* Radial menu overlay (tieni premuto L3/R3 → menu radiale WPF
  click-through; muovi lo stick destro verso un settore, rilascia per inviare **un solo** keybind —
  rigorosamente 1:1, nessuna sequenza; voci configurabili nello schema profilo e dalla GUI). Preset
  per **classe** come override applicabili sopra il preset di versione (schema `ClassPreset` nel Core,
  merge `ApplyTo`, esempi Warrior/Mage/Hunter in [`profiles/classes/`](profiles/README.md),
  selezionabili e applicabili dalla GUI). **Companion addon opzionale** (WoW/Lua) che espone in sola
  lettura lo stato di gioco via SavedVariables → [`addon/ControllerWarcraftCompanion`](addon/ControllerWarcraftCompanion/README.md);
  l'App lo legge solo se abilitato e lo usa **solo come contesto** (mai per guidare input). Schema
  profilo a `v1.2`, retro-compatibile con i file `v1.0`/`v1.1`.

- **Hardening input (post-Fase 4):** ✅ *Fatto.* Attrito segnalato da un tester: la modalità
  cursore era un toggle **fisso su R3**, non configurabile né disattivabile, e L3/R3 sono facili da
  premere per sbaglio inclinando la levetta. Introdotti: (a) **attivazione cursore configurabile**
  — pulsante rimappabile (`cursor.activationButton`: `None`/`RightThumb`/`LeftThumb`/`Start`) e
  modalità `Toggle` (default storico) o `Hold` (momentaneo), con `None` che disattiva la modalità;
  (b) **soglia di hold minimo** (`inputHardening.thumbClickMinHoldMs`) che scarta i click-stick
  troppo brevi/accidentali. Logica riusabile nel Core (`HoldGate`, puro e testabile — il tempo è
  iniettato dall'esterno); il `MappingEngine` legge tutto dal profilo. Schema a **v1.3**,
  retro-compatibile: i default riproducono esattamente il comportamento precedente (cursore su R3
  in Toggle, hold 0). **Precedenza** (documentata): radial menu > attivazione cursore > funzione
  storica del pulsante; l'hold minimo si applica a toggle/hold cursore, Tab-target e apertura del
  radial. Rigorosamente 1:1: si rimappa *quando* e *come* si legge l'input, mai *cosa* viene
  eseguito — nessuna automazione.

- **Test automatici & CI (post-Fase 4):** ✅ *Fatto.* Progetto `src/ControllerWarcraft.Tests`
  (xUnit, `net10.0-windows`) che copre i componenti **puri** del Core con casi limite: `HoldGate`
  (soglia hold minimo), `RadialMenuResolver` (geometria dei settori), `ResponseCurve` (curve di
  sensibilità), `ClassPreset.ApplyTo` (merge/override), `CompanionStateReader.TryParse` (parser
  tollerante) e `ProfileManager` (round-trip save→load, fallback ai built-in). Include un test
  esplicito di **retro-compatibilità** dei profili (un JSON privo dei campi recenti riproduce i
  default storici) e la validazione dei preset JSON reali. La CI (`ci.yml`) esegue `dotnet test`
  dopo la build su ogni push/PR verso `main`. Restano scoperti — perché richiedono XInput/SendInput
  o UI e non sono testabili headless — `MappingEngine`, l'App, la GUI e l'Overlay.

- **Onboarding: wizard di primo avvio + test controller (post-Fase 4):** ✅ *Fatto.* Attrito
  segnalato dai tester: (a) dimenticavano di impostare i keybinding dell'action bar in WoW; (b) non
  sapevano del prompt UAC/admin; (c) si confondevano con la modalità cursore (R3). La GUI ora riduce
  questi attriti dall'app stessa. Novità:
  - **Lettore XInput condiviso di sola lettura** nel Core (`Input/XInputReader` +
    `Input/ControllerReading`): fa P/Invoke di **esclusivamente** `XInputGetState`, nessun SendInput.
    App e Gui lo condividono senza duplicare il P/Invoke; la Gui **non può** iniettare input perché
    il Core non contiene alcun codice di output (SendInput resta esclusivo dell'App). L'App è stata
    aggiornata per leggere via questo componente (il `GamepadPoller` mantiene la normalizzazione di
    gioco), risolvendo l'accorpamento storico lettura+SendInput nel `NativeMethods` dell'App.
  - **Pannello di test del controller (live)**: tab dedicato nella GUI (e primo passo del wizard) che
    mostra in tempo reale stick, grilletti, D-pad e pulsanti (~60 Hz), con scelta dello slot XInput.
    Strumento di verifica e troubleshooting; **solo lettura**.
  - **Wizard di primo avvio**: mostrato automaticamente al primo lancio della GUI (settings.json
    assente o `setupCompleted = false`) e riapribile da un pulsante. Passi: benvenuto + test
    controller → scelta versione (+ preset di classe) salvata come profilo attivo → tabella dei
    keybinding da impostare in WoW → avviso UAC/admin e spiegazione della modalità cursore.
  - **Flag `setupCompleted`** in `AppSettings` (default false; assente ⇒ false ⇒ wizard mostrato una
    volta): retro-compatibile con i `settings.json` esistenti.
  - **Test aggiunti**: `ControllerReading.FromRaw` (normalizzazione pura raw→normalizzato), logica di
    onboarding (`NeedsSetup`, retro-compat del flag, `MarkSetupCompleted`, contenuti informativi).

## 10. Rilascio & CI/CD

Il rilascio è automatizzato via **GitHub Actions**, guidato dai **tag git** SemVer:

- **`.github/workflows/release.yml`** — al push di un tag `v*.*.*` compila e
  pubblica `src/ControllerWarcraft.App` (self-contained `win-x64`, single-file)
  su `windows-latest`, deriva la versione dal tag, comprime in uno zip versionato
  e crea una GitHub Release con release notes automatiche.
- **`.github/workflows/ci.yml`** — build in Release su push/PR verso `main`
  (tag esclusi) per garantire che tutto compili.

Dettagli e procedura in **[RELEASING.md](RELEASING.md)**. La Fase 1
(`ControllerWarcraft.App`) è ora integrata, quindi la pipeline è pienamente operativa.

**Fase 4:** lo zip include ora anche i preset di classe (`profiles/classes/`) e la cartella
`addon/` col companion **opzionale** (piccolo e self-contained; va copiato a mano in
`Interface/AddOns/`, non è richiesto per usare l'app).

## 11. Decisioni aperte

Prese (Fasi 0-1):
- **Linguaggio/stack:** C# / .NET (target `net10.0-windows`, l'SDK installato).
- **Output:** SendInput via P/Invoke (scelto per semplicità; Interception resta un'opzione futura per robustezza).
- **Scope MVP:** solo Ascension, con keybind hardcoded. Il profilo generico/multi-versione arriva in Fase 2.

Prese (Fase 2):
- **Formato config:** JSON via `System.Text.Json`, enum serializzati per nome, file leggibili
  e commit-abili. Schema versionato con `schemaVersion` per migrazioni future.
- **Architettura:** libreria condivisa `ControllerWarcraft.Core` (schema + `ProfileManager` +
  preset) referenziata sia dall'App (runtime) sia dalla Gui (editor), così l'enum `ScanCode` e i
  tipi di profilo sono un'unica fonte di verità.
- **Posizione profili:** preset sola-lettura accanto all'eseguibile (`<exe>/profiles/`, versionati
  nel repo); profili utente in `%APPDATA%/ControllerWarcraft/profiles/`. A parità di nome vince
  l'utente. Profilo attivo in `%APPDATA%/ControllerWarcraft/settings.json`.
- **GUI:** WPF (come da §6), MVVM leggero, nessun invio di input reale (solo editing profili).

Prese (Fase 3):
- **Overlay:** progetto WPF dedicato `ControllerWarcraft.Overlay` (libreria) ospitato dall'App
  su un thread STA con Dispatcher proprio. Motivazione: l'App resta un loop console sottile e le
  dipendenze WPF sono isolate in un componente riusabile e disaccoppiato (API a soli tipi semplici,
  nessun riferimento a App/Core). Click-through via stili estesi `WS_EX_TRANSPARENT | LAYERED |
  NOACTIVATE | TOOLWINDOW`. Fallback silenzioso al solo indicatore console se la UI non è disponibile.
- **Curve di sensibilità:** tipo `ResponseCurve` nel Core (Linear/Power/Exponential + esponente),
  applicato per-asse allo stick destro nel mouselook. Default `Linear` = comportamento storico
  (retro-compat). Editabile dalla GUI e presente nei preset JSON.
- **Layer +LB+RB:** aggiunto `AbilityLayer.Shoulder_LBRB` (priorità LB+RB > LB > RB > Base);
  preset mappano Shift+Ctrl+1..9. I profili senza questo layer restano validi (No-op).
- **Auto-switch:** logica di mapping processo→profilo pura nel Core (`AutoSwitchResolver`), lettura
  della finestra in primo piano nell'App (`ForegroundWatcher`, GetForegroundWindow +
  GetWindowThreadProcessId). Config in `settings.json` (`autoSwitchEnabled`,
  `pauseWhenGameNotForeground`, `processProfileMap`), tutti con default retro-compatibili.
- **Editing GUI:** binding di sistema (Salto/Tab-target/Annulla) e curve ora modificabili;
  aggiunto pannello impostazioni globali (overlay + auto-switch) che scrive `settings.json`.

Prese (Fase 4):
- **Radial menu:** costruito sul progetto `Overlay` (nuova `RadialMenuWindow` + `RadialMenuController`
  su thread STA come l'overlay di modalità). Trigger = L3 o R3 (`RadialTrigger`, default `None` =
  disattivo/retro-compat); mentre è tenuto premuto lo stick destro seleziona il settore e il rilascio
  invia **un solo** keybind. La geometria di selezione è pura e testabile nel Core
  (`RadialMenuResolver`, convenzione: voce 0 in alto, orario). Vincolo 1:1 garantito a livello di
  engine: un rilascio ⇒ al più un `TapKeybind`, esattamente come un'abilità; nessuna sequenza/timer.
- **Preset per classe:** tipo `ClassPreset` nel Core (override di `abilities` + eventuale `radialMenu`)
  con merge `ApplyTo` applicato **a tempo di editing** (GUI): il risultato è un normale profilo utente,
  quindi a runtime non esiste logica speciale. Esempi versionati in `profiles/classes/`. Per il modello
  external-only sono soprattutto *convenzioni documentate* (quale slot = quale abilità) + radial tarato
  sulla classe: le assunzioni sui keybind sono nella `description` di ciascun preset.
- **Companion addon:** **sì**, ma strettamente opzionale e in sola lettura. Canale = SavedVariables;
  lettore tollerante nel Core (`CompanionStateReader`), attivazione via `settings.json`
  (`companionEnabled` default false). Lo stato è **solo visualizzato** (overlay/console), mai usato
  per l'input. Versioni previste/testabili: Classic e Retail; Ascension da verificare (client custom).
- **Tasti funzione:** aggiunti `F1`-`F12` a `ScanCode` (naturali per le utility del radial), solo nomi
  nuovi ⇒ retro-compatibile.
- **Packaging:** la release include ora `profiles/classes/` e la cartella `addon/` nello zip (vedi §10).

Prese (Hardening input):
- **Attivazione cursore nel profilo:** i campi vivono in `CursorSettings` (`ActivationButton`,
  `ActivationMode`), coerenti con la scelta storica di tenere le impostazioni del cursore lì.
  Pulsanti ammessi solo "modali" (L3/R3/Start): non rubano uno slot azione. `None` = disattiva.
- **Debounce nel Core, non nell'App:** `HoldGate` è una struct pura (nessun orologio interno: il
  `dtMs` è passato dall'engine), così la logica di hold minimo è deterministica e testabile. Il
  `MappingEngine` misura il tempo con `Environment.TickCount64` in un overload di `Update`, ma
  espone anche `Update(snapshot, dtMs)` per i test.
- **Soglia condivisa:** un unico `thumbClickMinHoldMs` per tutti i trigger modali (L3/R3/Start),
  invece di soglie separate — più semplice da spiegare e tarare. Default 0 = nessun cambiamento.
- **Precedenza esplicita:** radial > cursore > funzione storica del pulsante, per evitare che due
  funzioni sullo stesso click-stick si attivino insieme.

Prese (Onboarding — wizard + test controller):
- **Lettore XInput nel Core, non nella Gui:** la lettura è un componente `XInputReader` di sola
  lettura nel Core, con un DTO normalizzato `ControllerReading` la cui conversione (`FromRaw`) è pura
  e testabile. Motivazione: condividere il P/Invoke tra App e Gui **senza** dare alla Gui la capacità
  di iniettare input — il vincolo è garantito strutturalmente (nel Core non esiste SendInput), non
  per convenzione. L'App riusa il lettore ma mantiene il suo `GamepadPoller` (deadzone/soglia di
  gioco), quindi nessuna regressione sul runtime.
- **Contenuti del wizard nel Core (`OnboardingInfo`):** `NeedsSetup`, la tabella dei keybinding WoW e
  le versioni suggerite sono dati puri, testabili senza WPF e riusabili; la Gui resta una vista
  sottile (WizardWindow + WizardViewModel).
- **`setupCompleted` in `AppSettings`:** un semplice flag booleano (default false). Wizard mostrato
  finché è false; *Fine* e *Salta* lo impostano a true. Retro-compatibile: assente ⇒ false ⇒ mostrato
  una volta, senza toccare gli altri campi.
- **Pannello riusato:** un unico `ControllerTestView` (UserControl) serve sia il tab della finestra
  principale sia il primo passo del wizard; il polling parte/si ferma su Loaded/Unloaded.

Ancora aperte:
- Interception driver (kernel) al posto di SendInput dove serve maggiore robustezza.
- Editing dei tasti di movimento (WASD) dalla GUI: per ora modificabili nel JSON.
- Editing grafico (drag) della disposizione dei settori del radial: per ora l'ordine è quello della
  lista di voci.
- Companion su Ascension: da verificare le regole del server privato prima di distribuirlo lì.
