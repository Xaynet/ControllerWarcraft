# Rilasciare ControllerWarcraft

Il rilascio e' **automatico** e guidato dai **tag git**. Non si costruisce nulla
a mano: si tagga un commit, si fa push del tag e la pipeline GitHub Actions
produce e pubblica la release.

## TL;DR

```powershell
git tag v0.1.0
git push origin v0.1.0
```

Il push del tag avvia il workflow [`.github/workflows/release.yml`](.github/workflows/release.yml),
che compila, pubblica, comprime e crea la GitHub Release con lo zip allegato.

## Schema di versioning (SemVer)

Si usa [Semantic Versioning](https://semver.org/): `vMAJOR.MINOR.PATCH`.

| Parte | Quando incrementarla |
|---|---|
| **MAJOR** | Cambiamenti incompatibili (es. formato profili non retro-compatibile) |
| **MINOR** | Nuove funzionalita' retro-compatibili (nuova modalita', nuovo layer) |
| **PATCH** | Bugfix retro-compatibili |

Il tag **deve** avere il prefisso `v` e matchare `v*.*.*` (es. `v0.1.0`, `v1.2.3`).
Tag che non rispettano questo pattern **non** avviano il rilascio.

La `v` iniziale viene rimossa dalla pipeline e il valore risultante viene passato
a `dotnet publish` via `-p:Version=` (e `AssemblyVersion`/`FileVersion`), cosi'
l'eseguibile riporta la versione corretta.

## Cosa produce la pipeline

Al push di un tag `v*.*.*`, su runner `windows-latest`:

1. Setup .NET 10 SDK (`actions/setup-dotnet`, `10.x`).
2. Deriva la versione dal nome del tag (rimuove la `v`).
3. `dotnet publish` dei **due eseguibili**, ciascuno self-contained `win-x64`,
   single-file (`PublishSingleFile=true`, native libs incluse nell'estrazione),
   con la versione iniettata da tag:
   - `src/ControllerWarcraft.App` → `cwapp.exe` (runtime);
   - `src/ControllerWarcraft.Gui` → `cwgui.exe` (editor dei profili, WPF).
4. Assembla uno staging con i due exe e una cartella `profiles/` condivisa (i
   preset JSON dal repo), poi comprime in `ControllerWarcraft-vX.Y.Z-win-x64.zip`.
5. Crea una **GitHub Release** sul tag e allega lo zip, con **release notes
   generate automaticamente** dai commit/PR (`generate_release_notes: true`).

L'autenticazione usa il `GITHUB_TOKEN` fornito da Actions; il workflow ha il
permesso `contents: write` necessario a creare la release.

## Continuous Integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) compila in Release l'intera
solution `ControllerWarcraft.slnx` (quindi **tutti** i progetti: Core, App, Gui,
Spike) su push e pull request verso `main`. I tag sono esclusi dalla CI
(`tags-ignore`) per non duplicare il lavoro con il workflow di release.

> Aggiungendo nuovi progetti alla solution vengono automaticamente coperti dalla CI:
> non serve toccare `ci.yml`. La build fallisce se un qualsiasi progetto non compila.

## Cosa viene rilasciato

Lo zip `ControllerWarcraft-vX.Y.Z-win-x64.zip` contiene:

```
cwapp.exe            # runtime: legge il controller ed emula KB/mouse
cwgui.exe            # editor dei profili di remap (WPF)
profiles/
  ascension.json
  classic.json
  retail.json
  README.md
RELEASING.md
```

`Overlay` e `Core` sono **librerie**: non hanno un exe proprio ed entrano come
dipendenze transitive dentro `cwapp.exe`/`cwgui.exe`. Entrambi gli eseguibili
cercano i preset in `<exe>/profiles`, quindi la singola cartella `profiles/`
condivisa alla radice dello zip va bene per tutti e due.

I due eseguibili sono definiti nel workflow tramite le variabili `APP_PATH` e
`GUI_PATH`. Per aggiungere un altro deliverable, aggiungere un `dotnet publish` e
copiarne l'exe nello staging.

## Prerequisiti sul remote (da configurare una volta)

La pipeline gira su GitHub Actions, quindi il repository deve avere un **remote
GitHub** configurato. Se il repo e' ancora solo locale:

```powershell
git remote add origin https://github.com/<utente>/ControllerWarcraft.git
git push -u origin main
```

Poi, per il primo rilascio:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

Note:
- Il `GITHUB_TOKEN` e' fornito automaticamente da Actions: **non** serve creare
  segreti manualmente per la release.
- Assicurarsi che in *Settings -> Actions -> General -> Workflow permissions* sia
  attivo *Read and write permissions* (necessario per creare la release). Il
  workflow richiede comunque `contents: write` in modo esplicito.

## Correggere una release sbagliata

Un tag e' immutabile per convenzione: preferire un nuovo tag PATCH
(`v0.1.1`). Se e' davvero necessario rifare lo stesso tag:

```powershell
git tag -d v0.1.0                 # elimina il tag locale
git push origin :refs/tags/v0.1.0 # elimina il tag remoto (e va rimossa la Release dalla UI)
git tag v0.1.0 <commit>
git push origin v0.1.0
```
