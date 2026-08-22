# MINI-111 — Rasta's strain-mentorship ladder

```yaml
task_id: MINI-111
title: Rasta stops asking for tomatoes and becomes the strain/production school for both career paths
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: B
budget:
  external_credits: 0
  stop_condition: Complete the strain-ladder mission chain and its unlock mechanism, capture evidence, build once, then return for the user's playtest before MINI-112.
reserved_files:
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scripts/Progression/ProgressionManager.cs
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - one new focused MINI-111 validator under Assets/UpIzUpMini/Editor/
  - Docs/WorkPackets/MINI-111.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity except through the canonical builder
  - all user-approved Sacat/Franki/Boss C chains, proportions, materials and placements
  - approved Lalay/Highland road topology and map anchors
  - the existing Boss-exploitation-stage unlock formulas (BlackSugarUnlocked etc.) - additive only, not replaced
  - CropBreedingStation's existing recipe/output mechanic - reused, not rewritten
depends_on:
  - MINI-110
```

## Intent

Per `Docs/CLAUDE-HANDOFF-CURRENT.md` section 6 (MINI-111): Rasta's chapter stops being a simple tomato-harvest check-in and becomes the strain/production school, teaching Bushers -> Black Sugar -> Purple -> Blue Cheese -> Purple Sugar (the `purple_black` hybrid, renamed player-facing) -> Sugar Cheese -> Purple Cheese in order, each tier story-gated so the player cannot see/buy a locked strain early.

## Why this matters beyond the literal ask

The existing crop-unlock booleans (`BlackSugarUnlocked`, `PurpleUnlocked`, etc.) are gated on `BossExploitationStage`/Boss-J-reputation - a WeedRoute-only track (`M9W`-`M11W`). A player on the Legitimate Farmer path never sees those missions and previously had NO route to any strain past the basics. Rasta's ladder is that route for both paths, additive to the existing formulas via an OR, not a replacement of them.

## Non-goals

- Do not touch `CropBreedingStation`'s recipe/output mechanic itself - only rely on its existing `IsCropUnlocked` gate.
- Do not remove or weaken the existing Boss-exploitation unlock formulas.
- Do not build Dog Life escalation (MINI-112), road/vehicle cleanup (MINI-113), Brakes missions (MINI-114), cellphone unlock (MINI-115) or TMAX repair (MINI-116).
- Do not expand save/load to persist `ProgressionManager` state - it is not currently persisted at all (a pre-existing gap this packet does not close), so `RastaTaught*` flags share that same limitation, not a new regression.

## Implementation plan

1. `Mission.unlocksCropId` (new field) - applied automatically the moment a mission with it set BECOMES current, mirroring the existing `commitToWeedRouteOnComplete` apply-on-transition pattern. Rasta's own missions never need bespoke NPC code to react to a specific interaction.
2. `ProgressionManager` gains `RastaTaught*` flags (one per non-base tier) and `MarkRastaTaught(cropId)`; `IsCropUnlocked` ORs them into the existing formulas.
3. M13's second objective changes from `HarvestCrop tomato x4` to `HarvestCrop bushers x3` (tier 1 - already unlocked from the start, no flag needed).
4. Six new Rasta missions inserted immediately after M13 (before M13B/M14, so the Boat Man/Guadeloupe chapter's fuller weight follows strain mastery, per the handoff's own ordering note): Black Sugar, Purple, Blue Cheese, Purple Sugar (hybrid), Sugar Cheese, Purple Cheese - each `TalkTo Rasta` (his teaching beat) then `HarvestCrop` of that tier, with `unlocksCropId` set to that tier's crop id.
5. Player-facing text calls the `purple_black` hybrid "Purple Sugar"; the internal save-facing crop id is unchanged.

## Acceptance scorecard

- [x] M13 asks for Bushers, not tomatoes. Verified directly against the built mission list.
- [x] Each of the six new missions is story-gated: the crop is not `IsCropUnlocked` before its mission starts, and becomes unlocked the moment the mission does. Verified per-tier in the focused validator.
- [x] The existing Boss-exploitation unlock path is untouched and still independently grants the same crops. Verified with a fresh `ProgressionManager` reaching `BlackSugarUnlocked` via `RecordBossJob()` alone, with zero Rasta-taught flags set.
- [x] `CropSelectionController`/`CropBreedingStation`/Boss C's seed offer all respect the new unlock state with no code changes at those call sites - only `ProgressionManager.IsCropUnlocked` was touched, all three already call through it.
- [x] Compile, focused validation, generated-scene rebuild, standing regressions (MINI-108/109/110), Windows build and headless smoke all pass.
- [x] Evidence: `Logs/Tasks/MINI-111/MINI-111-Rasta-StrainSchool-1280x720.png` and a full ladder-content dump (`mini111-ladder-content.txt`).

**Bug found and fixed along the way, not originally scoped**: `Mission.unlocksCropId` (the new field this whole mechanism depends on) was silently dropped during the scene build - `Mini011PhaseBSetup`'s manual field-by-field `SerializedProperty` copy from the in-memory `Mission` list to the built `MissionSystem` component had no line for it, so every mission's `unlocksCropId` baked in as empty regardless of what was set in code. Caught by the validator (`M13C2.unlocksCropId is '', expected 'black_sugar'`) before it could ship silently broken - the entire ladder would have looked correct in the editor's C# but never actually unlocked anything in the built game.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/Tasks/MINI-111/` | No |
| Unlock/mission logic | focused editor validator | `Logs/Tasks/MINI-111/` | No |
| Generated scene | canonical rebuild + standing regressions | `Logs/Tasks/MINI-111/` | No |
| Rasta/mission evidence | fixed screenshot | `Logs/Tasks/MINI-111/` | Yes |
| Runtime startup | Windows build + headless smoke | `Logs/Tasks/MINI-111/` | No |
| Mission/progression feel | user plays the ladder on both career paths | user feedback | Yes |

## Handoff

- Implementation complete.
- Files changed: `Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs` (`Mission.unlocksCropId`, applied on mission transition), `Assets/UpIzUpMini/Scripts/Progression/ProgressionManager.cs` (`RastaTaught*` flags, `MarkRastaTaught`, `IsCropUnlocked` OR, `UnlockEverything` parity), `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (M13's bushers swap, six new missions, the `unlocksCropId` serialization fix), new `Assets/UpIzUpMini/Editor/Mini111RastaLadderValidation.cs` and `Assets/UpIzUpMini/Editor/Mini111EvidenceCapture.cs`.
- Decisions made: inserted the ladder immediately after M13 (before M13B/Normy and M14/Boat Man), so the Guadeloupe chapter's fuller weight follows strain mastery per the handoff's own ordering note. `purple_black`'s player-facing name is "Purple Sugar" everywhere in this content; the save-facing ID is untouched. Harvest counts: 3 for the three base-tier crops (Bushers/Black Sugar/Purple, matching the existing pattern), 1 for each "prove/cross" tier (Blue Cheese and the three hybrids) since those are demonstrations of a new skill, not stockpiles.
- Visual locks added/changed: none.
- Known limitations: `ProgressionManager`'s state (including these new `RastaTaught*` flags) is not currently persisted by the save system at all - a pre-existing gap this packet does not close, so a save/reload mid-ladder would lose progress the same way `BossExploitationStage` already does today. Not yet played by a human on either career path.
- Next action: user plays Rasta's full ladder (ideally on both the Legitimate Farmer and WeedRoute paths, since this is the FIRST route to advanced strains for Legitimate players) before MINI-112.
- Ownership released.
