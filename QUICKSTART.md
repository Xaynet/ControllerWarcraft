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

## 3. Primo avvio guidato (wizard) — consigliato

Apri **`cwgui.exe`**: al primo lancio parte automaticamente un **wizard di primo avvio** che copre i
passi seguenti in modo guidato (ed è riapribile in qualsiasi momento dal pulsante *Wizard di primo
avvio*). Ti fa:

1. **testare il controller** in tempo reale (stick, grilletti, pulsanti) — così confermi subito che
   Windows lo vede e capisci la mappatura;
2. **scegliere la versione** (Ascension/Classic/Retail) ed eventualmente un preset di classe →
   diventa il profilo attivo;
3. mostrarti la **tabella dei keybinding da impostare in WoW** (vedi §5);
4. avvisarti del **prompt UAC** e spiegarti la **modalità cursore**.

> Il `cwgui.exe` e il suo pannello di test **leggono soltanto** il controller: non inviano mai input
> al gioco. Il tab **Test controller** resta disponibile come strumento di diagnosi.

I paragrafi §4-§6 restano validi come riferimento (e per chi preferisce la configurazione manuale).

## 4. Test sicuro dell'iniezione (prima del gioco)

L'app `cwapp.exe` invia **input reali** di tastiera/mouse: conviene provarla al sicuro.

1. Apri il **Blocco note** e tienilo in primo piano.
2. Avvia `cwapp.exe`.
3. Muovi lo **stick sinistro** → deve comparire `wasd`. Premi **X** → `1`.
4. Premi **BACK** sul controller per fermare l'app (rilascia sempre tutti i tasti).

Se funziona qui, l'iniezione input è a posto.

## 5. Configura i keybinding DENTRO WoW (una tantum, passo cruciale)

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

## 6. Scegli il profilo

Apri `cwgui.exe` (tab **Editor profili**):

- Seleziona la **versione**: Ascension / Classic / Retail.
- (Opzionale) applica un **preset di classe** (warrior / mage / hunter).
- (Opzionale) regola **sensibilità** e **curva** del mouselook, deadzone,
  inversione asse Y, e le voci del **radial menu**.
- (Opzionale) scegli i **pulsanti modificatori** dei layer (pannello *Modificatori di layer*):
  default **LB/RB**, ma puoi usare **LT/RT** se li trovi più comodi. Se scegli un grilletto come
  modificatore, quel grilletto **non spara più** la sua abilità (la GUI te lo segnala).
- **Salva.**

Il profilo attivo è in `%APPDATA%/ControllerWarcraft/settings.json`.

## 7. Gioca

1. Avvia `cwapp.exe` e **accetta il prompt UAC** (serve per inviare input al gioco,
   che gira da amministratore).
2. Avvia WoW ed entra in gioco.
3. L'**overlay** trasparente mostra modalità e layer correnti. Due aiuti visivi in più:
   - **Button-legend**: un pannello discreto (per default in basso a destra) ricorda cosa fa ogni
     pulsante **nel layer corrente**; per default compare **solo mentre tieni premuto** LB/RB
     (Base → +LB → +RB → +LB+RB), così ti ricorda al volo cosa c'è su quel layer.
   - **Indicatore modalità cursore**: quando sei in cursore, una **cornice colorata ai bordi** e un
     **badge** rendono impossibile non accorgertene.
4. Premi **BACK** per fermare l'app.

> Puoi regolare questi aiuti da `cwgui.exe` (o in `settings.json`): mostrare la button-legend sempre
> o solo con un modificatore, sceglierne l'angolo, e attivare/disattivare l'indicatore cursore.

## Comandi (preset default)

| Controller | Movimento / Combattimento | Modalità Cursore |
|---|---|---|
| Stick sinistro | Movimento (WASD) | Movimento |
| Stick destro | Camera (mouselook) | Cursore mouse |
| **LB / RB / LB+RB** (tenuti) *(modificatori configurabili)* | Layer abilità (Shift / Ctrl / Shift+Ctrl) | — |
| A | Salto | Click sinistro |
| X | Abilità 1 | Click destro |
| B | Abilità 2 | Escape |
| Y | Abilità 3 | — |
| RT / LT | Abilità 4 / 5 | — |
| D-pad ↑ → ↓ ← | Abilità 6 / 7 / 8 / 9 | — |
| **L3** (click stick sx) | Tab-target *(o radial menu)* | — |
| **R3** (click stick dx) | Attiva Cursore *(pulsante e modalità configurabili — o radial menu)* | Torna a Movimento |
| BACK | Esci | Esci |

- I **layer** si attivano **tenendo** i due modificatori *mentre* premi il tasto abilità.
  Priorità: **entrambi > mod1 > mod2 > Base**. **Modificatori configurabili** (pannello
  *Modificatori di layer* in `cwgui.exe`): default **LB** (→ Shift) e **RB** (→ Ctrl); in
  alternativa **LT/RT**. Se un grilletto è usato come modificatore, non spara più la sua abilità
  (precedenza al ruolo di modificatore) e l'etichetta del layer nell'overlay riflette il pulsante
  scelto (es. `+LT (Shift)`).
- **Modalità cursore — attivazione configurabile** (da `cwgui.exe`, pannello *Cursore*):
  - **Pulsante**: default **R3**; puoi scegliere **L3**, **Start**, o **None** (disattiva del
    tutto la modalità cursore). Se scegli L3, L3 non fa più Tab-target.
  - **Modalità**: **Toggle** (default — una pressione entra, un'altra esce) oppure **Hold**
    (momentaneo — cursore attivo *solo mentre tieni premuto* il pulsante; ottimo con Start o L3).
- **Pressioni accidentali** (pannello *Hardening input*): i click-stick L3/R3 sono facili da
  premere per sbaglio. Imposta un **hold minimo** in millisecondi (consigliato ~60-120 ms): un
  tocco più breve viene ignorato. Default **0** = comportamento storico. Si applica a cursore,
  Tab-target e apertura del radial.
- **Radial menu** (se attivo nel profilo): tieni premuto il trigger (L3/R3),
  inclina lo stick destro verso un settore e **rilascia** per inviare quella
  singola abilità. Rilascio al centro = annulla. Sempre **1:1**, nessuna sequenza.
  Se un pulsante è sia trigger radial sia attivazione cursore, **vince il radial**.

## Risoluzione problemi

- **In gioco non registra i tasti, ma sul Blocco note sì.** WoW gira spesso
  **come amministratore** (comune con i launcher dei private server, es. Ascension)
  e Windows blocca l'invio di input da un processo non elevato verso una finestra
  elevata. `cwapp.exe` **richiede automaticamente i permessi da amministratore**
  all'avvio (prompt UAC): **accetta il prompt**. Se lo hai rifiutato, riavvia
  `cwapp.exe` e conferma. (`cwgui.exe`, l'editor dei profili, non richiede admin.)
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
