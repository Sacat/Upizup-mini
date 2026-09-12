# MINI-166 — Fitted selectable outfits

## Cap brim size fix (Claude), 2026-09-12

User request: "fix the cap brim size" - the specific defect flagged but not fixed in the logo-mirror round above. The Lacos cap's `Brim()` function in `Mini166RepairAccessories.cs` projected the bill .108f forward and drooped it .022f down per unit t, with a x^2*.012f edge-lift curvature - the combined result put the bill's visible edge at eye/nose height instead of above the brow, and in side profile the tip reached past the nose.

Reduced forward reach .108f -> .058f (~46% shorter), droop .022f -> .008f, and edge-lift curvature .012f -> .006f, in two iterations - re-rendered and re-cropped after each change rather than trusting the coefficient edit alone. Attachment height (`bottom-.008f`, now `-.006f`) and width (`x*rx*1.04f`) were left essentially unchanged.

Side-profile result is unambiguous: the bill no longer reaches the nose and now reads as a normal-length cap bill (`Logs/Tasks/MINI-166/Repair/ClaudeBrimFix/Franki-Brim-After-Side.png` vs the original `Logs/Tasks/MINI-166/Repair/Franki-Polo-Trousers-Cap-Side.png`). Front-view result is a smaller, real improvement (bill sits at brow/eye height instead of extending to the nose) but a brim attached at brow height will always show as a band across roughly that height in a straight-on view - that's inherent to any cap silhouette, not a residual bug, and further iteration on this coefficient set showed diminishing returns. Stopped there rather than chasing sub-millimeter changes without better reference.

Verification: compile clean (`Logs/mini166-claude-brim-compile.log`), `Mini166Repair.Integrate` (`Logs/mini166-claude-brim-integrate.log`, INTEGRATE_PASS/PREVIEW_PASS), `Mini166RepairValidation.Run` (`Logs/mini166-claude-brim-tests.log`, REPAIR_TEST_PASS, actual Play Mode), Windows build (`Logs/mini166-claude-brim-build.log`, BUILD SUCCEEDED, 420,366,933 bytes - unchanged size, no new geometry/materials). Crown shape, LACOS text, shirt logo and shoulder geometry untouched. Motion not re-sampled - cap geometry change doesn't affect skinning/deformation, only a static accessory shape.

## Logo mirror fix (Claude), 2026-09-12

User feedback: "your wardrobe and accessories building are poor". Per AGENTS.md ("inspect actual rendered results before claiming a visual fix"), inspected the actual Codex-repair evidence PNGs under `Logs/Tasks/MINI-166/Repair/` before touching anything, and cropped/enlarged the chest logo area of `Franki-Shoulders-Front.png`. Found a real, confirmed bug: the "MIKE"/"LACOS" chest wordmark and the cap's "LACOS" crown text render as a horizontal mirror image ("MIKE" appeared as "3XIM"). Not a style complaint - the shared `Word()` glyph-quad generator in `Mini166RepairAccessories.cs` lays pixel columns out along +local-x on a front-facing quad strip, which this project's view setup renders backwards on screen for that panel.

First fix attempt (mirroring only the placement offset, `width-(c*4+col)*pixel`) partially worked - it corrected character order but left individual asymmetric glyphs (K, E) still internally mirrored, because the placement offset and the per-pixel quad-corner extension used the same local +x direction inconsistently. Caught this by re-rendering and re-cropping rather than trusting the first "looks better" pass - confirmed by inspection, not assumed.

Final fix: instead of touching the position/quad-winding math at all (risking flipped face normals/culling), feed it a pre-mirrored *source* picture - characters read in reverse order and each glyph's bits reversed column-wise (`glyph[row*3+(2-col)]`). Mirroring the source is exactly undone by the view's own mirror, restoring correct on-screen reading order with zero position/winding changes. Re-rendered and re-cropped both characters' chest logos after this fix - both now read "MIKE" correctly, left-to-right, upright. Before/after crops: `Logs/Tasks/MINI-166/Repair/ClaudeLogoMirror/`.

Also spotted, NOT fixed this round (separate, out of scope for a text-mirror fix): the Lacos cap's brim renders oversized, drooping down over the eyes almost to the nose - a real placement/sizing defect, flagged for a future targeted pass, not silently ignored.

Verification: compile clean (`Logs/mini166-claude-logo-mirror-compile.log`), `Mini166Repair.Integrate` (`Logs/mini166-claude-logo-mirror-integrate.log`, INTEGRATE_PASS/PREVIEW_PASS - regenerated assets and saved scene), `Mini166RepairValidation.Run` (`Logs/mini166-claude-logo-mirror-tests.log`, REPAIR_TEST_PASS in actual Play Mode), Windows build (`Logs/mini166-claude-logo-mirror-build.log`, BUILD SUCCEEDED, 420,366,933 bytes - same size as the preceding checkpoint since only quad ordering changed, no new geometry/textures). Motion was not re-sampled this round since no vertex positions or topology changed, only which pre-existing quads render at which columns - the shoulder geometry itself (`ShoulderForm`) was not touched and remains the pending-approval styling from the prior round. No EXE proof/-wardrobe-proof capture was run this round; the static renders above are the visual evidence for this specific fix.

## User revision — individual clothing and shoulders, 2026-09-12

Confirmed cause of matching initial appearance: generator used identical defaults for both characters; runtime selections and save fields were already separate. New defaults are character-specific. Expanded Play Mode tests prove the other character's selections, actual renderer meshes and indexed colours remain unchanged during Choose/Restore/Cancel/Apply; `Logs/mini166-individual-tests.log` has MINI166_REPAIR_TEST_PASS. Actual two-character SaveLoadSystem round trip still passes and the original PlayerPrefs save is restored by the harness.

Shoulder before captures: `Logs/Tasks/MINI-166/Repair/ShoulderBefore/*-Shoulders-Front.png` and Back.png (640x800, fixed idle .7s, neutral lighting). First upper-trap displacement was visibly too steep and rejected; final refinement reduces lift and widens the medial blend. Same bone weights and topology; normals follow the deformation Jacobian. Existing neckline, hands, waves, headphone and chain placement are preserved.

Preflight was invoked but its parser only recognizes the first claim (Claude/MINI-167), so it reported an ownership mismatch despite the explicit non-overlapping Codex/MINI-166 reservation below. Manually verified the standing reservation and no running Unity Editor before launch; no vehicle files were edited. The interrupted refinement call was rejected by automatic approval review due to account usage limits, then resumed on the user's explicit continue; no workaround was used.

Owner: Codex, same MINI-166 concurrent reservation. User explicitly authorizes restoring fuller shoulder/trapezius shape toward original fits and individually dressed characters. Reference: original retained shirt surfaces and prior built captures. Distinct starting outfits; preserve intentional saved selections and independent selection/colour/save state. No new items, bones, head or accessory changes. Budget: same mesh topology, materials and textures; zero external spending. Inspect before/after fixed shoulder views, exercise one-character changes and save/load, sample motion, rebuild EXE. Visual acceptance remains pending until user review. Rollback: 9881fa3. Vehicle work remains separate.

Final integration/compile: `Logs/mini166-shoulders-refined.log`, INTEGRATE_PASS and PREVIEW_PASS. Inspected both characters' Front and Back shoulder close-ups; refined slope fills the old dips without the first attempt's neck spikes. Final motion: `Logs/mini166-shoulders-motion.log`, REPAIR_MOTION_PASS (432 frames; tee and polo coverage explicitly retained despite new defaults). Inspected both running polo frames; no separation at shoulders/sleeves. Frame sequences remain under Repair/Motion. Content budgets unchanged: only four shirt meshes and scene defaults changed; no new textures/materials/rigs. User hands-on visual acceptance remains pending.

Final Windows build: `Logs/mini166-individual-build.log`, BUILD SUCCEEDED, 420,366,933 bytes packaged total, 15.41 seconds. Actual EXE proof: `Logs/mini166-individual-player.log`, WARDROBE_LIVE_PROOF_COMPLETE, clean exit and no Exception/Error matches. Visually inspected `Repair/Individual-Outfits.png` combining the actual GPU player captures (Franki left, Sacat right). Portraits: `Individual-Franki-portrait.png` and `Individual-Sacat-portrait.png`. New motion preview: `Repair/Shoulders-Run-Comparison.mp4`. Existing save data is intentionally not migrated or erased; Tab switches characters, safehouse E -> 5 edits only the controlled wearer, Apply then F5 persists. All authorized implementation and build work complete; exact styling remains for user review.

## Latest implemented state — Codex repair, 2026-09-12

This section supersedes the incomplete/failed attempts recorded below. Both characters now have all eight approved clothing designs: Mike crew tee, Lacos polo, straight jeans, tailored trousers, denim shorts, Lacos curved cap, Mike 90 and Mike 97. No Hat is a ninth menu entry, not a ninth design. Seven independent slot colours include Silver. All four bindings are separate skinned renderers with persistent asset references in GrandBayProof. No purchases or ownership grants were added.

### Repairs and source contract

- Replaced Sacat's collar-only implementation with complete fitted shirt and pants bindings. His fused material index was not an unsolvable blocker: preserve head/hands/waves in WardrobeBody, mask covered geometry, and fit the repaired continuous Franki garment topology through matching rest-bone matrices to Sacat's own bone palette. Never transplant the original body, face, rig, or Animator controller.
- Built distinct shoe meshes: angular Mike 90 panels with heel air windows, flowing Mike 97 strips with longer windows; separate upper/sole/rubber/accent/lace materials. Replaced primitive cap with a curved brim and panelled crown fitted to referenced scalp vertices. Headphones remain independently removable on Sacat; Franki has no headphone asset assignment.
- Replaced jagged triangle-centroid shorts cuts with interpolated boundaries; actual smoothly skinned lower-leg surfaces replace cloth-shaped painted legs. Trousers and jeans differ in shaping, not just colour. Cotton/denim have small generated weave textures. Fabric tint never reaches skin, buttons, soles or laces.
- Fixed stale native mesh buffers during repeat asset generation by assigning mesh channels explicitly instead of CopySerialized for Mesh. Same-frame preview baking now uses current bone matrices for readable meshes, with BakeMesh fallback for imported GPU-only assets. The actual game still uses SkinnedMeshRenderer normally.
- Fixed portrait indexed material-property blocks, initial front orientation, width-aware framing, and Restore opening colour state. Added Wear/Remove for watch/shades, kept headphone toggle, removed duplicate prototype-cap button (caps belong in Hats). Reused the real E -> 5 safehouse menu. Apply retains; F5 persists clothes/headphones; unowned watch/shades remain session try-ons.
- Set the two playable Animators to AlwaysAnimate and enabled offscreen skin updates on active character renderers, including hair/headphones, so the wardrobe camera does not depend on the gameplay camera's visibility. No Animator controller, avatar, locomotion clip, watch profile, chain profile, or vehicle file was replaced.

### Verification completed

- Final Windows build: `Logs/mini166-repair-build-final.log`, BUILD SUCCEEDED, 420,322,677 bytes total packaged output; 11.15 seconds. `Builds/GrandBayProof/UpIzUpMini.exe` plus adjacent data folder/DLLs. Scene payload and Assembly-CSharp.dll refreshed on 2026-09-12. The launcher file's own timestamp is not build freshness evidence.
- Final actual EXE proof: `Logs/mini166-repair-player-final.log`, WARDROBE_LIVE_PROOF_COMPLETE; process exited, no Exception/Error matches. Visually inspected `Repair/Final-Franki-player.png`, `Final-Franki-portrait.png`, `Final-Sacat-player.png`, `Final-Sacat-portrait.png`. Real GPU player and UI portrait now match clothing and headphone visibility; animation no longer depends on offscreen culling. Initial Built-* captures are superseded by Final-*.

- `Logs/mini166-repair-integrate.log`: MINI166_REPAIR_INTEGRATE_PASS and PREVIEW_PASS; saved scene integration, not just temporary previews.
- `Logs/mini166-repair-tests.log`: MINI166_REPAIR_TEST_PASS in actual Play Mode. All pieces/bindings/weights/bounds; distinct shoe meshes; fabric-only tint; actual SaveLoadSystem.Save/Load for both characters; legacy defaults; UI ChoosePiece, portrait colour, Restore/Cancel/Apply; accessory removal; character isolation. Existing PlayerPrefs save restored after testing.
- `Logs/mini166-repair-motion-final.log`: MINI166_REPAIR_MOTION_PASS, 432 frames across two characters, three outfits, walking/running/motorcycle-idle clips and a manual deep-knee-flex sweep. Inspected running, walking and deepest flex frames. The motorcycle idle clip is NOT a seated gameplay proof; the flex sweep tests deformation and does not add a crouch mechanic. Older files named Seated came from the initial mislabeled motorcycle-idle check and are superseded.
- Fixed views: `Logs/Tasks/MINI-166/Repair/*-Front.png`, `*-Side.png`, `*-Back.png`, and shoe closeups. Moving comparison: `Logs/Tasks/MINI-166/Repair/Shorts-Run-Comparison.mp4`. These are rendered animation samples, not human-played locomotion certification.
- `Logs/mini166-repair-visibility.log`: MINI166_VISIBILITY_PASS. Final Windows build and post-build proof results are recorded below when complete.

### Exact tool entry points

Use `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe` with `-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod <method> -logFile <absolute log path>`. One Editor at a time; no `-nographics` for renders. Each method exits itself.

Methods: `UpIzUpMini.EditorTools.Mini166Repair.Audit`, `.Preview`, `.Integrate`, `.FinalizeVisibility`, `.Motion`; `UpIzUpMini.EditorTools.Mini166RepairValidation.Run`; `UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer`. Preview generates/replaces task assets but does not save the scene; Integrate saves. Source renderers are retained disabled, so generation remains repeatable. Baseline scene backup: `Logs/Tasks/MINI-166/Repair/GrandBayProof-before.unity`.

Built-player verification uses `Builds/GrandBayProof/UpIzUpMini.exe -batchmode -wardrobe-proof "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-166\Repair\Built" -logFile <absolute player log>`. The explicit proof flag captures both real GPU-skinned players and the actual portrait textures, then exits. It never runs during normal play.

### Content budget and remaining human check

Per character: shirts about 8.7k triangles (includes preserved arms); jeans about 8k; trousers 7.9k; shorts 5.5k including legs; cap 2.4k; Mike 90 pair 2.8k; Mike 97 pair 6.1k. Exact counts in `Repair/Franki-budget.txt` and `Sacat-budget.txt`. These exceed the older aspirational low-poly capsule targets; no phone performance certification is claimed. Four to five material slots per garment, some empty; two 128x128 generated fabric textures, no cloth physics or new gameplay lights/colliders. Original high-detail face/hair/accessory assets retained. Zero external credits or uploads.

User should review exact styling and fit in the delivered EXE at safehouse E -> 5. Automated functional and animation checks do not imply the user has visually approved this final appearance. Concurrent MINI-167 vehicle work was preserved and is not part of the wardrobe checkpoint.

## Codex repair continuation — 2026-09-11

User rejects the partial wardrobe and authorizes completing the approved capsule now. Codex claims the same MINI-166. Audit all eight designs and four independent slots on both characters, replace colour-only footwear and crude cap, isolate skin from fabric colour, repair preview colour/rotation/accessory controls and verify save/cancel/apply. Use existing rigged surfaces in rest space, split fused Sacat geometry into dedicated renderers; one material index is not a blocker to classifying triangles. Preserve original head/wave/headphone/watch/chain geometry. Baseline Git is clean at takeover. Zero external spending. Finish with saved-scene visual evidence, sampled animation sequences, runtime validation and Windows build; report hands-on limitations honestly.

User authorizes implementing the approved wardrobe: Lacostes polo, Mike tee, jeans, denim shorts, trousers, Lacos cap, Mike 90 and Mike 97, fitted for both characters with independent colours. Build from the current repaired, skinned characters. Preserve wave hair, removable headphones, faces, hands, watch and chain placement.

Owner: Codex. Scope: new OutfitWardrobe runtime/data/editor tools and generated outfit meshes/materials; VisualWardrobePanel; CharacterEquipment; SaveLoadSystem; GrandBayProof scene; task and system documentation. No paid assets or external uploads. Budget: local mesh production and focused Unity verification. Existing free wardrobe access continues; no ownership grants.

Acceptance: actual mesh changes in all four clothing slots, distinct shirt/leg/footwear silhouettes, colour choice, Cancel/Apply and per-character save/load, no missing arms or lower legs with shorts, animation pose verification, fixed renders, compile and Windows build. Iterate on visible defects before integration. Existing dirty work preserved. Baseline scene and renderer meshes are checkpointed before integration.

## Checkpoint for Claude — 2026-09-11

Startup smoke: launched the new EXE with `-batchmode -nographics -logFile "E:\Unity\Up Iz Up Mini\Logs\mini166-smoke.log"`; it remained running for 15 seconds, then only that test process was stopped. No Exception/Error/Crash matches in the log. Repeated kinematic-body angular-velocity warnings remain. This is startup evidence, not outfit visual or interaction acceptance.

Incomplete at user's requested EXE/handoff checkpoint. Added OutfitWardrobe selection/tint/capture/restore data model, save fields/hooks, conditional legacy garment bypass, and current-scene mesh audit. No generated outfit geometry, scene attachment or clothing UI integration yet. Earlier acceptance paragraph describes required work, not completed results; no new source mesh/scene backup was created yet because no geometry integration occurred.

Unity Windows build completed successfully: `Logs/mini166-build.log`, marker `MINI-001 BUILD SUCCEEDED`, total packaged size 411,132,245 bytes, 20.55 seconds. Existing compiler warnings remain (VehicleSpawnController unreachable code; CharacterEquipment unused field). Output `Builds/GrandBayProof/UpIzUpMini.exe` with adjacent data files. Full outfit acceptance tests and visual proof remain outstanding. Detailed step-by-step continuation: `Docs/CLAUDE-CONTINUE-MINI-166.md`. Ownership released to None for Claude to claim.

## Shirt slot round 1 — 2026-09-11 (Claude, continued from the checkpoint above)

**Franki: real, verified, working.** `Franki_Shirt_Tee_Mike.fbx` (unmodified
ArmsRestored mesh/material - preserves MINI-157/161's skin-reveal paint on
the forearm) and `Franki_Shirt_Polo_Lacos.fbx` (same mesh + a new collar ring
+ placket + 3 buttons, built as real geometry rigid-weighted to
`mixamorig10:Neck`, joined without touching the original material) both
integrate onto the live `Ch28_Hoody` renderer via `Mini166IntegrateShirt.cs`
and render correctly in a full idle pose: collar sits at the neckline,
placket/buttons on the front chest, original navy fabric/sleeve-cuff shading
intact. Verified by render, not assumed:
`Logs/Tasks/MINI-166/Franki-Polo-Full-A.png`, `Franki-Polo-Full-B.png`.

**Real bugs found and fixed this round (both generic, benefit every future
slot, not just this one):**
1. First mesh-swap attempt rendered fully invisible (a gap where the torso
   should be; head/hands/feet floating disconnected in a T-pose-looking
   arrangement). Root cause: `OutfitWardrobe.Select()` set
   `SkinnedMeshRenderer.localBounds` directly from the raw source mesh's own
   bind-pose bounds, which is NOT the space Unity expects (rootBone-
   relative) - the renderer's cull volume ended up nowhere near the real
   geometry, so Unity silently skipped drawing an otherwise-correctly-skinned
   mesh. Fixed generically in `OutfitWardrobe.cs`: `renderer.updateWhenOffscreen
   = true` on selection, so Unity always recomputes real bounds from live
   skinned vertices instead of trusting a wrong static value.
2. First polo material pass wiped the mesh's single material to one flat
   colour, destroying the already-baked skin-reveal paint on the forearm
   (MINI-157/161's "repaint the UV region, don't cut geometry" technique
   lives IN the texture, not as a separate material slot) - the sleeve read
   as fully long instead of the approved short-sleeve. Fixed: never touch
   the base tee's original material; the polo's collar/placket get their own
   NEW material slot only, added via mesh `join()`, original slot untouched.
3. Collar/placket initially built on the character's BACK, not the chest - a
   Blender-space "forward" assumption that didn't survive FBX axis
   conversion into Unity. Fixed empirically (flipped `forward_sign`),
   confirmed correct in the actual Unity render, not re-guessed from Blender
   coordinates alone.

**Sacat: NOT working yet, NOT shipped.** `Sacat_Shirt_Polo_Overlay.fbx`
(collar/placket/buttons sized to Sacat's own neck via the same technique)
imports and binds without error, but the live renderer reports degenerate
`bounds.size=(0,0,0)` even with `updateWhenOffscreen=true` - the actual
skinned vertices appear to be collapsing to a single point, a real skinning
bug (likely the vertex-group/weight export for this specific standalone
overlay object), not yet root-caused. Nothing rendered for Sacat's polo.
Left as an open item rather than shipped broken.

**Not integrated to the live scene.** Everything above ran through
`Mini166IntegrateShirt.Preview` (no save) only - `GrandBayProof.unity` is
still unchanged. `Mini166IntegrateShirt.Integrate` (the save variant) has not
been run.

**Still outstanding for the Shirt slot:** fix Sacat's overlay skinning bug;
colour-tint currently double-tints the WHOLE texture including the skin-
reveal region since it's baked into one texture (known limitation, not
hidden); integrate to the live scene once both characters render clean;
motion proof (walk/run/crouch, not just idle); then the remaining three
slots (Pants, Hat, Shoes) per the original sequencing.

Evidence: `Logs/Tasks/MINI-166/Franki-Polo-Full-A.png`,
`Franki-Polo-Full-B.png` (real, good); `Sacat-Polo-Full-A/B.png`,
`Sacat-Polo-Chest-A.png` (show Sacat's existing base look only - overlay
piece did not render). Unity logs: `Logs/mini166_shirt_preview10.log` and
prior numbered rounds (failed attempts kept, not deleted, per this project's
diagnostic-tool convention). New files: `Tools/CharacterPipeline/
mini166_shirt_collar.py`, `Assets/UpIzUpMini/Editor/Mini166IntegrateShirt.cs`,
`Assets/UpIzUpMini/Art/Characters/Garments/Franki_Shirt_Tee_Mike.fbx`,
`Franki_Shirt_Polo_Lacos.fbx`, `Sacat_Shirt_Polo_Overlay.fbx`. Modified:
`Assets/UpIzUpMini/Scripts/Character/OutfitWardrobe.cs` (updateWhenOffscreen
fix).

### Sacat overlay: three fix attempts, still broken - stopping to report, not to keep guessing

After the two generic bugs above were fixed, Sacat's overlay bounds still
came back exactly `(0,0,0)`. Two further targeted attempts:

1. Rebuilt the mesh as genuinely single-bone (bones=[Neck], bindposes
   matching) in C#, bypassing whatever Blender/FBX bone-list ambiguity might
   exist - verified the source mesh DOES carry correct per-vertex weights
   and positions before export (checked directly in Blender). Bounds stayed
   `(0,0,0)`. Also found and fixed a real duplicate-assignment bug along the
   way (`overlayR.rootBone` was being set to Neck then immediately
   overwritten back to Hips by a leftover line from before the rewrite) -
   fixed, no change to the outcome.
2. Rebaked the mesh's vertices directly into the Neck bone's local space
   with an identity bindpose (removes matrix composition ambiguity
   entirely). Result got WORSE, not better: `RecalculateBounds()` now
   reports every vertex collapsing to the literal same point
   `(100.82, -7.04, 148.49)` - values in the hundreds, not meters, and zero
   spread across 832 vertices. This points at something rig-specific to
   Sacat (his root GameObject carries an unusual ~277.8-degree Y rotation
   in this scene, confirmed via a separate diagnostic log this round) -
   possibly a non-uniform or degenerate bone scale on his skeleton
   specifically, not yet confirmed.

**Stopping here rather than continuing to guess.** This needs a dedicated,
focused diagnosis (dump Sacat's actual Neck bone lossyScale/rotation and
compare against Franki's, which worked fine) as its own next step, not more
blind attempts stacked onto an already long session. Sacat's Shirt slot
piece is left unshippable/disabled - nothing broken was integrated.

### Sacat overlay: FIXED — real root cause found and resolved

The zero-size bounds traced all the way down to `srcR.sharedMesh` itself
being degenerate as-imported (vertex0 through vertex828 all reporting
~(0,0,0.02), confirmed directly - not a Unity-integration-side bug at all).
Root cause: the Blender script set `collar.parent = arm` before export.
Assigning `.parent` via Python does NOT auto-compute
`matrix_parent_inverse` (only Blender's UI "Parent" operator does that) -
so the FBX exporter had to compensate by baking the armature's own
transform into the exported vertex data, collapsing the absolute-world-
authored coordinates this script builds by hand. Fix: removed the parent
assignment entirely - the Armature modifier alone (`mod.object = arm`) is
sufficient for skin deformation; Franki's collar never needed parenting
either, since it was joined directly into an already-correct mesh object.

After the fix: `Sacat_ShirtOverlay` renders with real, correct
`bounds.size=(0.35, 0.20, 0.34)` at the right world position, verified by
render - a visible collar band appears at Sacat's neckline in both
Full-A/B screenshots (`Logs/Tasks/MINI-166/Sacat-Polo-Full-A.png`,
`Sacat-Polo-Full-B.png`). Open cosmetic item, not a bug: the collar
currently reads lighter than intended and sits close to his EXISTING
baked hoodie collar, so the two visually compete rather than reading as one
clean garment - worth a fit/colour pass before final visual sign-off, but
it is real, positioned, non-broken geometry.

**Integrated and saved.** `Mini166IntegrateShirt.Integrate` ran successfully
(`MINI166_SHIRT_INTEGRATE_PASS saved`, `Logs/mini166_shirt_integrate.log`).
A full project recompile after the save is clean (`Logs/mini166_final_compile.log`,
exit 0). `GrandBayProof.unity` now has:
- Both Franki and Sacat carrying an `OutfitWardrobe` component with a real
  Shirt slot (Tee/Polo for Franki bound to `Ch28_Hoody`; Tee(base)/Polo for
  Sacat bound to a new `Sacat_ShirtOverlay` child renderer, `Ch06` itself
  untouched).
- Default selection on both is Tee (existing proven look, unchanged) -
  nothing visually changes until a player actually selects Polo through
  wardrobe UI (which doesn't exist yet - see below).

**Shirt slot status: functionally complete for both characters, not yet
player-facing.** Still outstanding before the Shirt slot can be called
fully done: wire `VisualWardrobePanel` selection UI so a player can actually
choose Tee/Polo in-game; motion proof (walk/run/crouch, not just idle);
Sacat's collar fit/colour polish noted above; then the remaining three slots
(Pants, Hat, Shoes) per the original sequencing.

Windows build after this round: `MINI-001 BUILD SUCCEEDED:
Builds/GrandBayProof/UpIzUpMini.exe (412,037,349 bytes)`, `Logs/mini166_shirt_build.log`.
No hands-on smoke/playtest run this pass yet (batch-mode build only).

## Accessory fit fix (cap too high, watch position check) — 2026-09-11

User-reported: "the accessories are not on the player correctly like the
watch is too high in the wrong position, hat too high". Investigated with
real measurement/render rather than guessing, and found two separate issues:

1. **My own regression, found and fixed**: `CharacterEquipment.Refresh()`
   gated `cap_mike` (and the shirt/shorts/shoes tint garments) off entirely
   whenever `GetComponent<OutfitWardrobe>() != null` - but slots are being
   migrated one at a time (Shirt only, so far), so this blanket check
   silently killed the cap trial/purchase system and pants/shoes tints
   before their real replacements existed. Fixed with a proper per-slot
   check (`OutfitWardrobe.HasSlot(slot)`), so each old system stays alive
   until its own slot is actually wired.
2. **A real, pre-existing fit bug**: the first diagnostic attempt (pure
   Editor edit-mode) found NOTHING spawned at all for either accessory -
   traced to `CharacterEquipment.Refresh()` early-returning when
   `EconomyManager.Instance` is null outside Play Mode, so the tool was
   never exercising the real code path. Rebuilt as a proper Play-Mode batch
   check (`Mini166PlayModeAccessoryFit.cs`, following the proven
   `Mini165HeadphoneValidation.cs` SessionState+InitializeOnLoad pattern) -
   a first render attempt hijacked the live gameplay camera and produced a
   bird's-eye map screenshot instead of the character (a CameraFollow-style
   script was fighting the direct transform write every frame) - fixed by
   spawning a dedicated temporary camera instead, same as every other render
   tool this session.

With the real code path finally exercised: the cap's placement (`+10cm`
straight up from the `Head` bone) put the crown-sphere primitive's own
BOTTOM only ~4cm above Head - the actual hairline sits much higher (measured
`HeadTop_End` ~21cm above `Head` on this rig) - so it was floating with a
visible gap, never resting on the skull. Raised to `+16cm`, confirmed by a
real Play Mode render on both characters:
`Logs/Tasks/MINI-166/Sacat-PM-Head-Side.png`, `Franki-PM-Head-Side.png` - cap
now sits on the head correctly, front-to-back, no gap. The watch was checked
the same way and found to already be correctly placed (1.8cm from the hand
bone along the forearm axis, wraps the wrist properly in the render,
`Sacat-PM-Wrist.png`/`Franki-PM-Wrist.png`) - not changed; whatever the user
saw as "too high" may have been from a build predating MINI-152's wrist-
lowering fix, or a different in-game pose/angle not reproduced here.

Did not use external reference images this pass - real skeleton measurement
plus a real render (the technique already proven throughout this whole
project) was faster and more directly verifiable than sourcing photo
references for a rig-relative placement problem.

Files: `Assets/UpIzUpMini/Scripts/Character/CharacterEquipment.cs` (per-slot
gate fix + cap offset), `Assets/UpIzUpMini/Scripts/Character/OutfitWardrobe.cs`
(new `HasSlot` query), `Assets/UpIzUpMini/Editor/Mini166CheckAccessoryFit.cs`
(edit-mode attempt, kept for the record - found nothing, see above for why),
`Assets/UpIzUpMini/Editor/Mini166PlayModeAccessoryFit.cs` (the real, working
check).

Windows build after the accessory fit fix: `MINI-001 BUILD SUCCEEDED:
Builds/GrandBayProof/UpIzUpMini.exe (412,037,349 bytes)`,
`Logs/mini166_accfix_build.log`. Compile clean, no scene save needed (pure
C# logic change).

## Pants slot round 1 — 2026-09-11

Given how long Shirt's real bugs took to find and fix, scoped this round
down deliberately: **Jeans and Trousers for Franki only** - both pure
colour clones of the existing, already motion-proven `Ch28_Pants` mesh
(same mesh, cloned material, no new geometry, no Blender/FBX step at all).
Verified by render: `Logs/Tasks/MINI-166/Franki-Jeans-Full.png` (indigo),
`Franki-Trousers-Full.png` (charcoal, matches the current approved default
look) - both read clearly distinct and connected correctly in a full idle
pose. Integrated and saved (`MINI166_PANTS_INTEGRATE_PASS`), full recompile
clean, Windows build succeeded (412,039,429 bytes,
`Logs/mini166_pants_build.log`).

Also fixed a real latent bug found while wiring this in:
`Mini166IntegrateShirt.MergeDefaults` was wholesale REPLACING the whole
`defaults` array instead of merging per-slot - harmless the first time
(Shirt was the only slot), but would have silently wiped Pants' default the
next time Shirt integration re-runs. Fixed to resolve each existing
default's slot via the piece it names and only replace that slot's entry.

**Denim Shorts deliberately NOT attempted this round.** Before cutting a
knee-length hem, spent real effort trying to verify lower-leg skin coverage
exists under Franki's pants (the exact same class of gap MINI-159 found on
his arms) - a direct Blender inspection confirms `Ch28_Body`'s vertex data
DOES extend the full height range (0.06m to 1.76m world Z, 170/9012 verts
below knee height), but repeated render attempts to actually SEE that
region cleanly all missed (camera framing kept catching hands/arms instead
of legs - a `Sample()`-not-actually-posing-the-idle-clip quirk that also
showed up in this session's other render tools, still not root-caused).
Rather than cut a hem into geometry I haven't actually looked at, shorts are
deferred - flagged as a real open item, not silently dropped.

**Sacat's Pants slot also NOT wired this round.** Confirmed by render
(`Logs/Tasks/MINI-166/Sacat-LegSkin-Thigh.png`, only `Ch06` enabled) that his
leg is real continuous body-shaped geometry under the current paint - NOT
missing like Franki's arms were - so a texture-repaint approach (MINI-157's
proven technique) should work safely for jeans/trousers/shorts colour. Not
done this round because `Ch06` is his ENTIRE body in one renderer -
binding OutfitWardrobe's generic Pants slot to it directly would let
`Select()`'s `mesh==null → renderer.enabled=false` rule disable his whole
body by mistake. Needs a small, careful, Sacat-specific integration (tint
material index only, never touch `sharedMesh`/`enabled`), not a blind reuse
of Franki's pattern.

**Pants slot status: 2 of 3 planned designs shipped for 1 of 2 characters.**
Remaining before Pants can be called done: Sacat's jeans/trousers tint,
Denim Shorts for both (needs the leg-skin question actually resolved, not
re-guessed), wardrobe UI wiring, motion proof. Then Hat and Shoes slots
remain untouched.

## Wardrobe UI wired to the real safehouse E -> 5 path — 2026-09-11

User: "for the wardrobe the fittings should be the same when i press E in
the safehouse and 5." Confirmed the actual call path first
(`SafehouseInteractable.cs:89`, pressing 5 in the safehouse menu calls
`VisualWardrobePanel.Open(WardrobeEquipment)` - the exact same entry point
used everywhere) - the fittings just weren't reachable from that panel yet,
which was still showing the old "IN PRODUCTION" placeholder for Shirts/
Pants/Hats/Shoes regardless of what OutfitWardrobe actually had.

`VisualWardrobePanel.cs` now reads `wearer.GetComponent<OutfitWardrobe>()`
and, per tab, lists the real pieces for that slot (`WORN`/`WEAR` buttons)
plus colour swatches for any piece with tintable slots - falling back to
the old "IN PRODUCTION" message only for slot/character combinations that
genuinely aren't built yet (Hat, Shoes, and Sacat's Pants). Cancel now also
restores the `OutfitWardrobe` selection (was previously only restoring
trial accessories/legacy wardrobe, not the new outfit system) via a
captured `originalOutfit` snapshot on open.

Validated with a real Play Mode check
(`Mini166WardrobeUIValidation.cs`, `MINI166_WARDROBE_UI_PASS`) that opens
the panel through the IDENTICAL `VisualWardrobePanel.Open()` call
`SafehouseInteractable` makes, confirms Franki's Shirt/Pants slots are
listed, selects Polo, confirms it took, Cancels and confirms it reverted to
the opening Tee, reopens, selects Jeans, Applies, confirms the selection
persisted after close. `GameSave.sacatOutfit`/`frankiOutfit` (already wired
in an earlier checkpoint, `SaveLoadSystem.cs:152-153,221-222`) was not
re-tested this pass since it was already covered separately.

Compile clean, Windows build succeeded (412,040,965 bytes,
`Logs/mini166_ui_build.log`).

## Hat slot round 1 — 2026-09-11 (Franki only)

Built a real selectable Hat slot: "No Hat" (default, matches current
shipped look unchanged) and "Lacos Cap" - a pure C# combined-primitive mesh
(crown sphere + peak cube, same geometry as the existing `BuildCap()`
primitive, already positioned correctly this session's accessory-fit fix)
baked into the Head bone's own local space with an identity bindpose and a
single-bone rigid skin - the same technique that fixed Sacat's Shirt overlay
earlier. No Blender/FBX step at all, so that pipeline's risk class doesn't
apply here.

Verified by render for **Franki**: `Logs/Tasks/MINI-166/Franki-Hat-Front.png`
- sits correctly on the crown, matches the already-fixed cap placement.

**Sacat's Hat NOT wired this round.** The identical bake produced a cap
sitting at ear height instead of the crown - a THIRD Sacat-specific
placement bug this session (after the Shirt-overlay Blender-parenting bug
and the original cap-offset bug both characters shared). The bake math is
provably pose-independent (`head.localToWorldMatrix * head.worldToLocalMatrix
= identity` at the instant of baking, so the placement should be exact
regardless of any pose timing question), so this points at something
particular to Sacat's own transform chain - plausibly related to the
already-documented ~277.8-degree root rotation quirk, not yet confirmed.
Not shipped broken - Sacat's cap stays on the old, already-fixed
`CharacterEquipment.PositionOnBone` system for now (unaffected, still
correct per the earlier accessory-fit pass).

Integrated and saved (`MINI166_HAT_INTEGRATE_PASS saved (Franki only)`),
compile clean, Windows build succeeded (412,096,229 bytes,
`Logs/mini166_hat_build.log`).

**Hat slot status: 1 of 1 planned design shipped for 1 of 2 characters.**
Sacat's Hat placement bug is now the third open Sacat-specific item,
alongside Denim Shorts (both characters) and Sacat's Pants slot.

## Shoes slot round 1 — 2026-09-11 (Franki only)

Mike 90 (white) / Mike 97 (black) - colour clones of the existing
`Ch28_Sneakers` mesh, same low-risk pattern as Pants' Jeans/Trousers: zero
new geometry, no Blender step. One real thing checked before touching
anything: `Ch28_Sneakers` shares its material ("Ch28_body") with the skin
renderer - cloned into new `Material` instances rather than mutating the
shared asset, confirmed by render that skin colour elsewhere is unaffected.
Verified: `Logs/Tasks/MINI-166/Franki-Mike90-Feet.png`,
`Franki-Mike97-Feet.png`, `Franki-Mike97-Full.png` - both read as distinct,
connected sneakers in a full idle pose.

Integrated and saved (`MINI166_SHOES_INTEGRATE_PASS`), compile clean,
Windows build succeeded (412,098,293 bytes, `Logs/mini166_shoes_build.log`).

**All four slots now have at least one shipped design for Franki.** Sacat
remains on legacy systems for Pants/Hat (Shirt is done for him). Real
distinct Mike 90 vs 97 panel geometry (not just colour) is future work, same
honest scoping as jeans/trousers.

## Correction — Sacat's "Hat placement bug" was a render-tool bug, not a real one (2026-09-11)

Retracting part of the "three open Sacat placement bugs" finding above.
Investigated further per the user's request to continue: the Hat bake was
already proven pose-independent (`head.localToWorldMatrix * head.
worldToLocalMatrix = identity` at bake time), and a direct diagnostic
confirmed the combined mesh's HEAD-LOCAL bounds were byte-for-byte
equivalent between Franki and Sacat (`Center: (0.00, 0.17, 0.04), Extents:
~(0.10-0.11, 0.06, 0.14)` for both). The actual bug: the render tool's
camera used a FIXED world-space offset for "Front"/"Side" shots, which
doesn't account for Sacat's root object carrying a ~277.8-degree Y rotation
in this scene - his "Front" camera was actually viewing the BACK of his
head at a steep angle, and a correctly-placed cap looked wrong from that
angle. Fixed the render tool to use `root.transform.forward`/`right`
instead of fixed world axes (first attempt had the direction sign backwards
too - camera must stand where the face is looking TOWARD, not behind it).
With the fixed camera, Sacat's cap renders correctly at the hairline,
matching Franki's: `Logs/Tasks/MINI-166/Sacat-Hat-Front.png`.

**Hat slot is now real for BOTH characters** - integrated and saved,
compile clean, Windows build succeeded (412,208,613 bytes,
`Logs/mini166_hat2_build.log`).

This also means his remaining "Shirt-overlay parenting bug" (genuinely
confirmed and fixed via Blender vertex data, unrelated to camera framing)
stays a real, separate finding - only the Hat "bug" turns out to have been
a diagnosis artifact. Sacat's Pants slot remains the one still-open item
for him, and it hasn't been re-investigated with this same "check the
render tool's camera, not just the data" lens yet - worth revisiting before
assuming it's a real placement bug too.

## Sacat's Pants: confirmed real constraint, not a render artifact (2026-09-11)

After the Hat "bug" turned out to be a render-tool camera issue, re-checked
the Pants deferral with the same skepticism rather than assuming it was
also a false alarm. Flagged each of Ch06's 4 material slots a distinct
colour and rendered: `Logs/Tasks/MINI-166/Sacat-MatFlags-Full.png`,
`Sacat-MatFlags-Back.png`. Result: skin, shirt AND pants are ALL material
index 0 (`Ch06_body_Reshaped`) - the entire body silhouette renders one
flat colour. No separate pants-only material slot exists to safely tint.
This CONFIRMS the original deferral was correct: recolouring index 0 would
recolour his whole body, not just the pants. A real Pants slot for Sacat
needs the MINI-157 texture-repaint technique (classify polygons by bone
position, rasterize a new colour onto just the pants UV region) - real
production work, not a quick fix. Staying deferred, now for a confirmed
reason rather than an assumed one.

## Denim Shorts: leg-skin question now CONFIRMED, not just inconclusive (2026-09-11)

Applying the same lesson as the Hat correction (check the render tool
before trusting a "looks broken" result) - but this time the wide, proven
full-body framing recipe (used successfully all session for Shirt/Pants/
Shoes) plus a magenta background (MINI-159's own proven "isolated skin
render" technique) gave a clean, unambiguous result instead of another
miss: `Logs/Tasks/MINI-166/Franki-LegSkin-Front.png` shows Franki's
`Ch28_Body` ALONE (Hoody/Pants/Sneakers hidden) as a floating head, two
floating hands, and two small foot fragments - NOTHING connecting them.
**Franki's `Ch28_Body` genuinely has no real torso/leg skin geometry**,
the exact same class of gap MINI-159 found on his arms (the original asset
assumed clothing always covers the body, so nobody modelled skin that would
never be seen).

This is now a confirmed finding, not an inconclusive one: Denim Shorts for
Franki needs the same repair MINI-161 did for his arms (repaint the
existing pants geometry's own skin-tone region rather than deleting fabric,
using real sampled skin tone) before a knee-length hem can be cut safely -
real production work, correctly still not attempted this session.

Sacat's version of this check doesn't apply the same way: his `Ch06` is one
fused skin+clothing mesh with no separate "hide the clothes" option (the
"clothes" are a texture on the same continuous body surface, already
confirmed real and leg-shaped in an earlier check this session) - the
MINI-157 texture-repaint technique is the correct path for him regardless,
same conclusion as before.

Denim Shorts (both characters) remains the one deliberately-not-attempted
design, now backed by a real confirmed reason instead of an inconclusive
render.

## Denim Shorts: fixed and shipped for Franki (2026-09-11)

Built the real leg-skin repair after confirming the gap. First attempt
followed the exact proven Shirt-slot pattern (bisect in Blender, export
FBX, bone-remap-by-name in Unity) - the Blender-side geometry was verified
completely correct in isolation (4061 verts, 8051 polys, correct world
bounds, correct material_index split 4745/3306, no orphan vertex weights,
correct normals) and every Unity-side check passed too (bounds, bindposes,
boneWeights, submesh/material correspondence, GPU-skinning warmup timing) -
**yet the mesh rendered as completely invisible**, from every camera angle,
across nine separate diagnostic rounds. A sanity check proved it wasn't a
bones/renderer problem: re-selecting the already-working Jeans piece
immediately after the identical bone remap rendered perfectly fine on the
same renderer. The root cause was never found despite exhausting every
data-level check.

**Abandoned that approach entirely and switched technique**: instead of
re-exporting through Blender/FBX and remapping bones (a pattern that has
now caused real bugs three separate times this session - the Shirt overlay
parenting bug, and now this unexplained one), built the shorts geometry as
a pure C# operation directly on the ALREADY-WORKING live Jeans mesh -
`SkinnedMeshRenderer.BakeMesh()` to get real posed vertex positions,
classify each triangle by height against the hip/knee bone positions
(converted into the renderer's own local space), split into two submeshes
via `Mesh.SetTriangles(list, submeshIndex)`, assign the existing pants
fabric material to one and a new skin-tone material to the other. Zero
Blender/FBX round-trip, zero bone remapping - the mesh's vertices/bones/
bindposes are byte-identical to the working Jeans mesh, only the
triangle-to-submesh assignment changed.

**This worked immediately.** Verified by render:
`Logs/Tasks/MINI-166/Franki-Shorts-Legs.png`, `Franki-Shorts-Full-A.png` -
real denim-blue shorts with a jagged natural hem, connected bare legs in
Franki's real skin tone below it, no gaps, correct idle pose. Integrated
and saved (`MINI166_SHORTS_INTEGRATE_PASS`), compile clean, Windows build
succeeded (412,592,837 bytes, `Logs/mini166_shorts_build.log`).

**Lesson for future slots**: prefer building geometry variants as direct
C# operations on an already-proven-working live mesh (submesh/material
split, vertex position edits) over a Blender-export-then-bone-remap
round-trip wherever the change doesn't need new topology from scratch -
the C# path has now succeeded cleanly twice (this, and would have avoided
real time lost on the Shirt overlay) where the Blender round-trip has
caused three distinct, hard-to-diagnose bugs this session.

**Denim Shorts for Sacat remains not attempted** - his fused single-
material mesh (confirmed by the material-flag check) needs the real
MINI-157 texture-repaint technique, not this submesh-split technique
(there's no separate pants submesh to split on his mesh at all).
