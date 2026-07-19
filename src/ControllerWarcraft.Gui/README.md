# ControllerWarcraft — Gui (editor di profili + wizard + test controller)

Applicazione **WPF** per selezionare il profilo attivo, vedere/modificare le mappature e le
curve di sensibilità, e salvare. È l'anello "GUI Configuratore" di [ANALISI.md §5](../../ANALISI.md)
ed è costruita sullo stack consigliato in [§6](../../ANALISI.md) (C#/.NET + WPF + JSON).

> La GUI **non invia mai input** al gioco: legge e scrive soltanto i profili JSON tramite lo
> stesso `ProfileManager` usato dal runtime ([`ControllerWarcraft.Core`](../ControllerWarcraft.Core/)).
> Il pannello di test del controller **legge** il gamepad tramite il `XInputReader` di **sola
> lettura** del Core: non esiste alcun percorso, nella GUI, per emulare tastiera/mouse (SendInput
> vive solo nell'App).

La finestra è organizzata in due tab — **Editor profili** e **Test controller** — più un
**wizard di primo avvio** riapribile dal pulsante *Wizard di primo avvio*.

## Wizard di primo avvio

Compare **automaticamente al primo lancio** (quando manca `settings.json` o il flag
`setupCompleted` è `false`) e resta riapribile in qualsiasi momento. Guida l'utente nei punti che
davano più attrito ai tester:

1. **Benvenuto + test del controller** (pannello live, vedi sotto).
2. **Scelta della versione** (Ascension/Classic/Retail) + preset di classe opzionale → salvato come
   profilo attivo.
3. **Keybinding da impostare in WoW** (tabella 1-9, Shift+1-9, Ctrl+1-9, Shift+Ctrl+1-9), con nota
   che sono adattabili.
4. **Prompt UAC/admin** di `cwapp.exe` e funzionamento della **modalità cursore** (toggle R3, o
   Hold/None).

Al termine (*Fine*) o allo *Salta* imposta `setupCompleted = true` in `settings.json`, così non
riappare. È retro-compatibile: un `settings.json` esistente senza il flag lo tratta come `false`
e mostra il wizard **una volta**.

## Test controller (live)

Il tab **Test controller** (e il primo passo del wizard) mostra in tempo reale lo stato del
gamepad — posizione dei due stick, grilletti analogici, D-pad e tutti i pulsanti — leggendo XInput
~60 volte al secondo. Serve a confermare che il controller funziona e a capire la mappatura, ed è
un utile strumento di troubleshooting (drift dello stick, grilletto sporco, controller sullo slot
sbagliato). È possibile selezionare lo **slot XInput** (0-3).

## Cosa fa

- **Selezione profilo** — menu a tendina con tutti i profili (preset + utente). Mostra qual è il
  profilo attivo (da `settings.json`).
- **Editing impostazioni** — mouselook (sensibilità X/Y, inversione, deadzone e **curva di
  risposta** Linear/Power/Exponential), cursore (velocità, inversione, **pulsante e modalità di
  attivazione**), movimento (soglia, deadzone), con slider + campo numerico.
- **Attivazione modalità cursore & hardening input** — nel pannello *Cursore* scegli il **pulsante**
  (R3/L3/Start/None) e la **modalità** (Toggle/Hold); nel pannello *Hardening input* imposti l'**hold
  minimo** (ms) che scarta le pressioni accidentali di L3/R3. Default = comportamento storico.
- **Editing mappature** — tabella `(pulsante × layer) → keybind` completamente editabile, con i
  **4 layer** (Base/+LB/+RB/+LB+RB); aggiungi/rimuovi righe, cambia tasto e modificatori.
- **Editing binding di sistema** (Fase 3) — Salto / Tab-target / Annulla ora modificabili.
- **Impostazioni globali** (Fase 3) — overlay on/off, auto-switch profilo e mappa
  `processo → profilo`, con salvataggio in `settings.json`.
- **Radial menu** (Fase 4) — attiva/disattiva, scegli il trigger (L3/R3), la soglia di selezione e
  le **voci** (etichetta + un solo keybind). Ogni voce = un tasto (1:1): nessuna sequenza.
- **Preset di classe** (Fase 4) — scegli un preset (Warrior/Mage/Hunter/…) e premi *Applica*: gli
  override (abilità + radial) vengono uniti sul profilo corrente. Rivedi e poi *Salva*.
- **Salva** — scrive il profilo nella cartella utente (`%APPDATA%/ControllerWarcraft/profiles/`),
  lasciando i preset del repo intatti.
- **Imposta come attivo** — aggiorna `settings.json`; l'App lo caricherà al prossimo avvio.

## Come eseguire

```powershell
dotnet run -c Release --project src/ControllerWarcraft.Gui
```

oppure l'eseguibile compilato:

```powershell
./src/ControllerWarcraft.Gui/bin/Release/net10.0-windows/cwgui.exe
```

## Architettura

MVVM leggero, senza dipendenze esterne:

```
Mvvm/        ObservableObject          base INotifyPropertyChanged
             RelayCommand              ICommand delegante
             BoolToBrushConverter      evidenzia i pulsanti premuti nel pannello di test
ViewModels/  MainViewModel             elenco profili, impostazioni bindabili, comandi, wizard, test
             AbilityRowViewModel       riga editabile della tabella abilità
             KeybindEditorViewModel    adattatore editabile per i Keybind di sistema (record struct)
             ProcessMapRowViewModel    riga della mappa auto-switch processo → profilo
             RadialItemRowViewModel    riga editabile di una voce del radial menu (Fase 4)
             ControllerTestViewModel   polling live (sola lettura) del gamepad via Core XInputReader
             WizardViewModel           passi del wizard di primo avvio (logica dati nel Core)
Controls/    ControllerTestView.xaml   pannello di test riusabile (tab + wizard)
Windows/     WizardWindow.xaml         finestra modale del wizard di primo avvio
MainWindow.xaml(.cs)                   UI: tab Editor/Test + pulsante wizard + avvio wizard al 1° lancio
App.xaml(.cs)                          bootstrap WPF
```

Il modello dati (schema profilo, `ProfileManager`, preset), i contenuti puri del wizard
(`Onboarding/OnboardingInfo`) e il lettore XInput di sola lettura (`Input/XInputReader`) vivono nel
Core condiviso, quindi GUI e App restano sempre allineate sul formato e il P/Invoke di lettura non è
duplicato.

## Limiti attuali

- I **tasti di movimento** (WASD) restano modificabili solo nel JSON.
- Nessun avvio/arresto del runtime dalla GUI: si lancia `cwapp` separatamente. Integrazione
  (tray, start/stop) prevista nelle fasi successive.
- L'ordine dei settori del radial segue l'ordine della lista di voci: non c'è ancora un editor
  grafico (drag) della disposizione.
- Il percorso dei SavedVariables del companion si imposta a mano in `settings.json`
  (`companionSavedVariablesPath`).
