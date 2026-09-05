# Up Iz Up Mini — MINI-133 Range Rover wheel and road-effects polish

```yaml
task_id: MINI-133
title: Range Rover mapped wheel motion, front steering, tyre marks and exhaust
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: one bounded Range Rover visual-effects pass
  claude_time: 0
  external_credits: 0
  stop_condition: focused validation, compile and screenshots, then stop for user appearance/play approval
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/RangeRoverWheelVisuals.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/RangeRoverRoadEffects.cs
  - Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab
  - Assets/UpIzUpMini/Editor/Mini133RangeRoverVisualSetup.cs
  - Assets/UpIzUpMini/Editor/Mini133RangeRoverValidation.cs
  - Docs/Systems/Vehicles.md
  - Docs/CURRENT.md
  - Docs/WorkPackets/MINI-133.md
  - PROJECT-HANDOFF.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Range Rover handling, collider positions, spawn and approved scene placement
  - user-approved character models, accessories and proportions
depends_on:
  - MINI-070
  - MINI-124
  - MINI-128
```

## Intent

Map all four visible Range Rover wheels to the matching WheelColliders. All tyres rotate with travel, only the front pair visibly steer, rear tyres make bounded slip/brake marks, and a small exhaust puff comes from the rear tailpipe. Reuse the proven bike effects pattern without changing handling or scene placement.

## Non-goals

- No body remodel, handling retune, scene regeneration, vehicle damage system or asset purchase.
- No changes to TMAX/SuperMoto visuals or effects.
- Rider ejection remains the next separate vehicle-safety task from MINI-132.

## Acceptance scorecard

- [x] Four visible wheel meshes are explicitly identified and mapped left/right/front/rear.
- [x] All wheels spin from WheelCollider pose and only the front pair steer.
- [x] Rear slip/braking produces short, mobile-bounded tyre marks.
- [x] Small exhaust smoke originates behind the tailpipe and stops when the vehicle is unoccupied.
- [x] Range Rover handling, colliders, spawn and approved placement are unchanged.
- [x] Focused validation and compile pass.
- [ ] Screenshots are shown to the user and live driving appearance remains user-approved.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles and prefab setup runs | Unity batch setup | `Logs/Tasks/MINI-133/build-validate-capture.log` BUILD PASS | No |
| Four-way mapping, baseline physics, effect references and budgets | Focused prefab validation | `Logs/Tasks/MINI-133/validation.txt` PASS | No |
| Front steering and wheel-well fit | Graphics-enabled fixed render | `Logs/Tasks/MINI-133/range-rover-front-steering.png` | Yes |
| Rear wheel fit and tailpipe side | Graphics-enabled fixed render | `Logs/Tasks/MINI-133/range-rover-rear-wheel-tailpipe.png` | Yes |
| Rotation, steering direction, marks and smoke in motion | User drive test | Pending | Yes |

The first `-nographics` screenshot attempt crashed inside Unity's renderer after build and validation had already passed. Capture was rerun with graphics enabled and produced both images cleanly; this is an evidence-tool limitation, not a prefab/runtime exception.

## Handoff

- Files changed: two runtime components, one additive setup/capture tool, one focused validator, the Range Rover vehicle prefab, one rim material and current ledgers.
- Decisions made: retain the merged body and use reversible wheel overlays; map by explicit collider name/sign; reuse approved skid/smoke art under strict mobile limits.
- Known limitations: original wheels remain fused under the overlays; live driving must approve wheel motion and effect strength; Windows player not rebuilt.
- Next action: user drives the Range Rover and reports wheel alignment/steering, marks and smoke. Then take the separate MINI-132 rider-ejection repair.
- Ownership released: Yes
