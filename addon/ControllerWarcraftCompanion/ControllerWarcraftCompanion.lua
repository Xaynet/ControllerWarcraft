--[[
  ControllerWarcraft Companion — addon OPZIONALE (Fase 4, punto 3)
  ================================================================

  Cosa fa: legge alcune informazioni di stato del gioco (bersaglio, combattimento, vita/risorsa
  del giocatore) e le scrive nella propria variabile salvata `ControllerWarcraftCompanionDB`.
  L'app esterna ControllerWarcraft, SE l'utente lo abilita, può leggere quel file per mostrare
  contesto (es. il nome del bersaglio nell'overlay).

  Cosa NON fa (per scelta, e per rispettare la ToS — ANALISI §8):
    * NON invia MAI input, non usa SetBinding, non clicca pulsanti, non lancia abilità.
    * NON automatizza nulla: si limita a LEGGERE stato e a salvarlo.
    * NON è un requisito: l'app funziona al 100% senza questo addon.

  Limite noto: il client di norma scrive i SavedVariables su disco solo al LOGOUT / RELOAD UI
  (`/reload`), non in tempo reale. Quindi lo stato è uno SNAPSHOT, non un feed live. Va benissimo
  come contesto; per questo l'app non lo usa (e non deve usarlo) per guidare l'input.
]]

local ADDON_VERSION = "1.0"

-- La tabella salvata è dichiarata in SavedVariables nel .toc: viene creata/caricata dal client.
ControllerWarcraftCompanionDB = ControllerWarcraftCompanionDB or {}
local DB = ControllerWarcraftCompanionDB

-- Rileva la versione di gioco in modo difensivo (le API variano tra client).
local function DetectGameVersion()
  if WOW_PROJECT_ID and WOW_PROJECT_MAINLINE and WOW_PROJECT_ID == WOW_PROJECT_MAINLINE then
    return "Retail"
  elseif WOW_PROJECT_ID and WOW_PROJECT_CLASSIC and WOW_PROJECT_ID == WOW_PROJECT_CLASSIC then
    return "Classic"
  end
  -- Fallback: usa la build se disponibile.
  local _, _, _, tocversion = GetBuildInfo()
  return "Unknown(" .. tostring(tocversion) .. ")"
end

local function SafePct(cur, max)
  cur = cur or 0
  max = max or 0
  if max <= 0 then return 0 end
  return math.floor((cur / max) * 100 + 0.5)
end

-- Aggiorna la tabella salvata con lo stato corrente. Solo LETTURE di API di stato.
local function UpdateState()
  local hasTarget = UnitExists("target")
  DB.targetExists = hasTarget and true or false
  DB.targetName = hasTarget and (UnitName("target") or "") or ""
  DB.targetIsEnemy = (hasTarget and UnitCanAttack("player", "target")) and true or false
  DB.targetHealthPct = hasTarget and SafePct(UnitHealth("target"), UnitHealthMax("target")) or 0

  DB.inCombat = UnitAffectingCombat("player") and true or false
  DB.playerHealthPct = SafePct(UnitHealth("player"), UnitHealthMax("player"))
  DB.playerPowerPct = SafePct(UnitPower("player"), UnitPowerMax("player"))

  DB.gameVersion = DetectGameVersion()
  DB.addonVersion = ADDON_VERSION
  DB.updated = time() -- epoch (secondi). Solo indicativo: la scrittura su disco avviene a logout/reload.
end

-- Registrazione eventi (solo eventi di stato; nessuna azione di gioco).
local frame = CreateFrame("Frame")
frame:RegisterEvent("PLAYER_ENTERING_WORLD")
frame:RegisterEvent("PLAYER_TARGET_CHANGED")
frame:RegisterEvent("PLAYER_REGEN_DISABLED") -- entrata in combattimento
frame:RegisterEvent("PLAYER_REGEN_ENABLED")  -- uscita dal combattimento
frame:RegisterEvent("UNIT_HEALTH")
frame:RegisterEvent("UNIT_POWER_UPDATE")
frame:RegisterEvent("PLAYER_LOGOUT")

frame:SetScript("OnEvent", function(_, event, unit)
  if event == "UNIT_HEALTH" or event == "UNIT_POWER_UPDATE" then
    -- Filtra: solo player/target per non aggiornare a ogni unità della scena.
    if unit ~= "player" and unit ~= "target" then return end
  end
  UpdateState()
end)

-- Messaggio discreto una volta caricato (nessuna UI invadente).
if DEFAULT_CHAT_FRAME then
  DEFAULT_CHAT_FRAME:AddMessage("|cff88ccffControllerWarcraft Companion|r v" .. ADDON_VERSION
    .. " caricato (sola lettura, opzionale).")
end
