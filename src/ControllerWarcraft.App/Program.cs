using ControllerWarcraft.App.Engine;
using ControllerWarcraft.App.Input;
using ControllerWarcraft.App.Output;
using ControllerWarcraft.App.Profiles;

// ============================================================================
//  ControllerWarcraft — App (Fase 1: MVP giocabile)
//  Main loop volutamente sottile: si limita a fare polling, delegare al
//  MappingEngine e stampare l'indicatore di stato. Tutta la logica di mapping,
//  i layer e la macchina a stati vivono nel MappingEngine (vedi Engine/).
//
//  Mapping (profilo Ascension hardcoded) — riepilogo:
//    Stick sx        -> WASD (movimento)
//    Stick dx        -> camera (mouselook) / cursore (in modalita' Cursore)
//    LB / RB         -> modificatori di layer (Base / +LB=Shift / +RB=Ctrl)
//    A               -> Salto (movimento) | Click sinistro (cursore)
//    X               -> abilita' | Click destro (cursore)
//    B / Y / D-pad / grilletti -> abilita' secondo il layer
//    L3              -> Tab-target
//    R3              -> Toggle Modalita' (Movimento <-> Cursore)
//    Back            -> Uscita pulita
//  Mapping rigorosamente 1:1, nessuna automazione (ANALISI §8).
// ============================================================================

const int TickHz = 125;
const int TickMs = 1000 / TickHz;

var poller = new GamepadPoller(userIndex: 0);
var emulator = new InputEmulator();
var profile = new AscensionProfile();
var engine = new MappingEngine(profile, emulator)
{
    OnStatus = msg => Console.WriteLine($"  [{msg}]"),
};

Console.WriteLine("ControllerWarcraft — App (Fase 1: MVP)");
Console.WriteLine($"Profilo: {profile.Name}");
Console.WriteLine("Collega un controller Xbox. Apri WoW (o Blocco note per un test sicuro).");
Console.WriteLine("R3=cambia modalita' · L3=Tab-target · LB/RB=layer abilita' · BACK=esci.");
Console.WriteLine($"Modalita' iniziale: {MappingEngine.ModeLabel(engine.Mode)}");
Console.WriteLine("In attesa del controller...");

// Rilascio pulito su Ctrl+C.
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    engine.Reset();
    Console.WriteLine("\nUscita (Ctrl+C). Input rilasciati.");
    Environment.Exit(0);
};

bool wasConnected = false;

while (true)
{
    var snapshot = poller.Poll();

    if (!snapshot.Connected)
    {
        if (wasConnected)
        {
            engine.Reset();
            Console.WriteLine("Controller disconnesso. In attesa...");
            wasConnected = false;
        }
        Thread.Sleep(200);
        continue;
    }

    if (!wasConnected)
    {
        Console.WriteLine("Controller connesso. Vai.");
        wasConnected = true;
    }

    // Uscita esplicita su Back.
    if (snapshot.Back) break;

    engine.Update(snapshot);

    Thread.Sleep(TickMs);
}

engine.Reset();
Console.WriteLine("Uscita pulita. A presto.");
