# MINI-138 — Wheelie-mode crash sensitivity correction

```yaml
task_id: MINI-138
title: Stop rotational wheelie contacts from ejecting riders too easily
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: A
budget:
  codex_time: One bounded crash-filter and validation pass
  claude_time: 0
  external_credits: 0
  stop_condition: Focused regression, Windows build and startup smoke complete
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeCrashEjectionController.cs
  - Assets/UpIzUpMini/Editor/Mini134CrashDamageRecoveryValidation.cs
  - Docs/CURRENT.md
  - Docs/Systems/Vehicles.md
  - Docs/WorkPackets/MINI-138.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - All handling, wheelie-angle, rider fit, animation, effect and placement files
```

## Intent

During an active wheelie, prevent forced rotational/rear-body contact from ejecting the rider at modest road speed. Preserve real high-speed wall-crash ejection and the shared 89-degree wheelie cap.

## Non-goals

No wheelie angle, handling, animation, sparks, exhaust, wheels, seats, scene, prefab, NPC damage, or placement changes.

## Acceptance scorecard

- [x] Wheelie collisions require a higher impact threshold than normal riding.
- [x] Wheelie collisions also require sufficient real Rigidbody travel speed.
- [x] A 12 mph wheelie scrape cannot eject the rider.
- [x] A genuine high-speed wheelie wall collision can still eject.
- [x] Normal upright collision behavior is unchanged.
- [x] Existing collision-only/89-degree regression passes.
- [x] Windows player rebuild and startup smoke pass.
- [ ] Live crash feel remains for user playtest.
## Evidence

- Focused regression: `Logs/Codex-MINI-138-Validation.log` — PASS.
- Windows build: `Logs/Codex-MINI-138-Windows-Build.log` — Build Successful.
- Startup smoke: rebuilt player remained running for 12 seconds, then was deliberately terminated.

## Handoff

- Files changed: shared bike crash filter, existing crash regression, and MINI-138 records.
- Decisions made: active wheelies require both a 1.15x impact threshold and real speed at the normal 16.5 m/s crash floor.
- Visual locks changed: None.
- Known limitation: crash sensitivity is motion/feel work and still needs the user's real ride test.
- Next action: perform low-speed wheelie tail touches and one deliberate high-speed wall crash.
- Ownership released: Yes.