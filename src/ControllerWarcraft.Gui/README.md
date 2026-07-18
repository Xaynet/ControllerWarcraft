# ControllerWarcraft — Gui (Fase 2: editor di profili)

Applicazione **WPF** per selezionare il profilo attivo, vedere/modificare le mappature e le
curve di sensibilità, e salvare. È l'anello "GUI Configuratore" di [ANALISI.md §5](../../ANALISI.md)
ed è costruita sullo stack consigliato in [§6](../../ANALISI.md) (C#/.NET + WPF + JSON).

> La GUI **non invia mai input** al gioco: legge e scrive soltanto i profili JSON tramite lo
> stesso `ProfileManager` usato dal runtime ([`ControllerWarcraft.Core`](../ControllerWarcraft.Core/)).

## Cosa fa

- **Selezione profilo** — menu a tendina con tutti i profili (preset + utente). Mostra qual è il
  profilo attivo (da `settings.json`).
- **Editing impostazioni** — mouselook (sensibilità X/Y, inversione, deadzone), cursore
  (velocità, inversione), movimento (soglia, deadzone), con slider + campo numerico.
- **Editing mappature** — tabella `(pulsante × layer) → keybind` completamente editabile
  (aggiungi/rimuovi righe, cambia tasto e modificatori Shift/Ctrl/Alt).
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
Mvvm/        ObservableObject      base INotifyPropertyChanged
             RelayCommand          ICommand delegante
ViewModels/  MainViewModel         elenco profili, impostazioni bindabili, comandi
             AbilityRowViewModel   riga editabile della tabella abilità
MainWindow.xaml(.cs)               UI: selezione + pannelli impostazioni + DataGrid
App.xaml(.cs)                      bootstrap WPF
```

Il modello dati (schema profilo, `ProfileManager`, preset) vive nel Core condiviso, quindi GUI e
App restano sempre allineate sul formato.

## Limiti attuali (Fase 2 → Fase 3)

- I **binding di sistema** (Salto, Tab-target, Annulla) e i **tasti di movimento** sono mostrati
  ma non ancora editabili dalla GUI: modificabili nel JSON. Editing completo previsto in Fase 3.
- Le **curve di sensibilità** sono lineari (un fattore); l'accelerazione non lineare è Fase 3.
- Nessun avvio/arresto del runtime dalla GUI: si lancia `cwapp` separatamente. Integrazione
  (tray, start/stop, overlay) prevista nelle fasi successive.
