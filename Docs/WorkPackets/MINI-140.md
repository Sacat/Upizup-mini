# MINI-140 — E to mount / Q to wheelie control remap

```yaml
task_id: MINI-140
title: Mount/enter vehicles with E (normal interact key), wheelie with Q
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: A
budget:
  claude_time: One bounded input-remap and validation pass
  codex_time: 0
  external_credits: 0
  stop_condition: Focused validation, compile check and Windows build complete
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoVehicleInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoWheelieKeyRemap.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/CarInteractable.cs
  - Assets/UpIzUpMini/Scripts/Character/CellPhoneController.cs
  - Assets/UpIzUpMini/Editor/Mini140VehicleControlRemapValidation.cs
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab            # NARROW EXCEPTION - see note
  - Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab  # NARROW EXCEPTION - see note
  - Docs/CURRENT.md
  - Docs/Systems/Vehicles.md
  - Docs/WorkPackets/MINI-140.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - All approved TMAX/SuperMoto/Range Rover handling, animation, wheels, rider fit,
    effects, seats and scene placement

## Prefab edit note (narrow exception)

`TMAX_560.prefab` and `RangeRover_Vehicle.prefab` serialize their own `KeyCode`
values (`wheelieKey`/`dismountKey`/`mountKey` on the TMAX; `enterKey`/`exitKey` on
the Rover), which override the C# field defaults. Honouring the requested remap
therefore requires changing those integer key codes in the two prefab YAML files -
`101`/`102`/`113` = `KeyCode.E`/`F`/`Q`. This is an input-binding-only edit: no
mesh, transform, collider, wheel, seat, rider-anchor, effect or scene-placement
value was touched. The runtime-added `SuperMotoVehicleInteractable` and
`SuperMotoWheelieKeyRemap` take their new behaviour from code alone (no prefab).
  - The stale SuperMotoInteractable.cs / SuperMotoStockInteractable.cs / TmaxTestInput.cs
    (dead or test-only per Docs/Systems/Vehicles.md — left untouched)
depends_on:
  - MINI-139
```

## Intent

Move vehicle mounting/entering onto the game's normal interact key **E**, and the
bike wheelie onto **Q**, per the user's requested control scheme (Docs/Systems/Vehicles.md
open item "E for mount/enter vehicles, Q for wheelie").

Before: F mounts/dismounts every vehicle (also the melee Attack key); E is the
wheelie key on the bikes as well as the world interact key; Q calls the partner/gang.

After:
- **E** mounts and dismounts the TMAX, the SuperMoto and the car. Mount uses the
  existing interactable/prompt path plus each vehicle's own more generous mount-range
  key check, now bound to `KeyCode.E`. Dismount is a direct `KeyCode.E` check while
  mounted, guarded so the same key-press that mounts cannot instantly dismount.
- **Q** is the TMAX wheelie key (`BikeInteractable.wheelieKey`) and the SuperMoto
  wheelie key (`SuperMotoWheelieKeyRemap`, E removed as a wheelie trigger).
- **Q phone call** (`CellPhoneController`) is suppressed while the active character
  is riding any vehicle (`PlayerController.IsControlled == false`), so it does not
  fire alongside the wheelie. On foot, Q still calls the partner unchanged.
- **F** remains the melee Attack key only; it no longer mounts anything.

## Non-goals

No handling, physics, wheelie angle/rate, crash, spark, exhaust, animation, seat,
prefab, scene or placement changes. No touch/mobile input-facade work. No changes
to the dead SuperMoto scripts or the test-only `TmaxTestInput`. No new key for the
pause menu (its Q only reads while paused — no conflict).

## Acceptance scorecard

- [x] TMAX / Range Rover prefabs and the SuperMoto interactable default bind mount+dismount to E (validated).
- [x] TMAX `wheelieKey` is Q; SuperMoto wheelie remap reads Q only, E removed (validated).
- [x] `CellPhoneController` gates its Q call on `PlayerController.IsControlled` (validated).
- [x] The E press that mounts is guarded (`_mountedFrame` / `_enteredFrame`) so it cannot also dismount that frame.
- [x] F is untouched as the melee Attack key; no vehicle reads F any more.
- [x] Focused MINI-140 validation passes.
- [x] Standing MINI-065 / MINI-139 / MINI-137 regressions pass.
- [x] Compile clean and Windows build succeeds (fresh `level0` + `Assembly-CSharp.dll`).
- [x] 15-second startup smoke: player alive, no exceptions.
- [ ] Live control feel — walk-up-E mount, E dismount, Q wheelie, on-foot Q still calls partner — remains for the user's playtest.

## Evidence

- Focused validation: `Logs/Tasks/MINI-140/validate2.log` — `[MINI-140] PASS`.
- Regression: `reg065.log` (MINI-064/065 PASS), `reg139.log` (`[MINI-139] PASS`), `reg137.log` (`[MINI-137] PASS`).
- Windows build: `Logs/Tasks/MINI-140/winbuild.log` — `Build Successful`, `Builds/GrandBayProof/UpIzUpMini.exe` (389,008,595 bytes).
- Startup smoke: `Logs/Tasks/MINI-140/smoke.log` — ran 15 s, then deliberately terminated; no exception / NullRef / missing-component. Only pre-existing benign `CharacterController.Move called on inactive controller` warnings.

## Handoff

- Files changed: `BikeInteractable.cs`, `SuperMotoVehicleInteractable.cs`,
  `SuperMotoWheelieKeyRemap.cs`, `CarInteractable.cs`, `CellPhoneController.cs`,
  new `Mini140VehicleControlRemapValidation.cs`, plus the two prefab key-code
  edits (see the prefab edit note above), and the doc/record files.
- Decisions made: E via the normal interact path, keeping each vehicle's own
  wider mount-range key check bound to E (InteractionDetector's 2.75 m prompt
  range is unchanged, the bike's 4.25 m mount reach is preserved). Phone Q
  suppressed while `IsControlled` is false rather than rebound to another key.
- Visual locks changed: None. No mesh/transform/collider/seat/effect value touched.
- Known limitation: input feel is hands-on only; still needs the user's ride test.
- Next action: user playtests the control scheme against the new build.
- Ownership released: Yes.
