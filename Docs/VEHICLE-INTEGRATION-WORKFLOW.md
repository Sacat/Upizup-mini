# Vehicle Rider Integration Workflow

How to mount a character onto a new bike/vehicle, reusing the system proven on the TMAX (MINI-066) and the SuperMoto (MINI-119). Written after several dead-end approaches on the SuperMoto (ragdoll physics, a runtime-built Animation Rigging graph, Mecanim's built-in `OnAnimatorIK`) all failed the same way in real Play Mode despite passing everything checkable outside it — the process below is what actually held up.

## Use `VehicleRider` + `VehicleSeat`, not a new rig per vehicle

`VehicleRider.cs` and `VehicleSeat.cs` are the one shared system. Do **not** build a new Animation-Rigging (`RigBuilder`/`TwoBoneIKConstraint`) graph or a ragdoll per vehicle — both were tried for the SuperMoto and produced recurring, hard-to-diagnose failures (T-pose on mount, animation state that silently doesn't survive a scene save, duplicated components from re-running spawn logic). `VehicleRider` avoids all of that: it uses a real Animator-driven sustained pose (`HumanoidAnimationManager.BeginSustainedAction`) plus two authored keyframe poses (seated, wheelie) that are plain position/pitch offsets, blended by how deep the wheelie is.

### Steps for a new vehicle

1. **Find/author the vehicle's own anchor points.** A hand-grip and foot-peg Transform per limb, positioned on the actual model (`RightHandPos`/`LeftHandPos`/`RightFootPos`/`LeftFootPos` — reuse these exact names if the vendor asset already ships them, e.g. via `FindDeepByName`).
2. **Add a `VehicleSeat` component** to the vehicle root. `Configure()` it with the seat anchor Transform and the four hand/foot targets, plus the action ids (`"MountBike"` / `"RideBike"` are already baked into the shared character controller — reuse them unless the pose genuinely doesn't fit).
3. **Bake the seated pose per-vehicle, not on `VehicleRider` directly.** `VehicleRider`'s own `seatedOffset`/`wheelieOffset` fields are shared across every vehicle that uses it — call `seat.ConfigureSeatedPose(offset, pitch)` / `seat.ConfigureWheeliePose(offset, pitch)` on the vehicle's own `VehicleSeat` instead. Find the seated offset by mounting live, pausing, reading the character's local Position/Rotation relative to the seat anchor from the Inspector, and baking those exact numbers in (manual placement is authoritative — don't guess).
4. **For the wheelie pose specifically**, don't reuse another vehicle's absolute offset verbatim — it was calibrated against *that* vehicle's own seated baseline. Apply the same *relative* shift (`otherVehicleWheelieOffset - otherVehicleSeatedOffset`) onto this vehicle's own seated offset as a starting point, then fine-tune live via `TmaxWheelieTuner` (not TMAX-specific despite the name — it drives whichever `VehicleRider` is currently mounted).
5. **Write a lean interactable** (F to mount/dismount) mirroring `BikeInteractable`'s proven pattern — `SuperMotoVehicleInteractable.cs` is the reference for a vehicle whose own physics/input scripts already read the keyboard directly (no throttle/steer relay needed, unlike `BikeInteractable`+`TmaxBikeController`).
6. **Feed the wheelie blend every frame while mounted**: `rider.SetWheelieBlend(wheelie01)` from the vehicle's own live wheelie-progress source, plus `riderAnim.UpdateWheelieOverlay(wheelie01 > threshold)` (made public on `BikeRiderAnimation` for exactly this reuse) to also blend in the authored "BikeWheelie" animation pose — the position blend alone only translates the body, it never reshapes the arms, which reads as the hands not tracking during a wheelie if skipped.
7. **Add `SuperMotoHandFootLock`-style direct bone pinning on top**, not instead of, `VehicleRider`. Mecanim's built-in hand IK (`OnAnimatorIK`) failed twice on this project despite `IK Pass` being genuinely enabled — a continuous direct override (`Gadd420.IK.cs`, the vendor's own technique, attached to each hand/foot bone with `chainLength = 2` and `target` = the anchor) runs every `LateUpdate`, after the Animator, and can't silently under-apply the way Mecanim IK can. Retry attaching every frame until `animator.isHuman` is actually true — see the gotcha below for why a one-shot attempt can fail forever, not just for one frame.

## The Animator gotcha that cost the most time

**Check whether the Animator is really on the same GameObject you're adding scripts to, not a child.** On this project's own character (Sacat), the Animator lives on a child object (`Visual`), not the root. A script with `[RequireComponent(typeof(Animator))]` plus a same-object-only `GetComponent<Animator>()`, added to the root, makes Unity **silently auto-create a second, completely blank Animator** (no controller, no avatar) directly on the root to satisfy the requirement — and every `OnAnimatorIK`/bone-fetch call from then on talks to that dead ghost, never the real one. This is *not* a symptom that goes away by waiting longer or checking `isHuman` in a retry loop — the ghost's `isHuman` is false forever, because it has no avatar to bind at all.

Fix and prevention:
- Never assume the Animator is on the same object your script is attached to. Use `GetComponentInChildren<Animator>()` instead of `GetComponent<Animator>()` unless you've actually confirmed otherwise.
- Don't declare `[RequireComponent(typeof(Animator))]` on a script that might get added to an object that doesn't directly carry the Animator — it's actively harmful here, not just redundant.
- If something using `Animator.GetBoneTransform`/humanoid IK has ever mysteriously failed, check the actual character's Hierarchy for **two** Animator components before assuming anything about batch-mode limitations or timing races. Use `GetComponentsInChildren<Animator>(true)` and inspect `avatar`/`runtimeAnimatorController` on each — a blank one is unmistakable (`NULL`/`NULL`).

This exact bug was previously misdiagnosed as "the Editor's batch-mode testing can't bind an Avatar" — a conclusion reached and accepted four separate times before being properly root-caused. It never was a testing limitation; the real Animator binds and reports `isHuman = true` correctly even from an Editor script, the same way it does in real Play Mode.

## Pillion (second rider)

Reuse `BikeInteractable.BoardPillion`'s own pattern almost verbatim — the other main character (whichever one isn't currently driving) hops onto a second `VehicleSeat` automatically if nearby and not locked away doing something else. Two things worth doing differently from that original TMAX implementation, learned building the SuperMoto's version:

- **Give the pillion seat its own baked `ConfigureSeatedPose` too**, not just the driver's. It defaults to the same shared TMAX-calibrated value as the driver seat does — skipping this makes the passenger appear floating in the wrong spot relative to his own seat anchor, not just offset from the driver.
- **Check for the passenger every frame while mounted, not once at the moment the driver mounts.** A one-shot check (what the original TMAX pattern does) only works if the player always walks up and presses F — by then the other character is already nearby from normal following. A vehicle that can be mounted programmatically or instantly (e.g. an auto-mount-on-spawn dev flag) breaks that assumption completely, since the passenger has no time to catch up before the one-shot check already ran and failed.
- If the passenger doesn't need to grip anything (no handlebar within reach), leave the pillion seat's hand/foot targets unset OR reuse the driver's own animation pose (not a separate "cheer"/passenger-specific clip if the vendor's own one looks wrong on this rig) and pin the hands to simple side-resting target Transforms with the same `SuperMotoHandFootLock` technique, instead of fighting the authored clip's own hand placement.
- The whole system is already symmetric — nothing needs to be hardcoded per-character. Verify this directly (switch active to the other main character, mount, confirm the first one auto-boards as pillion) rather than assuming it, since it's easy to accidentally test only one direction.

## Reference implementation

- `Assets/UpIzUpMini/Scripts/Vehicles/VehicleRider.cs` / `VehicleSeat.cs` — the shared rider/seat system.
- `Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoVehicleInteractable.cs` — a complete, minimal mount/dismount interactable for a vehicle with its own input handling.
- `Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoHandFootLock.cs` — the direct bone-override hand/foot pin, with the avatar-ready retry pattern.
- `Assets/UpIzUpMini/Scripts/Vehicles/BikeRiderAnimation.cs` — `UpdateWheelieOverlay` (public) is the reusable piece for blending in an authored wheelie animation pose regardless of which vehicle drives it.
- MINI-119 commit history (search `git log --grep=MINI-119` in this repo) has the full story, including every dead end, in commit messages.
