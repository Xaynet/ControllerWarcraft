# ControllerWarcraft — Spike Fase 0

Prototipo minimo per de-riscare l'approccio esterno: legge un controller Xbox
(XInput) ed emula tastiera + mouse (SendInput). Nessun addon, nessuna dipendenza
esterna. Vedi [../../ANALISI.md](../../ANALISI.md) per il quadro completo.

## Mapping attuale (hardcoded)

| Input controller | Azione emulata |
|---|---|
| Stick sinistro   | W / A / S / D (movimento) |
| Stick destro     | Mouselook (tiene premuto tasto destro mouse + muove il mouse) |
| A                | Spazio (salto) |
| RB               | Tab (target successivo) |
| Grilletto destro | E (interact) |
| BACK             | Esci dallo spike |

Loop a ~125 Hz. Mapping rigorosamente 1:1 (nessuna automazione).

## Come eseguire

```powershell
dotnet run -c Release --project src/ControllerWarcraft.Spike
```

oppure l'eseguibile compilato:

```powershell
./src/ControllerWarcraft.Spike/bin/Release/net10.0-windows/cwspike.exe
```

## Come testare in sicurezza

1. **Prima prova**: apri il **Blocco note**, lancia lo spike e tienilo in
   foreground. Muovi lo stick sinistro: dovresti vedere comparire `wasd`.
   Premi A: va a capo. Cosi' verifichi l'iniezione tasti senza rischi.
2. **In gioco**: avvia WoW, entra in gioco, poi (Alt-Tab) lancia lo spike e torna
   sul gioco. Muovi gli stick. Premi **BACK** per fermare tutto.

## Note

- Target framework: `net10.0-windows` (l'SDK installato). Per `net8.0-windows`
  basta cambiare `<TargetFramework>` nel `.csproj` e avere quel runtime.
- Parametri regolabili in cima a `Program.cs`: `LookSensX/Y`, `InvertLookY`, `TickHz`.
- Alcuni giochi con mouselook potrebbero richiedere di regolare la sensibilita' o
  il segno di `InvertLookY`. E' la parte piu' delicata: se la camera "scappa" o va
  al contrario, agisci su quei valori.
