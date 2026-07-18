# ControllerWarcraft — Gui (editor di profili, Fase 2 → 4)

Applicazione **WPF** per selezionare il profilo attivo, vedere/modificare le mappature e le
curve di sensibilità, e salvare. È l'anello "GUI Configuratore" di [ANALISI.md §5](../../ANALISI.md)
ed è costruita sullo stack consigliato in [§6](../../ANALISI.md) (C#/.NET + WPF + JSON).

> La GUI **non invia mai input** al gioco: legge e scrive soltanto i profili JSON tramite lo
> stesso `ProfileManager` usato dal runtime ([`ControllerWarcraft.Core`](../ControllerWarcraft.Core/)).

## Cosa fa

- **Selezione profilo** — menu a tendina con tutti i profili (preset + utente). Mostra qual è il
  profilo attivo (da `settings.json`).
- **Editing impostazioni** — mouselook (sensibilità X/Y, inversione, deadzone e **curva di
  risposta** Linear/Power/Exponential), cursore (velocità, inversione), movimento (soglia,
  deadzone), con slider + campo numerico.
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
Mvvm/        ObservableObject         base INotifyPropertyChanged
             RelayCommand             ICommand delegante
ViewModels/  MainViewModel            elenco profili, impostazioni bindabili, comandi
             AbilityRowViewModel      riga editabile della tabella abilità
             KeybindEditorViewModel   adattatore editabile per i Keybind di sistema (record struct)
             ProcessMapRowViewModel   riga della mappa auto-switch processo → profilo
             RadialItemRowViewModel   riga editabile di una voce del radial menu (Fase 4)
MainWindow.xaml(.cs)                  UI: selezione + pannelli impostazioni + DataGrid
App.xaml(.cs)                         bootstrap WPF
```

Il modello dati (schema profilo, `ProfileManager`, preset) vive nel Core condiviso, quindi GUI e
App restano sempre allineate sul formato.

## Limiti attuali

- I **tasti di movimento** (WASD) restano modificabili solo nel JSON.
- Nessun avvio/arresto del runtime dalla GUI: si lancia `cwapp` separatamente. Integrazione
  (tray, start/stop) prevista nelle fasi successive.
- L'ordine dei settori del radial segue l'ordine della lista di voci: non c'è ancora un editor
  grafico (drag) della disposizione.
- Il percorso dei SavedVariables del companion si imposta a mano in `settings.json`
  (`companionSavedVariablesPath`).
