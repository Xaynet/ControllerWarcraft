# ControllerWarcraft Companion (addon opzionale)

Piccolo addon WoW/Lua che **espone in sola lettura** lo stato di gioco (bersaglio, combattimento,
vita/risorsa) per dare *contesto* all'app esterna ControllerWarcraft. È **strettamente opzionale**:
l'app funziona al 100% senza — è il principio dell'approccio external-only (vedi
[ANALISI.md §2](../../ANALISI.md)).

> **Non invia MAI input, non automatizza nulla.** Nessun `SetBinding`, nessun click, nessuna
> abilità lanciata. Legge stato e lo salva. Questo mantiene il vincolo 1:1 e riduce il rischio ToS
> (ANALISI §8): il companion **non può** guidare azioni, e l'app **non lo usa** per decidere input.

## Come funziona il canale app ↔ addon

L'ambiente Lua di WoW è **sandboxed**: un addon non può aprire socket né scrivere file arbitrari.
L'unico canale conforme è la **variabile salvata** (`SavedVariables`), che il *client* scrive su disco:

```
[WoW]/WTF/Account/<ACCOUNT>/SavedVariables/ControllerWarcraftCompanion.lua
```

L'addon aggiorna in memoria la tabella globale `ControllerWarcraftCompanionDB`; l'app legge quel
file (parser tollerante in `ControllerWarcraft.Core/Companion/CompanionStateReader.cs`) e ne mostra
il contenuto (es. `Target: Hogger (87%)` nell'overlay).

### ⚠️ Limite importante: NON è in tempo reale

Il client di norma scrive i `SavedVariables` su disco **solo al logout o a `/reload`**, non a ogni
tick. Quindi lo stato è uno **snapshot**, non un feed live. Va benissimo come contesto informativo;
**è anche il motivo per cui l'app non lo usa per l'input** (oltre al divieto di automazione).

## Campi esposti

| Campo | Tipo | Significato |
|---|---|---|
| `targetExists` | bool | Esiste un bersaglio |
| `targetName` | string | Nome del bersaglio |
| `targetIsEnemy` | bool | Bersaglio attaccabile |
| `targetHealthPct` | number | Vita bersaglio (0..100) |
| `inCombat` | bool | Giocatore in combattimento |
| `playerHealthPct` | number | Vita giocatore (0..100) |
| `playerPowerPct` | number | Risorsa primaria (0..100) |
| `gameVersion` | string | Retail / Classic / … (rilevata) |
| `addonVersion` | string | Versione addon |
| `updated` | number | Epoch dell'ultimo aggiornamento |

## Installazione

1. Copia la cartella `ControllerWarcraftCompanion/` in `[WoW]/Interface/AddOns/`.
2. Adatta `## Interface` nel `.toc` alla tua versione (vedi la tabella nel file); se necessario
   abilita **"Load out of date AddOns"** nella schermata di selezione personaggio.
3. Entra in gioco: comparirà un breve messaggio in chat. Fai `/reload` o esci per forzare la
   scrittura del file `SavedVariables`.
4. Nell'app: attiva il companion e imposta il percorso del file (GUI → *Impostazioni globali*, o
   `settings.json`: `companionEnabled: true`, `companionSavedVariablesPath: "…"`).

## Compatibilità per versione

| Versione | Stato | Note |
|---|---|---|
| **Classic** (Era/Wrath/Cata) | Previsto/testabile | API di stato standard usate qui presenti. Imposta l'`## Interface` corretto. |
| **Retail** | Previsto/testabile | Idem; rilevazione versione via `WOW_PROJECT_ID`. |
| **Ascension** | Da verificare | Client custom su server privato: **verificare le loro regole** sugli addon/tool esterni prima dell'uso. L'app resta comunque pienamente funzionante senza il companion. |

## Disattivazione

Rimuovi l'addon dalla cartella `AddOns/`, oppure lascialo installato e imposta
`companionEnabled: false` nell'app. In entrambi i casi l'esperienza di gioco con il controller è
identica: il companion è puramente additivo.
