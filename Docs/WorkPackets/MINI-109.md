# MINI-109 — Grand Bay progression repair and narrative bridge

```yaml
task_id: MINI-109
title: Repair Normy/Black Sugar/Rasta mission truth and prepare the Boat Man progression
request_owner: User
integrator: Claude
status: approved
approval_class: B
budget:
  external_credits: 0
  stop_condition: Complete only the functional mission-repair slice, capture evidence, build once, then return for the user's playtest before expanding the story.
reserved_files:
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Scripts/Economy/EconomyManager.cs
  - Assets/UpIzUpMini/Scripts/Progression/ProgressionManager.cs
  - Assets/UpIzUpMini/Scripts/Progression/ProgressionGate.cs
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - one new focused MINI-109 validator under Assets/UpIzUpMini/Editor/
  - Docs/WorkPackets/MINI-109.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity except through the canonical builder
  - all user-approved Sacat/Franki/Boss C chains, proportions, materials and placements
  - approved Lalay/Highland road topology and map anchors
  - paused MINI-107 modular-character proof assets
depends_on:
  - MINI-108
```

## Intent

Make the current playable mission chain truthful and completable before adding the larger Rasta, Boat Man, Gardey Zafeh, Dog Life and Guadeloupe chapter. The first Claude implementation pass must repair current blockers without simultaneously redesigning the map, characters, vehicles or mobile architecture.

## User-reported defects to reproduce first

1. `Clean Face` can remain stuck at Normy. For the current mission, Normy should take the required money once and complete the objective. Do not require repeated heat-service cooldown behavior to finish it.
2. `M10W` asks for Black Sugar delivery to Boss J, but a player carrying harvested Black Sugar can fail to advance. Validate the actual crop ID, inventory count, target ID, sale/delivery notification and mission objective together.
3. `M13` routes to Rasta's old location. Its marker and target must resolve from Rasta's current live anchor, not a stale interpolation.
4. Later objectives may be pre-satisfied before their mission becomes active. Choose and document one consistent rule per objective:
   - retrospective credit when the game can prove the player already owns/completed the requirement; or
   - progression lock when early access would break story or economy.
   Never silently force the player to repeat an expensive purchase they already made.

## Required implementation for this bounded packet

- Make Normy's `M5B` payment idempotent: one accepted mission payment completes the objective immediately and cannot be charged twice for the same objective.
- Keep Normy's reusable $100/20%-heat service as a later ambient feature, separate from the mission completion transaction.
- Make Black Sugar delivery to Boss J remove/credit the correct harvested inventory and advance exactly once. Preserve the save-facing crop ID `black_sugar` and Boss J's legacy target ID `BossK`.
- Resolve Rasta's objective marker from the generated current NPC placement. Do not move Rasta as part of this packet.
- Add focused validation covering: insufficient funds; successful Normy mission payment; duplicate interaction; Black Sugar inventory delivery; wrong crop rejection; correct Rasta marker; and save-compatible IDs.
- Rebuild `GrandBayProof.unity` only after the compound code/content change, then run compile, focused validation, existing mission/map regressions, Windows build and headless smoke.

## Explicit non-goals for MINI-109

- Do not yet build the full Normy shopping errand, Boat Man courier absence/timer UI, Rasta hybrid-strain ladder, Dog Life street roaming, priest missions, cellphone unlock mission, road grading, TMAX optimization or character replacement.
- Do not spend Hitem3D credits or import new assets.
- Do not change approved map layout or manually locked accessories.
- Do not convert the project to URP or the Input System.

## Acceptance scorecard

- [x] `Clean Face` completes after one valid Normy payment and never double-charges. Root cause: the mission's BribeNormy objective shared the ambient bribe's heat<=0.5 gate, which is exactly the state M5A leaves the player in. Fixed with a separate, idempotent mission-payment branch in `HandleNormy()`.
- [x] Normy's ambient heat service still works outside that one mission transaction (untouched, gated separately as before).
- [x] Three harvested Black Sugar can be delivered to Boss J and the mission advances once. Root cause: the interact gate checked only `bossSeedCrop` (hardcoded to Bushers), not any actually-held illegal crop. Fixed with `HasAnySellableIllegalStock()`.
- [x] Wrong/insufficient inventory does not advance the Black Sugar objective (unchanged - `TrySellCrops`/`HasAnySellableIllegalStock` both require `GetCount > 0`).
- [x] Rasta's mission marker matches his current generated location. Root cause: `Mini100GrandBayMapMigration.RemapMissionObjectives()` ran BEFORE the `MoveNamed` calls that place Rasta/Sacat/Franki at their final migrated positions, baking in stale pre-move coordinates. Reordered so remapping runs last. (A secondary, real bug in `BuildRastaMentor`'s own placement-formula duplication - missing the `farmRight*3f` offset - was also fixed by capturing his real built position directly instead of recomputing the formula.)
- [x] Existing `111111` mission-skip behavior remains available for testing (untouched).
- [x] Save-compatible IDs `BossK`, `black_sugar`, `MontineFarm` and `land_montine` remain unchanged internally; player-facing text says Highland. Verified directly in `Mini109MissionRepairValidation`.
- [x] Unity compile and focused/static regressions pass (`Mini109MissionRepairValidation`, `Mini108EarlyMissionValidation`, `Mini100GrandBayMapValidation`, `Mini095LalayMapLabSetup.BuildValidateCapture` all PASS).
- [x] A Windows build and built-player smoke pass (12s headless run, no fatal errors).
- [x] Fixed screenshots show Normy, Boss J and Rasta at their real positions - captured under `Logs/Tasks/MINI-109/` (`MINI-109-Normy-1280x720.png`, `MINI-109-BossJ-BlackSugar-1280x720.png`, `MINI-109-Rasta-1280x720.png`). These confirm NPC identity/position, not the live minimap widget itself (OnGUI/Canvas HUD elements don't render through this fixed-camera capture path) - user hands-on mission completion remains explicitly requested.

**Bonus fix beyond the original three, directly in scope of the packet's 4th ask** ("choose and document one consistent rule per objective"): added retrospective completion for `BuyItem` objectives - if the player already owns the item outright (e.g. bought the TMAX before M17 became active), the objective now credits immediately instead of demanding a repeat purchase `TryPurchase` would refuse anyway ("You already have X"). Every other objective kind keeps today's progression-lock behaviour; this was not extended blind to kinds without an equally provable "already done" signal.

## Evidence plan

| Claim | Method | Required result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/mini109-compile.log` | No |
| Mission logic | focused editor validator | `Logs/mini109-validation.log` | No |
| Generated scene | canonical scene rebuild + static/map checks | task log paths | No |
| Navigation/readability | fixed 1280x720 screenshots | `Logs/Tasks/MINI-109/` | Yes |
| Runtime startup | Windows build + headless smoke | `Logs/mini109-player-smoke.log` | No |
| Mission feel/completion | user plays Normy -> Black Sugar -> Rasta | user feedback | Yes |

## Follow-on packets after the user accepts MINI-109

1. `MINI-110` — Normy's food/pharmacy favour, Boat Man first-meeting dialogue, story bridge, character courier absence and visible return timer.
2. `MINI-111` — Rasta strain mentorship: Bushers -> Black Sugar -> Purple -> Blue Cheese -> Purple Sugar -> Sugar Cheese -> Purple Cheese, with price/rep progression.
3. `MINI-112` — Dog Life jealousy, crew-building prerequisite, block attack, 100-second pool respawn, limited group walks on Lalay and return-to-block leash.
4. `MINI-113` — road/intersection smoothing, farm hedge clearance, vehicle minimap markers and road-safe vehicle spawns.
5. `MINI-114` — Brakes/priest community missions and non-cash blessing rewards.
6. `MINI-115` — cellphone unlock/tutorial, partner call, later crew backup call and future touch-input mapping.
7. `MINI-116` — TMAX production repair: eliminate see-through geometry/material defects, optimize the oversized payload and revalidate riding on mobile budgets.

## Handoff state

- Implementation complete. All three reported defects reproduced with concrete evidence (not guessed) before being fixed, each with a distinct, confirmed root cause - see the scorecard above.
- Files changed: `Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs` (Normy mission-payment branch, `HasAnySellableIllegalStock`), `Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs` (retrospective `BuyItem` completion), `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (`_rastaPos` capture/reuse), `Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs` (reordered `RemapMissionObjectives` to run after NPC placement), new `Assets/UpIzUpMini/Editor/Mini109MissionRepairValidation.cs` and `Assets/UpIzUpMini/Editor/Mini109EvidenceCapture.cs`.
- Evidence: `Logs/Tasks/MINI-109/` (compile/rebuild logs, focused validation log, standing regression logs, player build log, 12s headless smoke log, three evidence screenshots).
- Ownership released. Stop here - do not start `MINI-110` in this pass. Ask the user to playtest the four acceptance cases listed in `Docs/CLAUDE-HANDOFF-CURRENT.md` section 10.
