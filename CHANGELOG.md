## MINI-172 — Roads, PCSS clearance and Geneva cricket ground (2026-09-19)

## MINI-190 - Pistol handling animation for all humanoids (2026-10-02)

Research-backed procedural system (two-hand IK, finger curls, recoil, draw/holster, low ready, reload) for Sacat, Franki and any NPC; Play Mode proof renders; no gameplay values changed. See `Docs/WorkPackets/MINI-190.md`.

## MINI-181 — SuperMoto wheelie no longer crashes; realistic wheelie and suspension (2026-09-27)

Fixed the real wheelie crash causes (flipped pitch sign triggering auto-recover mid-wheelie, two scripts fighting over bike rotation, vendor bone crash triggers, deceleration/damage crashes during wheelies). Wheelie is now a spring-damper balance-point motion (settles ~35deg, capped 55deg, soft landing), suspension retuned to real supermoto travel/sag/damping, rider leans with the wheelie's motion. See `Docs/WorkPackets/MINI-181.md`.

Smoothed eight expansion road centrelines without moving endpoints, added three paved seam/corner junctions, and refitted terrain beneath them. Measured the PCSS conflict to the entrance-facing right wing and moved it 3 m off Road 4 while narrowing the campus footprint. Geneva remains a modest multi-use grass field but now includes a central cricket strip, wickets, oval boundary and two small three-tier stands alongside football markings. Focused validation and the complete MINI-171 import regression pass; five new renders await user review. See `Docs/WorkPackets/MINI-172.md`.

## MINI-168 — Geneva playing field and coastal loop (2026-09-15)

Separate expansion copy now connects the bay through a roundabout and coastal stretch back to PCSS; school courtyard faces the road. Added simplified Geneva football pitch, goals, markings, benches and graded terrain. Fresh-session renders and static validation pass;11 road colliders,7.96% maximum grade,217480 extension triangles. Live game untouched; runtime testing/visual acceptance pending. Full instructional record: Docs/Maps/dm-dom-grand-bay-expansion-v1/GENEVA-WORKFLOW.md.
## MINI-168 — Berekua to high school review expansion (2026-09-14)

Built an isolated expansion with seven selected OSM road sections, an approximate campus entrance, 52 approved-family houses and school massing. Three terrain passes, four inspected renders, matching road colliders and a maximum measured 5.50% centreline grade. Source and live scene hashes unchanged; copy excluded from builds. School appearance, junction traversal, waterways, navigation and mobile performance remain review/follow-up items. Complete commands, corrections and evidence: Docs/Maps/dm-dom-grand-bay-expansion-v1/CLAUDE-CONTINUE.md.

## MINI-166 — Codex Air Max 90/97 fitted to both characters (2026-09-13)

User approved the new Blender previews for the main characters. Franki defaults to the 90 and Sacat to the 97; both remain selectable per character under existing IDs, with saved selections/colours preserved. Real Air cavities, reduced combined geometry and fitted ankle skin replace the tall legacy footwear; Mike270 and all non-shoe outfits/accessories are preserved. Shoe UI/save tests, full wardrobe regression, sampled idle/walk/run, final static renders, Windows build (437,302,389 bytes) and actual built-player/portrait proof pass. Exact process, source, measurements and remaining mobile/material limits: `Docs/WorkPackets/MINI-166-AirMax-Integration.md`.

## MINI-170 SuperMoto wheelie tip-over fixed — 2026-09-15

Scripted crash-ejection was already fully off; the bike was still physically tipping over during a wheelie with zero correction. Root-caused to two real bugs (SuperMotoUprightAssist disabling roll correction during wheelie; a stale pitch-measurement technique breaking the auto-recovery safety net) and fixed both, plus a test-harness gap that was hiding the first bug. Verified with the project's own real-scene regression test: roll went from climbing to 87.7deg and staying stuck, to holding at 1-2deg through a full 8s sustained wheelie. Compile clean, Windows build succeeded (437,304,645 bytes). See Docs/Systems/Vehicles.md.

## MINI-169 Highland/Farm Safehouse walkable interior — 2026-09-14

The first/free safehouse was already a real 3-walled room with a bed (survey found this before building anything) - just open-fronted like a shed. Added a real front wall with a walkable doorway, matching its existing construction exactly. Compile clean, scene saved, real renders confirmed, Windows build succeeded (437,304,133 bytes). Lalay House/Estate interiors and the Highland Mansion garage/integration are separate follow-up parts of the same task. See Docs/WorkPackets/MINI-169.md.

## MINI-166 Mike 270 shoe + rounded toe/heel fix — 2026-09-13

Added Mike 270 as a third selectable Shoes design (one oversized heel Air window), extending the existing in-game shoe generator. Also fixed a real toe/heel pinch-to-a-point bug shared by all three shoe designs (Mike90/97/270), ported from this session's Blender concept work. Compile clean, Integrate saved, full Play Mode wardrobe regression passed, Windows build succeeded (421,412,021 bytes). See Docs/WorkPackets/MINI-166.md "Mike 270 shoe + rounded toe/heel fix" section.

## MINI-166 Lacos Cap back strap + eyelets — 2026-09-13

Added a real back adjustment strap (with two snap details) and four side eyelets to the shipped in-game Lacos Cap, carrying over the shape language from a concept "DA" snapback cap render. Pure C# combined geometry, no Blender/FBX round trip. Compile clean, Integrate saved, full Play Mode wardrobe regression passed, Windows build succeeded (419,987,381 bytes). See Docs/WorkPackets/MINI-166.md "Lacos Cap back strap + eyelets" section.

## MINI-166 individual outfits and shoulder revision — 2026-09-12

Distinct starting clothing for Franki and Sacat; preserved individual saved choices and verified that wardrobe edits never change the other character's visible mesh or colour. Both shirt designs now have fuller, smoothly blended shoulder/trapezius geometry. Close-up and motion verification passed; rebuilt and inspected actual EXE captures. Windows output total 420,366,933 bytes. See Docs/WorkPackets/MINI-166.md revision section; exact styling awaits user review.

## MINI-166 completed wardrobe repair — 2026-09-12

Completed all eight fitted designs for both characters with independent colours, proper skin masks, smooth shorts legs, distinct 90/97 shoes and a curved cap. Fixed portrait colour/pose framing, offscreen hair/headphone updates, accessory removal and menu restoration. Actual save/load, menu and animation checks pass; rebuilt Windows player and inspected its real character/portrait outputs. Final build total 420,322,677 bytes. Evidence and exact commands: Docs/WorkPackets/MINI-166.md. Exact styling awaits user playtest; no vehicle edits included.

## MINI-166 checkpoint — 2026-09-11

Added outfit mesh-selection/tint/save foundations and mesh audit; no fitted outfit assets or clothing selector integration yet. Windows checkpoint build succeeded; see Logs/mini166-build.log. Detailed user-requested Claude continuation guide: Docs/CLAUDE-CONTINUE-MINI-166.md. Task remains incomplete.

## MINI-165 — Headphones as a removable head accessory (2026-09-10)

Sacat's existing headphones are now a separate skinned accessory. Open the home wardrobe, Accessories, then Headphones / REMOVE or WEAR; Apply keeps the choice, Cancel and Restore opening outfit restore it. Existing game saves capture the selection through sacatUnequippedItems; legacy saves default to wearing the headphones. Franki has no headphone assignment; this task does not add a second fitted asset.

Six disconnected pieces (6,760 triangles) were extracted without changing positions, UVs, normals, weights or materials. The wave scalp, face and repaired clothing remain in the original body mesh. Compile, Play Mode toggle/Cancel/Apply/GameSave JSON tests, saved-scene renders, Windows build (411,132,437 bytes) and player proof process passed. Logs and on/off images: Logs/Tasks/MINI-165; compiler/build logs: Logs/mini165-*.log. Automated player UI captures were blank and are not visual acceptance evidence; hands-on button layout review remains owed. See Docs/WorkPackets/MINI-165.md.
## MINI-164 hair continuation — 2026-09-10

Both playable characters now have black wave-textured scalp meshes in GrandBayProof. Franki uses a continuous shell fitted by ray intersections to his existing head; Sacat's fused cap submesh is replaced by a smooth scalp, preserving the live clothes, face, headphones, bone weights and other submeshes. The existing removable wardrobe cap remains available, and real Play Mode verified equip/remove leaves both wave meshes/materials intact.

Unity compile, saved-scene render and Windows build passed. Build: Builds/GrandBayProof/UpIzUpMini.exe (410,853,365 bytes). Evidence and exact commands: Docs/WorkPackets/MINI-164.md; Logs/Tasks/MINI-164/waves-test-build.log and waves-saved-evidence.log.

User visual acceptance is still owed. Existing prototype cap is visibly too low and is not fixed by this hair task. Sacat's forehead/temple seam and original headphones remain rough; existing white eyelashes and garment appearance are unchanged. No complete wardrobe/contact-sheet milestone is claimed.
# Up Iz Up Mini — Changelog
## MINI-163 — Franki's hair fixed and shipped; Sacat's attempted and rejected

Franki's glitchy hair texture was reading nonsense pixels from a material shared with the eyelashes - gave it its own material (flat colour + fade band + wave displacement), verified, integrated, built (409,318,629 bytes, 15s smoke clean). Attempted real hair for Sacat (a dome baked into his head, no separate mesh); caught two real problems (a texture repaint that broke the headphones, then a distorted bounding-box hair transplant) and shipped neither - his Ch06 renderer is unchanged. See Docs/WorkPackets/MINI-163.md.

## MINI-162 — Franki neckline fixed (took over from rate-limited Codex); hair scoped

Finished Codex's collar/neckline flatten with a targeted second smoothing pass on the collar rim, re-integrated, re-rendered and confirmed. Arms remain intact. Investigated the hair request with real renders: Sacat has no real hair mesh (a dome baked into his head), Franki's hair mesh has a broken texture. Both need real production work - scoped honestly, not started. See Docs/WorkPackets/MINI-162.md.

## MINI-161 — Franki missing-arm repair

Retained and repainted fresh sleeve geometry rather than deleting it. Narrow live-shirt replacement; isolated arm pose verification on both protagonists. Pants, Sacat clothes and accessories preserved. See Docs/WorkPackets/MINI-161.md.

## MINI-160 — visual vehicle dealer panel

Car Dealer NPC now opens a rotate-and-inspect real vehicle preview (mirrors the wardrobe panel's pattern) instead of a plain text list, then buys through the unchanged existing EconomyManager/VehicleSpawnController pipeline. Compiles clean, real preview renders verified for both stocked vehicles. Not yet hands-on played or built. See Docs/WorkPackets/MINI-160.md.

## MINI-159 — clothes integrated into the live game; Windows build

Reshaped/coloured shirt+pants (MINI-157/158) wired into the live playable Sacat/Franki via a bone-remap technique that reuses the exact tested mesh/material and touches nothing else. Two real bugs (overexposed render, missing texture from MINI-157's FBX export) caught and fixed before saving. Compile clean, build succeeded (403,574,245 bytes), 15s smoke clean. Accessories investigated, not redesigned - open item. See Docs/WorkPackets/MINI-159.md.

## MINI-158 — real motion proof for both reshaped garments

Dropping -nographics (keeping -batchmode) fixed the blank-headless-render issue from MINI-154/157 - proven by rerunning the identical tool and getting real images immediately. 132 real idle/walk/run screenshots: no shoulder/sleeve/collar/waist tearing on either Franki or Sacat. Sacat's render is overexposed (lighting/material config in the proof tool, not the mesh). Canonical scene verified unchanged. New BuildAndVerification.md lesson: a clean batch-mode exit does not mean a render tool actually rendered anything. See Docs/WorkPackets/MINI-158.md.

## MINI-157 — Sacat shirt/pants (texture-only method) + colour on both characters

Sacat's mesh is fused skin+clothing (no separable layer), so used a texture/UV-repaint method instead of Franki's topology cut: zero geometry change, real sampled skin tone revealed at the new short-sleeve/collar lines, garment colour painted over shirt/pants regions. Two real defects (face getting painted over) caught by rendering and fixed. Franki's already-reshaped garment now has colour too. Both FBX assets verified valid Humanoid on Unity import. Motion proof still blocked by the same headless-render environment limitation as MINI-154 - needs an open Editor session. No gameplay integration yet. See Docs/WorkPackets/MINI-157.md.

## MINI-156 — spawn moved to Highland Safehouse; Windows build

Sacat/Franki spawn position moved to the Highland Safehouse via a small additive live-scene patch (two Transform.position writes from the safehouse's own spawnPoint field, not a scene regeneration). Windows build succeeded (398,098,421 bytes) including this and MINI-155's arrow-key camera look; 15s headless smoke clean. See Docs/WorkPackets/MINI-156.md.

## MINI-155 — arrow keys pan/tilt camera like the mouse

GameInput.Move now reads WASD directly (arrow keys no longer walk the character); GameInput.Look adds an arrow-key vector on top of the existing mouse delta. No ProjectSettings edit; vehicle steering and the camera script itself untouched. Compile-clean. New Docs/Systems/Camera.md. See Docs/WorkPackets/MINI-155.md.

## MINI-154 — shirt/pants reshaped-from-existing-mesh preview (Franki, corrected)

Investigated Sacat/Franki's real mesh structure via Unity batch mode against the live scene, which caught a real mislabel: Franki has separate already-skinned Hoody/Pants objects (`Strong.fbx`/`Ch28_*`), Sacat's clothes are fused into one mesh (`Mainchar.fbx`/`Ch06`) — the reverse of an earlier stale comment. Edited Franki's existing Hoody/Pants topology directly into a short-sleeve crew top (hood/tall collar removed) and a relaxed-cuff trouser, avoiding the shoulder-gap/cylindrical-sleeve/exposed-hem failures of MINI-150. Real Blender renders, not concept art. Built a Unity motion-proof tool that runs clean without touching the canonical scene, but headless `-nographics` screenshot capture is blank in this environment (proved with a plain cube) — needs an open Editor session. No Unity Assets, scene, or accessories changed; Sacat not started. Awaiting user visual approval. See Docs/WorkPackets/MINI-154.md.

## MINI-149 — purchase-free trial foundation

Home wardrobe gains session-only existing accessory trials; no economy/save grants, no scene/art changes. New clothing and colour UI remain pending, not claimed complete.

## MINI-148 — isolated polo diagnostic

Blender fitting attempted, visually rejected for patchy/clipped topology. Recorded failed approach and next topology requirement; no gameplay/EXE changes.

## 2026-09-07 — wardrobe design approval

- User approved clothing and hat, plus revised Air Max 90/97-inspired Mike footwear sheet. Preserve independent colour selection requirement. Concept-only; no game/EXE changes.

## 2026-09-07 — MINI-147 clothing reference preview

- Record next wardrobe capsule and per-character/per-slot colour requirement; create clothing-only AI style preview. No gameplay or EXE changes; approval required before 3D production/integration.

## 2026-09-07 — MINI-146 approved watch and home wardrobe

- Connect approved watch prefab and individual fits to existing purchase. Add owned-home wear/remove and independent save fields with old-save defaults. Preserve chains and scene placements.
- Focused validation and Windows build pass; 12-second startup smoke clean. Live wardrobe/movement acceptance remains user test.

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

## MINI-173 — Lalay homes, Geneva and coastal bay (2026-09-19)
31 imported Lalay shanty roots disabled and replaced on existing lots with procedural veranda homes. Removed141 football objects from Geneva; retained cricket/community features and corrected stand tier direction. Clipped/graded expansion coast so the roundabout reads as bay-side; preserved road geometry. Validation passed:31 homes, zero active shanties/football, zero sampled house-road hits, matching terrain mesh/collider, minimum roundabout shore clearance8.524689m. Real-world references establish architectural types only. See Docs/WorkPackets/MINI-173.md for tools, commands, dimensions, sources, evidence and Claude continuation rules. No EXE rebuild.

## MINI-174 — bay seawall and house separation (2026-09-19)
Added65 joined seawall segments past the roundabout in expansion import scene. Audited169 generic house envelopes including roofs;72 conservative clashes resolved by adjusting62 houses. Fresh saved-scene validation passed:0 house overlap pairs,0 sampled house-road hits,130 wall colliders. Google Maps reference unavailable; wall design follows user's correction, with approximate dimensions. Commands, sources, exact relocations and inspected renders: Docs/WorkPackets/MINI-174.md. No EXE rebuild; visual/user gameplay review pending.

## MINI-175 — Grand Bay Credit Union on Lalay (2026-09-20)
Added labelled credit union in expansion import scene using published directory pin15.2407793,-61.3166228 and official Lalay address. Street-relative frontage setback5.187216 game metres; model is stylized, not a surveyed/photo-matched replica. Replaced one generic parcel house; moved conflicting clothing stall and NPC together16m along the street. Renders inspected. Tool, measurements, failed approaches, reference limitations and verification: Docs/WorkPackets/MINI-175.md. No EXE rebuild.

MINI-175 colour correction: credit union exterior changed from cream to white at user request; builder default updated to preserve the change.

MINI-175 final palette: white walls/trim, black windows, sign background, columns and ATM; white sign lettering. Render inspected; mini175-monochrome.log passed.

## MINI-176 — Claude workflow handoff and current-chain verification (2026-09-20)
Complete guide: Docs/CLAUDE-GRANDBAY-BUILDINGS-CHAIN-WORKFLOW.md, linked to the earlier Blender MINI-142 house guide. Covers current Grand Bay map/building dimensions, geographic/elevation rules, road/coast/lot validation, tools, source limitations, chain transfer measurements and future-character fitting workflow. MINI-176 experimental mesh remaps were superseded by Claude MINI-178; preserve the accepted Franki profile and unchanged Sacat. Fresh saved-expansion Play Mode verification passed (Logs/mini176-verify-current.log); existing canonical-scene wardrobe regression passed (Logs/mini176-wardrobe-regression.log). No new EXE, motion certification or live-scene promotion. Details: Docs/WorkPackets/MINI-176.md.

## MINI-180 — Grand Bay expansion promoted and Windows EXE rebuilt (2026-09-26)

At the user's explicit request, the saved expansion replaced the older map in `GrandBayProof.unity`. The canonical scene remains the sole build scene and retains its GUID; the former scene was backed up under ignored `Builds/PreExpansionSceneBackup`. The expansion still contains the mission system, minimap, player and existing gameplay points. Static scene validation and the Windows build passed. A 15-second headless player launch remained alive, with repeated kinematic-body angular-velocity warnings and no logged exception; visual play remains unverified. The separate road-junction preview was not applied. See `Docs/WorkPackets/MINI-180.md`.
# MINI-183 (2026-09-29)

Lalay's black-market trader now sells a fictional sidearm and repeatable ammunition, with clothing resale on key 0. Both playable characters share ownership and ammo. Added aim, recoil, firing, reload, NPC damage and heat response, plus save/load support and a temporary sidearm model. Unity compile, scene validation and Windows build passed; hands-on visual testing remains pending. No Hitem3D points spent.
# MINI-184 (2026-09-29)

Repaired the temporary sidearm attachment so it follows the animated right hand with a stable root scale and aims along the camera direction. A built-player diagnostic passed purchase, ammo use, NPC damage and reload; final compile, scene validation and Windows build passed. Natural aim motion and feel still require player review. No Hitem3D points spent.
# MINI-185 (2026-09-29)

Added M12T "Get Your Tool" after "Round the Village": meet the Lalay trader, buy the gun called a tool, and fire one test shot. Added stable mission ID saves and migration for old saves, including a one-time detour for players past M12 without the tool. Targeted scene validation, objective exercise, compile and Windows build passed; player-controlled UI/story review remains.
## MINI-186 — Modern Lalay Tool visual (2026-10-02)

Generated a fictional modern sidearm from an ImageGen concept with one 65-credit Hi3D attempt. Cleaned its raw 1.99M-triangle mesh to 2,998 triangles and a 1024 texture; replaced the primitive weapon visual on both player characters. Scene verification, in-engine static render and Windows build pass. Runtime aim pose awaits play review. See `Docs/WorkPackets/MINI-186.md`.

## MINI-187 — Lalay Tool colorways and proper hand anchors (2026-10-02)

Added three unbranded color materials and character proofs. Replaced the wrist-relative visual placement with explicit grip and muzzle anchors measured against each rig's curled hand. Both static character validations and Windows build pass; live aim motion remains to review. See `Docs/WorkPackets/MINI-187.md`.

## MINI-188 — Online-reference hand-fit adjustment (2026-10-02)

Added an upper backstrap anchor and adjusted the Lalay Tool toward the thumb-index web after comparing online references and character renders. Both character close-ups retain finger overlap; static alignment checks and Windows build pass. The live aiming pose remains unreviewed. See `Docs/WorkPackets/MINI-188.md`.
