# ControllerWarcraft — Gui (Fase 2: editor di profili)

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
MainWindow.xaml(.cs)                  UI: selezione + pannelli impostazioni + DataGrid
App.xaml(.cs)                         bootstrap WPF
```

Il modello dati (schema profilo, `ProfileManager`, preset) vive nel Core condiviso, quindi GUI e
App restano sempre allineate sul formato.

## Limiti attuali (Fase 3 → Fase 4)

- I **tasti di movimento** (WASD) restano modificabili solo nel JSON.
- Nessun avvio/arresto del runtime dalla GUI: si lancia `cwapp` separatamente. Integrazione
  (tray, start/stop) prevista nelle fasi successive.
- L'overlay è configurabile (on/off) ma non ha ancora il **radial menu** (Fase 4).
