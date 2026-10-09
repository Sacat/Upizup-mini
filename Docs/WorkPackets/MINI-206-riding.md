# MINI-206 Stage 4: why the TMAX feels robotic and glitches off-road, and how to put it on the SuperMoto physics

Cloud analysis of the code in the repo plus the serialized values in `TMAX_560.prefab` (prefab values override the C#
defaults). Nothing here was run in Unity. Line numbers refer to `Assets/UpIzUpMini/Scripts/Vehicles/TmaxBikeControllerCustom.cs`
on branch `cloud/sacat-legs`.

## 1. Diagnosis

The TMAX is a 480 kg rigidbody on two WheelColliders, but 13 assist passes run every physics step
(`FixedUpdate`, lines 529-545). Several of them overwrite the rigidbody's rotation or velocity directly, so physics and
input never get to produce motion on their own. Ranked by how much they hurt:

| # | Behaviour | Where | Prefab value | Effect |
|---|---|---|---|---|
| 1 | **Extra air gravity**, ForceMode.Acceleration | `ApplyExtraAirGravity`, line 888 | `extraAirGravity 2500`, `airborneGraceSeconds 0.01`, `airGravityRampSeconds 0` | 2500 m/s² (about 255 g) once both wheels have been off the ground for 0.01 s, i.e. one physics step. A crest, kerb drop or bump that unloads both WheelColliders adds about 50 m/s of downward speed in a single 0.02 s step. The bike slams into the ground, the springs fire it back up, it unloads again, and so on. **This is the main off-road glitch.** |
| 2 | **Kinematic yaw lock** | `ApplyYawLock`, lines 645-672 (`MoveRotation` at 668) | `yawLockStrength 1` | Heading = steering input × 110°/s, and nothing else can turn the bike. There's no slip, drift, counter-steer or weight; it turns like a cursor. **This is the main "robotic" feel.** |
| 3 | **Yaw spin damping with a 0°/s threshold** | `ApplyYawSpinAssist`, line 596 | `yawSpinThreshold 0`, `yawSpinDamping 250` | Removes almost all yaw rate every step on top of #2, so even the yaw the lock leaves is erased. |
| 4 | **Two upright systems, world-up relative** | `ApplyStability` (torque, line 1197) + `ApplyUprightAssist` (`MoveRotation` toward `LookRotation(fwd, Vector3.up)`, line 1508; then zeros roll angular velocity, line 1514) | `uprightStrength 11`, `uprightAssist 16` (32 below 12 km/h) | The physics body never rolls; the lean is only cosmetic (`VisualLeanRoot`). On a side slope (camber, off-road) "upright" means world-vertical rather than ground-normal, so one wheel unloads and the bike chatters sideways. The slerp factor is 0.32 per step, so it snaps rather than settles. |
| 5 | **Trike stabiliser springs** | `ApplyTrikeStabilizers` / `ApplyStabilizerSpring`, lines 953-987 | 6000 N/m, 400 damping, active once the front is airborne for 0.22 s | Off-road the front leaves the ground often, so two invisible outriggers start pushing the rear up at points beside the wheel: random sideways jolts and hops. |
| 6 | **Launch cap** | `ApplyLaunchCap`, line 572 | `maxUpwardLaunchSpeed 6` | Harmless alone, but with #1 it makes the vertical motion asymmetric (down is boosted, up is capped), which reads as "sticky". |
| 7 | **Hill-climb push** | `ApplyHillClimbAssist`, called at line 1024 | `hillClimbAssist 9` m/s² | A 9 m/s² push uphill whenever the terrain ahead is steep; on bumpy ground it switches on and off and surges. |
| 8 | **Kinematic wheelie** | `ApplyWheelie`, `MoveRotation`/`MovePosition` at lines 1367-1368 | | The wheelie is placed rather than balanced, which is why the earlier ejection workarounds (MINI-135/138) were needed. |
| 9 | **Suspension** | WheelColliders in the prefab: spring 23544, damper 7607 (ζ ≈ 1.6) | `TmaxRideDynamics` resets this at Awake to 23500/2200 front, 26000/2500 rear | Fine after MINI-193, but only cosmetic in effect: #1 and #4 dominate the body motion. |

Summary: the SuperMoto is a free physics body with a few gentle assists, while the TMAX is a kinematic object pretending to be
one. That is the difference the owner feels.

## 2. Two ways forward

### A. Quick: make the current TMAX less robotic (inspector values only, about 10 minutes, reversible)
On a copy of `TMAX_560.prefab`, or the prefab itself after a backup:

| Field | Now | Try |
|---|---|---|
| extraAirGravity | 2500 | **15** |
| airborneGraceSeconds | 0.01 | **0.3** |
| airGravityRampSeconds | 0 | **0.4** |
| yawLockStrength | 1 | **0.15** (0 = fully physical) |
| yawSpinThreshold | 0 | **70** (deg/s; only real spins are damped) |
| uprightAssist | 16 | **5** |
| uprightAssistLowSpeedBoost | 2 | **1.5** |
| trikeStabilizerDebounceSeconds | 0.22 | **0.6** |
| hillClimbAssist | 9 | **3** |
| yawAssist | 2 | **3** (gives back some turn-in once the yaw lock is weaker) |

Test: Lalay straight, the off-road area between Lalay and Highland, a kerb drop, a full-lock U-turn at 20 km/h, and a wheelie.
If a hedge hit still spins it too much, raise `yawLockStrength` in steps of 0.1, not back to 1.

### B. Recommended: run the TMAX on the SuperMoto physics, keep the TMAX look
`TMAX_560_SuperMoto.prefab` already exists. It is the Gadd420 Motorbike Physics Tool rig (`RB_Controller`, `AutoLeveling`,
`GroundAngle`, `PlayerLeaning`, rider `IK`, `CrashController`) plus the project's SuperMoto layer (`SuperMotoSuspensionTuning`
24000/2300 and 28000/2700, `SuperMotoWheelieAssist` balance-point wheelie, `SuperMotoUprightAssist`,
`SuperMotoKinematicRider`, pillion). Lean is produced by roll torque on the rigidbody (`RB_Controller.AddTorque`, lines 465-480,
`AddRelativeTorque(-forward * input * leanTorque)` limited by `maxLeanAngle`), and `AutoLeveling` levels against the
`GroundAngle` surface direction rather than world up. Note: `RB_Controller` also sets `RigidbodyConstraints.FreezeRotationZ`
(lines 153/218). How that interacts with the roll torque is not something I can confirm without running Unity, but MINI-170
measured the SuperMoto's real roll reaching 87.7°, so the body does roll physically in play. Its yaw is never locked.

Steps for the local machine (nothing here edits the SuperMoto itself):
1. Duplicate `TMAX_560_SuperMoto.prefab` as `TMAX_560_SM.prefab`. Work only on the duplicate.
2. Drop the MINI-206 Stage 1 FBX (`TMAX_Body`, `TMAX_FrontForkAssembly`, wheels, …) under the duplicate's root, origin on the
   ground between the tyres, facing +Z.
3. Move the two WheelColliders onto `FrontAxle` / `RearAxle` from the FBX, radius 0.29 m (15-inch rim + 120/70 and 160/60
   tyres). Move `Collider_GRP` boxes to cover the TMAX body. Move `COG` to about 0.45 m up, 0.05 m behind mid-wheelbase.
   Mass 220 kg (real TMAX wet weight with a rider is about 300 kg; the SuperMoto tuning assumes a light bike, so start near
   its own mass and only raise it if the bike feels floaty).
4. Add `Draft/TmaxSuperMotoVisualBinder.cs` (enable the `MINI206_DRAFT` define), assign the FBX instance. It hides the
   SuperMoto body meshes, hangs the TMAX wheels on the vendor wheel holders, and steers the fork around the real 26° raked
   axis. It warns in the console if a WheelCollider is more than 2 cm from the TMAX axle.
5. Seats: the vendor rider and `SuperMotoKinematicRider` use `HipPos`, `FeetPos/*`, `HeadFollowPos`. Move these onto the FBX
   empties (`Seat_Driver`, `FootPeg_L/R`). Pillion: the SuperMoto pillion path (MINI-119) uses its own seat object; place it
   on `Seat_Pillion`, `PillionPeg_L/R` and `PillionHandle_L/R`.
6. Spawn the duplicate with the existing SuperMoto wiring (`VehicleSpawnController` / `Mini119SuperMotoBikeSetup` path, which
   attaches `SuperMotoVehicleInteractable`, `GaddInputAdapter`, the assists and the wheelie key remap). Ride it next to the
   current TMAX and compare.
7. If accepted, point the TMAX dealer/spawn entry at `TMAX_560_SM.prefab`. Keep `TMAX_560.prefab` for rollback.

What to expect: the bike and both riders lean physically, the wheelie is a balance-point wheelie, and bumps go through real
springs with no air-gravity slam. The SuperMoto's animations and the bike itself are not touched; this is a second prefab
using the same scripts.

Risks: the vendor rider IK is tuned for the SuperMoto's riding position. The TMAX seat is lower and the footboards are
forward, so expect a round of seat/peg empty adjustments (step 5). That's a visual tuning pass, not a physics change.

## 3. Draft code (new files, not compiled by me)
- `Assets/UpIzUpMini/Scripts/Vehicles/Draft/TmaxSuperMotoVisualBinder.cs`, wrapped in `#if MINI206_DRAFT` so it cannot break a
  build until you opt in. Steps 2-4 above.

The quick path A needs no code. A ground-normal-relative upright assist would be the next code change for the current TMAX
if path B is not taken. That is a small edit to `ApplyUprightAssist` (use the averaged WheelCollider ground hit normal instead of
`Vector3.up` on line 1497), left for the local session because existing scripts are out of scope for this lane.
