# MINI-154 — shirt/pants: sound-method investigation + first reshaped preview

## ⚠️ CORRECTION (same session): character identity was swapped

Everything below that says "Sacat" (`Strong.fbx`/`Ch28_*`) is factually
**Franki's** clothing mesh, not Sacat's. Verified directly against the
LIVE `GrandBayProof.unity` scene (`Mini154FindRealSource.cs`,
`Logs/Tasks/MINI-154/unity-find-real-source.log`):

- The real playable **Sacat** GameObject → Animator avatar `MaincharAvatar`
  → `Assets/UpIzUpMini/Art/Characters/Mainchar.fbx` → single fused mesh `Ch06`.
- The real playable **Franki** GameObject → Animator avatar `StrongAvatar`
  → `Assets/UpIzUpMini/Art/Characters/Strong.fbx` → six separate meshes
  `Ch28_Body/Eyelashes/Hair/Hoody/Pants/Sneakers`.

This is the **reverse** of the comment in `Mini016CharacterImport.cs`
("Franki (Mainchar) ... Sacat (Strong)"), which is stale/wrong and should
not be trusted for character identity again — verify against the live
scene's actual `Animator.avatar` name or `GameObject.Find("Sacat")` instead
of a code comment. The technical work below (edit-the-existing-mesh method,
the reshaped short-sleeve top + relaxed pants, the axis-mapping fix) is
still valid and reusable — it is just **Franki's** garment, not Sacat's.
Sacat (`Mainchar.fbx`/`Ch06`) still needs its own investigation (fused
single-mesh structure, same as originally suspected for "Franki" before
this correction — the two characters' structures are simply swapped from
what this document assumed).

```yaml
task_id: MINI-154
title: Choose a garment-fit method that avoids MINI-148/MINI-150's failures, produce one evidence render
request_owner: User
integrator: Claude
status: implementing
approval_class: C
budget:
  claude_time: one session, investigation + one Blender preview iteration
  external_credits: 0
  stop_condition: stop at rendered preview for user approval before any Unity/rig integration
reserved_files:
  - Docs/WorkPackets/MINI-154.md
  - Docs/Systems/Characters.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - Tools/CharacterPipeline/mini154_garment_from_existing.py
  - Logs/Tasks/MINI-154/
protected_files:
  - Assets/UpIzUpMini/Art/Characters/Mainchar.fbx
  - Assets/UpIzUpMini/Art/Characters/Strong.fbx
  - all accessory/watch/chain assets and CharacterEquipment.cs (untouched this pass, per explicit user instruction to leave accessories alone)
depends_on:
  - MINI-153 (rejected MINI-150 prototype, defined the current shirt/pants-only scope)
```

## Intent

Give Sacat and Franki one properly fitted shirt (polo/tee direction, approved concept
Logs/Tasks/MINI-147/Wardrobe-Concept-v1.png) and one pair of pants that actually
**replace** the character's current clothing — not a new mesh layered over the
existing outfit. Must survive idle/walk/run without shoulder/hip/knee tearing before
any home-wardrobe integration.

## References

- Approved concept: `Logs/Tasks/MINI-147/Wardrobe-Concept-v1.png` (shirt/pants direction only; footwear/hat/accessories deferred).
- Rejected prior attempts: `Docs/WorkPackets/MINI-148.md` (scan/decimate shell — patchy, clipped), `Docs/WorkPackets/MINI-150.md` + `Docs/WorkPackets/MINI-153.md` (isolated continuous shell — shoulder gaps, cylindrical sleeves, exposed hem).
- Approved modest base (not used this pass): `Docs/CharacterPipeline/MINI-105/AccuRigOutput/Sacat-ModularBase-Rigged-VisualProof.blend` — this is the MINI-107 modular pipeline body, which still fails Unity motion deformation and is explicitly not to be used as a playable swap.

## Investigation finding (this session)

Inspected the actual playable FBX files directly with Blender in background mode
(`Tools/CharacterPipeline/mini154_garment_from_existing.py` inspect step,
log `Logs/Tasks/MINI-154/inspect-mainchar-strong.txt`):

- `Mainchar.fbx` (Franki) and `Strong.fbx` (Sacat) each import as **one Armature
  (65 bones) with SIX separate skinned mesh objects already bound to it**:
  `Ch28_Body`, `Ch28_Eyelashes`, `Ch28_Hair`, `Ch28_Hoody`, `Ch28_Pants`,
  `Ch28_Sneakers`. Clothing is not fused into the body mesh — it is already
  independent, already-skinned geometry sharing the live, motion-proven skeleton.
- Both FBX files report **identical** mesh/vertex/vertex-group data. Sacat and
  Franki are the same base geometry; per-character identity is carried by material/
  texture assignment in Unity, not separate meshes. One reshaped garment pair
  serves both characters.
- `Ch28_Hoody` (3,810 verts) already has correct vertex groups down through
  `LeftForeArm`/`LeftHand` etc. — full-length sleeves, hood included.
  `Ch28_Pants` (3,986 verts) already has correct vertex groups through the leg
  chain — full-length trousers.

## Chosen method (why this avoids the prior two failures)

Do **not** decimate a body scan (MINI-148) and do **not** build an isolated new
shell from scratch (MINI-150). Instead, **edit the existing, already-skinned
`Ch28_Hoody`/`Ch28_Pants` meshes directly** in Blender:

- Shorten `Ch28_Hoody`'s sleeves to short-sleeve length by dissolving the
  forearm portion and closing a new cuff rim, remove the separate hood-flap
  geometry, and adjust the hem — all as topology edits on a mesh that is
  already one continuous, correctly-bound surface, so there is no new seam,
  no cylindrical sleeve add-on, and no weight-transfer step (edited vertices
  keep their existing vertex-group weights from the source mesh; only the
  new cut-rim verts need weights, which they inherit from their neighbors
  during the edit).
- Taper `Ch28_Pants` slightly toward a fitted-trouser silhouette instead of a
  baggy leg, again as an edit on already-bound geometry.
- Both stay bound to the exact same 65-bone armature already proven to
  animate Mainchar/Strong correctly in game — no new bind pose, no new
  skeleton, no motion gate to invent from zero.

Integration path (not yet executed — gated on this preview's approval): at
runtime/prefab level, swap the `SkinnedMeshRenderer.sharedMesh` on the
existing `Ch28_Hoody`/`Ch28_Pants` renderer objects to the new reshaped
meshes (same bone array, same bindposes) — this is a mesh-data swap on an
already-correctly-bound renderer, not a new attachment/rig/weight-transfer
system. That step is Unity-side and out of scope for this packet.

## Non-goals

- No shoes, hats, or other accessories (explicit user instruction — accessories
  stay exactly as they are, still visually rejected, untouched here).
- No Unity import, prefab, scene, or CharacterEquipment change this pass.
- No new economy/wardrobe UI wiring this pass.
- No claim of finished, approved, or integrated clothing.

## Result (this session, Sacat only)

Both edits applied directly to `Ch28_Hoody`/`Ch28_Pants` inside `Strong.fbx`
(Sacat) via `Tools/CharacterPipeline/mini154_garment_from_existing.py`,
using bone-derived cut planes in the mesh's own local space (verified
empirically: local Y = height, local Z = depth, local X = width — an
earlier draft of this script had the height/depth axes swapped and
produced a broken pants render before this fix):

- **Shirt**: sleeves bisected at 45% shoulder→elbow (world X 0.3315) on both
  arms, leaving a clean short-sleeve opening with no seam and no separate
  cylinder piece — it is the original continuous sleeve, just shortened.
  Collar/hood bisected at 82% up the neck bone, removing the tall standing
  collar and the full hood flap, leaving a small stand-up/crew opening with
  no gap between chin and fabric. Result: 2,627 verts (from 4,218).
- **Pants**: the lowest ~16cm above each ankle (below the knee-band) had its
  X/Z cross-section relaxed toward the knee-band radius (capped at 1.6x to
  avoid blow-up — an earlier attempt used the wrong cross-section plane
  entirely and produced a giant flared disc at both ankles; this is fixed),
  softening the jogger cuff toward a straighter leg. 292/308 verts moved.
- Both stay bound to the same skeleton already proven to animate Sacat in
  game — no new bind pose, no weight transfer, no new rig.

Renders (bind/rest pose, whole character): `Logs/Tasks/MINI-154/Reshaped-Front.png`,
`Reshaped-Back.png`, `Reshaped-ThreeQuarter.png`. Baseline (unedited) for
comparison: `Baseline-Front.png`, `Baseline-Back.png`. Reusable source:
`Logs/Tasks/MINI-154/Sacat-Garment-Prototype.blend`.

**What this avoids from MINI-148/MINI-150**: no shoulder gap (sleeve is the
original continuous shoulder-to-cuff surface, just shortened), no
cylindrical add-on sleeve, no exposed base clothing at the hem (waistband
still meets the pants cleanly, since nothing at the torso/hip boundary was
touched).

**What this does NOT yet prove**: motion (still a static bind-pose render —
per this project's own repeated lesson, static approval is not a motion
gate), Franki (Mainchar.fbx/Ch06 uses a fused single-mesh body+clothes
structure, not separate objects like Sacat's — needs a different
extraction method, not started), material/colour (still wearing the
original hoody's black glossy/quilted material — colour is explicitly a
separate, later step per the user's own scope), Unity import/skin binding
(not attempted this pass), collar/placket detail (reads as a plain
crew/mock-neck, not yet a true polo placket — acceptable as a base, further
refinement possible).

## Motion proof attempt: blocked by a headless-render environment limitation

Built `Mini154GarmentMotionProof.cs` to import the reshaped garment as a
self-contained Humanoid character, drive it with the project's shared
`StarterAssetsThirdPerson.controller`, sample Idle/Walk/Run clips at 11
frames via `PlayableGraph`/`AnimationClipPlayable` (same technique as
`Mini145WatchProof.cs`), and capture front/side screenshots per frame.
It ran clean (no exceptions, correct bounds/position logged) but every
captured PNG was a flat solid color — **including a plain diagnostic Cube
primitive rendered through the identical camera/RenderTexture path**,
which proves this is not a problem with the garment mesh or the script's
logic, but a `-batchmode -nographics` limitation of this Unity install/
environment: `Camera.Render()` to a `RenderTexture` produces no visible
pixels here at all. This matches a limitation this project already hit
and recorded (`Docs/Systems/UI.md`: "Hidden player screenshot was black:
visible built-player proof is required").

**This is not fixable by writing more script** — it needs a real graphics
device. The menu command is built and ready:
`Up Iz Up Mini/MINI-154/Garment Motion Proof`. Run it from inside an
**open, interactive** Unity Editor session (Window doesn't need to be
maximized, just not `-nographics` batch mode) and it should produce real
screenshots at `Logs/Tasks/MINI-154/Motion-<Idle|Walk|Run>-<frame>-<Front|Side>.png`.
It opens `GrandBayProof.unity`, temporarily spawns the reshaped-garment
character at `(0, 300, 0)` (out of the way, in open air), samples the
clips, captures, then destroys the temporary character again and does
**not** save the scene (verified by a before/after file hash check).

## Acceptance scorecard (this packet only)

- [x] Investigated actual mesh/rig source and recorded findings
- [x] Chose and documented a method different from both rejected attempts
- [x] Static front/back/three-quarter render of the reshaped shirt+pants on the real mesh (Franki, not Sacat — see correction above)
- [ ] User visual approval of the render
- [ ] Same treatment for Sacat (different source structure — fused `Ch06` mesh, not started)
- [ ] Unity import/skin-swap/motion proof — script built and verified not to touch the canonical scene, but blocked on this environment's headless-render limitation; needs to be run inside an open Unity Editor (menu: `Up Iz Up Mini/MINI-154/Garment Motion Proof`)

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Mesh/rig structure understood (both files) | Unity batch-mode inspect (authoritative, not raw-FBX reimport) | `Logs/Tasks/MINI-154/unity-inspect-garment-source-2.log` | No |
| Baseline unedited look | Blender headless render | `Logs/Tasks/MINI-154/Baseline-Front.png`, `Baseline-Back.png` | No |
| Reshaped shirt/pants (Sacat) | Blender headless topology edit + render | `Logs/Tasks/MINI-154/Reshaped-Front.png`, `Reshaped-Back.png`, `Reshaped-ThreeQuarter.png` | Yes — visual approval required before integration |
| Compiles | Unity batch compile (new `Mini154InspectGarmentSource.cs` editor tool) | ran clean via `-executeMethod`, see log above | No |

## Handoff

- Files changed: `Assets/UpIzUpMini/Editor/Mini154InspectGarmentSource.cs`, `Mini154FindRealSource.cs`, `Mini154GarmentMotionProof.cs` (new, read-only/proof-only tools, no gameplay/asset change); `Assets/UpIzUpMini/Art/Characters/Garments/Sacat_ReshapedGarments.fbx` (misnamed — it is actually Franki's reshaped garment mesh, imported for the motion-proof tool only, not referenced by any prefab/scene/gameplay code); `Tools/CharacterPipeline/mini154_garment_from_existing.py` (new); `Logs/Tasks/MINI-154/*` (new evidence, incl. reusable `Sacat-Garment-Prototype.blend`, also misnamed for the same reason). No changes to any playable Asset (`Mainchar.fbx`, `Strong.fbx`, materials, prefabs, scene) — the canonical scene file hash was verified unchanged after the motion-proof tool ran.
- Decisions made: garment method = direct topology edit of the existing already-skinned `Ch28_Hoody`/`Ch28_Pants` — this is **Franki's** mesh (`Strong.fbx`), not Sacat's; the character-identity mapping used all session was backwards until caught mid-session (see correction at top). Sacat needs a separate method since Sacat's source (`Mainchar.fbx`/`Ch06`) is a fused single mesh.
- Visual locks added/changed: none — this is a PROPOSED preview, not a visual lock.
- Known limitations: Franki only (mislabeled "Sacat" throughout the filenames/tool names — real identity corrected in this doc, not yet renamed on disk); no Sacat treatment; no motion proof (script ready, blocked by a headless-render environment limitation, needs to run inside an open Unity Editor); no Unity gameplay integration; no colour/material pass; collar is a plain crew, not a true polo placket; no accessory work (explicitly deferred by user).
- Next action: (1) user visual approval of `Reshaped-Front/Back/ThreeQuarter.png` (note: this is Franki, not Sacat) vs `Baseline-Front/Back.png`; (2) run `Up Iz Up Mini/MINI-154/Garment Motion Proof` inside an open Unity Editor to get real idle/walk/run screenshots; (3) do the same investigation+reshape for Sacat's `Ch06` mesh; (4) only then Unity gameplay integration.
- Ownership released: yes, at the end of this session's evidence pass (see PROJECT-HANDOFF.md).
