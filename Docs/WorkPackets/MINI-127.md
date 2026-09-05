# Up Iz Up Mini — MINI-127 TMAX ride repair

```yaml
task_id: MINI-127
title: Remove duplicate startup bike and restore purchasable TMAX controls
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: A
budget:
  codex_time: one focused controller repair, validation and Windows rebuild
  claude_time: 0
  external_credits: 0
  stop_condition: one startup TMAX, easier mounting, working custom-controller input, clean TMAX controller log
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeRiderAnimation.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs
  - Assets/UpIzUpMini/Editor/Mini127TmaxRideRepairValidation.cs
  - Builds/GrandBayProof/
  - Docs/WorkPackets/MINI-127.md
  - Docs/Systems/Vehicles.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
depends_on:
  - MINI-126
```

## Intent

Make the same TMAX used by the shop and test spawn mount reliably and respond to W/S/A/D, while preventing the old saved SuperMoto/Koss test bike from appearing beside it.

## Root cause

`BikeInteractable` was resolving the newer SuperMoto-facing `TmaxBikeController` facade. On `TMAX_560.prefab`, its vendor `RB_Controller` has empty wheel arrays plus null fork/centre-of-gravity references, producing `IndexOutOfRangeException` every frame. The original fully wired `TmaxBikeControllerCustom` remains on the prefab and exposes the complete input/state contract that `BikeInteractable` needs.

## Non-goals

- Do not change the TMAX body, wheels, physics tuning, scene placement, shop price, or ownership behavior.
- Do not purchase/download an alternative model in this task.

## Acceptance scorecard

- [x] `BikeInteractable` drives `TmaxBikeControllerCustom`
- [x] Incompatible vendor components are disabled immediately on spawned/purchased TMAX instances
- [x] Saved `StockDemoSuperMoto` is inactive during the temporary TMAX test
- [x] Mount range is forgiving
- [x] Compile and focused validation pass
- [x] Windows EXE rebuilt
- [ ] User confirms mount/controls and visuals

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compile | Unity batch compile | `Logs/Tasks/MINI-127/compile.log`, clean | No |
| Wiring | Focused validator | `Logs/Tasks/MINI-127/validation.log`, pass | No |
| Startup | 18-second built-player smoke/log | Repair logs present; no vendor steering/nitrous error | No |
| Ride feel | User playtest | Pending | Yes |

## Handoff

- Files changed: `BikeInteractable.cs`, `BikeRiderAnimation.cs`, `VehicleSpawnController.cs`, focused validator and current handoff/ledger files
- Decisions made: Keep the existing TMAX model and restore its proven controller
- Known limitations: Real mounting and driving feel require user playtest
- Next action: User tests rebuilt EXE with F, W/S/A/D, Space and E
- Ownership released: Yes
