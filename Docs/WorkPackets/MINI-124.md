# MINI-124 — TMAX spinning wheels and steering assembly

```yaml
task_id: MINI-124
title: Replace TMAX wheel overlays with black SuperMoto-reference wheels and synchronized steering
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: one bounded prefab integration and verification pass
  claude_time: 0
  external_credits: 0
  stop_condition: compile, focused validation, static screenshots and a short motion proof are ready for user review
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini124TmaxWheelSteeringUpgrade.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxWheelVisuals.cs
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
  - Assets/UpIzUpMini/Vehicles/TmaxWheelBlack.mat
  - Logs/Tasks/MINI-124
  - Docs/Systems/Vehicles.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity
depends_on:
  - MINI-066 TMAX riding baseline
  - MINI-119 SuperMoto reference hierarchy
```

## Intent

The TMAX should show real black wheel meshes that visibly spin, the front wheel should steer, and the handlebar/grip targets should turn with it. Existing TMAX physics, rider placement, wheelie behavior and purchase/save behavior remain unchanged.

## References

- Existing in-game reference: `Assets/MotorbikePhysicsTool/Prefabs/Bikes/SuperMoto.prefab`
- Target prefab: `Assets/UpIzUpMini/Vehicles/TMAX_560.prefab`
- User direction: use the SuperMoto as the reference; black wheels; front wheel and handlebar must turn.

## Non-goals

- No vehicle-physics retuning.
- No road or gameplay-scene edits.
- No paid generation, package import or broad model replacement.
- No changes to approved seat, rider, pillion or wheelie poses.

## Acceptance scorecard

- [x] Front and rear wheel visuals are black and fitted to the measured axle transforms.
- [x] Both wheels spin from WheelCollider world poses.
- [x] Front wheel, fork/handlebar pivot and hand targets steer together.
- [x] Rear wheel does not steer.
- [x] Existing TMAX physics and interaction validators still pass.
- [x] Static close and mobile-distance screenshots captured.
- [x] Short motion evidence captures steering and wheel spin.
- [ ] User visual approval received.

## Implementation plan

1. Back up the current TMAX prefab and inspect the SuperMoto wheel/fork hierarchy.
2. Build a reversible editor upgrade that reuses source meshes/material slots without touching source assets.
3. Extend the existing `TmaxWheelVisuals` synchronization only as needed for a separate steering pivot and grip targets.
4. Validate prefab wiring, compile, render fixed screenshots and capture motion.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity 6000.3.10f1 batch imports/compiles during all runs | `Logs/Tasks/MINI-124/*.log`, no compiler errors | No |
| Behavior | MINI-124 focused validation + original MINI-065 structural validation + real-physics drop/drive/wheelie test | `validation.txt`, `mini065-validation.log`, `mini065-drop-2.log`: PASS | Yes for hands-on steering/hand follow |
| Appearance | fixed before/after screenshots | `TMAX-MINI124-before-*.png`, `TMAX-MINI124-after-*.png` | Yes |
| Motion/feel | 48-frame articulation capture encoded at 24fps | `TMAX-MINI124-steering-wheel-motion.mp4` | Yes; transform proof is not a real ride |

## Handoff

- Files changed: `TmaxWheelVisuals.cs`, `TMAX_560.prefab`, `TmaxWheelBlack.mat`, `Mini124TmaxWheelSteeringUpgrade.cs`, workflow/handoff documents
- Decisions made: preserve physics; replace visuals only; SuperMoto is the hierarchy/mesh reference; retain stable root-level hand-target paths
- Visual locks added/changed: none pending user approval
- Known limitations: the original scan contains fused static wheels/handlebar geometry; new moving parts overlay it. Perfect removal requires later Blender separation.
- Next action: user reviews screenshots/motion, then real-play checks steering and hand tracking; adjust fit if requested before Windows build.
- Ownership released: yes
