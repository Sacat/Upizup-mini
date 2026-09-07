# Up Iz Up Mini — Changelog
## 2026-09-07 — Whole-watch wrist rotation clarification

- Turn both complete watch attachments90 degrees around the forearm, preserving size/axial placement; back up profiles and capture side views. Unity proof passes; gameplay/EXE unchanged.

## 2026-09-07 — Watch face orientation revision

- Turn round case/dial/crown90 degrees clockwise on both preview characters; bracelet, wrist fits and triangle budgets unchanged. Unity proof passes; no gameplay/EXE changes.

## 2026-09-07 — MINI-145 round mobile watch proof

- Original round gold-watch meshes with1644/788-triangle LODs, one shared atlas material and separate cuff fit profiles.
- Isolated Unity previews on actual Sacat/Franki; 36 idle/walk/run attachment samples each pass, original gameplay scene unchanged.
- Watch fit awaits user approval; no gameplay, purchase, chain or EXE changes.

## 2026-09-07 — MINI-144 isolated wardrobe audit and watch concept

- Resume user-approved wardrobe production; original Blender gold-watch preview and reusable read-only rig audit added outside Assets.
- Document individual slots/colours, safehouse preview/apply/cancel and preservation of purchases and approved chain.
- Mobile Unity deformation remains unresolved; preview needs optimization and wrist-fit approval. No gameplay or EXE changes, no external spend.

## 2026-09-07 — MINI-143 combat contact and gang activation

- Block walking/turning/jumping through kicks; align hit windows to sampled existing clips and fade duration.
- Retry player contact during active strike window, applying damage once; preserve damage/Franki strength and wall/range checks.
- Initialize pooled NPC original scale before first respawn reset; four Dog Life members now visible at unchanged block. Stop patrol motion during ragdoll/disabled controller.
- Focused/shared-impact checks, Windows build and12s startup pass (0 exceptions/inactive-controller warnings). Static screenshot recorded; live combat feel awaits playtest.
## 2026-09-06 — MINI-142 approved environment and crop art

- Applied86 detailed generic houses with LODs, sparse batched grass, new carrot/banana and baked green/purple bud visuals across14 plots; IDs and growth logic preserved.
- Patched measured Dog Life road overlap in place;219 collider samples pass. Preserved281 gameplay transforms, special properties and immutable original-scene backup.
- Unity renders inspected/shown, user requested continuation/build. Windows build passed; live playtest and phone profiling remain. See Docs/WorkPackets/MINI-142.md.
## 2026-09-05 — MINI-141 Blender art previews

- Added a repeatable standalone Blender generator and inspected house/grass and carrot/banana/cannabis preview renders. Awaiting user approval; gameplay unchanged.
- Recorded geometry budgets and required integration/LOD/material/collision work; no Hitem3D credits spent.
- Recorded the suspected legacy-road versus MB-proof mismatch near Dog Life. Exact gap and repair remain unverified in Unity.

## 2026-09-04 — MINI-140 E-to-mount / Q-to-wheelie control remap

- Vehicle mount / enter / dismount is now E (the world interact key) for the TMAX, SuperMoto and Range Rover, routed through the normal interaction path while each vehicle keeps its own wider mount reach.
- The bike wheelie is now Q (TMAX `wheelieKey`; `SuperMotoWheelieKeyRemap` reads Q only, E removed). F is the melee Attack key only and is no longer read by any vehicle.
- The phone's Q call-partner action is suppressed while the active character is mounted; on foot it is unchanged.
- The E press that mounts a vehicle is guarded so it cannot also dismount the same frame.
- Updated the serialized key codes in `TMAX_560.prefab` and `RangeRover_Vehicle.prefab` — input bindings only, no mesh/transform/seat/effect/placement change.
- Focused MINI-140 plus standing MINI-065 / MINI-139 / MINI-137 validations pass; Windows build succeeds and a 15-second startup smoke stayed alive with no exceptions. Live control feel approval remains.
## 2026-09-01 — MINI-139 TMAX 12 mph wheelie sustain floor

- Set an effective 12 mph (19.31 km/h) minimum for both starting and sustaining TMAX wheelies.
- Routed below-threshold wheelies into the existing smooth zero-degree recovery target rather than snapping the bike down.
- Preserved rear-contact-chatter tolerance, the 89-degree cap, crash filtering, handling, animation, effects, wheels and placement.
- Focused MINI-139 and standing MINI-138 validations pass; Windows build and 12-second startup smoke pass. Live feel approval remains.
## 2026-08-31 — MINI-138 wheelie crash sensitivity correction

- Distinguished forced-wheelie rotational contact velocity from real bike travel speed.
- During an active wheelie, rider ejection now requires a 15% harder impact plus at least 16.5 m/s of actual Rigidbody speed.
- Preserved normal upright crashes, genuine high-speed wheelie wall crashes, NPC impact handling, the 89-degree cap, animation, effects and handling.
- Focused regression and Windows build pass; the player remained alive through the 12-second startup smoke. Live feel approval remains.
## 2026-08-31 — MINI-137 TMAX wheelie effects and SuperMoto lift pose

- Reduced TMAX exhaust frequency, puff size, maximum opacity and particle budget without moving the approved right-rear outlet.
- Added a mobile-bounded rear-underside spark effect gated to a ridden TMAX at the 89-degree cap and at least 12 mph.
- Applied the in-game SuperMoto's proven 0.22 authored lift blend to TMAX riders only while mounted and restored their previous setting on dismount.
- Preserved handling, wheels, seats, IK, pillion, crash behavior, scene and prefab placement.
- Focused MINI-137 and standing MINI-136 validations pass; Windows build succeeds and the player remained alive through the startup smoke. Live visual/motion approval remains.


## 2026-08-31 — MINI-136 89-degree wheelie cap and collision-only crash

- Added one shared 89-degree commanded-wheelie ceiling and applied it to both TMAX and SuperMoto, including runtime clamping of serialized/tuner values.
- Removed the angle/tilt polling, backward kick and over-angle ejection path from bike crash detection.
- Preserved MINI-135's filtered hard-collision rider-ejection path and all approved handling, wheels, rider fit and scene placement.
- Focused MINI-136 and standing MINI-132 validations pass; Windows player rebuilt and remained running through the 12-second startup smoke. Live motion approval remains.

## 2026-08-31 — MINI-135 crash threshold and seller impact correction

- Replaced raw collision-magnitude bike ejection with contact-normal closing speed, ignored upward road/ground contacts, and raised hard-obstacle/NPC ejection thresholds.
- Set 90 degrees as the wheelie balance point, preserved rider attachment through 95 degrees, and added a backward angular kick before driver/pillion ejection beyond 95.
- Added idempotent runtime CharacterControllers to stationary town NPCs so sellers receive vehicle collision/trigger hits and retain protected ragdoll recovery/return-to-post behavior.
- Focused MINI-135 and standing MINI-132 validations pass; Windows player rebuilt and final startup smoke passed. Live feel approval remains.


## 2026-08-30 — MINI-132 shared vehicle-impact and combat foundation

- Made Franki's whole combo 25% stronger with 30% heavier impact, rather than strengthening only his final kick.
- Added distinct slower/weaker three-move Mixamo combinations for police and gang fighters.
- Added one speed/contact-driven collision adapter to the TMAX, SuperMoto and cars; all hits route through `NpcCombatHealth` and the existing humanoid ragdoll.
- Added protected seller/named-NPC recovery: get up where they fell, restore health, then walk back to the exact original post. Ambient police/villagers fade and return later; Dog Life keeps its external pool.
- Focused validation and Unity compile pass; Windows player rebuilt and short smoke run contained no exception. Live motion approval and rider-ejection extension remain.

## 2026-08-30 — MINI-131 TMAX wheel lock and restrained turn lean

- Replaced cumulative TMAX wheel positioning with cached local hub rest points reapplied in a final late frame pass; forced-displacement validation stays stable over 250 cycles.
- Changed the held turn animation into a short 0.38-second accent with a stronger entry threshold and faster return to neutral.
- Capped rider cosmetic-roll following at 52% and procedural roll at 9 degrees without changing the bike's physics, approved seat offsets, IK or wheelie.
- Moved the bounded exhaust outlet slightly lower and farther to the right-rear.
- Focused and standing TMAX validations pass; rebuilt Windows player starts and remains running for the smoke window without an exception.

## 2026-08-30 — MINI-129/130 rider fit and Dog Life respawn

- Advanced only the TMAX driver by 0.12m in both seated and wheelie poses, preserving approved height, side placement and pitch and leaving the pillion unchanged.
- Fixed pooled Dog Life reactivation by refreshing `NpcCombatHealth` references during respawn and safely handling renderer objects replaced while the NPC was inactive.
- Extended the Dog Life validator to reproduce the real stale/null renderer-cache failure before reactivation.
- TMAX and Dog Life focused validation pass; rebuilt Windows player succeeds and a 12-second smoke run has no Dog Life respawn NullReference exception.

## 2026-08-30 — MINI-128 TMAX parking, effects and pillion alignment

- Locked the empty test TMAX at the exact Farm Shop/Produce Buyer road midpoint until the player mounts it and added a location alert tied to the existing orange minimap marker.
- Added bounded right-rear exhaust puffs plus SuperMoto-reference tyre smoke and black tread marks that activate only under rear-wheel braking or measured slip.
- Preserved the pillion's approved height/offset while changing its seat anchor to a neutral forward rotation and its ride pose to the stable SuperMoto-style `RideBike` pose.
- Focused validation, standing TMAX validation and the Windows build pass. Live player inspection confirmed movement, visible smoke/marks and two forward-facing riders; the unrelated pre-existing gang-respawn exception remains.

## 2026-08-30 — MINI-127 purchasable TMAX control repair

- Confirmed the spawned and Car Dealer TMAX are the same prefab; no alternative shop-only TMAX existed.
- Reconnected mount/riding input to the prefab's fully wired `TmaxBikeControllerCustom` and disabled the incomplete vendor controller stack for every spawned or purchased TMAX.
- Increased the effective F-mount radius to 4.25m and hid the saved overlapping SuperMoto comparison bike during the temporary TMAX test.
- Focused validation and Windows build pass. An 18-second standalone smoke run logs the repair with no vendor steering/nitrous exception; the unrelated pre-existing gang-respawn exception remains.

## 2026-08-30 — MINI-126 TMAX market-road placement and Windows rebuild

- Moved the temporary TMAX test spawn to the road midpoint between the Farm Shop and Produce Buyer stalls.
- Aligned the bike along Lalay using the perpendicular of the stall-to-stall direction; an existing TMAX is moved to the same test position instead of duplicated.
- Rebuilt `Builds/GrandBayProof/UpIzUpMini.exe`; fresh scene/code payload confirmed and the standalone remained running for a 15-second smoke check.
- The spawn itself logged successfully. Existing non-fatal gang-respawn and vendor nitrous exceptions remain visible in the smoke log and were not expanded into this bounded build task.

## 2026-08-30 — MINI-125 TMAX test spawn

- Added a temporary TMAX-only runtime spawn beside the active character for the MINI-124 wheel/steering playtest.
- Kept Sacat/Franki on foot with the normal walk-up/F mount flow; no Range Rover, SuperMoto, auto-mount, ownership grant, or save-data change.
- Added duplicate protection and the existing vehicle minimap marker.
- Unity compile and the standing MINI-064/065 TMAX prefab/scene validator pass; real riding and appearance still need user confirmation.

## 2026-08-30 — MINI-124 TMAX black spinning wheels and steering

- Replaced the TMAX's invisible thin wheel-disc illusion with fitted black SuperMoto tyre meshes while retaining its original WheelColliders and all physics values.
- Added a visual fork/handlebar steering pivot driven by the front WheelCollider steer angle; preserved the established root-level hand-target paths and made them follow the pivot at runtime.
- Added a repeatable upgrade/validation/evidence tool plus a recoverable pre-change prefab copy under `Logs/Tasks/MINI-124`.
- Focused validation, original MINI-064/065 structural validation and the real-physics drop/drive/wheelie regression pass. Static screenshots and a 2-second articulation proof are ready for user review.
- Known limitation: the old wheels/bars are fused into the scan's main mesh, so the new moving geometry overlays them until a later Blender separation pass.

## 2026-08-29 — MINI-123 complete remaining-road MB proof

- Extended the isolated MB Road System proof from Highland to every remaining phase-one road without touching `GrandBayProof.unity`.
- Converted eight mapped routes plus the measured `way/387239000`–`way/23042701` connector: nine active MB roads, 815 road-mesh vertices and nine disabled rollback roads.
- Retained line-free 6.2m paved roads, a 4.8m dirt Highland farm spur and zero generated non-Lalay sidewalks.
- Rebuilt both Highland–Lalay bridge joins as terrain-aware, collidable transitions sampled only from authoritative road/terrain surfaces; added two-direction driving-height evidence and deck-overlap validation.
- Unity focused validation and the `approved_graybox` map-district gate pass. Full-network visual approval and a later vehicle test still gate live migration.

## 2026-08-22 — MINI-119 follow-up 5 (bike bump/debounce, steep-ledge classification)

- User: "the wheelieing bumps a bit high randomly on the road... i think its the back collider." Confirmed - it's the invisible rear trike-stabilizer spring (a documented MINI-080 bug class: it stacks a second spring on top of the WheelCollider suspension if the front wheel loses contact for a "sustained" period, debounced at 0.12s). The softer suspension from the previous round settles more slowly by design, so an ordinary bump can now legitimately keep the front airborne past that old debounce without anything being wrong - firing the stabilizer on plain road bumps. Raised the debounce to 0.22s.
- User: "the bike also behaves wierd by steep ledges now but the smaller ledges it does very good." Root cause: the ledge/solid-obstacle height classification cut off at 0.6m - a steep ledge's contact point could land above that and get reclassified as a "solid obstacle" (walls/vehicles/NPCs), a branch that only ever capped yaw, nothing for roll/pitch at all. Raised the classification height to 1.2m (still well under any real wall/vehicle) so steep-but-legitimate ledges keep the full ledge treatment, and added a softer roll/pitch safety cap to the solid-obstacle branch itself for whatever's left above that.
- Full vehicle+scene rebuild pipeline, all standing validators, and the MINI-065 drop test all re-run clean. Windows build succeeded.

## 2026-08-22 — MINI-119 follow-up 4 (suspension root-cause fixes for both vehicles)

- User asked to try a third-party "motocross bike" physics asset; declined per policy (no downloading/installing untrusted packages, no exceptions) and instead researched real Unity WheelCollider best practice, which turned up two concrete, well-documented bugs in our own setup:
  1. **`forceAppPointDistance` was never set on the bike (defaulted to 0) and was explicitly set to `0f` on the Range Rover.** Unity's own scripting reference and community documentation call this out directly as a real, common cause of WheelCollider jitter/instability on rough terrain - the suspension force applies at the collider's own local origin instead of below the Rigidbody's real centre of mass. Fixed on both vehicles (`0.3f`, the commonly-documented starting point).
  2. The bike's suspension had been deliberately tuned very stiff earlier this session (short travel, 3.2x critical damping) specifically to stop an unrelated bounce/launch problem - but that made it barely compress at all on a sharp hit, transmitting most of a sidewalk/ledge impact straight into the chassis. Safe to soften now: `ApplyLaunchCap`/`extraAirGravity` (added since that earlier fix) are independent systems for "don't bounce too high," so the suspension no longer has to fight that battle alone. Travel raised 0.17->0.20m, damping ratio lowered 3.2x->1.6x critical (still no oscillation, far less rigid) - a real step toward off-road-style compliance without changing the visual model's wheel-arch geometry.
- Full vehicle+scene rebuild pipeline, all standing validators, and the MINI-065 drop test all re-run clean (drop test roll numbers improved slightly, not regressed). Windows build succeeded.

## 2026-08-22 — MINI-119 follow-up 3 (smoothed impact corrections, ramp jump height)

- Bike/car "movements are not so smooth when it hits a sidewalk/ledge": found the cause - the yaw-lock re-snap, the ledge-reaction suppression, the launch cap, and the car's collision-spin cap were all instant, single-physics-step hard corrections (Slerp/Lerp with t=1, or a raw `Mathf.Clamp`). Technically correct end state, but reads as an abrupt jerk. Eased all four to settle over a handful of steps (~0.05-0.1s) via exponential smoothing instead of one, at rates still fast enough that none of the underlying fixes (no turning, no flipping, no launch) are weakened.
- Range Rover: "it should go off the ground a little more after hitting a ramp" - `maxUpwardLaunchSpeed` raised 6->10 m/s; the self-righting/collision-spin-cap systems (not this number) are what actually keep a hard hit from reading as abnormal, so this can afford to be generous again.
- Considered switching to a third-party Unity vehicle physics asset per the user's own question; recommended against it (see chat) - kept refining the existing custom controllers instead.
- Full vehicle+scene rebuild pipeline, all 8 standing validators, and the MINI-065 drop test all re-run clean. Windows build succeeded.

## 2026-08-22 — MINI-119 follow-up 2 (vehicle anti-flip, launch cap, camera smoothing)

- Researched Unity's own documented vehicle-physics practice (rollover prevention via `Rigidbody.maxAngularVelocity`, stabilizer/anti-roll bars, in-flight self-righting; camera jitter fixes via smoothing rotation the same way position already was) before implementing - not a guess, and no third-party asset was imported (would require downloading an untrusted package; the researched techniques were applied to our own existing controllers instead).
- Range Rover (`CarController`) had NO anti-flip system at all previously - only downforce and a low centre of mass, neither of which stops a genuine hard-collision angular impulse. Added: `Rigidbody.maxAngularVelocity` capped (was Unity's default 7 rad/s); a launch cap that clamps upward vertical velocity directly at the source; a self-righting assist (stronger while airborne) that pulls the car back toward level roll/pitch; and a collision-triggered hard clamp on roll/pitch spin specifically (yaw/spin-outs from a side hit are left alone - that's expected, not the reported "flipping").
- Bike: added the same launch-cap fix (`ApplyLaunchCap`) for "front/back would go up fast then come back down quickly" - a straight linear vertical velocity spike from a hard hit, distinct from every rotation-based fix already in place, capped directly rather than only reacted to afterward. Also lowered `Rigidbody.maxAngularVelocity`.
- Camera: rotation was previously a raw, unsmoothed `LookAt` snap every frame (only position was smoothed) - now smoothed the same way, reducing perceived jitter during any hard physics moment independent of the underlying physics fix.
- Full vehicle+scene rebuild pipeline, all 8 standing validators, and the MINI-065 drop test all re-run clean. Windows build succeeded.

## 2026-08-22 — MINI-119 follow-up (bike final tuning + road/terrain smoothing)

- Bike anti-spin: went through several rounds with the user hands-on (a gradual yaw damper, then a collision-triggered hard cap, then a ledge/sidewalk-height-classified full suppression) before landing on the actual fix - yaw stopped being physics-derived entirely and became a direct, kinematic function of the player's own steering input (`ApplyYawLock`), the same proven technique already used for the wheelie's pitch. Final user-tuned values baked into `TmaxBikeController`'s defaults and the prefab's explicit-write list: `yawLockStrength=1`, `yawSpinThreshold=0`, `yawSpinDamping=250`, `extraAirGravity=2500`, `airborneGraceSeconds=0.01`, `airGravityRampSeconds=0`.
- Live in-game tuning: `TmaxWheelieTuner`'s dev panel was temporarily shipped on the real, purchasable TMAX (`IncludeDevTuner`) with wide-range sliders for all of the above, plus a "print all bike physics values" button so the user's final numbers could be read exactly rather than guessed. Flipped back off once tuning finished. `VehicleSpawnController` also gained a temporary dev auto-spawn (both vehicles next to the player at scene start) so testing didn't require reaching/affording the dealer.
- User made manual placement edits directly in `GrandBayProof.unity` (farm privacy hedge scale/position/rotation, farm plots off the road, farm safehouse off the road) and asked for them to be preserved permanently rather than redone after every future rebuild. Captured read-only via a new `Mini119ReadManualEdits` tool and replayed as a permanent override in `Mini100GrandBayMapMigration.RestoreManualHighlandFarmEdits()` - the scene builder can be re-run freely now without losing this placement.
- Terrain/road smoothness ("no ledge should be 90 degree steep, i want to drive properly"): added `CreateKerbRamp` ribbons bridging the road-to-sidewalk height step with a real slope instead of bare terrain. A new read-only measurement tool (`Mini119RoadSteepnessScan`) found and fixed a real, previously-undiscovered bug: `Mini095LalayMapLabSetup.BuildRoadGapConnectors`'s "tiny seam" auto-connector had a mathematically self-contradictory threshold check and had never sealed a single road gap, ever, in any prior build. Fixed the logic, sealed one genuine unconnected road gap (`way/387239000` to `way/23042701`), and patched two ribbon-edge junction gaps where roads share an identical centreline point but their rendered edges didn't line up at a sharp angle (`Mini100GrandBayMapMigration.PatchJunctionEdgeGaps`).
- Full vehicle+scene rebuild pipeline, all 8 standing validators (058/065/109/110/111/112/113/119), and the MINI-065 real-physics drop test all re-run clean. Windows build succeeded.

## 2026-08-22 — MINI-119

- Dialogue truncation ("not showing full dialogue" on the boss NPCs): root cause was a background-panel height cap (330px in `MissionHUD.ResizeBanner`), not hold duration - long text rendered past the dark backing with nothing behind it, illegible against the game world except in the middle, exactly the reported symptom. Raised the cap to 620px.
- Boat Man's most common repeat line ("nothing to load") now rotates through 3 lines instead of always the exact same sentence.
- Fixed a villager's stale, pre-migration patrol route (was pointed at the old map's terrain) - `PlaceRoadsideNpc` now resets `PatrolNPC` waypoints after repositioning, generically, for every patrol-enabled NPC it places.
- Rasta now sells Blue Cheese seed directly ($650 for 3 seeds) once his own teaching mission actually unlocks it, with a themed line; never advertised early, never re-offered while already stocked.
- Blue Cheese/Purple Sugar's plant-selection number keys swapped (7=Blue Cheese, 8=Purple Sugar) to match Rasta's real teaching order; added a minimap marker for the breeding station; extended per-crop hints to every Rasta-taught base strain (Black Sugar/Purple/Blue Cheese), not just the three hybrids.
- War Story (M16) now requires a real `DefeatAllRivals` objective (every Dog Life member in the block knocked out) before laying low - previously winnable by just walking in.
- Bike: `motorTorque` raised past MINI-118's mass-parity value (which only preserved the old power-to-weight ratio, not actually stronger); new slope-proportional hill-climb assist (0 on flat ground, full strength on a real incline/ledge under the rear wheel); new yaw-spin assist that damps only the portion of yaw angular velocity above a real-world-tuned threshold, so a hedge/ledge hit still spins - just far less wildly - while ordinary steering-induced yaw is untouched.
- Cheat `000000` now also sets Normy reputation to 100, matching every other faction it already unlocks.
- New focused validator (`Mini119BatchFixValidation`) covers every item above with a real gameplay-state assertion; full vehicle+scene rebuild pipeline, every standing validator (058/065/109/110/111/112/113), and the MINI-065 real-physics drop test all re-run clean. Windows build succeeded.
- Manual road-intersection/farm-hedge adjustment instructions given directly to the user, per their own request to do that part by hand - not implemented as code this round.

## 2026-08-22 — MINI-118

- Bike mass increased ~2.18x (220kg -> 480kg, deliberately above real-world TMAX weight) so ordinary bumps impart a proportionally smaller velocity change and stop launching the bike. The suspension spring/damper are formula-derived from mass, so sag depth and damping ratio stay identical - only stiffness/damping scale up to match.
- The wheelie mechanic (fully kinematic, `MoveRotation`-driven) and the lean/stability corrections (`ForceMode.Acceleration`, mass-independent by Unity's own definition) needed no changes at all - confirmed by code reading, not assumed.
- `motorTorque`/`brakeTorque`/`wheelieRearTorqueBoost` scaled by the same mass ratio, since these ARE real mass-dependent drive/brake forces, so acceleration/braking/wheelie-forward-creep don't feel weaker as an unintended side effect.
- Measured a real, if minor, side effect: real wheelie pitch dropped from an 85.9deg baseline to 77.4deg (still passing the drop test's 76.6deg minimum, but with far less margin) - compensated by raising `wheelieRiseRate`, re-verified at 89.6deg.
- Added a second, independent safety net per the user's own follow-up suggestion: extra downward "air gravity" once the bike has been genuinely airborne (both wheels off the ground) for a sustained period, gated off during an actual wheelie so it can never fight the intentional lift.
- All standing vehicle/faction/map validators pass except one unrelated, pre-existing issue (`Mini069GangWarValidation`, an NPC-position check untouched by this task) flagged separately. Windows build and headless smoke clean.

## 2026-08-22 — MINI-113

- Fixed the one real road-ribbon bump: measured every road-pair rather than guessing, found the Highland inroad/farm-spur junction had a 0.286m vertical step (right at the existing static check's own tolerance ceiling), fixed it to 0.000m with a localized post-migration mesh blend.
- Found and fixed a real, more serious bug: `BikeHomePoint` (the bike's save/load return point) was never included in the map migration's tracked-and-repositioned objects, and was measured 303.52m from the real, migrated safehouse - fixed to 7.34m.
- Investigated the farm-hedge/road overlap and corrected an earlier wrong measurement: `Collider.ClosestPoint` is unreliable on non-convex road MeshColliders and was falsely reporting 0.00m clearance; re-measured with real mesh vertices and found the hedge was never actually on the road (0.92m-15.21m genuine clearance). A defensive no-op safety net was kept anyway.
- Added minimap markers for owned/usable vehicles, attached at purchase time.
- New focused validator, all standing regressions (MINI-058/100/108/109/112) pass, Windows build and headless smoke clean.

## 2026-08-22 — MINI-117

- Fixed the `Mini058FactionsValidation` failure found during MINI-112's regression pass: the validator never initialized `MissionSystem`, so the recruiter's own `IsMissionReached("M15")` gate always refused regardless of anything else - a validator gap, not a real gameplay bug.
- Found and fixed a second, real bug while fixing the first: `GangMemberInteractable`'s Chevy recruitment path checked a combined mission+reputation gate before its own, more specific reputation check - meaning that second check's clearer refusal message could never actually fire. Split the checks so each does its own job.
- Added three new in-game hints (`GameplayHintController`) explaining the breeding-station process for each hybrid crop and its plant-selection number key (Purple Sugar = 7, Sugar Cheese = 9, Purple Cheese = 0), per the user's direct request.
- All standing validators (MINI-108 through MINI-112, plus MINI-058 and MINI-100) pass. Windows build succeeded; 12-second headless smoke test clean.

## 2026-08-22 — MINI-112

- Found and fixed a real steering-contention bug: `FactionBrawler` and `PatrolNPC` both drove the same `CharacterController` every frame with no coordination, so a Dog Life member mid-chase/fight was fought over by two movement systems at once. `PatrolNPC` now yields while `FactionBrawler.IsEngaged`, which also delivers "stop chasing, return to block, resume block behaviour" for free - once a fight/chase ends, the member simply resumes its own ambient waypoint wander.
- Added a real respawn cooldown: defeated Dog Life members previously reset instantly the moment their GameObject was reactivated, however little time had passed. `NpcCombatHealth.LastDefeatedAt` plus a `RivalGangSpawner`-side ~100-second check now gates each member's reactivation individually.
- Added a jealousy dialogue line for Dog Life, gated on the existing `CropUnlocked` condition (already true via either the Boss-exploitation path or MINI-111's Rasta ladder) - no new dialogue-condition machinery needed.
- Added an occasional small-group walk: up to two active, uninvolved (`!IsEngaged`, not knocked out) members periodically walk further down the road on a temporary waypoint swap, then return to their normal spot.
- New focused validator (`Mini112DogLifeEscalationValidation`). Windows build and a 12-second headless smoke test pass.
- **Found but left unfixed, deliberately out of scope**: `Mini058FactionsValidation` now fails on a `GangMemberInteractable` reputation-gate line, in a file this packet doesn't touch or reserve - flagged as a new blocker for a future task rather than absorbed here.
- Hands-on playtesting of a real Dog Life confrontation remains explicitly requested before MINI-113 begins.

## 2026-08-22 — MINI-111

- Rasta stops asking for tomatoes: M13's harvest objective now asks for 3 Bushers, tier one of a new production/strain school.
- Added six new Rasta missions (M13C2-M13C7), immediately after M13, teaching Black Sugar -> Purple -> Blue Cheese -> Purple Sugar (the `purple_black` hybrid, renamed player-facing) -> Sugar Cheese -> Purple Cheese in order. Each crop is locked until its own mission becomes current, then unlocks automatically via a new `Mission.unlocksCropId` field.
- New `ProgressionManager.RastaTaught*` flags OR into the existing `IsCropUnlocked` checks - additive to the pre-existing Boss-exploitation-stage unlock path, not a replacement. This gives the Legitimate Farmer career path (which never sees the WeedRoute-only M9W-M11W missions) its first-ever route to any strain past the basics.
- Found and fixed a real bug along the way: the scene builder's manual field-by-field copy from the in-memory mission list to the built `MissionSystem` component had no line for the new `unlocksCropId` field, so every mission's unlock would have silently baked in as empty - caught by the new focused validator before it shipped broken.
- New focused validator (`Mini111RastaLadderValidation`), confirming per-tier story-gating and that the pre-existing Boss-exploitation path still independently works. Standing regressions (MINI-108/109/110), a Windows build, and a 12-second headless smoke test all pass. Evidence under `Logs/Tasks/MINI-111/`.
- Hands-on playtesting on both career paths remains explicitly requested before MINI-112 begins.

## 2026-08-22 — MINI-110

- Added Normy's item favour ("Small Ting", mission M13B) between M13 and M14: a new `ObjectiveKind.DeliverItem` requires actually holding a bought `food_bakes` and `pill_energy` (item-ID/inventory truth via a new `EconomyManager.TrySpendConsumable`), not just visiting the shops.
- M14's briefing now carries Normy's uncertain street-info hint (somebody may be taking the boys' stock, he doesn't know who, check the Boat Man about Gardey Zafeh), reusing the existing next-mission-briefing banner.
- Rewrote the Boat Man's first-meeting dialogue to the approved four-line introduction exchange, replacing the old generic one-liner.
- Added a HUD courier-timer readout, shown only while a Guadeloupe trip is active, naming the away character and a live countdown - connects to `GuadeloupeTrade`'s existing lock/hide/timer API rather than recreating travel.
- Found and fixed a real bug along the way: the map migration's mission-marker remapper had no case for `DeliverItem`, which would have left M13B's Normy marker stale after migration - the same class of bug MINI-109 fixed for Rasta.
- New focused validator (`Mini110NarrativeBridgeValidation`). Standing regressions (including MINI-109's own validator), a Windows build, and a 12-second headless smoke test all pass. Evidence and screenshots under `Logs/Tasks/MINI-110/`.
- Hands-on playtesting (Normy's favour -> Boat Man intro -> a real Guadeloupe courier run) remains explicitly requested before MINI-111 begins.

## 2026-08-22 — MINI-109

- Fixed Clean Face (M5B) getting permanently stuck at Normy: the mission's BribeNormy objective previously had no path except Normy's ambient heat-service bribe, which refuses to do anything once heat is at/near zero - exactly the state M5A ("go rest and cool down") leaves the player in. The mission payment is now a separate, idempotent transaction that doesn't touch the ambient cooldown/heat gate.
- Fixed Black Sugar delivery to Boss J (M10W) sometimes not advancing: the interact-time gate only checked the player's Bushers count (hardcoded), not any of Boss J's actually-sellable illegal crops, so a player holding only harvested Black Sugar never triggered a sale attempt at all. Now checks for any sellable illegal stock actually held.
- Fixed M13's Rasta marker pointing at his old location: `Mini100GrandBayMapMigration.ApplyToOpenScene` moved Rasta (and Sacat/Franki) to their final migrated positions AFTER `RemapMissionObjectives()` had already baked in their pre-move positions. Reordered so remapping runs last, against final positions.
- Added a consistent retrospective-completion rule: a `BuyItem` objective now credits immediately if the player already owns the item outright, instead of forcing a repeat purchase `EconomyManager.TryPurchase` would refuse anyway. Every other objective kind keeps today's progression-lock behaviour.
- New focused validator (`Mini109MissionRepairValidation`) reproduces all three defects and proves the retrospective-completion rule against the real built scene. Standing mission/map regressions, a Windows build, and a 12-second headless built-player smoke test all pass. Evidence and screenshots under `Logs/Tasks/MINI-109/`.
- Hands-on mission playtesting (Clean Face -> Black Sugar delivery -> Rasta) remains explicitly requested from the user before MINI-110 begins.

## 2026-08-21 — MINI-109 handoff preparation

- Added a compact, detailed Claude handoff covering the game vision, current architecture, generated-scene truth, completed systems, visual locks, map/character/asset pipelines, tool/live-control rules and verification contract.
- Replaced the outdated next-agent prompt with a ready-to-paste Claude startup prompt.
- Created the approved bounded MINI-109 packet for the stuck Normy objective, Black Sugar delivery and stale Rasta marker.
- Split the larger requested progression into proposed MINI-110 through MINI-116 packets so the user can playtest between changes.
- Updated current story/design truth; no gameplay code, scenes, assets or build were changed.

## 2026-08-21 — MINI-108

- Added a universal blinking yellow objective that remains on the minimap edge for every distant marked mission, plus a one-time navigation hint.
- Added a dynamically sized transparent black mission/dialogue background and retained E fast-forward for the opening exchange.
- Rebuilt the early route: K goes directly to Boss J; L runs an increasingly frustrating legal-farming loop before rejoining Boss J.
- Replaced the flawed cooldown with Highland safehouse rest, two distinct clean-police conversations, and Normy's $100 service removing 20% heat.
- Added `111111` mission-skip testing, renamed M6 to `Stock Up`, made Paro's first meeting dialogue-only, improved Boss J delivery dialogue, built curved bananas with a brown hanging stalk, protected bridge approaches and tightened the Boss C-side shanty collider.
- Map-lab, migrated map and focused MINI-108 validations pass. The Windows build succeeds and its 12-second headless startup contains no errors; hands-on runtime acceptance remains. The user froze the temporary banana for later Hitem3D replacement.

## 2026-08-21 — MINI-106

- Created independent 25k, 12k and 4.5k mobile LOD candidates for the approved Sacat modular base.
- Preserved the 101-bone Humanoid/finger hierarchy, one material, zero unweighted vertices and four maximum skin influences.
- Added an isolated Unity three-level LODGroup prefab and corrected the Blender/Unity 90-degree visual-axis difference after fixed screenshots rejected two lying-down attempts.
- Unity validation and upright static comparison pass; playable replacement, animation deformation and Android device profiling remain gated.

## 2026-08-21 — MINI-105

- Produced and locally rigged the user-approved Sacat modular base with full separate finger chains.
- Added an isolated Unity Humanoid import package, external 2K texture, Standard material and proof prefab; playable Sacat and all locked accessories remain unchanged.
- Unity validation passed: valid Humanoid, 30/30 finger joints, one skinned mesh/material, four maximum skin influences and fixed front/back screenshots.
- The 100k-triangle source remains a high-quality LOD0 candidate; mobile LOD generation and an animation-deformation proof are required before gameplay integration.

## 2026-08-20 — MINI-102

- Moved Backstreet to the user-marked south side of Lalay, connected both ends to the main road, and removed the accidental northern route.
- Established deliberate Lalay placements for shops, Boss J, Normy, police, Dog Life, Not Ah Word, Boss C's Rover, the car dealer and Paro; added Brakes in white at the church and kept Boat Man/boat at the jetty.
- Added a GTA-style rotating minimap with progression-aware blips and a transparent red/blue wanted overlay at 50%+ heat.
- Replaced remaining player-facing Montine terminology with Highland while retaining legacy save IDs.
- Static map/scene validation and fixed visual evidence pass; hands-on driving, NPC-motion and minimap-feel acceptance remain owed.

## 2026-08-20 — MINI-100

- Migrated the approved VA-005 Lalay/Highland map into the generated playable `GrandBayProof` scene and removed the old synthetic terrain/road/coast roots.
- Preserved gameplay-facing object names and systems, relocated players, safehouses, farming, NPCs, vehicles and the Guadeloupe boat role, and updated respawn and plantation-risk positions.
- Retained the separate approved map-lab and commit `aed8854` as rollback backups.
- Added a reusable migration validator and fixed evidence cameras. Migration, scene wiring, NavMesh and Windows build checks pass; user walking/driving acceptance remains owed.

## 2026-08-20 — MINI-098

- Added the user-drawn inland-to-coastal connector as stable, collidable map data rather than a rendered annotation.
- Corrected Lalay sampling and built 82 dense roadside buildings: 59 regular one-/two-storey houses and 23 shanties.
- Enforced the eastern coastal/lower-bay no-house areas and added a terrain-following sand-and-stone bay treatment.
- Rebuilt and validated the separate map-lab; the playable gameplay scene remains unchanged pending visual approval.

## 2026-08-15 — MINI-000

- Created the clean Unity 6 Mini project.
- Added agent coordination, game-design, map-strategy, task, and verification documentation.
- No gameplay or asset import is claimed complete.

## 2026-08-15 — MINI-001

- Built `GrandBayProof.unity` via a repeatable editor script: Lalay road with 8 simple buildings, a dirty Montine farm path/clearing/plot, one NPC, one player capsule, a three-quarter follow camera (~44° downward), and a directional light.
- Added `PlayerController` (walk/run), `ThirdPersonFollowCamera`, and an interaction system (`IInteractable`, `InteractableBase`, `NPCInteractable`, `FarmPlotInteractable`, `InteractionDetector`) with OnGUI world-tracked "[ E ] Talk" / "[ E ] Plant" prompts.
- Batch-mode compile and a custom static scene-wiring validation both pass. Headless Play-mode verification is blocked by an unrelated Unity Editor Search-module bug in this environment (see `PROJECT-HANDOFF.md`); a manual Play-mode check is requested before this is called fully verified.
- Added a Windows standalone build (`Mini001Build.cs` → `Builds/GrandBayProof/UpIzUpMini.exe`, not committed — gitignored). A headless run of the built player confirmed zero console errors across the core Update loop, sidestepping the Editor bug above; visual/prompt confirmation is still pending a manual look.

## 2026-08-15 — MINI-011 (Phase A)

- Corrective-rebuild request received rejecting MINI-001's primitive-geometry visuals; scoped as `MINI-011`, superseding `MINI-004`-`MINI-009`.
- Audited `E:\Assets` (read-only) instead of the five requested Asset Store URLs (none present locally, none of which I can acquire without account access). Selected `Arteria3d Shanty Town`/`Shanty Town 2` + tropical foliage packs as a coherent local substitute — see `Docs/ASSET-REGISTER.md`.
- Confirmed the supplied heightmap is independently documented as not Unity-importable; terrain will be hand-authored instead, per user decision.
- Wrote `Docs/MINI-011-VISUAL-PLAN.md` (Phase A deliverable) and stopped at its gate for user go/no-go before any production scene/script work begins.
- Acquired the 5 requested free packages to the user's Unity account via browser, plus discovered 3 already-owned relevant packages (Demo City Mobile Friendly, Human Basic Motions FREE, Human Melee Animations FREE).
- Imported all 8 packages into `Assets/` via CLI (`-importPackage` against the user's downloaded Asset Store cache, since the Package Manager GUI isn't drivable). Added `com.unity.ugui` to `Packages/manifest.json`. Full-project compile clean. Confirmed Low Poly Character Pack rigs convert to valid Humanoid avatars, resolving the biggest risk flagged for Smart/Strong switching. `GrandBayProof.unity` itself is still untouched — Phase B production work has not started.
- Rendered actual samples of candidate building packs before committing (new capability: `Camera.Render()` to PNG from batch mode). Demo City and POLYGON Starter Pack both turned out to be generic modern/industrial kits, not Caribbean village style — rejected. Added Arteria3d Shanty Town's textured shanty structures/props (genuine corrugated-tin look, confirmed by render); Shanty Town 2's buildings turned out unfixably untextured (source pack has no real texture files for them) and were removed.
- **Phase B built:** `GrandBayProof.unity` rebuilt with a sculpted terrain (coast/village-shelf/hills), textured grass, a visible sea, a bending road, ~30-40 densely-placed houses (real Shanty Town structures + hand-built modular houses with proper pitched roofs, doors, windows), vegetation, and the farm path/clearing. Player and NPC are now real Humanoid characters with basic Idle/Walk/Run animation, not capsules. Found and fixed a real bug via the new render-based visual-check workflow (Aquaset's palm trees were wired to URP materials and rendered magenta on this Built-in RP project — swapped to the pack's own Built-In material variants) and a density problem (first pass was too sparse) before shipping a build. Windows build produced, zero errors in a headless run, launched for the user to test.
- **Phase B bugfix pass** from user hands-on testing: fixed inverted roof pitch (was rendering as a valley, not a gable — sign error in the roof rotation), removed Shanty Town variants 16/18/20 which have no matching material in the source pack (the "white shanty house" bug), scaled Shanty Town meshes up 2.1x (imported undersized), roughly doubled house density again, rescaled `OnGUI` prompt/feedback text against a 1080-tall reference (was fixed-pixel-size, unreadably small — also a mobile-readiness fix), fixed an `AnimatorController` caching bug that could mask animation fixes across rebuilds, and gave the two characters distinct skin tones (darker for the controllable character/Smart, lighter for the NPC placeholder/Strong) via a per-instance material clone of the pack's `skin` material slot. Full story (`Docs/STORY.md`) and a future shop/accessories/vehicles/property extensibility note (memory) recorded from the user's latest messages. Phase C scope (NPCs, HUD meters, crop selection/growth, Tab switching) is now clearly listed in `TASKS.md`.
- **Pause menu + mouse-look:** Esc opens a Resume/Quit pause menu (mouse-clickable UGUI buttons, keyboard-navigable, Q quits while paused); `ThirdPersonFollowCamera` now orbits horizontally under mouse control while the cursor is locked. Hit and fixed a real Unity 6 API break along the way (`Resources.GetBuiltinResource<Font>("Arial.ttf")` throws — renamed to `LegacyRuntime.ttf`). Verified via a render of the pause menu (caught and fixed a `GameObject.Find`-doesn't-search-inactive-objects bug in the verification script itself) plus a clean headless run before shipping the build.
- **Phase C:** economy layer (`CropDefinition`/`EconomyManager`/`CropSelectionController` — shared money/inventory/heat, 1-4 crop selection), a real `FarmPlot` state machine across 6 plots (light/dark soil, 3-stage growth, green→red tomato colour), 4 role-based NPCs (Villager/Police/Shopkeeper/Buyer), a HUD (health/stamina/heat/money/crop/name), and a real second controllable character — Strong, lighter-skinned, switchable via Tab (`CharacterSwitchManager`/`FollowController`/`CharacterVitals`, independent health/stamina, shared economy). Then, per the user's explicit ordering, two deferred fixes: `ThirdPersonFollowCamera` rewritten as a proper spherical orbit so mouse-look now covers vertical pitch too, and Shanty Town buildings (which never had a `Collider` — FBX imports don't get one automatically) now block the player via bounds-fitted `BoxCollider`s. Compile clean on the first attempt for the whole addition; scene builder, static validation (updated for the new component types), and a real Windows build with a **zero-error 10-second headless run** all passed before shipping. Nothing in Phase C has been hands-on playtested yet.
## 2026-08-15 - MINI-023

- Added slower police pursuit with officer stamina, exhaustion stops and recovery.
- Added mission failure at zero health and safehouse respawning/restoration.
- Restored Sacat then Franki naming and added a persistent name/full inventory HUD.
- Added Boss K and vagrant weed routes, black-market clothing resale, and 50/30 heat rules.
- Restricted police proximity heat to carrying weed or weed seeds.
- Added E companion crop orders and two post-weed missions.
- Rebuilt the generated scene; compile, Windows build and player initialization pass.
## 2026-08-15 - MINI-029

- Added branching legitimate-farming versus weed-route progression.
- Added four faction reputations, Boss K exploitation/withheld payments, Boss M/Boss P, locked Black Sugar/Purple, land searches/sabotage, and Grand Bay-gated Guadeloupe access before future Roseau expansion.

## 2026-08-20 — MINI-084

- Added a repo-scoped Up Iz Up Mini production skill shared through `.agents/skills`.
- Added the canonical Codex/Claude production loop, work-packet template, visual approval/lock register, compact current-state handoff, and a read-only ownership/dirty-tree preflight check.
- Documented the low-budget Map Truth, Hitem3D/Blender modular-character, bike IK, combat, NPC, visual QA, and mobile-first gates.
- Audited the current world, character/animation/vehicle, and mobile/QA foundations without changing gameplay, scenes, prefabs, packages, or project settings.
- Did not commit because the working tree already contained a large unrelated uncommitted MINI-052–083 batch; absorbing it into this documentation checkpoint would be unsafe.

## 2026-08-20 — MINI-085

- Preserved the complete current project state on `codex/mini-085-baseline-20260820` at checkpoint `b367fc5` before testing.
- Passed Unity compile, generated-scene rebuild, TMAX/Rover/faction/gang/chain validators, a fresh Windows build, and a clean 15-second headless player smoke.
- Generated current scene/vehicle evidence and inspected the 1280x720 build live: H tutorial, Tab switching/objective progression, inventory, E prompts, camera orbit, basic movement, and both hero idle poses work.
- Recorded two concrete opening issues for the next small fix: the active player name is absent from the HUD and the safehouse roof can obstruct the initial camera. No visual appearance was locked without user approval.

## 2026-08-20 — MINI-086 chain completion

- Captured Sacat's user-approved `GoldChain18k` transform into a permanent placement profile and visual lock `VA-001`.
- Added an immediate post-purchase equipment refresh so a wearable appears on the protagonist who bought it without relying only on event subscription order.
- Kept the cleaned real chain on Boss C and removed Boss J's chain at the generated-scene source.
- Added a transaction/distribution validator proving Sacat's purchase equips only Sacat, Boss C has the real chain, and Boss J has none.
- Rebuilt and statically validated GrandBayProof, produced a fresh Windows build, and completed a clean 12-second headless player smoke.

## 2026-08-20 — MINI-089 dialogue wording

- Replaced the generic locked-progression response with the user's exact line: `Keep doing your ting. I'll maybe organize you when you build up ur self`.
- Added a focused locked-Boss-C validator, rebuilt the generated scene, and produced a successful Windows build.

## 2026-08-20 — MINI-090 input and interaction foundation

- Added one named-action input facade over the unchanged Legacy Input Manager, with injectable virtual movement, look, and button state for future mobile controls.
- Migrated core on-foot movement, run, jump, camera look, interaction, cloning, farmhand assignment, character switching, tutorial, and melee while preserving every existing PC key.
- Passed focused validation, canonical scene rebuild, Windows build, and a 15-second headless player smoke.

## 2026-08-20 — MINI-091 NPC and companion intelligence

- Added stable trailing formations and stop/resume hysteresis for the inactive hero and recruited followers.
- Added bounded, allocation-free nearby-character separation plus blocked wait/retry behavior while retaining terrain-friendly direct companion steering.
- Applied dynamic safety and staggered timing to patrol NPCs.
- Added explicit police Patrol, Chase, Search, Recover, and Down states with line-of-sight and last-known-position searching while preserving police stamina.
- Preserved and validated the existing last-rival escape, defeated-rival despawn, and distance-pool respawn lifecycle.

## 2026-08-20 — MINI-092 combat contact truth

- Added one reusable windup/active/recovery timeline and forward fist-path resolver for player, companion, and gang attacks.
- Removed immediate omnidirectional proximity damage: targets must remain in front, inside the short swept path, and unobstructed when the active moment arrives.
- Enforced one hit per swing, recovery/cooldown, and a seven-stamina player cost.
- Corrected max-heat escalation so it applies when police are struck, not unrelated rivals carrying the same health component.
- Passed focused contact validation, companion and gang regressions, canonical rebuild, Windows build, and a 15-second headless smoke.

## 2026-08-20 — MINI-093 police melee retaliation

- Chasing police now punch at close range through the shared windup/active/recovery contact system and apply 12 player damage once per valid swing.
- Behind/out-of-contact/dead targets are rejected, and the 1.1-second cooldown prevents rapid repeated damage.
- Damage flows through `CharacterVitals`, so zero health continues into the existing mission-failure and safehouse-respawn system.
- Passed saved-scene default checks, deterministic damage/death validation, and the MINI-092 combat regression without rebuilding the scene or Windows player.
# MINI-094 — Grand Bay map-truth preview

- Added a reusable licensed OSM/Overpass map export and overhead rendering tool.
- Added the first coordinate-based `MAP-ANCHORS.json` with local metre conversion and explicit verification states.
- Produced a 1600x1000 Lalay-to-beach planning screenshot without modifying the working gameplay scene.
- Recorded Highland as the first remote planting district; its exact turnoff remains pending local confirmation.

## 2026-08-20 — MINI-095 Lalay/Highland map lab

- Built a separate repeatable Unity map-lab from 130 OSM/user-confirmed road lines and a licensed Copernicus GLO-30 terrain crop.
- Added the explicit Lalay-to-Highland inroad, first active Highland farm, three reserved future parcels, coastal jetty, waterways, landmarks, grey Lalay sidewalks, and 69 close low-poly house masses.
- Smoothed Lalay into a bump-free maximum 1.5% continuous grade with a wide yard transition while retaining stronger Highland relief; densified road meshes so secondary roads follow terrain and reject house overlap.
- Recorded the user's approved road network and original Lalay street target as `VA-004`; the playable `GrandBayProof` scene remains untouched pending a separate migration task.

## 2026-08-20 — MINI-096 reusable world expansion

- Added a mandatory gated workflow for every future Dominica district, island or unrelated map.
- Added reusable district packet and manifest templates plus safe scaffold/validation commands.
- Registered Lalay/Highland as `dm-dom-grand-bay-lalay-highland-v1` with sources, bounds, anchors, grading, connections, passability, approvals, rejected evidence, budgets and migration state.
- Proved a fresh scaffold validates, incomplete map truth is rejected, existing districts cannot be overwritten, and Lalay cannot advance past graybox until its corrected player-height view is approved.
- No Unity scene, prefab, package, project setting or production map data changed.
## 2026-08-20 — MINI-099 accepted map graybox

- Curated the phase-one Grand Bay world to nine connected, collidable roads and four centered two-vehicle bridges.
- Corrected road/terrain conformity, Lalay and Highland grades, coastal sand/no-house land use and jetty reach.
- Built 115 Lalay houses and 18 Highland buildings; registered one active and seven inactive farm parcels.
- Added `VA-005`, advanced the district to `approved_graybox`, and documented the reusable Caribbean map recipe.

## 2026-08-20 — MINI-101 runtime map correction

- Moved seven Lalay shops and their sellers into deterministic roadside lots, replacing only conflicting placeholder houses and keeping the paved road clear.
- Regraded the Lalay/Highland connector and farm spur as one shared gentle corridor; flattened and relocated the active farm, safehouse and breeding station off the road.
- Added repeatable road-join, shop-clearance and farm-level validators plus fixed map screenshots.
- Added a road-following runtime navigation-link chain because the legacy Edit-Mode Sacat-to-farm NavMesh check still reports a partial route.
- Rebuilt `Builds/GrandBayProof/UpIzUpMini.exe`; runtime driving and companion navigation remain the user acceptance gate.
# 2026-08-21 — MINI-107 (paused)

- Added the reusable Up Iz Up Mini character manifest schema, Sacat manifest, production workflow, generic Blender LOD tool, PowerShell runner and isolated Unity motion-proof builder.
- Confirmed the original 100K Sacat rig animates correctly with the owned Humanoid clips.
- Rejected the current decimated mobile LODs for motion because joint deformation tears; no playable character or gameplay scene was changed.
# 2026-08-29 — MINI-121 MB Road System Lalay proof

- Added a reversible, isolated Lalay spline-road proof using the already-installed MB Road System 2.2.1 package.
- Preserved the approved Map Lab and live gameplay scene; the proof scene contains an inactive two-road legacy rollback root.
- Retained the sourced Lalay X/Z path, sampled its approved collider heights, reused the approved 6.2m width/material, and generated matching road collision.
- Validation passed with two MB roads, two legacy backups and 478 generated vertices; captured fixed before/after overhead and player-height evidence. Awaiting user visual approval; no live migration performed.

# 2026-08-29 — MINI-122 line-free Lalay asphalt and connected junction

- Rebuilt the accepted isolated spline proof with dark line-free asphalt; the package's painted-line colour texture is not used.
- Connected both Lalay spline segments through one MB intersection with two road anchors and a non-colliding visual junction cover.
- Kept the lined sample available for a possible later secondary road and retained both inactive legacy roads for rollback.
- Passed batch compile and focused validation with 478 road vertices; captured player-height, overhead and junction evidence. The live gameplay scene and EXE remain unchanged pending visual approval and vehicle testing.
- Follow-up visual correction inset both spline ends into a 48-segment rounded apron with one continuous collider, added a curved proof-only sidewalk cleanup, removed the two blue house masses obstructing that junction, and revalidated at 470 road vertices. The source Map Lab remains unchanged.
