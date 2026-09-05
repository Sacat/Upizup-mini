# Up Iz Up Mini — MINI-135 Crash threshold and seller impact correction

```yaml
task_id: MINI-135
title: Reduce false rider ejection, add the 90–95 degree wheelie fall-back window, and restore seller vehicle hits
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: one bounded playtest-feedback pass
  claude_time: 0
  external_credits: 0
  stop_condition: focused validation, regression validation, Windows build, and user playtest handoff
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeCrashEjectionController.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleImpactResponder.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Editor/Mini134CrashDamageRecoveryValidation.cs
  - Docs/Systems/Vehicles.md
  - Docs/Systems/Combat.md
  - Docs/CURRENT.md
  - Docs/WorkPackets/MINI-135.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - DECISIONS.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Prefabs/Characters/Sacat.prefab
  - Assets/UpIzUpMini/Prefabs/Characters/Franki.prefab
depends_on:
  - MINI-134
```

## Intent

Riders should stay mounted during ordinary bumps and minor collisions. Ninety degrees is the wheelie balance point; the bike may continue to 95 degrees, then it and all riders fall back together. Vehicle contact must affect protected sellers as reliably as police while preserving seller recovery/return behavior.

## Non-goals

- Do not change approved rider fit, wheel mapping, steering, suspension, or scene placement.
- Do not change seller defeat policy or allow permanent seller removal.
- Do not regenerate the gameplay scene.

## Acceptance scorecard

- [x] Minor impacts, upward ground contacts, glancing scrapes and ordinary NPC contact are filtered from easy ejection.
- [x] 90 degrees is treated as balanced; ejection begins only beyond 95 degrees.
- [x] Past 95 degrees, a backward angular kick precedes shared driver/pillion ejection; live motion approval remains.
- [x] Police and stationary seller collision/trigger targets both resolve to shared combat health.
- [x] MINI-135 focused validation and MINI-132 regression pass.
- [x] Windows player rebuilt and startup smoke passed.
- [ ] User motion/feel approval remains explicitly owed.

## Evidence

- `Logs/Codex-MINI-135-Validation.log`: focused threshold, contact filtering, idempotent seller collider and protected seller impact PASS.
- `Logs/Codex-MINI-135-MINI132-Regression.log`: standing combat/impact regression PASS.
- `Logs/Codex-MINI-135-Windows-Build.log`: final Windows build SUCCESS, Unity return code 0.
- Final rebuilt player remained alive for a 10-second startup smoke; matching exception scan was empty.
- Human check owed: crash/wheelie motion, final sensitivity, and seller fall/get-up appearance.
