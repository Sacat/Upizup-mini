## MINI-166 — OutfitWardrobe: Shirt/Pants/Hat/Shoes slots real for Franki, partial for Sacat (2026-09-11)

`OutfitWardrobe` (slot/piece/binding/tint model, scaffolded in an earlier
checkpoint) now drives real, player-selectable garments through the actual
wardrobe UI (see `UI.md`'s matching entry). Status per slot:

- **Shirt**: Tee/Polo, BOTH characters. Franki = full mesh swap on
  `Ch28_Hoody` (bone-remap-by-name). Sacat = a small collar/placket overlay
  renderer parented alongside `Ch06` (Ch06 itself never touched, since
  disabling it via `mesh==null` would erase his whole body).
- **Pants**: Jeans/Trousers, Franki only - colour clones of `Ch28_Pants`,
  zero new geometry.
- **Hat**: No Hat/Lacos Cap, Franki only - a pure C# combined-primitive mesh
  (crown+peak, matching the existing `BuildCap()` shape) baked into the Head
  bone's own local space, single-bone rigid skin, no Blender step.
- **Shoes**: Mike 90/Mike 97, Franki only - colour clones of
  `Ch28_Sneakers`. That mesh shares its material with the skin renderer -
  clone into a NEW `Material` instance, never mutate the shared asset.

**Two generic bugs found and fixed in `OutfitWardrobe.Select()` itself
(benefit every slot, not just the one that found them):**
1. `renderer.localBounds = piece.mesh.bounds` used the raw source mesh's
   own bind-pose bounds, which is NOT the space Unity's
   `SkinnedMeshRenderer.localBounds` expects (rootBone-relative) - the
   renderer's cull volume ended up nowhere near the real geometry, so Unity
   silently skipped drawing an otherwise-correctly-skinned mesh (looked
   "invisible" even though the skin math was right). Fixed with
   `renderer.updateWhenOffscreen = true` instead of trusting a computed
   static bounds value.
2. Swapping to a plain-recoloured material can destroy an already-baked
   "reveal skin via texture paint" trick (MINI-157/161's technique for
   faking a short sleeve on a long-sleeve mesh) if the new material wipes
   the WHOLE mesh to one flat colour instead of only touching the intended
   region/slot. Never blanket-replace an existing garment's material array;
   add new colour variants as their OWN new material slot.

**`CharacterEquipment.Refresh()` regression found and fixed**: it used to
gate `cap_mike` and the shirt/shorts/shoes tint garments off entirely
whenever `GetComponent<OutfitWardrobe>() != null` - correct only once ALL
four slots are migrated, but slots are being wired in one at a time. Fixed
with `OutfitWardrobe.HasSlot(slot)` - each legacy system now stays alive
per-slot until its own `OutfitWardrobe` binding actually exists.

**Real accessory-fit bugs found and fixed via a proper Play-Mode render**
(the first attempt, pure Editor edit-mode, found NOTHING spawned at all -
`CharacterEquipment.Refresh()` early-returns when `EconomyManager.Instance`
is null outside Play Mode, so that first tool was never exercising the real
code path - see `BuildAndVerification.md` for the general lesson): the cap
was floating ~6cm above the skull (the crown-sphere primitive's own radius
meant its actual surface never reached the true hairline at the old +10cm
offset) - raised to +16cm, confirmed by render on both characters. Watch
checked the same way and found already correctly placed - not changed.

**Sacat now has THREE separate, unexplained placement/geometry issues**,
all found this session, none blindly re-attempted a second time without new
information:
1. His Shirt-slot collar overlay initially collapsed to a degenerate single
   point - root cause found: `collar.parent = arm` in the Blender script set
   a parent without Blender auto-computing `matrix_parent_inverse` (only the
   UI "Parent" operator does that), so the FBX exporter baked the
   armature's transform into the vertex data. Fixed by removing the parent
   assignment - the Armature modifier alone is sufficient for deformation.
2. His Pants slot is not wired at all this round - confirmed his leg IS
   real continuous body-shaped geometry under the current paint (not
   missing like Franki's arms were, MINI-159), but `Ch06` is his entire body
   in one renderer, so a generic OutfitWardrobe binding would risk
   `Select()`'s `mesh==null -> renderer.enabled=false` rule disabling his
   whole body. Needs a dedicated tint-only (never touch `sharedMesh`/
   `enabled`) approach.
3. His Hat placement (identical bake technique to the Shirt-overlay fix,
   proven correct for Franki) put the cap at EAR height instead of the
   crown. The bake math is provably pose-independent
   (`head.localToWorldMatrix * head.worldToLocalMatrix = identity` at the
   instant of baking), so this is NOT a pose-timing issue - points at
   something particular to his own transform chain, plausibly related to
   his root object's ~277.8-degree Y rotation in this scene (also noted in
   finding #1's investigation), not yet confirmed. Not shipped.

Denim Shorts (both characters) deliberately not attempted - repeated render
attempts to actually SEE Franki's lower-leg skin region cleanly all missed
(camera framing kept catching hands/arms, a `Sample()`-not-posing-the-idle-
clip quirk that showed up across multiple tools this session and is still
not root-caused), and this project does not cut a hem into geometry it
hasn't actually looked at. A direct Blender vertex-data check DOES confirm
`Ch28_Body`'s mesh extends the full height range and has some geometry
below knee height (170/9012 verts) - inconclusive on its own, real geometry
work still needed. See `Docs/WorkPackets/MINI-166.md` for full detail,
exact numbers, and evidence paths.

## MINI-165 — Headphones as a removable head accessory (2026-09-10)

Sacat's existing headphones are now a separate skinned accessory. Open the home wardrobe, Accessories, then Headphones / REMOVE or WEAR; Apply keeps the choice, Cancel and Restore opening outfit restore it. Existing game saves capture the selection through sacatUnequippedItems; legacy saves default to wearing the headphones. Franki has no headphone assignment; this task does not add a second fitted asset.

Six disconnected pieces (6,760 triangles) were extracted without changing positions, UVs, normals, weights or materials. The wave scalp, face and repaired clothing remain in the original body mesh. Compile, Play Mode toggle/Cancel/Apply/GameSave JSON tests, saved-scene renders, Windows build (411,132,437 bytes) and player proof process passed. Logs and on/off images: Logs/Tasks/MINI-165; compiler/build logs: Logs/mini165-*.log. Automated player UI captures were blank and are not visual acceptance evidence; hands-on button layout review remains owed. See Docs/WorkPackets/MINI-165.md.
## MINI-164 hair continuation — 2026-09-10

Both playable characters now have black wave-textured scalp meshes in GrandBayProof. Franki uses a continuous shell fitted by ray intersections to his existing head; Sacat's fused cap submesh is replaced by a smooth scalp, preserving the live clothes, face, headphones, bone weights and other submeshes. The existing removable wardrobe cap remains available, and real Play Mode verified equip/remove leaves both wave meshes/materials intact.

Unity compile, saved-scene render and Windows build passed. Build: Builds/GrandBayProof/UpIzUpMini.exe (410,853,365 bytes). Evidence and exact commands: Docs/WorkPackets/MINI-164.md; Logs/Tasks/MINI-164/waves-test-build.log and waves-saved-evidence.log.

User visual acceptance is still owed. Existing prototype cap is visibly too low and is not fixed by this hair task. Sacat's forehead/temple seam and original headphones remain rough; existing white eyelashes and garment appearance are unchanged. No complete wardrobe/contact-sheet milestone is claimed.
# Characters & Animation
## MINI-163 — Franki's hair fixed and shipped; Sacat's hair attempted and rejected

**Franki**: root cause of the glitchy hair texture confirmed - `Ch28_Hair`'s
material pointed at the shared body atlas, sampling nonsense pixels there
(and shared that exact material with `Ch28_Eyelashes`, so it couldn't be
edited in place). Gave it its own material (flat dark colour + a fade-band
second material near the bottom rim + a small wave displacement), verified
by rendering, integrated via the proven bone-remap-by-name technique
(only `Ch28_Hair` touched, every other renderer confirmed unchanged by
name). Shipped in the current build.

**Sacat**: attempted real hair (he has none - a dome baked into `Ch06`'s
own geometry, per MINI-162). Two real mistakes caught before shipping,
neither glossed over: (1) repainting the shared body atlas directly broke
the headphones' colour via UV-tile pixel reuse - fixed by switching to a
separate flat-material index instead, same technique already proven safe
elsewhere. (2) Fitting Franki's hair shape onto Sacat's head via a
bounding-box remap produced a visibly distorted band across the face -
a real bug in that fitting method, not fixed this pass. **Not shipped** -
`Sacat_Hair.fbx` exists on disk but nothing live references it; Sacat's
`Ch06` renderer is unchanged. He still has no real hair. See
`Docs/WorkPackets/MINI-163.md`.

## MINI-162 — collar/neckline finished; hair scoped as real follow-up (Claude, took over from rate-limited Codex)

Codex flattened the hood remnant (`mini162_flatten_hood.py`, 1047 rear
verts reshaped, arm verts confirmed unchanged) but left a jagged collar
rim not yet cleaned up when it hit its usage limit. User authorized Claude
to continue. Fix: a second, tighter, more-repeated smooth pass
(`mini162_smooth_collar.py`) targeted just the collar-rim band, re-exported
over `Franki_ArmsRestored.fbx`, re-integrated via `Mini161ArmRepair.Integrate()`,
re-rendered and looked at before calling it done. Neckline now reads as a
normal crew back; arms remain intact.

**Hair investigated with real renders, not guessed at**: Sacat has no real
hair mesh - the "cap" seen in every prior screenshot is a smooth metallic
dome baked directly into the `Ch06` head geometry itself (searched by
name, no separate cap object exists), with a visible hard seam at ear
level where it meets skin. Franki has a real separate `Ch28_Hair` mesh
that IS enabled/textured, but the texture is genuinely broken (a glitchy
white/brown stripe pattern) - reads as "bald" because the broken texture's
tone blends with skin, not because hair is hidden. Giving both characters
real short-wave/shape-up hair is real production work (new geometry for
Sacat, texture fix or new mesh for Franki) - not started, scoped honestly
rather than rushed. See `Docs/WorkPackets/MINI-162.md`.

## MINI-161 — Franki arm continuity repaired (2026-09-08)

Fresh Strong.fbx import retains Ch28_Hoody sleeve surface and original skinning. Bisect at prior 45% arm/82% neck thresholds without deletion, triangulate before export (Unity discarded one untriangulated cut polygon on first attempt), repaint UVs using median Head-weighted skin samples. New Franki_ArmsRestored.fbx/png/mat replaces ONLY Franki Ch28_Hoody through existing MINI159 bone-name remap; all other skinned mesh/material/bone references checked unchanged. Sacat and pants/accessories untouched. Never rerun broad MINI159 integration expecting this source: Mini161ArmRepair is the narrow repair entry point.

48 isolated captures (36 close and 12 distant), both arms of both characters across three idle/walk/run samples. Images inspected; Franki's missing gap is closed, Sacat continuity confirmed. Actual sample bone positions change across frames. Logs/Tasks/MINI-161 contains images, pose positions, source audit, and exact pre-repair binary scene backup. Retained sleeve folds/cuff silhouette and flat skin paint are a repair compromise, not newly sculpted anatomical arms. No live motion/wardrobe purchase acceptance or phone profiling claimed. Existing full clothing swap/colour workflow remains separate work.

## MINI-159 CORRECTION — Franki's short sleeve has a real arm gap; delete-based method was wrong, handed to Codex

**User-caught, confirmed real, not a rendering artifact.** After the live
integration, the user reported the arm looked wrong (hand floating away
from the sleeve). Diagnosed with an isolated skin-only render (hid every
renderer except `Ch28_Body`, magenta background so any gap is obvious):
**`Ch28_Body` has essentially no upper-arm/forearm geometry at all** - only
a small stub near the wrist. The original asset was authored assuming the
arm is ALWAYS covered by a long sleeve, so nobody modeled skin that would
never be seen. MINI-154's method (bisect the sleeve short, delete the
fabric past the cut, assume `Ch28_Body`'s own skin shows through
underneath) was built on a wrong assumption never actually verified for
the arms specifically - it was checked at the character level (Franki has
separate mesh objects, unlike Sacat) but not down to "does the skin mesh
actually cover the now-exposed area." MINI-158's motion-proof renders
missed this because none of the sampled frames/camera framings happened to
expose the gap clearly enough to notice - a genuine miss, corrected here
rather than left standing.

**The fix (not yet done - handed to Codex per user request "codex is
great at that")**: apply the SAME method already proven correct for Sacat
(MINI-157) to Franki's sleeve/collar regions instead - do **not** delete
the fabric past the cut line; **keep** that geometry (it is already
correctly shaped and already correctly skinned to the arm/neck bones,
since it WAS the sleeve) and repaint its texture to a sampled skin tone
instead, exactly like `Tools/CharacterPipeline/mini157_sacat_garment.py`'s
`reveal_sleeve`/`reveal_neck` classification+rasterization approach - just
applied to `Ch28_Hoody`'s own texture instead of Sacat's shared
`Ch06_body` atlas. Concretely:

1. Start from a FRESH import of `Strong.fbx` (the original, unedited -
   `Sacat-Garment-Prototype.blend`'s saved state already has the flawed
   delete-based cut baked in and should not be reused as a base).
2. Keep `mini154_garment_from_existing.py`'s sleeve/collar CUT-HEIGHT
   thresholds (they were fine - the silhouette length was correct) but
   change the operation from "bisect + delete past the line" to "bisect,
   then repaint the UV footprint of the faces past the line with sampled
   skin tone" (copy the classify+rasterize pattern from
   `mini157_sacat_garment.py` almost directly - `Ch28_Hoody` shares the
   same `Ch28_body` material/texture as the skin, so the same
   "sample skin tone from Head-bone-weighted vertices, rasterize UV
   triangles" technique applies unchanged).
3. Re-verify with an ISOLATED skin/sleeve-only render at multiple camera
   distances (not just the whole-character shots that missed this the
   first time) before trusting it.
4. Re-run the MINI-159 live-scene integration tool
   (`Mini159IntegrateGarments.cs`) to re-wire the corrected mesh onto the
   live Franki - the bone-remap integration technique itself was sound and
   does not need to change, only the source mesh being fed into it.
5. Re-run `Mini158`'s motion proof, this time also including a close-up
   arm-only isolated render (magenta/solid background, hide all but the
   skin renderer) at several pose frames, not just whole-body shots - that
   specific check is what was missing before.

**Ownership released without finishing this** - the user asked to hand
this specific fix to Codex. Do not re-attempt without checking this entry
first; the wrong assumption and the working fix pattern are both already
known, no need to re-diagnose from scratch.

## MINI-159 — clothes integrated into the LIVE playable characters; accessories still open

The motion-tested (MINI-158), coloured (MINI-157) reshaped shirt/pants are
now actually on the live playable Sacat and Franki in `GrandBayProof.unity`
- not just an isolated proof. Integration technique: reuse the exact
already-imported, already-motion-tested mesh/material, only rebuild each
renderer's `bones`/`rootBone` array mapped BY BONE NAME onto the live
character's own existing skeleton (same rig, same rest pose, two separate
imports of the same source) - no Animator/avatar/hierarchy change, no other
component's serialized reference touched.

**Two real bugs caught before saving anything, by rendering and looking:**
(1) the live scene's own lighting/skybox blew the first verification render
out to near-solid-white - fixed by using the same controlled ambient
settings MINI-158's isolated proof used, on characters temporarily moved
to open air (then restored). (2) Sacat still rendered as a flat white mask
after that - `material.mainTexture` was literally null: MINI-157's Blender
FBX export claimed `embed_textures=True` but silently did not embed the
repainted texture. Fixed by copying the repainted PNG directly into
`Assets/.../Garments/` and wiring it onto the material with a small
dedicated tool, confirmed by re-rendering before trusting it.

Verified in the actual render: Sacat's new short-sleeve top/pants show
correctly alongside his existing cap+headphones (proves accessory
attachment, which is generic over `Animator.GetBoneTransform`, was
unaffected by the mesh swap). Scene saved; full project compile clean;
Windows build succeeded and 15s headless smoke shows zero exceptions.

**Accessories investigated, not redesigned this pass**: close-up renders
of `cap_mike`/`shades_ray` trial-equipped show Sacat's visible cap doesn't
match `BuildCap()`'s actual primitive colour (red) - what's rendering is
probably a different, already-baked-in part of his design, not confirmed.
Franki shows no visible cap/shades at all in the same test - unconfirmed
whether off-frame or a real attach failure. Also: Sacat's collar-seam
texture boundary (MINI-157) is rougher up close than previously described.
None of this fixed yet - real follow-up work, not silently dropped from
the handoff. See `Docs/WorkPackets/MINI-159.md`.

## MINI-158 — real motion proof: both reshaped garments pass idle/walk/run

The MINI-154/MINI-157 "headless render comes back blank" limitation was
the `-nographics` flag, not the environment as a whole - dropping it (keep
`-batchmode`) on the exact same tool immediately produced real images.
See `BuildAndVerification.md`'s new ledger entry: a clean batch-mode exit
code does NOT mean a render tool actually rendered anything: always
eyeball at least one output before trusting it.

Real idle/walk/run motion evidence (132 screenshots,
`Logs/Tasks/MINI-158/`) for both Franki (topology-cut method, MINI-154)
and Sacat (texture-repaint method, MINI-157): **no shoulder/sleeve/collar/
waist tearing on either character at any sampled frame.** This is the
first actual motion confirmation either reshaped garment has had - static
renders alone were never sufficient proof (this project's own repeated
lesson, see this file's MINI-107 entry). Sacat's render came out
overexposed (lighting/material config issue in the proof tool, not a mesh
defect - silhouette/boundaries still read correctly) - open item, not yet
fixed. See `Docs/WorkPackets/MINI-158.md`.

## MINI-157 — Sacat's shirt/pants (texture-only method) + colour on both characters

Sacat (`Mainchar.fbx`/`Ch06`) is ONE fused skin+clothing mesh with no
hidden second skin layer under the clothes - Franki's topology-cut method
(delete-past-the-cut, reveal skin underneath) does not apply, it would
leave a real hole. Method used instead: classify every polygon by
bone-derived world position into shirt/pants/reveal-skin, then for
reveal-skin faces (short-sleeve cutoff, collar band) rasterize their
EXISTING UV triangles onto a copy of the diffuse texture with the
character's own sampled skin tone (sampled from real Head-bone-weighted
vertices, not guessed) - the geometry there is already correctly arm/neck
shaped, it's just currently textured as cloth. Zero vertices/faces moved,
added, or deleted - the lowest-risk method available for a fused mesh.
For shirt/pants faces, same rasterization with a chosen garment colour.

**Two real defects caught by rendering and looking, not assumed fixed from
code:** (1) classifying purely by position painted over the whole head,
since head and neck-collar occupy the same spatial region - the face
looked OK in the render only because a separate Eyelashes submesh sat on
top, masking it; fixed by requiring dominant-vertex-group-weight checks
(really Head-bone-owned?) before ever treating a face as paintable. (2) a
second pass excluded head/hand faces from the reveal-skin branches but let
them fall through to the shirt/pants branches instead, painting the whole
face navy - fixed by returning "unchanged" for head/hand-dominant faces as
the very first check, not a later filter.

Franki's colour: his Hoody/Pants are already separate objects (MINI-154),
so this is a straight material clone + Base Color set (flat tint, same
idea `CharacterEquipment.ApplyGarment` already uses). A Mix/multiply node
attempt silently produced no visible change first try (confirmed by
rendering, not assumed) - replaced with directly setting Base Color.

Both new FBX assets verified to import as valid Humanoid with correct
renderer/vertex counts (`Mini157VerifyReshaped.cs`). Motion proof hits the
same headless-render environment limitation MINI-154 already documented
(not re-proven, cause is environmental not asset-specific) - needs
`Mini154GarmentMotionProof.cs` extended to Sacat and run inside an open
Unity Editor. See `Docs/WorkPackets/MINI-157.md`.

## MINI-154 — shirt/pants: edit-the-existing-mesh method, first real preview (Franki — corrected)

**Character identity correction (same session):** the live scene proves
`Sacat` → `Mainchar.fbx`/`Ch06` (one fused mesh) and `Franki` → `Strong.fbx`/
`Ch28_*` (six separate meshes) — the OPPOSITE of `Mini016CharacterImport.cs`'s
comment, which is stale. Verify character identity against the live scene
(`GameObject.Find`/`Animator.avatar` name), never a code comment, going
forward. Everything below originally written as "Sacat" is **Franki's**
mesh. Also hit and documented: headless `-batchmode -nographics` Unity
screenshot capture (`Camera.Render()` to a `RenderTexture`) produces blank
output in this environment even for a plain primitive cube — not a bug in
any specific tool, a real environment limitation (matches this file's own
MINI-151 "Hidden player screenshot was black" note). Motion-proof tools
must be run inside an open, interactive Unity Editor, not pure batch mode.

Investigated the actual Unity-imported source (authoritative — a raw Blender
re-import of the same FBX is non-deterministic when the file bundles
multiple character variants, do not trust it; use a Unity batch-mode
`SkinnedMeshRenderer` dump instead, see `Mini154InspectGarmentSource.cs`).
Finding: **Sacat (`Strong.fbx`) and Franki (`Mainchar.fbx`) do NOT share
identical structure.** Sacat has SIX separate skinned mesh objects already
bound to the same skeleton (`Ch28_Body/Eyelashes/Hair/Hoody/Pants/Sneakers`).
Franki is ONE fused mesh (`Ch06`) with three material submeshes
(`Ch06_body`/`eyelashes`/`body1`) — clothing is not a separate object there.

Chosen method (Sacat): edit the existing `Ch28_Hoody`/`Ch28_Pants` topology
directly (bisect+delete at bone-derived cut planes) instead of building new
isolated geometry (MINI-150) or decimating a scan (MINI-148). Because the
edited geometry is a subset of an already-continuous, already-correctly-
skinned surface, there is no shoulder seam, no separate sleeve cylinder, and
no weight-transfer step. Result: clean short-sleeve crew top (hood + tall
collar removed) and a straighter-leg trouser (jogger ankle cuff relaxed).
Renders: `Logs/Tasks/MINI-154/Reshaped-Front/Back/ThreeQuarter.png` vs.
`Baseline-Front/Back.png`. Source: `Logs/Tasks/MINI-154/Sacat-Garment-Prototype.blend`.
Tool: `Tools/CharacterPipeline/mini154_garment_from_existing.py`.

**Axis pitfall hit and fixed this session**: these FBX meshes are Y-up in
their raw local vertex data (local Y=height, local Z=depth, local X=width)
even though the object's `matrix_world` rotates that into Blender's Z-up
world space. A first draft that cut using local Z as "height" produced a
garbage result (neckline that didn't touch the hood at all; ankle "radius"
computed from X/Y instead of X/Z blew the pants out into giant flared
discs). Fixed by deriving every cut plane from local Y (height) and
X/Z (cross-section) explicitly, verified against printed vertex
local-vs-world coordinate pairs before trusting any threshold. Future
garment edits on this asset family: verify this axis mapping empirically
before writing cut logic, don't assume it.

Not yet done: Franki (needs submesh-region extraction from the fused `Ch06`
mesh, a different technique), motion/idle-walk-run proof (still bind-pose
only), Unity import/skin-binding, colour/material, true polo placket detail.
Accessories untouched (explicit user instruction — still rejected, not
this task's scope). See `Docs/WorkPackets/MINI-154.md`. User visual
approval of the Sacat preview is the next gate before continuing.

## MINI-153 — rejected continuous polo fit; latest user scope

MINI-150 front render inspected: shoulder gaps and exposed vest at hem, sleeves too cylindrical. Do not integrate. Pants still unbuilt, modular mobile rig still fails motion gate. Latest request is shirt and pants ONLY, leave remaining accessory errors for Claude. See WorkPackets/MINI-153.md. No new gameplay changes or build in this inspection.

## MINI-152 — wrist lowering and head attachment correction

User explicitly revised previously approved watch axial placement: too high. Runtime now projects watch along forearm to 18mm before hand bone, preserving profile rotation/scale/radial offset and untouched rollback profiles. Cap/shades now use metre offsets and cancel head bone scale instead of generic bone-local metres. Built screenshot Logs/Tasks/MINI-151/Wardrobe-WristFix.png shows watch nearer hand; user acceptance and Franki/live movement proof still owed. Prototype cap still overlays original fused hat; this is NOT finished hat replacement. New shirt/pants remain next task. No chain edits. Build Logs/MINI-152-wrist-build.log succeeded.

## MINI-149 — session-only try-on

CharacterEquipment.SetTrialItem/ClearTrialItems maintain a separate allowlisted set (watch/cap/shades), never economy ownership or saved wardrobe. Safehouse free submenu selects trials; original cap/shades remain prototype art and are labelled. Generic garments and colours remain unfinished. User authorizes autonomous next work and free outfit tests, preserving accessories. See MINI-149 packet/tests; no scene or art changes.

## MINI-148 — rejected polo-shell experiment

Do not extract/decimate scan faces and present as a finished garment: MINI-148 produced patchy seams and body clipping. Evaluated-world-space shell also cannot simply reapply original rig modifier (double transform). Script Tools/CharacterPipeline/mini148_polo_preview.py retained for audit only. Need clean continuous garment topology, controlled clearance, region hiding and bind-space skinning. No live asset changed; approved concepts and watch/chain locks remain intact. WorkPackets/MINI-148.md records rejected evidence.

## MINI-147 final design approval

User accepted shirts/pants/hat from first sheet; superseded its court trainers with Mike 90 and Mike 97 Air Max-inspired styles and approved new footwear sheet "yes that's it". Preserve both references under Logs/Tasks/MINI-147. Per-item/per-character colours required. Proceed to fitted shirt/motion proof next; concept acceptance is not a playable-body swap or paid-credit authorization. Details and original-label caveat in WorkPackets/MINI-147.md.

## MINI-147 — clothing capsule reference gate (2026-09-07)

Watch/home wardrobe accepted as good for now. Next explicitly shirt, pants, hat, shoes and per-item colours. Imagegen concept sheet in Logs/Tasks/MINI-147/Wardrobe-Concept-v1.png is NOT implemented clothing. Approval pending. WorkPackets/MINI-147.md records material-mask/slot/body-family contract and proposed budgets. Existing ApplyGarment is tint-only; do not call it removable geometry. Do not fit over bulky original clothes or ship failed mobile body. First shirt proof requires compatible skinned base, region hiding and real motion evidence. No Hitem3D credits spent.

## MINI-146 — watch integration ledger (2026-09-07)

Proven: CharacterEquipment uses watchPrefab + per-character watchPlacement exactly like approved chain placement, without touching chains. Approved MINI-145 fits locked by user. Hidden-item set separates ownership from wearing; CaptureWardrobe/RestoreWardrobe enable future slots without a new economy. Current UI supports watch only. Canonical scene patched references only; all transforms/profile hashes guarded. Editor removal must use DestroyImmediate on instances outside Play Mode; runtime uses Destroy. Purchase screenshots Logs/Tasks/MINI-146 show actual approved prefab. Do not re-fit or regenerate characters. Full wardrobe/body LOD repair and live animation clipping are not signed off.


## Current state

MINI-145 round gold watch is an isolated Unity candidate: 1644/788-triangle LODs, one atlas material, independent Sacat/Franki cuff profiles and fixed screenshots. Original characters and chains untouched; no purchase integration. See Docs/WorkPackets/MINI-145.md and VA-009 (PROPOSED). Clothing/body work remains unfinished.

2026-09-07: User accepted MINI-143 gameplay and explicitly resumed wardrobe production. MINI-144 isolated base-body audit and Blender gold-watch concept are evidence-ready; no playable swap. See Docs/WorkPackets/MINI-144.md. Target one polo/jeans/trainers/cap/gold-watch capsule and existing chain, separate saved outfits, safehouse changing. User asks to match original clothed-character quality/approach. The previous mobile motion gate remains failed until re-proven.

Sacat and Franki are switchable protagonists with separate vitals/positions and shared economy/progression. MINI-107 (modular Sacat production - Hitem3D generation, AccuRIG rigging, mobile LODs) is **paused mid-task**: the original 100k rig animates correctly and remains the playable character; the derived 25k/12k/4.5k mobile LODs pass static checks but fail motion deformation and are NOT approved for playable integration. Do not resume this without the user explicitly requesting character production again, and repair the LOD motion deformation before any playable swap.

User-approved manual placements are visual locks - e.g. Sacat's and Boss C's two-piece chain placement (`VA-001`/`VA-002`-style register entries in `Docs/VISUAL-APPROVAL-REGISTER.md`). Never recalculate or replace an approved transform; treat it the same way `MapGeneration.md` treats manual world placement - authoritative, bug-hunt elsewhere first.

## Architecture

- `Assets/UpIzUpMini/Scripts/Character/HumanoidAnimationManager.cs` - the reusable action-layer foundation EVERY system with character animation depends on (Combat, Vehicles). See `Combat.md`'s "what didn't work" entry - this component's own `actions` list is NOT what actually plays at runtime; the shared `StarterAssetsThirdPerson.controller` asset is.
- Character production pipeline (MINI-105/107): Hitem3D generates candidates -> AccuRIG (free, local) auto-rigs -> Blender owns topology/UVs/skeleton/weights/bind pose/export. `Docs/CharacterPipeline/System/` and `Tools/CharacterPipeline/` hold the manifest system. `Docs/CHARACTER-PRODUCTION-WORKFLOW.md` is mandatory reading before touching this - read it in full, this file is a ledger, not a substitute.
- One canonical full-finger Humanoid body per wardrobe family; clothes are separate skinned geometry sharing the same skeleton/bind pose, hiding covered body regions; accessories use named per-character attachment profiles.
- Animation retargeting: any real Mixamo/motion-capture Humanoid clip retargets automatically onto ANY valid Humanoid avatar via Mecanim - confirmed repeatedly working this session (see `Combat.md`) - no per-character remapping needed as long as both source and target are genuinely Humanoid.

## What worked / what didn't

- **MINI-145 whole-wrist clarification:** User meant whole accessory orbit, not dial roll. Apply rotation around bone's forearm axis to BOTH attachment position and orientation, retaining axial distance/scale. Updated Sacat/Franki profiles by+90 localY and captured new Side.png views. No mesh changes. BuildCapture preserves these profiles; RotateAroundWristOnce is a one-shot user revision, not the normal rebuild entry point.

- **MINI-145 rotation revision:** User approved design and requested90 degrees clockwise. Rotate case/dial/crown geometry only, not entire bracelet attachment; keeps band's fit and separate profiles identical. Captures verify crown at right; budgets and attachment checks unchanged. Generator preserves this orientation for future rebuilds.

- **MINI-145:** Rebuilding the approved watch silhouette as deliberately low-poly pieces (rather than decimating each tiny high-poly part) reduced 29,800 to1,644 triangles while retaining circular case, hands, markers and bracelet; 788-triangle distant mesh, 32x8 colour/metallic atlas, one shared Standard material. Fit profiles on LeftLowerArm preserve original character scale. Existing long sleeves require over-cuff preview fitting; moving toward the hand hides the dial. Never auto-recompute approved profiles: proof tool now preserves stored values. 36 sampled idle/walk/run frames prove only attachment stability, not live clipping-free motion. Gameplay integration waits for screenshot approval.

- **MINI-144:** Original and mobile FBXs have no missing/non-normalized weights; mobile has <=4 influences. Isolated Blender bone perturbations are similar across all four meshes, so do not assume a new retopology fixes the Unity-specific tearing. Next inspect Unity LOD bone bindings/bindposes with matched motion proof. Compare evaluated rest/posed data, not raw vertices against evaluated world-space vertices.
- **MINI-144:** Existing CharacterEquipment provides individual ownership, colour-only garments and primitive cap/watch; preserve chain profile exactly. New original Blender watch concept is preview-only (29.8k evaluated triangles/144 parts); bake/merge/LOD after visual approval, never import this heavy preview directly. Evidence: Logs/Tasks/MINI-144/Gold-Watch-Preview.png. No credits spent.

- **(MINI-107, unresolved) The 25k/12k/4.5k mobile LODs fail motion deformation despite passing static checks.** A static pose/silhouette check is NOT sufficient proof a decimated rig is safe to ship - it must be watched moving before being approved. This is the same lesson as `Combat.md`/`Vehicles.md`'s repeated "batch/static checks can pass while real behaviour fails" theme, here applied to mesh decimation instead of code.
- **(2026-08-15) Local scan-derived meshes were often absurdly oversized for mobile** (~2,000,000-triangle photogrammetry scans, 183MB each) with no growth-stage variants - decimated to ~0.2% of original triangle count via vertex-clustering while still visibly reading correctly, then the multi-hundred-MB sources were deleted, keeping only the small decimated result.
- **(2026-08-28, this session) Real motion-capture clips retarget cleanly onto this project's Humanoid characters with zero extra rigging work** - confirmed via direct rendered comparison (`Combat.md`'s Mixamo jab-punch fix). This is a strong argument for sourcing new character ANIMATION from real mocap (Mixamo, ActorCore, similar) rather than hand-keyframed free packs, which have repeatedly looked wrong (see `Combat.md`'s cartoonish-pack lesson).

## Open items

- MINI-107 LOD motion-deformation repair - paused, not resolved. Do not integrate the failed LOD candidates.
- TMAX see-through/oversized-payload issue is arguably a Characters/asset-pipeline problem more than a Vehicles one - see `Vehicles.md`'s open items, listed there since it's vehicle-specific, but the underlying cause (normals/backface holes, texture import settings) is the same class of problem as character asset production.

## Key files

- `Assets/UpIzUpMini/Scripts/Character/HumanoidAnimationManager.cs`
- `Docs/CHARACTER-PRODUCTION-WORKFLOW.md` (mandatory before any character/wardrobe/rig work)
- `Docs/CharacterPipeline/System/`, `Tools/CharacterPipeline/`
- `Docs/VISUAL-APPROVAL-REGISTER.md` (locked placements - never recalculate)
- `Docs/ASSET-REGISTER.md` MINI-AST-121 (Sacat Modular Base Rigged, the MINI-107 candidate)
