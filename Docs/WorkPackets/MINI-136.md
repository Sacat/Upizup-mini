# MINI-136 — 89-degree wheelie cap without angle crashes

```yaml
task_id: MINI-136
title: Cap bike wheelies at 89 degrees and remove angle-triggered crashes
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: A
budget:
  codex_time: One bounded vehicle-controller and validation pass
  claude_time: 0
  external_credits: 0
  stop_condition: Focused validation, regression compile, Windows build and smoke complete
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeCrashEjectionController.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxBikeControllerCustom.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoWheelieAssist.cs
  - Assets/UpIzUpMini/Editor/Mini134CrashDamageRecoveryValidation.cs
  - Docs/CURRENT.md
  - Docs/Systems/Vehicles.md
  - Docs/WorkPackets/MINI-136.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - DECISIONS.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - All user-approved rider fit, wheel, handling, character and scene placement files
depends_on:
  - MINI-135
  - D-013 revised by the user on 2026-08-31
```

## Intent

Both supported bikes must never command more than 89 degrees of wheelie pitch. Wheelie angle alone must never crash or eject driver/pillion; only the existing filtered hard-collision path may eject riders.

## Non-goals

No steering, suspension, wheel mapping, rider pose, impact threshold, scene, prefab, or visual-placement change.

## Acceptance scorecard

- [x] TMAX effective wheelie target cannot exceed 89 degrees.
- [x] SuperMoto effective wheelie target cannot exceed 89 degrees.
- [x] Extreme tilt does not invoke angle-based ejection.
- [x] Filtered hard physical collisions can still invoke ejection.
- [x] Focused validation and standing vehicle regression pass.
- [x] Windows player rebuild and startup smoke pass.
- [ ] Live wheelie feel remains for user playtest.

## Implementation plan

Clamp each controller at a shared 89-degree contract, make angle ejection explicitly return false, retain collision ejection unchanged, then update the existing focused validator and project records.

## Evidence

- Focused validation: `Logs/Codex-MINI-136-Validation.log` — PASS.
- Standing vehicle-impact regression: `Logs/Codex-MINI-136-MINI132-Regression.log` — PASS.
- Windows build: `Logs/Codex-MINI-136-Windows-Build.log` — Build Successful; built `Assembly-CSharp.dll` timestamp 2026-08-31 12:48:54.
- Built-player smoke: `Logs/Codex-MINI-136-Player-Smoke.log` — remained running for 12 seconds with no matched exception/error/crash/failed line.

## Handoff

- Files changed: reserved controllers, focused validator, and MINI-136 documentation records.
- Decisions made: D-013 revised to separate wheelie limit from physical crash detection.
- Visual locks added/changed: None.
- Known limitations: Motion feel requires the user's real playtest.
- Next action: User tests sustained 89-degree wheelies and one deliberate hard wall collision.
- Ownership released: Yes.
