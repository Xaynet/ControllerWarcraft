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

### Companion addon opzionale (fase futura)
Dove l'addon è supportato, un piccolo companion può esporre lo stato di gioco
(target, cooldown) per targeting/context migliori. **Deve restare opzionale**: mai
un requisito.

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
- **Fase 2 — Profili & config:** sistema profili JSON, GUI di remap, profili per Ascension/Classic/Retail.
- **Fase 3 — UX:** layer multipli, curve sensibilità, overlay indicatore modalità, auto-switch profilo.
- **Fase 4 — Polish:** radial menu overlay, companion addon opzionale, preset per classe.

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

## 11. Decisioni aperte

Prese (Fasi 0-1):
- **Linguaggio/stack:** C# / .NET (target `net10.0-windows`, l'SDK installato).
- **Output:** SendInput via P/Invoke (scelto per semplicità; Interception resta un'opzione futura per robustezza).
- **Scope MVP:** solo Ascension, con keybind hardcoded. Il profilo generico/multi-versione arriva in Fase 2.

Ancora aperte:
- Interception driver (kernel) al posto di SendInput dove serve maggiore robustezza.
- Companion addon: sì/no e per quali versioni.
- Layer aggiuntivo +LB+RB (4° stato) se servono più slot.
