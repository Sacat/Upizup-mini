# MINI-139 — TMAX 12 mph wheelie sustain floor

```yaml
task_id: MINI-139
title: Require 12 mph to start and sustain a TMAX wheelie
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: A
budget:
  codex_time: One bounded TMAX wheelie-gate and validation pass
  claude_time: 0
  external_credits: 0
  stop_condition: Focused validation, standing regression, Windows build and smoke complete
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxBikeControllerCustom.cs
  - Assets/UpIzUpMini/Editor/Mini139TmaxWheelieSpeedValidation.cs
  - Docs/CURRENT.md
  - Docs/Systems/Vehicles.md
  - Docs/WorkPackets/MINI-139.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - All approved TMAX handling, animation, wheels, rider fit, effects and placement
depends_on:
  - MINI-138
```

## Intent

Require at least 12 mph (19.31 km/h) to start or sustain a TMAX wheelie. When speed falls below the threshold, drive the existing wheelie target toward zero using its current recovery rate.

## Non-goals

No snap-down, wheelie-cap, rise/recovery rate, steering, crash, spark, animation, prefab, scene, or placement changes.

## Acceptance scorecard

- [x] 12 mph is the effective minimum wheelie speed.
- [x] A wheelie cannot start below 12 mph.
- [x] A raised wheelie cannot remain sustained below 12 mph.
- [x] Dropping below the threshold selects the existing zero target/recovery path.
- [x] Rear-wheel contact chatter remains excluded from the sustain gate.
- [x] Focused and standing wheelie/crash validations pass.
- [x] Windows build and startup smoke pass.
- [ ] Live lowering feel remains for user playtest.
## Evidence

- Focused validation: `Logs/Codex-MINI-139-Validation.log` — PASS.
- Standing MINI-138 regression: `Logs/Codex-MINI-139-MINI138-Regression.log` — PASS.
- Windows build: `Logs/Codex-MINI-139-Windows-Build.log` — Build Successful.
- Startup smoke: rebuilt player remained running for 12 seconds, then was deliberately terminated.

## Handoff

- Files changed: TMAX custom controller, focused validator, and MINI-139 records.
- Decisions made: 12 mph is the effective start/sustain floor; below it uses the existing recovery rate.
- Visual locks changed: None.
- Known limitation: lowering feel is motion work and still needs the user's real ride test.
- Next action: start a wheelie at 12+ mph, then release throttle until speed falls below 12 mph and confirm the front lowers smoothly.
- Ownership released: Yes.