## MINI-166 revision — individual outfits and fuller shoulders (2026-09-12)

Franki starts in navy tee/jeans/Mike 90; Sacat in green polo/black trousers/Mike 97. Existing saves preserve their selected clothes. UI clearly identifies the individual wearer; tests prove the other character's mesh/colour stays untouched. Both shirt designs have a fuller shoulder/trapezius slope. Compile, Play Mode, motion and final EXE proof pass; Windows build 420,366,933 bytes. User styling review remains. Details: Docs/WorkPackets/MINI-166.md revision section.

## MINI-167 — bikes 50% harder to crash (2026-09-12)

`BikeCrashEjectionController.hardImpactSpeed` raised 16.5 -> 24.75 m/s
(+50%) per the user's direct request. Applies to both TMAX and SuperMoto.
Standing MINI-134 crash validator updated and re-passing. Windows build
succeeded (420,321,141 bytes). Done in parallel with Codex's concurrent
MINI-166 wardrobe work (non-overlapping files). See
`Docs/Systems/Vehicles.md`'s MINI-167 entry.

## MINI-166 — wardrobe complete and rebuilt (2026-09-12)

Both characters have eight fitted clothing designs, seven colours, independent slots, clean shorts/legs, distinct Mike 90/97 shoes and a curved cap. Removable Sacat headphones and waves preserved. Play Mode save/load/menu tests, animated deformation checks, final Windows build (420,322,677 bytes total) and actual EXE player/portrait captures passed. Safehouse E -> 5; Apply retains choices, F5 saves. Exact styling is ready for the user's in-game review. See `Docs/WorkPackets/MINI-166.md` latest section. This supersedes the historical incomplete wardrobe entries below; concurrent MINI-167 vehicle work remains separate.

## MINI-166 handed off to Codex (2026-09-11)

User is switching this task from Claude to Codex. See
`Docs/CODEX-CONTINUE-MINI-166.md` for the handoff pointer - it routes to
`Docs/Systems/Characters.md`/`UI.md`'s MINI-166 entries and
`Docs/WorkPackets/MINI-166.md` for the actual history. Ownership released
to `None` in `PROJECT-HANDOFF.md`; Codex should claim MINI-166 (same task
ID) before continuing.

## MINI-166 — all four wardrobe slots real for Franki; Sacat has three open placement bugs (2026-09-11)

Continued from the checkpoint below. All four slots (Shirt/Pants/Hat/Shoes)
now have at least one real, player-selectable design for Franki, wired
through the actual E -> 5 safehouse wardrobe UI and verified by render/Play
Mode tests, each step built and checkpointed (latest:
`Builds/GrandBayProof/UpIzUpMini.exe`, 412,098,293 bytes). Sacat: Shirt is
done (Tee/Polo via a collar overlay); Pants and Hat are NOT wired for him -
three separate placement/geometry bugs found this session, all traced far
enough to rule out easy causes but not fully root-caused, all documented
rather than shipped broken (see `Docs/Systems/Characters.md`'s MINI-166
entry). Denim Shorts (both characters) also deferred - leg-skin coverage
never got a clean, trustworthy visual check. No motion proof yet (idle pose
only). Full detail: `Docs/WorkPackets/MINI-166.md`.

## MINI-166 — EXE checkpoint; outfits incomplete (2026-09-11)

Runtime outfit selection/save foundations compile and Windows build passed (`Logs/mini166-build.log`, 411,132,245 bytes total). Fitted meshes and clothing UI are NOT integrated; existing clothing appearance remains. User requested Claude continuation instructions: read `Docs/CLAUDE-CONTINUE-MINI-166.md` and claim MINI-166. Owner released to None.

## MINI-165 — Headphones as a removable head accessory (2026-09-10)

Sacat's existing headphones are now a separate skinned accessory. Open the home wardrobe, Accessories, then Headphones / REMOVE or WEAR; Apply keeps the choice, Cancel and Restore opening outfit restore it. Existing game saves capture the selection through sacatUnequippedItems; legacy saves default to wearing the headphones. Franki has no headphone assignment; this task does not add a second fitted asset.

Six disconnected pieces (6,760 triangles) were extracted without changing positions, UVs, normals, weights or materials. The wave scalp, face and repaired clothing remain in the original body mesh. Compile, Play Mode toggle/Cancel/Apply/GameSave JSON tests, saved-scene renders, Windows build (411,132,437 bytes) and player proof process passed. Logs and on/off images: Logs/Tasks/MINI-165; compiler/build logs: Logs/mini165-*.log. Automated player UI captures were blank and are not visual acceptance evidence; hands-on button layout review remains owed. See Docs/WorkPackets/MINI-165.md.
## MINI-164 hair continuation — 2026-09-10

Both playable characters now have black wave-textured scalp meshes in GrandBayProof. Franki uses a continuous shell fitted by ray intersections to his existing head; Sacat's fused cap submesh is replaced by a smooth scalp, preserving the live clothes, face, headphones, bone weights and other submeshes. The existing removable wardrobe cap remains available, and real Play Mode verified equip/remove leaves both wave meshes/materials intact.

Unity compile, saved-scene render and Windows build passed. Build: Builds/GrandBayProof/UpIzUpMini.exe (410,853,365 bytes). Evidence and exact commands: Docs/WorkPackets/MINI-164.md; Logs/Tasks/MINI-164/waves-test-build.log and waves-saved-evidence.log.

User visual acceptance is still owed. Existing prototype cap is visibly too low and is not fixed by this hair task. Sacat's forehead/temple seam and original headphones remain rough; existing white eyelashes and garment appearance are unchanged. No complete wardrobe/contact-sheet milestone is claimed.
# Up Iz Up Mini — Current State
## MINI-163 — Franki's hair fixed and shipped; Sacat's attempted and rejected (2026-09-08)

Franki's broken/glitchy hair texture is genuinely fixed - own dedicated
material (no longer sharing the corrupted shared atlas with the
eyelashes), a wave-textured crown, and a fade band near the hairline.
Verified by rendering before and after saving, integrated into the live
scene, Windows build succeeded (409,318,629 bytes), 15s smoke clean.
`Builds/GrandBayProof/UpIzUpMini.exe` is ready for playtest. Sacat still
has no real hair - two attempts this pass both caught real problems
before shipping (a first attempt corrupted his headphones' colour via
shared-texture pixel reuse; a second attempt, transplanting Franki's hair
shape via a bounding-box remap, produced a visibly distorted result) and
neither was integrated. Sacat's `Ch06` is unchanged from before this
session. See Docs/WorkPackets/MINI-163.md and Characters ledger.

## MINI-162 — Franki neckline finished; hair scoped as real next task (2026-09-08)

Claude took over MINI-162 from Codex (rate-limited, user authorized
handoff) and finished the collar/neckline fix Codex had started: the
raised hoodie-shaped bump behind Franki's neck is flattened and the
jagged collar rim Codex hadn't gotten to is now smoothed, verified by
rendering. Arms remain intact (MINI-161 unaffected). Investigated the
user's "don't want bald heads, want short waves + shape-up" request with
real renders before touching anything: **Sacat has no real hair mesh at
all** (the "cap" is a dome baked into his head geometry, not a real
accessory), **Franki has a real hair mesh but a genuinely broken/glitchy
texture**. Both need real production work, not a toggle - not started,
honestly scoped rather than rushed. See Docs/WorkPackets/MINI-162.md and
Characters ledger.

## MINI-161 — Franki arm gap repaired; both characters checked

Applied fresh-source retained-sleeve repaint to Franki Ch28_Hoody only. Both arms of Franki and Sacat inspected in isolated idle/walk/run pose captures; no missing sleeve-to-hand gap observed. Sacat, pants and accessories unchanged. New source: Franki_ArmsRestored.fbx/png/mat. Preview/import checks pass without discarded polygons after triangulation. Build/smoke outcome in WorkPackets/MINI-161.md. Full wardrobe purchasing/swapping and accessory fit are not completed by this repair. Preserve Claude's existing dirty work; no blanket commit or reset.

## MINI-160 — visual vehicle dealer panel: see it, rotate it, buy it (2026-09-08)

New `VisualVehicleDealerPanel.cs` mirrors the wardrobe panel's exact
pattern - Car Dealer NPC now opens a rotate-and-inspect real 3D vehicle
preview (the same prefab that actually spawns on purchase) instead of a
plain number-key text list, with a Buy button that calls the unchanged
existing purchase pipeline. Compiles clean; real rendered previews of both
stocked vehicles verified (not assumed). Not yet hands-on played, no build
made this pass. See Docs/WorkPackets/MINI-160.md and Vehicles ledger.

## MINI-159 CORRECTION — Franki's arm has a real gap, handed to Codex (2026-09-08)

User caught a real defect in the live build: Franki's short sleeve leaves
the hand floating disconnected from the shoulder. Confirmed with an
isolated skin-only render: `Ch28_Body` has almost no arm geometry - the
original asset assumed the arm is always covered by a long sleeve.
MINI-154's delete-the-sleeve-fabric method was wrong for this reason (an
assumption never checked down to the arm specifically). Fix path is
documented in full in the Characters ledger's MINI-159 CORRECTION entry:
reuse the exact retexture-not-delete method already proven correct for
Sacat (MINI-157) on Franki's sleeve/collar instead of deleting fabric.
User asked to hand this specific fix to Codex - released without
finishing it. Read the Characters ledger entry before starting, the wrong
assumption and working fix pattern are already known.

## MINI-159 — clothes now live in-game on Sacat and Franki; Windows build ready for playtest (2026-09-08)

The motion-tested, coloured reshaped shirt/pants (MINI-157/158) are wired
into the actual live `GrandBayProof.unity` Sacat and Franki — not just an
isolated proof anymore. Integration reused the exact tested mesh/material
and only remapped each renderer's bones by name onto the live skeleton, so
nothing else (Animator, other components' references) was touched. Two
real bugs (an overexposed verification render, then a genuinely missing
texture reference from MINI-157's FBX export) were caught by rendering and
looking, then fixed, before the scene was saved. Compile clean, Windows
build succeeded (403,574,245 bytes), 15s headless smoke shows zero
exceptions. `Builds/GrandBayProof/UpIzUpMini.exe` is ready for the user's
playtest. Accessory placement (cap/shades primitives) was investigated but
NOT redesigned this pass — Franki shows no visible cap/shades under the
same test that shows something on Sacat, unconfirmed why; flagged as open,
not fixed. See Docs/WorkPackets/MINI-159.md and Characters ledger.

## MINI-158 — real motion proof: both reshaped garments survive idle/walk/run (2026-09-08)

The blank-headless-render limitation from MINI-154/157 was just the
`-nographics` flag — dropping it on the identical tool immediately produced
real screenshots. 132 real motion frames now exist
(`Logs/Tasks/MINI-158/Franki-*.png`, `Sacat-*.png`): **no shoulder, sleeve,
collar, or waist tearing on either character through idle/walk/run.** This
is the first actual motion confirmation either reshaped garment has had.
Sacat's render is overexposed (lighting/material issue in the proof tool,
not the mesh — silhouette still reads correctly) — open item. Canonical
scene verified unchanged. Still no Unity gameplay integration — that's the
next step once the user reviews this evidence. See Docs/WorkPackets/MINI-158.md
and Characters ledger.

## MINI-157 — Sacat's shirt/pants (texture-only) + colour on both, awaiting approval (2026-09-07)

Sacat's clothing is fused into one mesh with his skin (no hidden layer
underneath), so the topology-cut method used on Franki (MINI-154) doesn't
apply there. Used a texture/UV-repaint method instead: zero geometry
change, real skin tone (sampled from the character's own Head-bone
vertices) painted over the newly-short sleeve and lowered collar, garment
colour painted over the shirt/pants regions. Two real defects (accidentally
painting over the face) were caught by rendering and looking, then fixed -
not assumed correct from the code. Franki's already-reshaped garment
(MINI-154) now has colour too (navy shirt, charcoal pants). Both new FBX
assets verified to import as valid Humanoid in Unity. Real renders:
`Logs/Tasks/MINI-157/Sacat-Reshaped-*.png`, `Franki-Colored-*.png`. Motion
proof still blocked by the same headless-render environment limitation
MINI-154 hit (not asset-specific) — needs an open Unity Editor session to
actually capture idle/walk/run screenshots. No Unity gameplay integration
yet; accessories untouched, still rejected. See Docs/WorkPackets/MINI-157.md
and Characters ledger.

## MINI-156 — spawn moved to Highland Safehouse; Windows build ready (2026-09-07)

Sacat and Franki now spawn at/beside the Highland Safehouse (the free
starting property) instead of out in the Lalay area, via a small additive
live-scene patch (two `Transform.position` writes reading the safehouse's
own `spawnPoint` field) — not a scene regeneration. Windows build succeeded
(398,098,421 bytes) with this plus MINI-155's arrow-key camera look; 15-second
headless smoke shows zero exceptions/errors. `Builds/GrandBayProof/UpIzUpMini.exe`.
Actual spawn placement and arrow-key feel still need the user's playtest.
See Docs/WorkPackets/MINI-156.md and Docs/Systems/MapGeneration.md.

## MINI-155 — arrow keys now pan/tilt the camera like the mouse (2026-09-07)

`GameInput.Move` reads WASD directly instead of Unity's default Horizontal/
Vertical axes (which also bound arrow keys), and `GameInput.Look` adds a new
arrow-key vector on top of the existing mouse delta with the same sign
convention. Arrow keys no longer walk the character; held Right/Left/Up/Down
now pans/tilts the camera the same way moving the mouse does. No
`ProjectSettings/InputManager.asset` edit; vehicle throttle/steer (still
reads the raw axis directly) and the camera's own mouse-look code are both
untouched and unaffected — riding still uses arrow keys to steer, and the
camera ignores look input entirely while orbit-locked during a ride, so
there's no conflict. Compile-clean. New `Docs/Systems/Camera.md` ledger.
Held-arrow pan/tilt rate/feel still needs the user's hands-on check. See
Docs/WorkPackets/MINI-155.md.

## MINI-154 — shirt/pants: real reshaped preview from existing mesh (Franki, corrected), motion proof blocked by environment (2026-09-07)

Investigated the actual playable meshes two ways: raw Blender re-import was
non-deterministic (do not trust it), so switched to a Unity batch-mode
`SkinnedMeshRenderer` dump against the LIVE scene, which is authoritative
and also caught a real bug: **`Sacat` → `Mainchar.fbx`/`Ch06` (one fused
mesh) and `Franki` → `Strong.fbx`/`Ch28_*` (six separate mesh objects)** —
backwards from a stale code comment this session initially trusted. So
everything below built as "Sacat" this session is actually **Franki's**
garment. For Franki: edited the existing already-skinned `Ch28_Hoody`/
`Ch28_Pants` topology directly (short-sleeve + hood/collar removed; jogger
ankle cuff relaxed) instead of building new isolated geometry, avoiding
MINI-150's shoulder-gap/cylindrical-sleeve/exposed-hem failures because the
result is a subset of one already-continuous, already-correctly-skinned
surface. Real renders (not concept art):
`Logs/Tasks/MINI-154/Reshaped-Front/Back/ThreeQuarter.png` vs.
`Baseline-Front/Back.png`. Built a Unity motion-proof tool (idle/walk/run,
`Up Iz Up Mini/MINI-154/Garment Motion Proof` menu) that runs clean and
does not touch the canonical scene, but headless `-nographics` screenshot
capture produces blank output in this environment (proved with a plain
cube through the same code path) — needs to run inside an open Unity
Editor for real screenshots. Still owed: user approval of the silhouette,
Sacat (different method required — fused mesh), motion proof, Unity
gameplay integration, colour. Accessories untouched per explicit user
instruction — still rejected, not addressed. See Docs/WorkPackets/MINI-154.md
and Characters ledger.

## MINI-153 — shirt/pants request; prototype rejected at fit gate

User says accessories remain wrong but explicitly wants to move on: shirt and pants only, Claude handles rest. Polo-Front.png from MINI-150 inspected and rejected for shoulder gaps/hem exposure. No fitted production pants yet; no garment integration or new EXE. Current EXE is MINI-152. Read Docs/WorkPackets/MINI-153.md for exact blockers and next steps. Do not call current visual wardrobe a clothing swap system; only accessory trials work.

## MINI-152 — watch lowered; accessory placement test build

Watch moved toward hand per explicit user correction; original profile size/rotation untouched. Head prototype offsets corrected for bone scale. EXE rebuilt; screenshot Logs/Tasks/MINI-151/Wardrobe-WristFix.png inspected. Franki and moving fit still need playtest. Original fused hat remains beneath prototype: full replacement unfinished. Next requested scope is shirt and pants only. See WorkPackets/MINI-152.md and Characters ledger.

## MINI-151 — visual wardrobe test build

Home E -> 5 opens visual accessory preview with rotation and Apply/Cancel. Trials remain per-character and session-only; no purchases needed. New shirts/pants/hat/90/97 footwear and colours remain IN PRODUCTION, not implemented. Approved watch/chain assets untouched. See WorkPackets/MINI-151.md and UI ledger. MINI-150 continuous polo prototype is paused outside Assets and not visually approved.

## MINI-149 — free accessory try-on foundation; clothing unfinished

Home E -> 5 Wardrobe -> 6 Free try-on: 1 tries selected existing watch/cap/shades, 3 next, 2 clears trials. Session only, no purchase/save grants, separate protagonists. New approved shirt/pants/hat/90/97 shoes and per-item colours are NOT implemented; MINI-148 rejected shell remains outside game. User now authorizes autonomous implementation and testing clothes without purchase. Next actual garment production, not repeated approval requests. See WorkPackets/MINI-149.md. Owner released.

## MINI-148 — first polo prototype rejected; game unchanged

Isolated Blender body-surface shell failed visual review (patchy/clipped topology after decimation). No playable integration or EXE change. Logs/Tasks/MINI-148 contains diagnostic renders; these are NOT approved garment screenshots. Next clean continuous polo topology with real armholes/hem/collar, correct bind-space transfer and covered-body masking, then motion proof. Do not repeat the scan-shell shortcut. See WorkPackets/MINI-148.md. Owner released.

## MINI-147 — wardrobe design direction approved (2026-09-07)

User approved shirts, pants and hat, replacing original court shoes with Air Max 90/97-inspired Mike options; footwear sheet approved "yes that's it". References: Logs/Tasks/MINI-147/Wardrobe-Concept-v1.png (exclude old trainers) and Footwear-90-97-Concept.png. Selectable colours remain per item/per character. Concept-only; no body/game/EXE changes. Next fitted shirt proof, not more concept redesign. See WorkPackets/MINI-147.md final approval. Owner released.

## MINI-147 — shirt/pants/hat/shoes concept awaiting approval (2026-09-07)

User accepts watch/home wardrobe as good for now and requests shirt, pants, hat and shoes with individual colours, Nike/Lacoste style. Clothing-only concept shown from built-in imagegen: Logs/Tasks/MINI-147/Wardrobe-Concept-v1.png. Includes polo/tee, jeans/shorts/trousers, cap and trainers; Mike/Lacos fictional labels retained from earlier brief. No game, mesh, save or EXE changes. Next: approve style then first fitted shirt/motion proof, resolving modular-body Unity deformation gate before any body swap. Details Docs/WorkPackets/MINI-147.md. Owner released.

## MINI-146 — approved watch integrated and Windows build ready (2026-09-07)

User approved whole-wrist fitting with "yes perfect". Existing per-character watch purchase now uses the approved mobile prefab and exact protected fits. Owned safehouse: E, 5 Wardrobe, 1 Wear / 2 Remove; rest/save afterward. Ownership and wearing remain separate per Sacat/Franki; missing old-save fields default to owned items worn. Chains, watch assets and every scene transform preserved. Focused purchase/resale/home/save-JSON checks pass, build 398087331 bytes, 12-second headless startup survives with no error/exception matches. Purchased-watch screenshots inspected; actual menu/save/reload and moving wrist clipping require user playtest. Full clothing catalogue and base-body repair remain future work. See Docs/WorkPackets/MINI-146.md. Owner released.

## MINI-145 round watch wrist previews ready (2026-09-07)

Latest clarification supersedes dial-only interpretation: user meant entire watch around wrist. Both profiles now orbit90 degrees around forearm, size/axial position unchanged. Side.png screenshots show result; Logs/MINI-145-whole-wrist-turn.log passes. No gameplay integration yet; revised fitting awaits confirmation.

Latest user revision: design accepted; case/dial/crown now90 degrees clockwise, crown at right. Both fit profiles untouched. Updated close-ups and successful repeat check in Logs/MINI-145-watch-rotation.log. No shop/EXE integration yet.

User requested fully round case and mobile optimization, shown on both actual protagonists. Isolated prefab/scene and separate fit profiles now exist; 1644/788 triangles, one material, two32x8 maps. Unity compile and36 attachment samples per character pass; canonical scene hash unchanged. Screenshots under Logs/Tasks/MINI-145 show the watch OVER the original sleeve cuffs. User approval pending; no gameplay/chain/shop/EXE changes. Next connect approved watch to watch_rollie/CharacterEquipment, not a new economy. Wardrobe/clothes/base-body repair still unfinished. See Docs/WorkPackets/MINI-145.md. Owner released.

## MINI-144 wardrobe production resumed; isolated watch preview (2026-09-07)

User confirms MINI-143 works well. Wardrobe next: match original clothes quality, one polo/jeans/trainers/cap/gold-watch plus untouched chain; individual saved outfits and changing at home. Original Blender gold-watch preview is ready for approval in Logs/Tasks/MINI-144/Gold-Watch-Preview.png. No gameplay/assets/EXE changes. Base-body audit found no unweighted mobile vertices and similar Blender bone perturbations; Unity tearing remains unresolved and requires actual rig-binding/motion comparison. Preview watch is not mobile-ready yet (29.8k triangles); optimize only after design approval. Docs/WorkPackets/MINI-144.md is the current continuation; older pause instructions are superseded by this user request, but no body swap is approved. Owner released.

## MINI-143 kick/contact and Dog Life activation fixes built (2026-09-07)

Kick-only walking/turning/jumping lock now covers full kick/recovery. Existing kick clips sampled on real rigs: peak extension Sacat0.794s and Franki0.680s; damage windows corrected from premature0.22s. Attacks retry contact during their active window but deal damage only once; original damage/Franki multiplier retained. Dog Life's first inactive reset previously copied an uninitialized zero homeScale, hiding members; initialization now precedes reset. Four members verified visible at unchanged positions, with no chest/house-box overlaps. Patrols yield during ragdoll/disabled-controller states. Focused combat/gang and MINI132 regression pass; Windows build397918803bytes and12s smoke:0 exceptions,0 inactive-controller warnings. Scene/art/vehicles unchanged. Normal-speed combat and actual block arrival still need user playtest. Evidence/workflow: Docs/WorkPackets/MINI-143.md.

## MINI-142 house/crop art and local road repair applied; Windows build ready (2026-09-06)

After the Unity house screenshot, the user requested "continue and try to build exe before tokens expire". Promoted the additive review to GrandBayProof.unity with an exact original-scene backup hash guard; 281 protected gameplay transforms unchanged. Added 86 generic house exteriors with LODs, 423 grass tufts, carrot/banana and baked green/purple buds across 14 plots (seven existing illegal strain IDs retained). Crop-stage checks and 219 local road lane samples pass. Windows build succeeded (397,915,219 bytes), fresh level0 2026-09-06; path Builds/GrandBayProof/UpIzUpMini.exe. Review copy and immutable backup retained. Real driving, harvesting/LOD appearance and phone performance still need playtest/device profiling. See Docs/WorkPackets/MINI-142.md; never rerun the full scene generator.

## MINI-141 house/grass/crop previews awaiting visual approval (2026-09-05)
Smoke caveat for MINI-142:15s built-player startup remained running with no fatal/exception matches, but inactive CharacterController.Move warnings repeated heavily. Record in WorkPackets/MINI-142.md; needs NPC diagnostic, not claimed clean runtime.


Actual Blender model previews and sources are in Logs/Tasks/MINI-141 (houses/grass and carrot/banana/cannabis sheets). No Assets, scenes, prefab, controls or EXE changes. User requested images before integration. Geometry budgets measured, but production mesh consolidation, LODs, materials, colliders, growth-stage wiring and phone profiling are still owed. Road issue located by user near Dog Life on Lalay: evidence suggests legacy live road differs from isolated MB proof; exact live gap still requires capture/raycast inspection. Packet and reusable generator: Docs/WorkPackets/MINI-141.md, Tools/ArtPreview/mini141_preview.py. Owner released; next step is the user's visual decision.

## MINI-140 E-to-mount / Q-to-wheelie control remap ready for playtest (2026-09-04)

Vehicle mount / enter / dismount is now **E** — the same key as every other world
interaction — for the TMAX, SuperMoto and Range Rover, routed through the normal
`InteractionDetector` path while each vehicle keeps its own wider mount reach. The
bike **wheelie is Q** (TMAX `wheelieKey`, and `SuperMotoWheelieKeyRemap` now reads
Q only, E removed). **F** is the melee Attack key only and is no longer read by any
vehicle. The phone's **Q** call-partner action is suppressed while the active
character is mounted (`PlayerController.IsControlled`), so a Q tap mid-wheelie
never summons anyone; on foot it is unchanged. The E press that mounts is guarded
so it cannot also dismount the same frame. Two prefabs (`TMAX_560`,
`RangeRover_Vehicle`) had their serialized key codes updated — input bindings only,
no mesh / transform / seat / effect / placement change. Focused MINI-140 plus
standing MINI-065 / MINI-139 / MINI-137 validations pass; Windows build succeeded
and a 15-second startup smoke stayed alive with no exceptions. Control feel is
hands-on only and still needs the user's ride test.
## MINI-139 TMAX 12 mph wheelie floor ready for playtest (2026-09-01)

The TMAX now requires at least 12 mph (19.31 km/h) both to begin and to sustain a wheelie. If speed drops below the floor while raised, the controller selects its existing zero-degree target and lowers the front at the already-approved recovery rate; it does not snap down. Rear-wheel ground chatter remains excluded from the sustain gate, preserving the proven deep-wheelie behavior. The shared 89-degree cap, crash filter, handling, rider animation, sparks, exhaust, wheels, scene and prefab are unchanged. Focused MINI-139 and standing MINI-138 validations pass; Windows build and 12-second startup smoke pass. Live lowering feel remains for the user's test.
## MINI-138 wheelie crash sensitivity corrected, ready for playtest (2026-08-31)

Wheelie-mode collisions no longer eject from rotational tail/rear-body contact alone. While either bike is above 8 degrees of active wheelie, ejection now requires a 15% harder contact plus real Rigidbody travel speed of at least 16.5 m/s (59.4 km/h); therefore a 12 mph wheelie scrape cannot throw the rider even if Unity reports a large rotation-driven relative velocity. Upright crash thresholds, NPC impacts, the 89-degree cap, handling, animation, sparks and placement are unchanged. The focused collision regression and Windows build pass; the rebuilt player survived the 12-second startup smoke. Live wheelie contact feel still requires the user's playtest.
## MINI-137 TMAX wheelie effects and SuperMoto lift pose ready for playtest (2026-08-31)

TMAX exhaust now emits roughly half as often, uses smaller puffs, caps at 24 particles and peaks at 22% opacity. A separate rear-underside spark effect is capped at 20 short-lived particles and activates only while ridden at the shared 89-degree wheelie cap and at or above 12 mph (19.31 km/h). The TMAX rider temporarily uses the same 0.22 authored wheelie-lift blend proven on the in-game SuperMoto, restoring the character's previous value on dismount. Handling, wheels, seats, IK, pillion, crash logic, scene and prefab placement are unchanged. Focused MINI-137 and standing MINI-136 validations pass; Windows build succeeds and remained alive through a 12-second startup smoke. Spark placement, exhaust taste and rider motion still require the user's live playtest.


## MINI-136 89-degree wheelie cap and collision-only crash ready for playtest (2026-08-31)

TMAX and SuperMoto now share a hard 89-degree commanded-wheelie ceiling. Runtime clamps prevent prefab or tuning values from requesting more than 89 degrees. Bike tilt/wheelie angle no longer triggers rider ejection or a backward kick; only MINI-135's filtered hard physical collision path can eject driver and pillion. Steering, suspension, wheel mapping, rider fit, scene placement and impact thresholds are unchanged. Focused MINI-136 validation, standing MINI-132 regression, Windows rebuild and a 12-second startup smoke pass. Real wheelie and hard-collision feel still require the user's playtest.

## MINI-135 crash sensitivity and seller-hit correction ready for playtest (2026-08-31)

Playtest feedback from MINI-134 is addressed. Hard collision ejection now requires 16.5 m/s of closing speed into a non-ground surface instead of raw collision magnitude, upward-facing road/ground contacts are ignored, glancing scrapes use only the normal component, and NPC contact has an even higher 24.75 m/s ejection threshold. Ninety degrees is the balance point; riders remain attached through 95 degrees, then the bike receives a backward angular kick and both driver/pillion eject through the existing ragdoll path. Stationary sellers now receive an idempotent CharacterController hit body at runtime, allowing vehicle collisions/triggers to reach the same protected health/recovery path as police without regenerating or moving the scene. MINI-135 focused validation, MINI-132 regression, Windows rebuild, and final startup smoke pass. Real riding is still required to approve sensitivity and fall-back motion.


## MINI-134 crash, damage, and recovery pass ready for playtest (2026-08-30)

TMAX and SuperMoto now share a hard-crash rider-ejection controller: solid impacts at 11.5 m/s or an over-balance wheelie beyond 92 degrees release the driver and pillion into a temporary physics ragdoll, then recover only after a minimum lie time and physical settling. Normal NPC contact requires a higher threshold before ejection. TMAX, SuperMoto and cars also share lightweight collision durability with automatic upright recovery, while NPC vehicle-hit momentum is reduced/capped and nonfatal NPC ragdolls wait for settling before standing from their landing location. MINI-134 focused validation, the standing MINI-132 regression, Unity compile, Windows rebuild and a 12-second player startup smoke pass. Live crashing is still required to approve ejection reliability, impact strength, get-up timing and vehicle recovery feel.


## MINI-133 Range Rover wheel and road-effects polish ready for playtest (2026-08-30)

The driveable Range Rover now has four explicitly mapped visual wheels over the source model's fused wheels. All four follow their matching WheelCollider for suspension and spin, while only the front pair follow steering. Five dark-metal spokes per wheel make rotation readable without changing the verified 2,500 kg physics, wheel colliders, handling, spawn or scene placement. Twin rear tyres produce short marks and small smoke only while braking/slipping above 7 km/h; a separate right-rear tailpipe produces bounded exhaust only while driven. Focused prefab validation and Unity compile pass. Front/rear screenshots are in `Logs/Tasks/MINI-133`; live driving is still required to approve wheel fit, rotation, steering direction and effect strength. No Windows player was rebuilt in this packet.

## MINI-132 shared impact combat ready for live playtest (2026-08-30)

Every TMAX, SuperMoto and car now routes real Rigidbody collision speed/contact through one `VehicleImpactResponder` into `NpcCombatHealth` and the existing SuperMoto-derived humanoid ragdoll. Existing shop sellers and named interactables are upgraded lazily on first impact, always recover even from a heavy hit, align their animated root to the fallen body, then walk back to their exact captured post. Ambient villagers/police use a 100-second fade/return policy; Dog Life keeps its proven external pool. Franki's entire five-move chain now deals 25% more damage and 30% more impact than Sacat, while police and gangs use separate three-move Mixamo combinations with lower damage and slower timing. MINI-132 validation, Unity compile and Windows build pass; a short standalone smoke contained no exception. Vehicle-hit direction/feel and seller get-up/walk-back still require the user's live playtest. Rider ejection on wall crash/over-vertical wheelie remains the next separate extension of MINI-132.

## MINI-131 TMAX wheel lock and restrained rider lean ready for playtest (2026-08-30)

TMAX wheel visuals now rebuild their positions from immutable authored local hub points in a final late frame pass, rather than reading their last world position back as the next baseline. A focused validator deliberately displaced both wheels 250 times and confirmed zero planar drift after correction. The rider now needs a stronger turn before the authored lean starts, plays it only as a 0.38-second accent, returns to neutral faster, follows at most 52% of cosmetic bike roll, and is capped at 9 degrees of procedural roll. The exhaust outlet was also moved slightly lower and farther to the right-rear at the user's request. Focused MINI-131 plus standing MINI-065/124/127 checks pass, the Windows player is fresh, and a 12-second startup smoke remained running with no exception. User must confirm wheel stability and rider feel during a sustained ride with repeated turns. Next requested combat packet: give gang/police multiple combinations while keeping them slower and weaker than Sacat/Franki.

## MINI-129/130 TMAX rider fit and Dog Life respawn repaired (2026-08-30)

The TMAX main rider is moved 0.12m farther forward in both normal riding and wheelie keyframes; approved height, side placement and pitch are unchanged, and the pillion is untouched. The Dog Life respawn failure was traced to `NpcCombatHealth.ResetForRespawn` using stale/uninitialized cached renderers while pooled members were inactive: it now refreshes runtime references at the respawn boundary and safely ignores replaced renderers. The focused TMAX and Dog Life validators pass, a fresh Windows build succeeded, and a 12-second built-player smoke run contains no `NpcCombatHealth.ResetForRespawn`/NullReference exception. User should visually confirm the new rider position and defeat/wait/respawn one Dog Life member in normal play.

## MINI-128 TMAX parking, road effects and pillion alignment ready for playtest (2026-08-30)

The temporary TMAX now stays locked at the exact Lalay midpoint between the Farm Shop and Produce Buyer until mounted, with an on-screen location alert and the existing orange vehicle minimap marker. While ridden it emits a bounded mobile-conscious exhaust puff from the right rear and reuses the installed SuperMoto tread/smoke art only during rear-wheel braking or measured slip. The pillion keeps the approved seat height/offset but now uses a neutral forward-facing anchor and the stable `RideBike` pose instead of the rotated cheer-tail pose. Focused validation, the standing TMAX checks and a fresh Windows build pass. A live player inspection confirmed the parked spawn, successful movement, visible right-rear smoke, black road marks and two forward-facing riders. The existing `NpcCombatHealth.ResetForRespawn` gang exception remains unrelated. User should now confirm sustained riding feel and whether the smoke/mark amount looks right during normal play.

## MINI-127 purchasable TMAX controls repaired, awaiting ride feel confirmation (2026-08-30)

The spawned and Car Dealer-purchased TMAX are the same `TMAX_560.prefab`; no second better purchasable model was hidden in the project. The ride failure came from `BikeInteractable` feeding an incomplete vendor controller whose wheel arrays and steering references are unwired, while the prefab's original `TmaxBikeControllerCustom` remains fully wired. Mount input now feeds that custom controller, incompatible vendor behaviours are disabled immediately on spawn/purchase, the effective F-mount radius is 4.25m, and the saved `StockDemoSuperMoto` comparison bike is hidden only during the temporary TMAX test so the two bikes no longer overlap. Focused validation and Windows build pass; an 18-second player smoke run logs the repair and has no `RB_Controller.HandleSteering` or `NitrousManager` exception. The pre-existing `NpcCombatHealth.ResetForRespawn` gang error remains unrelated. User must confirm W/S/A/D, Space brake and E wheelie feel in the rebuilt player.

## MINI-126 market-road TMAX build ready (2026-08-30)

The temporary TMAX test spawn now uses the real midpoint between `Stall_FARM SHOP` and `Stall_PRODUCE BUYER`, ground-snaps onto the Lalay road, and faces perpendicular to the stall-to-stall line so the bike is aligned along the road. A fresh Windows player was built at `Builds/GrandBayProof/UpIzUpMini.exe`; `level0` and `Assembly-CSharp.dll` are newer than the spawn code, and a 15-second smoke run logged the TMAX at `(-8.00, 10.46, -152.09)` and remained running. The smoke log also contains existing repeated `NpcCombatHealth.ResetForRespawn` and vendor `Gadd420.NitrousManager` exceptions; neither stack includes the TMAX spawn path, but they remain a separate runtime-cleanup item. User must confirm the bike's visible placement and ride in the real build.

## MINI-125 TMAX test spawn ready (2026-08-30)

The upgraded MINI-124 TMAX now spawns once beside the active character at runtime for immediate inspection and riding. This temporary path spawns only the TMAX, leaves Sacat/Franki on foot, uses the normal walk-up/F mount flow, adds the vehicle minimap marker, does not write ownership/save data, and does not spawn the Range Rover, SuperMoto, or stock demo bike. Unity compile and the standing MINI-064/065 prefab/scene validator pass. Real spawn position, wheel motion, steering and rider-hand following still require the user's Play Mode check.

## MINI-124 TMAX wheel and steering upgrade evidence ready (2026-08-30)

The TMAX prefab now carries fitted black SuperMoto-reference wheel meshes: both spin from the existing WheelCollider poses and the front tyre steers. A separate visual fork/handlebar pivot follows the same steering angle, while Sacat/Franki's established root-level hand targets are driven from that pivot without changing their stable prefab paths. Physics is unchanged (480kg; wheel radii `0.30352196`); focused validation, the original MINI-064/065 validator and the real-physics drop/drive/wheelie regression pass. Before/after screenshots and a two-second motion proof are in `Logs/Tasks/MINI-124`. The original scan's old wheels/handlebar remain fused into its body under the new moving overlay; user visual approval and a real ride/hand-follow playtest are still required before an EXE build or visual lock.

## MINI-123 complete isolated MB-road network proof, awaiting full-network visual approval (2026-08-29)

The reversible MB Road System evaluation now covers the two approved Lalay spline roads plus all nine remaining phase-one roads in `MapLab_MBRoad_LalayHighlandProof.unity`. The remaining network totals 815 road-mesh vertices, uses line-free 6.2m paved roads and one 4.8m dirt Highland farm spur, generates no sidewalks outside the Lalay main street, and retains all nine replaced ribbons disabled for rollback. Both Highland–Lalay bridge ends use terrain-aware, collidable transitions and were approved by the user as VA-008 (“looks good for now so go ahead”). Unity focused validation and the approved-graybox map gate pass; `GrandBayProof.unity` remains untouched. Next gate: user reviews `Logs/Tasks/MINI-123/MBRoad-Complete-Remaining-Network-Overhead-1280x720.png`; only after approval should a separate bounded live-migration/vehicle-test packet be claimed.

## MINI-119 follow-up chain: NPC ragdoll, Koss purchase, world population, zone/tuning fixes (2026-08-28)

Long single-session continuation directly after the SuperMoto-mounting entry below. Full detail and every commit's exact reasoning: `git log --grep=MINI-119`; a compact index is in `PROJECT-HANDOFF.md`'s matching entry. Summary of what shipped:

- **NPC ragdoll-on-hit**: Police/Villager/Gang NPCs now ragdoll on every landed hit — fatal hits lie 3s then scale-fade and deactivate, non-fatal hits lie 2s then recover and resume walking. Shopkeepers/dealers/mission NPCs unchanged (no ragdoll ever attached).
- **Koss bike purchase**: the SuperMoto is now buyable at the Car Dealer for $2,500 (cheapest vehicle) via the exact same wiring the dev spawn already uses (extracted into a shared `WireSuperMotoInstance` helper, not duplicated) — parked, walk up, press F.
- **World population + zone boundary**: 7 new Villager NPCs added along Lalay/Highland. The Lalay/Highland `AreaNameDisplay` zone boundary went through three user-directed corrections before landing on its final form: Lalay spans the Dog Life block to the Car Dealer; Highland is a tight circle on the farm/safehouse cluster starting at the bridge. Verified point-by-point against real landmark coordinates via `AreaNameDisplay.ResolveArea`, not hand math.
- **Farm hedge containment fix**: a newly-added villager was confirmed (via a new geometric containment check) standing inside the Highland farm's hedge enclosure — moved outside to the entrance gap.
- **Wheelie back-clip damping**: measured (not guessed) that the wheelie pose's position/pitch math isn't the cause of the reported back-through-handlebar clipping — it's the authored wheelie-overlay animation clip. Dialed its blend weight down from 0.45 to 0.22 while mounted on the SuperMoto specifically (restored on dismount, since the component is shared with the TMAX). Still needs the user's visual confirmation on the exact number.
- **Auto-mount disabled**: `AutoMountSuperMotoOnSpawn` is off again per the user's request now that mounting/riding is proven — the dev bike spawn itself stays on for future testing.
- **Not yet started**: combat mechanics. User wants "fighting first, then shooting after"; narrowed to fixing the existing warped-punch-pose/over-generous-hit-detection bugs first (not new combos). Recommended a paid, real-mocap unarmed/boxing animation pack over a free one (see PROJECT-HANDOFF.md entry for the specific recommendation) — no purchase made yet, needs the user's approval.

Every item above was verified via a dedicated Unity batch-mode check against the real `GrandBayProof.unity` scene before committing, and a fresh Windows build was produced after each meaningfully-testable change. **Not yet hands-on played** by the user except via their own live direction mid-session (the zone corrections, the wheelie-damping approach) — this is the next thing needed before anything else starts.

## MINI-119 follow-up: SuperMoto rider mounting confirmed working end-to-end, user hands-on verified (2026-08-28)

Sacat now mounts, rides, and wheelies the SuperMoto with hands/feet genuinely tracking the handlebar/pegs through the whole motion — user-confirmed "works perfect" after real Play Mode testing. Got there via `VehicleRider`/`VehicleSeat`, the same proven system the TMAX (MINI-066) already used, after two other approaches (a runtime-built ragdoll, then a runtime-built Animation Rigging graph) each hit recurring, hard-to-reproduce failures. The real, final blocker for hand/foot tracking specifically was a single root cause misdiagnosed as an "Editor batch-mode Avatar-binding limitation" **four separate times earlier in this same task**: `VehicleRider` had `[RequireComponent(typeof(Animator))]` plus a same-object-only `GetComponent<Animator>()`, but Sacat's real Animator lives on a child (`Visual`), not the root — so Unity silently auto-created a second, permanently blank Animator (no avatar, no controller) on the root the instant the component was added, and every hand-IK system since (Mecanim `OnAnimatorIK`, then a direct bone-override) was talking to that dead ghost. Found by the user's own live Inspector check (not batch tooling), fixed by removing the `RequireComponent` and switching to `GetComponentInChildren<Animator>()`; the already-baked ghost component was cleaned out of the saved scene. Full process (seat/pose baking, wheelie-blend wiring, the hand/foot lock technique, and this exact Animator gotcha) is written up in `Docs/VEHICLE-INTEGRATION-WORKFLOW.md` for reuse on the next vehicle. Windows build succeeded; user tested the real exe, not just Editor Play Mode.

## MINI-119 follow-up: SuperMoto revert + re-integration + roll-corrector, awaiting playtest (2026-08-24)

The Motorbike Physics Tool integration was fully reverted mid-session at the user's explicit request after repeated real-play failures despite passing batch tests, then re-integrated with a deliberately narrower scope: SuperMoto's own vendor riding physics for normal control, TMAX's own already-proven kinematic `ApplyWheelie`/`ApplyTrikeStabilizers` ported over just for the wheelie (not reinvented against the vendor asset a third time), and a new, separate continuous roll-corrector (`SuperMotoUprightAssist`) that returns the bike to 0 degrees of lean during normal riding - the user's own explicit ask, kept intentionally apart from the wheelie system. Three real bugs were found and fixed along the way: the E key never reaching the wheelie at all (a `RequireComponent`-blocked `Input_Manager` destroy that Unity silently no-op'd, found from the user's own `Player.log`, not a batch test); a lean-torque operator-precedence bug in the vendor's own `RB_Controller` (the same class of bug already fixed once elsewhere) that silently stopped player steering from correcting a lean past 45deg; and a self-reinforcing stuck-pitch loop where a fold-back-prone `asin` pitch measurement was getting baked directly into the bike's real rotation every frame. Two torque-based attempts at the roll-corrector were built and rejected after real testing (one resonated into a violent tumble, one was too weak and let the lean creep) before landing on a kinematic `MoveRotation`-based approach with no gain to tune and nothing to resonate. Full batch verification (real-scene wheelie, real measured front-wheel lift, at-rest stability, ledge-hit, ramp climb, ramp+turn, auto-recover, the original TMAX bike) all pass; Windows build succeeded each round. See the MINI-119 follow-up entry in `PROJECT-HANDOFF.md` for full detail. **Honest limitation**: all of this round's verification is batch-mode - this exact task has already proven batch tests can pass while real gameplay fails (that's how the Input_Manager and world-axis-constraint bugs were found). The user has hands-on confirmed the wheelie works and that the pitch-lock fix addressed what they were seeing, but has not yet given a full end-to-end sign-off on this exact build (`5004058`).

## MINI-119 follow-up complete: bike tuning + road/terrain smoothing (2026-08-22)

Went through several live, hands-on rounds tuning the bike's anti-spin behaviour directly with the user in the real game scene (via a temporarily-shipped tuner panel). The actual working fix turned out to be a full architecture change for yaw: it's no longer physics-derived at all - it's now a direct, kinematic function of the player's own steering input, the same technique already proven for the wheelie's pitch. Final tuned numbers reported and baked into `TmaxBikeController`'s defaults; the dev tuner panel is off again. The user also manually repositioned the farm hedge/plots/safehouse directly in `GrandBayProof.unity` and asked for that to survive future rebuilds - it now does, via a read-only capture tool plus a permanent replay step in the migration script, so `BuildScene` can be re-run freely without losing it. Separately fixed the terrain/road smoothness complaint ("no 90 degree ledges"): added real ramp geometry between road and sidewalk (previously a bare, uneven terrain gap), and while investigating a reported broken road junction, found and fixed a genuine, previously-undiscovered bug where the map generator's own gap-sealing logic could never actually seal anything (a self-contradictory threshold check) - now fixed, with one real road gap sealed and two ribbon-edge junction mismatches patched. Full rebuild pipeline, all 8 standing validators, and the drop test all re-run clean; Windows build succeeded. Not yet hands-on driven since this exact pass.

## MINI-119 complete, awaiting playtest (2026-08-22)

Claude fixed a full batch of post-playtest reports in one pass (see `Docs/WorkPackets/MINI-119.md` for the full writeup). Dialogue truncation on the boss NPCs traced to a background-panel height cap (330px, not hold duration - the user explicitly corrected an initial wrong diagnosis of that), raised to 620px. The Boat Man's most common repeat line ("nothing to load") now rotates instead of always repeating. A villager's stale, pre-migration patrol route (pointed at the old map's mountain) fixed generically for every patrol-enabled NPC placed via `PlaceRoadsideNpc`, not just this one case. Rasta now sells Blue Cheese seed directly ($650/3 seeds) strictly after his own teaching mission unlocks it, themed on the user's own suggested line. Blue Cheese/Purple Sugar's plant-selection number keys swapped to match Rasta's real teaching order (7=Blue Cheese, 8=Purple Sugar); added a minimap marker for the breeding station; extended per-crop hints to every Rasta-taught base strain, not just the three hybrids. War Story (M16) now requires a real `DefeatAllRivals` objective (every Dog Life member in the block knocked out), not just walking in. The bike's `motorTorque` raised past MINI-118's mass-parity value (which had only preserved the OLD power-to-weight ratio, not actually made hills easier); added a slope-proportional hill-climb assist and a yaw-spin damping assist that only clips the portion of spin above a real threshold, so a hedge/ledge hit still spins - just far less wildly - while ordinary steering yaw is untouched. Cheat `000000` now also grants Normy reputation, matching every other faction. A new focused validator, every standing validator (058/065/109/110/111/112/113), and the MINI-065 real-physics drop test all re-run clean; Windows build succeeded. Manual road-intersection/farm-hedge adjustment instructions were given directly to the user in chat per their own explicit request, not implemented as code. Ownership released. Not yet hands-on playtested.

## MINI-118 complete, awaiting playtest (2026-08-22)

Claude made the TMAX substantially heavier (220kg -> 480kg) to fix "bumps too high on any bump" - confirmed via code reading first that the wheelie (kinematic) and lean/stability (`ForceMode.Acceleration`) systems are already mass-independent by design, so this couldn't touch them. Suspension spring/damper are formula-derived from mass so sag/damping feel stays identical; a fixed-size bump now imparts a proportionally smaller velocity change (Δv = impulse/mass) - the actual physics of "harder to launch." Scaled motor/brake torque by the same ratio since those ARE real mass-dependent forces. Found and compensated a real, measured side effect (wheelie pitch dropped from 85.9deg baseline to 77.4deg; raised `wheelieRiseRate` and re-verified at 89.6deg). Added a second, independent safety net per the user's own suggestion: extra downward force once genuinely airborne for a sustained period, off during an actual wheelie. All vehicle validators pass except one pre-existing, unrelated issue (`Mini069GangWarValidation` - NPC positioning) flagged separately. Windows build and smoke clean. Ownership released. Not yet driven by a human.

## MINI-113 complete, awaiting playtest (2026-08-22)

Claude cleaned up world/navigation issues. Measured every road-junction pair first: only the Highland inroad/farm-spur junction had a real bump (0.286m, fixed to 0.000m). Found a more serious bug along the way: `BikeHomePoint` (the bike's save/load return point) was orphaned 303.52m from the migrated safehouse - fixed to 7.34m, a real save/load correctness bug, not cosmetic. Also corrected an earlier wrong measurement of the farm hedge (a Unity Collider API unreliable on non-convex meshes gave false "on the road" readings; real vertex measurement showed the hedge was always genuinely clear, 0.92-15.21m). Added minimap markers for owned vehicles. All standing validators, Windows build, and headless smoke pass. Ownership released. Not yet hands-on driven/walked.

## MINI-117 complete, awaiting playtest (2026-08-22)

Claude fixed the `Mini058FactionsValidation` failure found during MINI-112 - two real bugs, not one. First, the validator itself never initialized `MissionSystem`, so the recruiter's own mission-reached gate always refused regardless of anything else (the real recruiter path was never actually broken). Fixing that exposed a second, genuine bug: `GangMemberInteractable`'s Chevy recruitment path checked a combined mission+reputation gate before its own more specific reputation check, so that second check's clearer message could never fire - split so each check does its own job. Also added three in-game hints (per the user's direct follow-up) explaining the breeding-station process and plant-selection number key for each hybrid crop (Purple Sugar=7, Sugar Cheese=9, Purple Cheese=0). All standing validators pass, Windows build and a 12s headless smoke test clean. Ownership released. **Five rounds deep without a playtest**: MINI-109 through MINI-112 and now MINI-117 have all shipped back-to-back per the user's own repeated choice to skip the playtest gate each time.

## MINI-112 complete, awaiting playtest (2026-08-22)

Claude built Dog Life's escalation slice. Found and fixed a real bug: `FactionBrawler` and `PatrolNPC` both drove the same `CharacterController` with no coordination, so a chasing/fighting Dog Life member had ambient wandering and combat steering fighting each other, and nothing ever walked them back to the block afterward. A new `FactionBrawler.IsEngaged` flag fixes this - `PatrolNPC` now yields while engaged, which delivers "stop chasing, return to block, resume block behaviour" without new steering code. Added a real ~100s respawn cooldown (`NpcCombatHealth.LastDefeatedAt`, was previously instant-reset on reactivation), a jealousy dialogue line reusing the existing `CropUnlocked` condition, and an occasional two-member group walk down the road. New focused validator, Windows build, and a 12s headless smoke test all pass. Ownership released. **New blocker found, NOT this task's fault**: `Mini058FactionsValidation` now fails on a pre-existing `GangMemberInteractable` reputation-gate issue in a file this packet never touched - needs its own investigation. **Four rounds deep without a playtest**: MINI-109 through MINI-112 have all shipped back-to-back per the user's own repeated choice to skip the playtest gate each time.

## MINI-111 complete, awaiting playtest (2026-08-22)

Claude built Rasta's full strain-mentorship ladder. M13 now asks for 3 Bushers instead of tomatoes; six new missions (M13C2-M13C7) follow immediately, each teaching one tier (Black Sugar -> Purple -> Blue Cheese -> Purple Sugar hybrid -> Sugar Cheese -> Purple Cheese) and unlocking that crop the moment the mission becomes current, via a new `Mission.unlocksCropId` field. This is additive to the pre-existing Boss-exploitation unlock path (`BlackSugarUnlocked` etc., still independently verified working) - and it's the FIRST route to any advanced strain for Legitimate Farmer players, who never see the WeedRoute-only M9W-M11W missions. A real bug was found and fixed along the way: the scene builder's manual mission-copy code had no line for the new field, so it would have silently unlocked nothing at all in the built game despite looking correct in source - caught by the new focused validator. Standing regressions (MINI-108/109/110), Windows build, and a 12s headless smoke test all pass. Evidence under `Logs/Tasks/MINI-111/`. Ownership released. **Three rounds deep without a playtest**: MINI-109, MINI-110 and MINI-111 have all shipped back-to-back per the user's own repeated choice to skip the playtest gate each time - all three now await a single combined human playtest before `MINI-112` (Dog Life escalation) begins.

## MINI-110 complete, awaiting playtest (2026-08-22)

Claude added Normy's item favour (mission M13B, "Small Ting") between M13 and M14 - a new `ObjectiveKind.DeliverItem` requires actually holding a bought `food_bakes` and `pill_energy`, not just visiting the shops. M14's briefing now carries Normy's uncertain street-info hint pointing at the Boat Man/Gardey Zafeh. The Boat Man's first-meeting dialogue is the approved four-line introduction. `HUDController` gained a courier-timer readout, visible only while a Guadeloupe trip is active, reading `GuadeloupeTrade`'s existing lock/hide/timer API (untouched). Found and fixed a real bug along the way: the map migration's marker remapper had no case for `DeliverItem` and would have left Normy's M13B marker stale after migration - same class of bug as MINI-109's Rasta fix. Focused validator, standing regressions (including MINI-109's own), Windows build, and a 12s headless smoke test all pass. Evidence under `Logs/Tasks/MINI-110/`, including a forced-state HUD screenshot. Ownership released. **Note**: the user chose to proceed to MINI-110 without first playtesting MINI-109 - both rounds' fixes now await a human playtest: Clean Face -> Black Sugar delivery -> Rasta (MINI-109), then Normy's favour -> Boat Man intro -> a real Guadeloupe courier run (MINI-110), before `MINI-111` begins.

## MINI-109 complete, awaiting playtest (2026-08-22)

Claude fixed all three MINI-109 defects, each with a distinct confirmed root cause: Normy's Clean Face payment shared the ambient bribe's zero-heat gate (fixed with a separate idempotent mission-payment branch); Boss J's Black Sugar sale gate checked only Bushers count, not any held illegal crop (fixed with `HasAnySellableIllegalStock`); Rasta's M13 marker was baked in by `Mini100GrandBayMapMigration.RemapMissionObjectives()` BEFORE he was moved to his final migrated position (fixed by reordering). Also added a retrospective-completion rule for `BuyItem` objectives (owning an item outright now credits the objective instead of demanding a repeat purchase). Focused validator, standing mission/map regressions, Windows build, and a 12s headless smoke test all pass. Evidence and screenshots are under `Logs/Tasks/MINI-109/`. Ownership released. The user still needs to hands-on playtest: Clean Face -> Black Sugar delivery -> Rasta, per `Docs/CLAUDE-HANDOFF-CURRENT.md` section 10, before `MINI-110` begins.

## Claude handoff — MINI-109 ready (2026-08-21)

The current operational handoff is `Docs/CLAUDE-HANDOFF-CURRENT.md`; the copy-paste startup prompt is `Docs/NEXT-CHATGPT-PLUS-HANDOFF.md`. Claude's first bounded packet is `Docs/WorkPackets/MINI-109.md`: fix the stuck Clean Face/Normy transaction, Black Sugar delivery to Boss J and Rasta's stale mission marker, then stop for the user's playtest. The larger approved sequence—Normy favour/Boat Man timer, Rasta strain ladder, Dog Life escalation, road/hedge/vehicle marker cleanup, Brakes missions, phone unlock and TMAX optimization—is documented but must not be implemented as one uncontrolled batch.

## Current gameplay packet — MINI-108 (2026-08-21)

MINI-108 rebuilds the early playable flow while MINI-107 remains paused. Every marked mission now uses one universal blinking yellow minimap objective that clamps to the radar edge when distant. The opening dialogue has a dynamically sized transparent black background. The early choice now lets K go directly to Boss J or L play a short increasingly frustrating legal-farming branch before both rejoin Boss J. The broken cooldown was replaced by a Highland-safehouse rest mission and a clean-face mission requiring two different regular officers plus Normy's $100/20%-heat service. Map/mission/focused gates, the Windows build and a 12-second built-player headless startup pass. Hands-on UI timing and full mission progression remain for the user. The temporary procedural banana is frozen; replace it later with an approved Hitem3D production asset.

## Paused character work — MINI-107 (2026-08-21)

MINI-107 is paused at the user's request while they playtest the game. The reusable character manifest/workflow/tooling is in place. The approved 25k/12k/4.5k Sacat LODs pass static checks but fail motion deformation; they are not approved for playable integration. The original 100K Sacat rig animates correctly. Resume from `Docs/WorkPackets/MINI-107.md` and repair LOD topology/weight transfer before any character swap.

Read this at the start of every task. `PROJECT-HANDOFF.md` remains the full audit trail; search it for the active task and the systems you will touch rather than loading all completed history into every session.

## Project

- Path: `E:\Unity\Up Iz Up Mini`
- Unity: `6000.3.10f1`
- Full-size reference project: `E:\Unity\Up iz up` (read-only)
- Current production state: systems 1–3 are integrated, MINI-108 through the full MINI-119 chain (SuperMoto riding/wheelie/pillion, NPC ragdoll-on-hit, Koss bike purchase, expanded Lalay/Highland population and zone boundary, wheelie-clip damping) are built and batch-verified. Hands-on acceptance of this session's MINI-119 follow-up work is the immediate remaining gap - see the top entry in this file.
- Current task/owner: authoritative only in the `### Current claim` block of `PROJECT-HANDOFF.md` (currently `None` - MINI-119's follow-up chain was released after this session).
- Last known good gameplay checkpoint: `3adb5c0` (`MINI-093` police melee retaliation; focused validation passed, no new Windows build).

## What exists

- Switchable Sacat and Franki, locomotion/jump/stamina/health, inventory/economy/save-load.
- Farming, crop progression/cloning, plots/land, shops/apparel/accessories, food/pharmacy.
- Missions, dialogue/dialect foundation, bosses, reputation, gangs, police/heat, safehouses, Guadeloupe abstraction.
- TMAX riding/wheelie/pillion and Range Rover driving/passengers, radio, interaction prompts.
- NPC patrol/navigation foundations, melee/hit reaction foundations, recruitment/following.
- Fixed-camera editor snapshots, many feature validators, Windows builds, and headless built-player smoke tests.

## Sources of truth

- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` generates `GrandBayProof.unity`. Manual scene edits are overwritten.
- `GrandBayProof.unity` now uses the approved sourced/compressed Lalay-to-Highland phase-one world: ten connected collidable road ribbons including the south Backstreet, four bridge groups, dense Lalay housing, 18 Highland homes (including five small apartments), a church/sand/jetty coast, one gameplay farm and seven inactive future parcel anchors.
- `Docs/MAP-ANCHORS.json` and compact Unity map/height data now exist. `VA-004` locks the source road/street target and `VA-005` locks the corrected migration graybox; the untouched map-lab and commit `aed8854` are the rollback backups.
- `Docs/WORLD-EXPANSION-WORKFLOW.md` is mandatory for all map work. The first live manifest is `dm-dom-grand-bay-lalay-highland-v1`, currently at `graybox`; it cannot advance until a corrected player-height Lalay view is approved.
- Locomotion uses the authored StarterAssets controller and `MotionSpeed`; do not regenerate it casually.
- `HumanoidAnimationManager` is the reusable action-layer foundation.
- Bike seating already uses `VehicleSeat`, `VehicleRider`, `BikeRiderAnimation`, and Humanoid IK. Improve profiles/clips/gates rather than starting over.

## Highest risks

1. Visual acceptance: Sacat's two-piece chain placement is locked as `VA-002`; its visibility/swing while walking still needs the user's playtest. Most other movement and appearance remain unapproved.
2. Visual debt: many changes compile or pass harnesses but have not been watched in real Play Mode. Static screenshots cannot prove animation, combat, riding, driving, NPC movement, or UI timing.
3. Map runtime risk: the migration passes static/map gates, but the legacy Edit-Mode NavMesh path check remains partial. A road-following navigation-link chain is present; the user still needs to walk, drive and bring a companion from Lalay to Highland to accept it.
4. Mobile architecture: Built-in RP, legacy Input Manager, no touch/safe-area layer, no Android build gate.
5. Build size: latest audited Windows build was about 388.6 MB; textures about 280.6 MB. TMAX source contributed about 172.2 MB and Range Rover about 43.5 MB.
6. Combat visuals: contact is now timed/forward/LOS checked, but the current sword-like placeholder clip can still look warped and needs a later approved animation replacement.
7. Character modularity: current clothes mostly recolor existing meshes; matching height does not make rig/bone scale or garment fit identical.

## Workflow now in force

- Read `Docs/AI-PRODUCTION-WORKFLOW.md`.
- Create one bounded packet from `Docs/WORK-PACKET-TEMPLATE.md`.
- Run `Tools/AIWorkflow/Invoke-Preflight.ps1`.
- Parallelize read-only audits and non-overlapping files only.
- One integrator owns Unity scenes, prefabs, packages, settings, imports, generated world data, and final visual integration.
- Record accepted appearance/placement in `Docs/VISUAL-APPROVAL-REGISTER.md`.
- For map work, scaffold/validate a stable district with `New-MapDistrict.ps1` and `Test-MapDistrict.ps1`; never skip its current gate.
- Do not spend Hitem3D credits before the reference/asset card is approved.

## Recommended next sequence

MINI-109 through the full MINI-119 SuperMoto/follow-up chain documented above are done and batch-verified. The user needs to hands-on playtest this session's build first (ragdoll fade/recover, Koss purchase and ride, new villagers, zone labels, wheelie feel, auto-mount off) before anything else starts.

1. User playtests the current build and reports back.
2. Combat bug-fix pass (unclaimed, no MINI number assigned yet): fix the existing warped punch pose and over-generous hit detection - reproduce both first, per standing convention, rather than guessing. The user is deciding whether to buy a paid fighting/boxing animation pack (recommended over free, per this file's top entry) before this starts.
3. Only after that: combos/new attack moves, then shooting mechanics - explicitly sequenced by the user as "fighting first, then shooting after". Do not start shooting before the fighting pass is accepted.
4. Resume MINI-107 only when the user requests character production again; repair animated LOD deformation before any playable swap.
5. Preserve Android and render/input migrations as separate approval packets.

## Update rule

Keep this file short. Update facts and priorities at task completion; put detailed evidence, command logs, and historical narrative in the task entry inside `PROJECT-HANDOFF.md`.
