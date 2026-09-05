# Up Iz Up Mini — MINI-128 TMAX parking and road effects

```yaml
task_id: MINI-128
title: Keep the test TMAX visible and add mobile-bounded exhaust/skid effects
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: B
budget:
  codex_time: one focused vehicle-effects pass, validation and Windows rebuild
  claude_time: 0
  external_credits: 0
  stop_condition: TMAX stays parked, shows exhaust smoke when ridden, and leaves black marks only under real braking/slip
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxRoadEffects.cs
  - Assets/UpIzUpMini/Editor/Mini128TmaxRoadEffectsSetup.cs
  - Assets/UpIzUpMini/Editor/Mini064TmaxAssetPrep.cs
  - Assets/UpIzUpMini/Editor/Mini065TmaxPhysicsTest.cs
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
  - Builds/GrandBayProof/
  - Docs/WorkPackets/MINI-128.md
  - Docs/Systems/Vehicles.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
depends_on:
  - MINI-127
```

## Intent

Prevent the temporary market-road TMAX from rolling away while empty, make its location obvious, and give it lightweight exhaust smoke plus SuperMoto-style tyre smoke/black skid marks while ridden.

## Non-goals

- Do not change the approved TMAX model, wheels, rider pose, handling or world placement.
- Do not restore the broken vendor bike controller.
- Do not emit permanent marks continuously during ordinary rolling; marks require braking or measured wheel slip.

## Acceptance scorecard

- [x] Empty TMAX applies its parking brake and remains at the market-road spawn
- [x] Spawn alert identifies the Farm Shop/Produce Buyer location and minimap marker
- [x] Exhaust smoke is bounded for mobile and only active with a rider
- [x] Rear tyre smoke and black marks appear during braking or real slip
- [x] Pillion keeps its approved height but uses the SuperMoto-style forward-facing seat rotation and ride pose
- [x] Focused validator and standing TMAX validator pass
- [x] Windows player rebuilt and smoke checked
- [ ] User visually accepts effects and confirms real ride behaviour

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compile | Unity batch compile | `Logs/Tasks/MINI-128/compile-pillion.log` — pass | No |
| Wiring/budget | Focused validator | `Logs/Tasks/MINI-128/validation-pillion.log` — pass; particle cap 36, mass 480, custom controller retained | No |
| Windows build | Unity build pipeline | `Logs/Tasks/MINI-128/build-pillion.log` — success; `Builds/GrandBayProof/UpIzUpMini.exe` | No |
| Startup/parking | Built-player log | Spawn/effects-ready/parked messages at `(-8.00, 10.16, -152.09)`; no TMAX-specific exception | No |
| Moving effects and pillion | Live player inspection | Successful forward movement; visible right-rear smoke, curved black tread mark and two forward-facing riders | Yes — user taste/feel acceptance remains |

## Handoff

- Files changed: `BikeInteractable.cs`, `VehicleSpawnController.cs`, new `TmaxRoadEffects.cs`, new `Mini128TmaxRoadEffectsSetup.cs`, `Mini064TmaxAssetPrep.cs`, `Mini065TmaxPhysicsTest.cs`, `TMAX_560.prefab`, build and task/system documentation
- Decisions made: Reuse the installed SuperMoto skid prefabs/materials through a TMAX-specific adapter; keep vendor controller disabled
- Known limitations: Sustained handling and effect intensity remain a human taste check. The pre-existing `NpcCombatHealth.ResetForRespawn` gang exception is unrelated and remains open.
- Next action: User rides the rebuilt TMAX and confirms smoke amount/position, road-mark frequency and pillion appearance through normal turns/wheelies.
- Ownership released: Yes
