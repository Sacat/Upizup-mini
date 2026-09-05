# MINI-137 — TMAX exhaust, wheelie sparks and rider pose polish

```yaml
task_id: MINI-137
title: Reduce TMAX exhaust, add capped-wheelie sparks, and reuse the SuperMoto wheelie blend
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: One bounded vehicle-effects and rider-animation pass
  claude_time: 0
  external_credits: 0
  stop_condition: Focused validation, regression compile, Windows build and smoke complete
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxRoadEffects.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeInteractable.cs
  - Assets/UpIzUpMini/Editor/Mini137TmaxWheeliePolishValidation.cs
  - Docs/CURRENT.md
  - Docs/Systems/Vehicles.md
  - Docs/WorkPackets/MINI-137.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - All user-approved rider fit, wheel, handling, character and scene placement files
depends_on:
  - MINI-136
```

## Intent

Make TMAX exhaust lighter and less frequent, emit a mobile-bounded spark effect from the rear underside only while the bike is at its 89-degree wheelie cap and moving at least 12 mph, and use the SuperMoto rider's proven wheelie-animation blend on the TMAX.

## Non-goals

No handling, wheel, seat, IK, pillion, crash, scene, prefab, or placement change.

## Acceptance scorecard

- [x] TMAX exhaust is less dense and more transparent.
- [x] Sparks require the 89-degree cap and at least 12 mph.
- [x] Spark particles are tightly mobile-bounded.
- [x] TMAX rider uses the SuperMoto wheelie clip weight while mounted and restores the prior weight on dismount.
- [x] Focused validation and standing vehicle regression pass.
- [x] Windows player rebuild and startup smoke pass.
- [ ] Live appearance and motion remain for user playtest.
## Evidence

- Focused validation: `Logs/Codex-MINI-137-Validation.log` — PASS.
- Standing MINI-136 regression: `Logs/Codex-MINI-137-MINI136-Regression.log` — PASS.
- Windows build: `Logs/Codex-MINI-137-Windows-Build.log` — Build Successful.
- Startup smoke: rebuilt player remained running for 12 seconds and was then deliberately terminated; the requested custom log file was not emitted, so this proves startup survival only.

## Handoff

- Files changed: TMAX effects adapter, TMAX rider mount behavior, focused validator, and MINI-137 records.
- Decisions made: reuse the current in-game SuperMoto rider's exact authored lift blend (0.22) only while mounted on TMAX.
- Visual locks added/changed: no scene or prefab placements changed.
- Known limitations: spark location/appearance, exhaust taste and animation feel require the user's real playtest.
- Next action: hold a TMAX wheelie at 89 degrees above 12 mph and check rear-underside sparks, exhaust strength and rider lift.
- Ownership released: Yes.