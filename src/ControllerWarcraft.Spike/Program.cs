using ControllerWarcraft.Spike;
using static ControllerWarcraft.Spike.Native;

// ============================================================================
//  ControllerWarcraft — Spike Fase 0
//  Obiettivo: dimostrare la fattibilita' dell'approccio ESTERNO.
//    Left stick  -> WASD (movimento)
//    Right stick -> mouselook (tiene premuto RMB + muove il mouse)
//    A           -> Space (salto)
//    RB          -> Tab (target successivo)
//    Right trigger-> E (interact / azione)  [semplice test tasto analogico->digitale]
//    Back        -> esci pulito
//  Loop ~125 Hz. Nessuna automazione: mapping 1:1.
// ============================================================================

const int TickHz = 125;
const int TickMs = 1000 / TickHz;

// Sensibilita' mouselook: pixel di movimento mouse per tick a stick pieno.
const double LookSensX = 18.0;
const double LookSensY = 14.0;
const bool InvertLookY = false;

Console.WriteLine("ControllerWarcraft — Spike Fase 0");
Console.WriteLine("Collega un controller Xbox. Apri WoW (o Blocco note per un test sicuro).");
Console.WriteLine("Left stick=movimento, Right stick=camera, A=salto, RB=Tab, RT=E. BACK per uscire.");
Console.WriteLine("In attesa del controller...");

// Stato dei tasti tenuti premuti, per fare press/release puliti.
var held = new HashSet<ScanCode>();
bool rmbHeld = false;
bool prevA = false, prevRB = false, prevRT = false;

// Rilascia tutto all'uscita, anche su Ctrl+C.
void ReleaseAll()
{
    foreach (var sc in held) KeyUp(sc);
    held.Clear();
    if (rmbHeld) { RightMouseUp(); rmbHeld = false; }
}

Console.CancelKeyPress += (_, e) => { e.Cancel = true; ReleaseAll(); Environment.Exit(0); };

void Hold(ScanCode sc, bool want)
{
    if (want && held.Add(sc)) KeyDown(sc);
    else if (!want && held.Remove(sc)) KeyUp(sc);
}

bool wasConnected = false;

while (true)
{
    if (XInputGetState(0, out var state) != 0)
    {
        // Controller assente: rilascia tutto e aspetta.
        if (wasConnected) { ReleaseAll(); Console.WriteLine("Controller disconnesso."); }
        wasConnected = false;
        Thread.Sleep(200);
        continue;
    }

    if (!wasConnected) { Console.WriteLine("Controller connesso. Vai."); wasConnected = true; }

    var pad = state.Gamepad;
    var buttons = (GamepadButton)pad.wButtons;

    if (buttons.HasFlag(GamepadButton.Back)) break;

    // ---- Movimento: left stick -> WASD ----
    Hold(ScanCode.W, pad.sThumbLY > LeftThumbDeadzone);
    Hold(ScanCode.S, pad.sThumbLY < -LeftThumbDeadzone);
    Hold(ScanCode.D, pad.sThumbLX > LeftThumbDeadzone);
    Hold(ScanCode.A, pad.sThumbLX < -LeftThumbDeadzone);

    // ---- Camera: right stick -> mouselook ----
    double rx = Normalize(pad.sThumbRX, RightThumbDeadzone);
    double ry = Normalize(pad.sThumbRY, RightThumbDeadzone);
    bool lookActive = rx != 0 || ry != 0;

    if (lookActive && !rmbHeld) { RightMouseDown(); rmbHeld = true; }
    else if (!lookActive && rmbHeld) { RightMouseUp(); rmbHeld = false; }

    if (lookActive)
    {
        int dx = (int)Math.Round(rx * LookSensX);
        int dy = (int)Math.Round(ry * LookSensY) * (InvertLookY ? 1 : -1);
        MouseMoveRelative(dx, dy);
    }

    // ---- Azioni (edge-triggered: reagisci alla pressione, non a ogni tick) ----
    bool a = buttons.HasFlag(GamepadButton.A);
    if (a && !prevA) { KeyDown(ScanCode.Space); KeyUp(ScanCode.Space); }
    prevA = a;

    bool rb = buttons.HasFlag(GamepadButton.RightShoulder);
    if (rb && !prevRB) { KeyDown(ScanCode.Tab); KeyUp(ScanCode.Tab); }
    prevRB = rb;

    bool rt = pad.bRightTrigger > 30;
    if (rt && !prevRT) { KeyDown(ScanCode.E); KeyUp(ScanCode.E); }
    prevRT = rt;

    Thread.Sleep(TickMs);
}

ReleaseAll();
Console.WriteLine("Uscita pulita. A presto.");

// Normalizza un asse stick in [-1..1] applicando la deadzone radiale semplice.
static double Normalize(short value, short deadzone)
{
    if (value > deadzone) return (value - deadzone) / (double)(32767 - deadzone);
    if (value < -deadzone) return (value + deadzone) / (double)(32768 - deadzone);
    return 0;
}
