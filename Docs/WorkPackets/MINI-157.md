# MINI-157 — Sacat's shirt/pants (texture-only method) + colour on both

```yaml
task_id: MINI-157
title: Sacat shirt/pants pass (fused-mesh method) + colour on Sacat and Franki
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: C
budget:
  claude_time: one session, Blender texture/UV pass x2 (iterated twice on Sacat after a real defect), colour pass x2 (one broken node approach, one working)
  external_credits: 0
  stop_condition: stop at rendered preview for user approval before any Unity/rig integration
reserved_files:
  - Tools/CharacterPipeline/mini157_sacat_garment.py
  - Tools/CharacterPipeline/mini157_franki_color.py
  - Assets/UpIzUpMini/Art/Characters/Garments/
  - Assets/UpIzUpMini/Editor/Mini157VerifyReshaped.cs
  - Logs/Tasks/MINI-157/
  - Docs/WorkPackets/MINI-157.md
  - Docs/Systems/Characters.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Art/Characters/Mainchar.fbx
  - Assets/UpIzUpMini/Art/Characters/Strong.fbx
  - all accessory/watch/chain assets and CharacterEquipment.cs (untouched, per standing user instruction)
depends_on:
  - MINI-154 (Franki's reshape; character-identity correction)
```

## Intent

"go n make what you need to make on sacat and test everything do color as
well" — do Sacat's own version of the shirt/pants work (his source is
structurally different from Franki's, see MINI-154's correction), and add
colour to both.

## Method: texture/UV repaint, zero topology change

`Mainchar.fbx`'s `Ch06` is ONE fused skin+clothing mesh (confirmed by Unity
batch inspection) — unlike Franki's `Ch28_*`, which already has separate,
already-skinned Hoody/Pants objects Franki's topology-cut method (MINI-154)
could edit directly. Ch06 has no hidden second "bare skin" layer under the
clothing, so cutting the sleeve short the same way Franki's was cut would
leave a real hole, not reveal skin.

Method used instead (`mini157_sacat_garment.py`), verified against Unity's
own batch-mode inspection of the FBX, not assumption:

1. Classify every `Ch06_body` polygon by world position (bone-derived
   height/width thresholds, same technique as MINI-154) into shirt / pants
   / reveal-skin-sleeve / reveal-skin-neck / unchanged.
2. For "reveal-skin" faces (the forearm past the new short-sleeve cut, the
   neck band above the new collar line): the geometry there is already the
   real arm/neck shape (a real anatomical mesh, just currently textured as
   cloth) — rasterize its existing UV triangles onto a copy of the diffuse
   texture with the character's own sampled skin tone (sampled from
   vertices dominantly weighted to the Head bone, not guessed). Zero
   vertices or faces moved, added, or deleted.
3. For "shirt"/"pants" faces: same rasterization, painted with the chosen
   garment colour.
4. Export a new FBX referencing the repainted texture. Original
   `Mainchar.fbx` and its source textures are untouched.

**A real defect was caught and fixed mid-session, twice:**
- First pass classified purely by position (narrow X + high Z = "neck"),
  which also caught the entire face/head (8,849 of ~24k faces) since the
  head sits in that same spatial region — the face rendered visually
  intact only because the separate `Ch06_Eyelashes` submesh sat on top,
  but detail was being overwritten underneath. Fixed by requiring the
  classification look at dominant vertex-group weight (is this face
  *really* Head-bone-owned, regardless of position) before ever treating
  it as paintable.
- Second pass excluded head/hand faces from the *reveal* branches but let
  them fall through to the *"shirt"/"pants"* branches instead, which
  painted the whole face navy blue (caught by rendering and looking at
  it, not assumed fixed from the code alone). Fixed by returning
  `"unchanged"` for head/hand-dominant faces as the very first check.
- Third pass (evidence below) shows a clean face, intact headphones/cap,
  and a short-sleeve navy top with correctly bare (skin-toned, not
  black-turned-blue) forearms.

Franki's colour (`mini157_franki_color.py`): his Hoody/Pants are already
separate objects from MINI-154, so this is just cloning the material and
setting Base Color directly (unlinked from the original texture — a flat
tint, same idea `CharacterEquipment.ApplyGarment` already uses elsewhere
in this project, pattern/shading detail traded for a guaranteed-visible
result this pass). A first attempt used a Mix/multiply shader node that
silently produced no visible colour change (confirmed by rendering, not
assumed) — replaced with the simpler direct-Base-Color-set approach, which
worked.

Colours used (approved concept direction): shirt navy `(0.09, 0.16, 0.32)`,
pants charcoal `(0.24, 0.24, 0.26)` — same two colours applied to both
characters for a consistent first pass; per-character/per-item colour
selection remains future work per the concept brief.

## Non-goals

- No shoes, hats, or other accessories (still untouched, still rejected).
- No Unity gameplay/prefab/scene/CharacterEquipment integration this pass.
- No motion proof with real screenshots (see below — blocked by environment).
- No claim of finished or approved clothing.

## Motion test: same environment limitation as MINI-154, not re-solved

Verified both new FBX files import into Unity cleanly as valid Humanoid
characters (`Mini157VerifyReshaped.cs`, `Logs/MINI-157-verify.log`) with
correct renderer/vertex counts — this proves the assets are structurally
sound for Unity, but is NOT motion evidence. Actually sampling idle/walk/run
and capturing screenshots hits the same `-batchmode -nographics`
blank-render limitation documented in MINI-154 (proved there with a plain
cube through the identical code path) — not re-proven again here to save
time, since the cause is environmental, not asset-specific, and unchanged
since that finding. `Mini154GarmentMotionProof.cs`'s menu command
(`Up Iz Up Mini/MINI-154/Garment Motion Proof`) still needs updating to
point at Sacat's new asset and must be run inside an **open** Unity Editor,
not this batch automation, to actually produce screenshots.

## Acceptance scorecard

- [x] Investigated Sacat's actual mesh structure (fused, not separable — different from Franki)
- [x] Chose and implemented a method appropriate to that structure (texture/UV-only, zero topology risk)
- [x] Caught and fixed two real defects via rendering + looking, not assumption
- [x] Static front/back/three-quarter render of Sacat's reshaped+coloured result
- [x] Static front/back render of Franki's coloured result
- [x] Both FBX assets verified to import as valid Humanoid in Unity
- [ ] User visual approval
- [ ] Motion proof with real screenshots — blocked by environment, needs an open Editor session
- [ ] Unity gameplay integration — not started, gated on approval above

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Ch06 structure (fused, single mesh) confirmed | Unity batch-mode inspect | `Logs/MINI-156...` / earlier MINI-154 logs | No |
| Sacat reshaped+coloured preview | Blender headless UV/texture edit + render | `Logs/Tasks/MINI-157/Sacat-Reshaped-Front/Back/ThreeQuarter.png` | Yes |
| Franki coloured preview | Blender headless material edit + render | `Logs/Tasks/MINI-157/Franki-Colored-Front/Back.png` | Yes |
| Both FBX valid Humanoid, correct renderer/vertex counts | Unity batch-mode import+inspect | `Logs/MINI-157-verify.log` | No |
| No compile errors introduced | Unity batch compile | `Logs/MINI-157-import-check.log`, 0 `error CS` matches | No |
| Motion/feel | — | blocked by headless-render environment limitation (see MINI-154) | Yes — needs open-Editor run |

## Handoff

- Files changed: new `Tools/CharacterPipeline/mini157_sacat_garment.py`, `mini157_franki_color.py`; new `Assets/UpIzUpMini/Editor/Mini157VerifyReshaped.cs` (read-only check); new `Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Ch06_Reshaped.fbx`, `Franki_ReshapedGarments_Colored.fbx`; new `Logs/Tasks/MINI-157/*` evidence. No changes to `Mainchar.fbx`, `Strong.fbx`, any scene, or accessories.
- Decisions made: Sacat's method is texture/UV repaint (zero topology change), not a geometry cut, because his source has no separable skin-vs-clothing layers. Colour is a flat tint for both characters this pass, not texture-preserving.
- Visual locks added/changed: none — PROPOSED, not locked.
- Known limitations: no motion proof (environment-blocked, not asset-blocked); no Unity integration; a small collar-fabric remnant is still visible near the back of Sacat's neck (the `ax < 0.14` radius threshold didn't catch the full collar width) — cosmetic, not structural; colours are flat/uniform, not texture-preserving tints; only one colour combination tried per character.
- Next action: user reviews the renders; if approved, (1) update `Mini154GarmentMotionProof.cs` to include Sacat and get real motion screenshots from an open Editor session, (2) then Unity gameplay integration for both characters.
- Ownership released: yes, at the end of this session's pass.
