# MINI-181: SuperMoto crash-proof wheelie, real balance-point wheelie, real-bike suspension

Date: 2026-09-27. Owner: Claude (released). System ledger: `Docs/Systems/Vehicles.md`.

## Request (user, verbatim)
"the rider crashes extremely too easy especially in wheelieing. can you fix it and make it real hard to crash while wheeling. also make the bikes move more like a real bike with my physics springs etc. even how the character wheelies the bike"
Also: "can you also document so chatgpt can know exactly how you did it just incase for the next handoff and make a backup just in case"

## Acceptance criteria
1. Holding/tapping the wheelie key (Q) for 8 s on the real Lalay spawn never sets `RB_Controller.isCrashed` and never triggers auto-recover.
2. Wheelie settles at a balance point (~35 deg) instead of ramping to ~89 deg; hard cap 55 deg so it cannot loop.
3. True roll stays ~0 during the wheelie; the rear wheel stays grounded.
4. Suspension visibly softer and more realistic (travel, sag, rebound).
5. Rider pose reacts to the wheelie's motion, not only its angle.
6. Standing regression `Mini134CrashDamageRecoveryValidation` still passes; Windows build succeeds.

## Backups (made BEFORE any change)
- Git tag `backup/pre-mini181-bike-physics-2026-09-27` (at HEAD; note HEAD does not contain the uncommitted MINI-180 scene work).
- Git ref `refs/backup/mini181-uncommitted` -> stash object `89e1c4fad88f86e7af4c7541c9d8e5ebb91dab14`: the WHOLE uncommitted working tree at that moment (restore any file with `git checkout refs/backup/mini181-uncommitted -- <path>`, or inspect with `git stash show -p 89e1c4f`).
- File copy: `Backups/MINI-181-pre-bike-physics-2026-09-27/` (all of `Assets/UpIzUpMini/Scripts/Vehicles` + `Assets/MotorbikePhysicsTool/Scripts`).
- Rollback = copy the four edited scripts back from that folder and delete `SuperMotoSuspensionTuning.cs` + the one `AddComponent<SuperMotoSuspensionTuning>()` line in `VehicleSpawnController.cs`.

## Root causes found (measured, not guessed)
Collision ejection (`BikeCrashEjectionController.ShouldEjectForImpact`) was already `return false` since MINI-167, so the crashes came from elsewhere:

1. **Pitch sign flip (the main wheelie "crash").** `SuperMotoWheelieAssist.MeasurePitch()` and `SuperMotoUprightAssist.TruePitchDeg()` (both from the MINI-170 "port verbatim") returned `SignedAngle(flat, forward, up x flat)`, which is NEGATIVE for nose-up. Every caller builds its target as `Euler(-pitch)` (nose-up positive). Result: a 35 deg wheelie measured as ~70 deg of "roll", `ApplyAutoRecover()` (limit 60 deg, 0.35 s) fired mid-wheelie: velocity zeroed, bike teleported +1 m, rider ragdoll reset. That is what the user saw as crashing. The same flip made the upright assist steer the nose toward the mirrored pitch on slopes. Fix: negate the return value in both.
2. **Two scripts MoveRotation-ing the bike in the same FixedUpdate.** `SuperMotoUprightAssist` (MINI-170 made it run during wheelies) rebuilt rotation from the PREVIOUS step's pitch and, when it ran after the wheelie assist, erased the wheelie pitch. The wheelie still `MovePosition`ed around the rear contact, so the bike floated metres up, gathered falling speed, then stopped dead on touchdown (a deceleration crash). Fix: the upright assist skips `MoveRotation` while a wheelie is up (the wheelie builds yaw*pitch with zero roll anyway); its roll-spin damping still runs.
3. **Vendor `TriggerToCollider` on 5 ragdoll bones** set `isCrashed` whenever a bone trigger touched anything not tagged Player (road, kerb, own tail). Disabling does not stop `OnTriggerEnter`, so `SuperMotoWheelieAssist.Awake` now destroys them.
4. **Deceleration crash during wheelies.** `CrashController.decelerationSpeedForCrash` is now 9999 while the wheelie is up (and its speed samples reset), 32 m/s-per-0.05 s otherwise (was 20).
5. **`VehicleDamageController`** wore the bike down to a forced ejection from ground contacts. Now ignores contacts with normal.y > 0.55 and anything while a wheelie is active. Walls/cars still damage.

## Realism changes
- **Balance-point wheelie** (`SuperMotoWheelieAssist.ApplyWheelie`): the front angle is a spring-damper toward a target instead of a linear ramp. Q held -> `balancePointDeg` 36 (full throttle lerps 60% of the way to `rampCeilingDeg` 55); released -> 0. Lift k=20, zeta=0.42 (pops up with a small natural wobble); drop k=11, zeta=0.85 (clean set-down). Hard cap 55. Below half the start speed the front comes down by itself. A fast set-down gives the front wheel a downward impulse (`landingThump` 0.9 x weight) so the fork compresses like a real landing. New public `PitchVelocityDegPerSec`.
- **Suspension** (new `SuperMotoSuspensionTuning`, added in `WireSuperMotoInstance` right after the wheelie assist): front travel 0.22 m, spring 24000, damper 2300; rear travel 0.20 m, spring 28000, damper 2700; target (sag) 0.4. Vendor was 30000/2500 at target 0.1. Bike mass 400 kg -> damping ratio ~0.5, ~35-40% static sag.
- **Rider** (`SuperMotoRiderWheelieLink`): wheelie pose blend = angle/ceiling + pitchVelocity/400 while up, so the rider throws weight back as the front rises and eases forward as it drops.

## Files changed
- `Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoWheelieAssist.cs`
- `Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoUprightAssist.cs`
- `Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoRiderWheelieLink.cs`
- `Assets/UpIzUpMini/Scripts/Vehicles/VehicleDamageController.cs`
- `Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs` (one AddComponent line)
- NEW `Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoSuspensionTuning.cs`
- `Assets/UpIzUpMini/Editor/Mini119RealSceneWheelieTest.cs` (harness fixes, below)
No scene, prefab, vendor script or project setting was modified. The TMAX is untouched (it uses `TmaxBikeControllerCustom`).

## Test harness fixes (`Mini119RealSceneWheelieTest`)
Parked bikes now have `RB_Controller`, the key remap, the wheelie assist and anytime-reset DISABLED until mounted (`VehicleSpawnController.SetBikeInputEnabled`). The test never mounted, so it silently ran with none of them (bike crawled at 1 km/h, wheelie never started). It now calls `SetBikeInputEnabled(instance, true)` before the reflected `Start()` calls, also invokes `SuperMotoSuspensionTuning.Awake`, records `everCrashed`/max wheelie/remaining TriggerToCollider count (`MINI-181 CRASH CHECK` line) and prints a 0.1 s `MINI181TRACE` for the first 4 s (height, wheelie angle, measured pitch, true roll, wheel grounding, speed).

## How to verify (commands)
```
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -batchmode -quit -projectPath "E:/Unity/Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini119RealSceneWheelieTest.Run -logFile Logs/mini181-wheelie.log
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -batchmode -quit -projectPath "E:/Unity/Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini134CrashDamageRecoveryValidation.Validate -logFile Logs/mini181-crashreg.log
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -batchmode -quit -projectPath "E:/Unity/Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer -logFile Logs/mini181-build.log
```

## Evidence
- Before the pitch-sign fix: trace shows the wheelie reaching 52 deg, then the ramp snapping to 0, speed 16 -> 3 km/h and height +1 m at t=1.2 s (auto-recover). Before the MoveRotation fix: height climbed 8.7 -> 21 m with fwdY ~0 and vertical speed -31 m/s before touchdown.
- After: `Logs/mini181-wheelie.log`. Pops to ~52 deg, settles at 33-38 deg for the rest of the 8 s, true roll 0.0, rear wheel grounded throughout, speed builds 8 -> 48 km/h, `everCrashed=False`, `TriggerToCollider` left = 0.
- `Logs/mini181-crashreg.log`: `[MINI-138] PASS`.
- `Logs/mini181-build.log`: build succeeded, 441,655,061 bytes, `Builds/GrandBayProof/UpIzUpMini.exe`.

## Open / next
- Hands-on ride is the real gate: wheelie feel, suspension on the expansion roads and speed bumps, landings.
- Tuning knobs are public fields on `SuperMotoWheelieAssist` (balancePointDeg, liftStiffness/liftDamping, dropStiffness/dropDamping, landingThump, rampCeilingDeg) and `SuperMotoSuspensionTuning`.
- The batch harness runs in edit mode with reflected lifecycle calls; the rider ragdoll/IK visuals are not exercised by it.
- Not committed together with the uncommitted MINI-180 scene promotion; see handoff note.
