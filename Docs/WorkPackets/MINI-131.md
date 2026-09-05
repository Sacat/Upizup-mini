# Up Iz Up Mini — MINI-131 TMAX wheel lock and rider lean damping

```yaml
task_id: MINI-131
title: Keep TMAX wheel visuals on their hubs and reduce excessive rider turn swing
request_owner: User
integrator: Codex
status: evidence_ready_pending_user_ride
approval_class: C
budget:
  codex_time: one bounded vehicle synchronization and lean-tuning pass
  claude_time: 0
  external_credits: 0
  stop_condition: wheels remain hub-locked by architecture, rider lean is bounded and smoothed, validators/build pass, then stop for user ride approval
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxWheelVisuals.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeRiderAnimation.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxRoadEffects.cs
  - Assets/UpIzUpMini/Editor/Mini131TmaxVisualSyncValidation.cs
  - Docs/WorkPackets/MINI-131.md
  - Docs/Systems/Vehicles.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
depends_on:
  - MINI-124
  - MINI-129
```

## Intent

The added TMAX wheel meshes must stay concentric with their physics hubs throughout a long ride. Sacat/Franki should still react to turns, but with a smaller, smoother body lean that cannot swing broadly across the bike.

## References

- User playtest: wheels begin correctly and drift out of place after time; rider animation swings too much during turning/leaning.
- Existing reference: SuperMoto's restrained rider synchronization and the TMAX's approved neutral seated position.

## Non-goals

- Do not change TMAX physics, model scale, approved rider height/forward placement, pillion fit, wheel size, or world spawn.
- Do not replace animation assets or rebuild the vehicle model.
- Do not change exhaust behaviour beyond the user's requested small right-rear outlet placement correction.

## Acceptance scorecard

- [x] Wheel visuals are written after physics/animation and remain locked to WheelCollider poses without accumulated offsets.
- [x] Rider turn lean is clamped, damped and returns smoothly to neutral by architecture.
- [x] Existing wheel spin, front steering, handlebar targets, riding and wheelie remain wired.
- [x] Focused and standing TMAX validators pass.
- [x] Windows player rebuilt and startup smoke checked.
- [ ] User visually approves a sustained ride with turns.

## Implementation plan

1. Inspect the wheel and rider update order and identify any cumulative/local-space drift.
2. Make visual transforms stateless and apply them after all Animator/physics updates.
3. Reduce/clamp/smooth only the rider's steering roll contribution.
4. Add a focused structural/regression validator, rebuild once, and stop for real ride approval.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/Tasks/MINI-131/compile-final.log` | No |
| Wheel/lean structure | 250-cycle forced displacement + focused validator | `Logs/Tasks/MINI-131/validation-final3.log` PASS | No |
| Standing regression | MINI-065/124/127 | `Logs/Tasks/MINI-131/mini065.log`, `mini124.log`, `mini127.log` PASS | No |
| Windows build | Unity player build | `Builds/GrandBayProof/UpIzUpMini.exe`; `level0`/Assembly-CSharp fresh 2026-08-30 15:14 | No |
| Runtime startup | 12-second built-player smoke | `Logs/Tasks/MINI-131/player-smoke.log`; remained running, no exception | No |
| Sustained motion/feel | User ride | Pending | Yes |

## Handoff

- Files changed: `TmaxWheelVisuals.cs`, `BikeRiderAnimation.cs`, `BikeInteractable.cs`, `TmaxRoadEffects.cs`, focused validator and current handoff/ledger files.
- Decisions made: cached local hub points preserve the approved asymmetric scan fit without allowing positional accumulation; a brief lean accent plus bounded roll is safer than destructively cutting the animation asset.
- Visual locks added/changed: None; this is a requested revision to unapproved motion
- Known limitations: Motion requires a sustained real ride to accept
- Next action: User rides for several minutes with repeated left/right turns and checks wheel hubs, rider swing and smoke position.
- Ownership released: Yes
