# ControllerWarcraft — Guida al primo avvio

Giocare a World of Warcraft con un **controller Xbox** invece di mouse e tastiera.
Funziona su **Ascension, Classic e Retail** perché l'app è *esterna*: legge il
controller ed emula tastiera+mouse, senza addon obbligatori. Il mapping è
rigorosamente **1:1** (nessuna automazione — vedi [ANALISI.md §8](ANALISI.md)).

> Piattaforma: **Windows**. I file di release sono self-contained: **non serve
> installare .NET**.

## 1. Prerequisiti

- Un controller Xbox (cablato o wireless già accoppiato a Windows).
- Verifica che Windows lo veda (Pannello di controllo → *Dispositivi e stampanti*
  / *Configura controller di gioco USB*).

## 2. Scarica ed estrai

1. Vai su **GitHub → Releases** e scarica `ControllerWarcraft-vX.Y.Z-win-x64.zip`.
2. Estrailo in una cartella, es. `C:\ControllerWarcraft`.

Contenuto dello zip:

```
cwapp.exe            # runtime: legge il controller ed emula KB/mouse
cwgui.exe            # editor dei profili di remap
profiles/            # preset per versione (ascension/classic/retail) + classes/
addon/               # companion addon OPZIONALE (WoW/Lua)
RELEASING.md
```

## 3. Test sicuro (prima del gioco)

L'app invia **input reali** di tastiera/mouse: conviene provarla al sicuro.

1. Apri il **Blocco note** e tienilo in primo piano.
2. Avvia `cwapp.exe`.
3. Muovi lo **stick sinistro** → deve comparire `wasd`. Premi **X** → `1`.
4. Premi **BACK** sul controller per fermare l'app (rilascia sempre tutti i tasti).

Se funziona qui, l'iniezione input è a posto.

## 4. Configura i keybinding DENTRO WoW (una tantum, passo cruciale)

L'app manda dei **tasti**; il gioco deve avere le abilità legate a quei tasti.
In WoW: *Menu di gioco → Tasti (Keybindings)* imposta gli slot dell'action bar:

| Barra | Tasti |
|---|---|
| Principale (slot 1-9) | `1` … `9` |
| Seconda barra | `Shift+1` … `Shift+9` |
| Terza barra | `Ctrl+1` … `Ctrl+9` |
| Quarta barra (4° layer) | `Shift+Ctrl+1` … `Shift+Ctrl+9` |

Sono i default di molte configurazioni WoW. Se le tue differiscono, adatta i
binding in gioco **oppure** il profilo con `cwgui.exe`.

## 5. Scegli il profilo

Apri `cwgui.exe`:

- Seleziona la **versione**: Ascension / Classic / Retail.
- (Opzionale) applica un **preset di classe** (warrior / mage / hunter).
- (Opzionale) regola **sensibilità** e **curva** del mouselook, deadzone,
  inversione asse Y, e le voci del **radial menu**.
- **Salva.**

Il profilo attivo è in `%APPDATA%/ControllerWarcraft/settings.json`.

## 6. Gioca

1. Avvia `cwapp.exe`.
2. Avvia WoW ed entra in gioco.
3. L'**overlay** trasparente mostra modalità e layer correnti.
4. Premi **BACK** per fermare l'app.

## Comandi (preset default)

| Controller | Movimento / Combattimento | Modalità Cursore |
|---|---|---|
| Stick sinistro | Movimento (WASD) | Movimento |
| Stick destro | Camera (mouselook) | Cursore mouse |
| **LB / RB / LB+RB** (tenuti) | Layer abilità (Shift / Ctrl / Shift+Ctrl) | — |
| A | Salto | Click sinistro |
| X | Abilità 1 | Click destro |
| B | Abilità 2 | Escape |
| Y | Abilità 3 | — |
| RT / LT | Abilità 4 / 5 | — |
| D-pad ↑ → ↓ ← | Abilità 6 / 7 / 8 / 9 | — |
| **L3** (click stick sx) | Tab-target *(o radial menu)* | — |
| **R3** (click stick dx) | Toggle → Cursore *(o radial menu)* | Toggle → Movimento |
| BACK | Esci | Esci |

- I **layer** si attivano **tenendo** LB e/o RB *mentre* premi il tasto abilità.
  Priorità: **LB+RB > LB > RB > Base**.
- **Radial menu** (se attivo nel profilo): tieni premuto il trigger (L3/R3),
  inclina lo stick destro verso un settore e **rilascia** per inviare quella
  singola abilità. Rilascio al centro = annulla. Sempre **1:1**, nessuna sequenza.

## Risoluzione problemi

- **In gioco non registra i tasti, ma sul Blocco note sì.** Se WoW è avviato
  **come amministratore** (comune con alcuni launcher di private server, es.
  Ascension), avvia **anche `cwapp.exe` come amministratore**: Windows blocca
  l'invio di input da un processo non elevato verso una finestra elevata.
- **Camera troppo veloce o invertita.** Regola sensibilità X/Y e l'inversione
  dell'asse Y in `cwgui.exe`.
- **Nessun movimento.** Assicurati che la finestra di WoW sia in **primo piano**.
  Con l'auto-switch attivo, l'app si mette in pausa quando il gioco non è a fuoco.
- **Il controller non viene rilevato.** Ricollegalo/riaccoppialo e verifica che
  Windows lo elenchi tra i dispositivi di gioco prima di avviare `cwapp.exe`.

## Companion addon (opzionale)

Serve **solo** se vuoi vedere contesto di gioco (es. nome del bersaglio)
nell'overlay. **Non è necessario per giocare.**

1. Copia la cartella `addon/ControllerWarcraftCompanion` in
   `<cartella di WoW>/Interface/AddOns/`.
2. Abilita il companion in `cwgui.exe` (o in `settings.json`).

L'addon è in **sola lettura**: espone stato, non invia mai input.

## Nota importante (ToS)

- Il mapping è **1:1**, senza automazione: è l'uso tollerato dei remapper di input.
- **Ascension** è un server privato con client custom: **verifica le regole del
  server** sugli strumenti di input esterni prima di usarlo. La responsabilità
  finale dell'uso è dell'utente.

---

Per lo sviluppo e l'architettura vedi [ANALISI.md](ANALISI.md). Per il processo di
rilascio vedi [RELEASING.md](RELEASING.md).
