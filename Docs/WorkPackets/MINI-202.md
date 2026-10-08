# MINI-202: TMAX driver and pillion move with the bike (lean, squat/dive, bumps, eyes on the road)

```yaml
task_id: MINI-202
title: TMAX riders lean/pitch with the body, absorb bumps, keep their heads level; based on the SuperMoto's behaviour
request_owner: User ("tmax bike and the character pillion animation move more realistic like the leaning wheelie animation ... bike should have better physics like bumping based on the terrain, shocks working ... check the other bikes and do it based on that")
author: Claude cloud session (code only; no Unity available)
status: code written, NOT compiled or play-tested
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxRideDynamics.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxBikeControllerCustom.cs (two lines + two properties)
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleRider.cs (additive, defaults off; shared with the SuperMoto)
```

## What was found (read from the code and the TMAX_560 prefab)

- MINI-193 already gives the TMAX body real suspension (front 0.22 m / 23500 / 2200, rear 0.20 m / 26000 / 2500, about 45% sag;
  the SuperMoto's MINI-181 values are 0.22 / 24000 / 2300 and 0.20 / 28000 / 2700), a spring-damper lean, and squat/dive pitch.
  The wheels move with the WheelCollider suspension (`TmaxWheelVisuals`), and the grips follow the steering pivot.
- **But all of that moves only `VisualLeanRoot` (body, wheels, fork, grips).** In the prefab, `Seat`, `PillionSeat`, the foot targets
  and the pillion grab/foot targets are direct children of `TMAX_560`, siblings of `VisualLeanRoot`. So the scooter leans up to
  about 19.5 degrees and squats/dives while both riders stay upright. The hands reach for leaned grips and the feet for unleaned pegs.
  `BikeInteractable` compensated by copying 52% of the lean onto each rider, capped at 9 degrees (MINI-131).
- The SuperMoto (the "other bike") is a real physics bike whose whole rigidbody leans, so its riders lean 1:1 with it automatically.

## Change

1. `TmaxRideDynamics.AttachRiderAnchors` (called from the controller's `Awake`) re-parents those 8 anchors under `VisualLeanRoot`
   at runtime, keeping their world pose. At rest nothing moves. While riding, the riders lean, squat and dive with the body
   around the tyre contact line (VisualLeanRoot's pivot is at the bike origin on the ground), the same as on the SuperMoto. The
   prefab asset and the editor validators that read it are unchanged. Toggle: `TmaxRideDynamics.ridersLeanWithBody`.
2. `BikeInteractable`: when the anchors ride on the body (`TmaxBikeControllerCustom.RidersLeanWithBody`), the old partial
   lean-copy is set to 0, otherwise the lean would be doubled. Only the deliberate extra hang (`riderExtraLeanDegrees`, prefab 0)
   remains. The pillion copies 35% of that.
3. Rider bounce: `TmaxRideDynamics` measures the bike's vertical acceleration each physics step and runs a spring-damper per
   rider (driver 11 rad/s, zeta 0.55; pillion 8 rad/s, zeta 0.45, 1.25x amplitude, max 4.5 cm), plus a small torso nod.
   `VehicleRider.SetBounce` adds it to the seated keyframe. Bumps and landings are absorbed by the riders' legs with a slight
   lag, instead of the riders being bolted to the frame.
4. Eyes on the road: `VehicleRider.SetLookAhead` uses Animator look-at IK (head 1, body 0.05, clamp 0.55) toward a point 12 m
   ahead at head height. Heads stay level while the bodies lean or wheelie. The pillion looks past the driver's shoulder,
   toward the inside of the turn. `BikeInteractable.riderLookAheadWeight` = 0.7 (0 = off).

Untouched: steering, drive, wheelie control, crash/ejection, suspension values, seat keyframes, hand/foot IK weights, the
SuperMoto (it never calls the new `VehicleRider` methods, so its behaviour is identical).

## Test on the local machine

1. Compile; open `GrandBayProof`; ride the TMAX with the other character as pillion.
2. Corners at 30+ km/h: both riders tilt with the scooter, knees and feet stay on the body/pegs, hands stay on the grips,
   and heads stay level.
3. Hard braking / throttle: riders dip forward / sit back with the body (squat/dive).
4. Kerb drop, rough ground (the off-road between Lalay and Highland): riders sink a few centimetres and recover, with no jitter.
5. Wheelie (Q): the driver's wheelie keyframes still apply; heads keep looking ahead; the pillion stays seated.
6. SuperMoto: unchanged.

If the lean looks too strong, lower the TMAX's `maxVisualLean` (15). To return to the old behaviour, untick
`TmaxRideDynamics.ridersLeanWithBody`, set `riderLookAheadWeight` to 0 and untick `riderBounce`.

## Risks
- Not compiled here (no Unity or C# compiler in the cloud sandbox).
- The authored lean-accent clip (MINI-131) now plays on top of a fully leaned rider. If the torso swings too far in sharp turns,
  reduce that clip's weight, not the body lean.
- `LockBodyToHandlebars` (bodyLockWeight) is unchanged; with the grips and the seat now leaning together, the hand-to-grip
  offset should get smaller, not larger.
