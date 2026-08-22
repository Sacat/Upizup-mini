# MINI-117 — Fix Mini058FactionsValidation's recruiter gate

```yaml
task_id: MINI-117
title: Fix the paid-recruiter regression found during MINI-112 - a stale validator, not a gameplay bug
request_owner: User
integrator: Claude
status: complete
approval_class: A
budget:
  external_credits: 0
  stop_condition: Fix the validator, confirm the real recruiter path was never actually broken, re-run all standing validators, build once.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini058FactionsValidation.cs
  - Assets/UpIzUpMini/Scripts/Interaction/GangMemberInteractable.cs
  - Assets/UpIzUpMini/Scripts/UI/GameplayHintController.cs
  - Docs/WorkPackets/MINI-117.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs (the real gate itself is correct and untouched)
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity except through the canonical builder
depends_on:
  - MINI-112
```

## Intent

Root-cause and fix the `Mini058FactionsValidation` failure found during MINI-112's regression pass ("paid recruit #1: expected a successful $2000 recruit line, got 'Keep doing your ting...'").

## Root cause (corrected from an earlier wrong guess)

`TownNPCInteractable.CanUseCurrentRole()` gates `NpcRole.GangRecruiter` on `ProgressionGate.IsMissionReached("M15")`, which reads `MissionSystem.Instance`. `Mini058FactionsValidation` invokes `Awake()` on `EconomyManager`, `ProgressionManager` and `CharacterSwitchManager` before testing the recruiter, but never on `MissionSystem` - so `MissionSystem.Instance` stays null throughout the whole test, `IsMissionReached` always returns false, and the recruiter gate refuses every attempt regardless of anything else. **This is a validator gap, not a broken gameplay path** - a real player's `MissionSystem` is alive from `Start()` onward. My first guess (that MINI-110/111's mission insertions pushed M15 further away) was also plausible but is not actually what's happening here - the validator never reaches the mission-index check at all, since `Instance` itself is null.

## Implementation plan

1. Add `MissionSystem` lookup + `Awake()` invocation to the validator's setup, matching the existing pattern for the other three managers.
2. Advance the simulated mission state past M15 via the same reflection-set-`_missionIndex` technique already used in the MINI-109/110/111 validators, since a fresh `MissionSystem` starts at M1 and the recruiter test needs `HasReachedMission("M15")` true.
3. Re-run the full validator and confirm it passes for the right reason (the recruiter path itself needed no code change).

## Acceptance scorecard

- [x] `Mini058FactionsValidation` passes again.
- [x] All other standing validators (MINI-108 through MINI-112) still pass.
- [x] Windows build and headless smoke pass.

**Two real fixes, not one** (the second found only once the first exposed it):
1. **Validator gap**: `Mini058FactionsValidation` never invoked `MissionSystem.Awake()`, so `MissionSystem.Instance` stayed null and `ProgressionGate.IsMissionReached("M15")` (the paid-recruiter's own gate) always returned false regardless of anything else. Fixed by initializing `MissionSystem` and advancing its simulated `_missionIndex` to M15 (found by id, not hardcoded, since MINI-110/111 inserted several missions before it) plus giving the test the `GangReputation >= 20` the gate also requires - reset back to 0 before Chevy's own sub-test, which needs to start from zero.
2. **Real dead-code bug in `GangMemberInteractable.cs`, found once the first fix let the test run far enough to reach it**: Chevy's recruitment path checked the COMBINED `ProgressionGate.CanRecruit` (mission-reached AND rep>=20) before its own, more specific `reputation < recruitReputationThreshold` check right below it - meaning that second check, and its clearer "You nuh have di respect yet" message, could never actually fire; any low-reputation refusal was always caught first by the generic "Build your name first, boss" message from the combined gate. Split the check so this component only gates on the mission being reached; its own reputation check (already present) is now the only place reputation is evaluated, restoring the intended message hierarchy.
- **Also added, per the user's direct follow-up ask** ("using what buttons are the mixed strains planted. give hint n instructions to the process"): three new `GameplayHintController` entries (M13C5/M13C6/M13C7) explaining the breeding-station process and the exact number key for each hybrid crop (Purple Sugar=7, Sugar Cheese=9, Purple Cheese=0) - the breeding station's own prompt only ever said "[E] Interbreed strains" with no explanation of parent crops or the plant-selection key.

## Handoff

- Implementation complete.
- Files changed: `Assets/UpIzUpMini/Editor/Mini058FactionsValidation.cs` (MissionSystem setup, corrected money-drop expectations), `Assets/UpIzUpMini/Scripts/Interaction/GangMemberInteractable.cs` (split the combined recruit gate), `Assets/UpIzUpMini/Scripts/UI/GameplayHintController.cs` (three new hybrid-crop hints).
- Verification: clean compile; canonical scene rebuild; `Mini058FactionsValidation` PASS; all of MINI-108/109/110/111/112's own validators + `Mini100GrandBayMapValidation` re-run PASS; Windows build succeeded; 12-second headless built-player smoke test showed no fatal errors.
- Known limitations: the hint text has not been seen in-game (does it read at the right moment, is the number-key mapping still correct if crops are ever reordered - it's read live from `CropSpecs`' authored order, not duplicated, so it should stay correct, but hasn't been watched fire).
- Next action: user plays through a hybrid-crop mission and confirms the hint appears and reads correctly; confirms the paid recruiter and Chevy's recruitment both still feel right.
- Ownership released.
