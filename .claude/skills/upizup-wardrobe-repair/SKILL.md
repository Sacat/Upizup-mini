---
name: upizup-wardrobe-repair
description: Fix and extend Up Iz Up Mini's fitted character wardrobe (MINI-166) - shirts, pants, hats, shoes and accessories generated through Mini166Repair's Unity C# mesh tools. Use for garment/accessory defects, sizing, tint, persistence and UI issues on Franki or Sacat.
---

# Up Iz Up Mini — Wardrobe Repair Skill

## Purpose

Fix or extend the fitted, selectable wardrobe on Franki and Sacat without
regressing the other character, other slots, saves, or protected placements.

Full detail, exact logs and failure history: `Docs/CLAUDE-WARDROBE-COMPLETE-HANDOFF-MINI-166.md`
and `Docs/WorkPackets/MINI-166.md`. Read only the sections relevant to the
current defect - this skill is the distilled index, not the replacement.

## Key insight: this wardrobe is built in C#, not Blender

Unlike the houses, the current wardrobe generator does NOT round-trip through
Blender/FBX. It builds/edits skinned meshes directly in Unity C# by reading
the character's existing rigged surface, cutting/reshaping it, and writing
back Mesh assets. Prefer this approach for any new garment/accessory work
unless a real Blender need is demonstrated (see `upizup-blender-modeling`
skill's "Existing skinned clothing" section) - repeated unexplained
Blender/FBX bugs are why this switch happened originally.

## Ownership

- Franki = Strong/Ch28 (separate mesh objects). Sacat = Mainchar/Ch06
  (originally fused). Verify against live renderer evidence, not old comments
  - names have been swapped by mistake before.
- Claim MINI-166 in `PROJECT-HANDOFF.md` before editing; do not start a new
  task ID for a wardrobe follow-up.
- Canonical scene: `Assets/UpIzUpMini/Scenes/GrandBayProof.unity`.

## Implementation map

| File | Responsibility |
|---|---|
| `Assets/UpIzUpMini/Scripts/Character/OutfitWardrobe.cs` | Per-character pieces, slot bindings, selection, indexed colour blocks, capture/restore |
| `Assets/UpIzUpMini/Scripts/Character/CharacterEquipment.cs` | Legacy compatibility, accessory equip/remove |
| `Assets/UpIzUpMini/Scripts/Character/WardrobePreviewMesh.cs` | Immediate CPU skinning for readable preview meshes |
| `Assets/UpIzUpMini/Scripts/UI/VisualWardrobePanel.cs` | Wearer-only UI, portrait, ChoosePiece/RestoreOpeningOutfit/Apply/Cancel |
| `Assets/UpIzUpMini/Scripts/SaveLoadSystem.cs` | Separate per-character outfit/headphone save fields |
| `Assets/UpIzUpMini/Editor/Mini166Repair.cs` | Constants, audit, visibility finalization |
| `Assets/UpIzUpMini/Editor/Mini166RepairMesh.cs` | Surface conversion, clipping (`Clip`), weights, bone retarget, `Shape` builder, asset/material writing |
| `Assets/UpIzUpMini/Editor/Mini166RepairClothes.cs` | `BuildCharacter`, per-character defaults, shirts/pants/legs, `ShoulderForm` |
| `Assets/UpIzUpMini/Editor/Mini166RepairAccessories.cs` | Cap (`Cap`), shoes (`Shoes`), wordmark text generator (`Word`) |
| `Assets/UpIzUpMini/Editor/Mini166RepairRender.cs` | Static capture tool (`RenderAll`, `Capture`, `ShoulderBaseline`) |
| `Assets/UpIzUpMini/Editor/Mini166RepairMotion.cs` | Sampled walk/run/deformation frames |
| `Assets/UpIzUpMini/Editor/Mini166RepairValidation.cs` | Real Play Mode regression harness |
| `Assets/UpIzUpMini/Art/Characters/Garments/Outfits166/` | Generated meshes/materials/weave textures (regenerated in place, not versioned by filename) |

## Core mesh techniques already proven here

- `Surface`: reads positions/normals/UVs/weights/bind poses into character-root
  space; exposes rest-bone queries (`s.Rest("Neck")`, `s.Bone("Head")`).
- `Retarget`: matches bone suffixes and remaps a garment built for one
  skeleton onto another by rest-matrix multiplication - this is how Sacat's
  fused-material blocker was actually solved, not by declaring it unsolvable.
- `Clip`: cuts polygons at a boundary plane and interpolates position/normal/
  UV/weight for the cut edge (used for shorts hems, trouser cuffs).
- `Shape`: accumulates vertices + per-material-slot triangles, emits one
  skinned `Mesh`.
- `Asset`: writes into an EXISTING Mesh asset by explicit channel assignment.
  **Never use `EditorUtility.CopySerialized` for the Mesh itself** - it has
  previously produced stale/corrupted native vertex buffers here. It remains
  fine for non-Mesh objects.
- Procedural surface displacement (e.g. `ShoulderForm`): compute a
  displacement field in rest space, move vertices, then recompute normals via
  a central-difference Jacobian inverse-transpose rather than leaving stale
  undeformed normals.
- Small embossed text/logos are drawn as literal pixel-grid quads (`Word()`
  in `Mini166RepairAccessories.cs`), not textures. Mirroring bugs here are
  about local-axis/view orientation, not UVs - see "Known failure patterns" below.

## Body-shape/muscle-definition work: compare against real reference

For anything that changes anatomy (shoulder slope, bicep/deltoid/chest
bulges, body proportions in general - not garment cut), don't tune purely by
iterating against the model's own renders in isolation. Pull in real
reference (an actual muscular-build photo, or an existing approved character
reference if one exists) and explicitly note what's similar and what
differs (e.g. "a real deltoid cap tapers over roughly 25% of upper-arm
length before blending into the bicep; ours does X"). Call this comparison
out in the response, not just internally.

This is a direct user request (`character-shape-reference-comparison`
memory): the MINI-166 muscle-definition pass needed several correction
rounds precisely because early attempts were tuned by trial-and-error
against the model's own output rather than checked against real anatomical
proportions first - a bicep bulge ballooned the sleeve fabric, a chest
bulge looked like implants. A reference comparison up front would have
caught both faster.

Also use the diagnostic-log + exaggerated-render technique when a new
displacement function's effect isn't visible at the intended magnitude:
temporarily log the computed displacement's peak value to confirm the
function is actually reaching the vertices you expect, and render one
deliberately oversized test pass to see the true shape/location before
tuning the magnitude down - cheaper than guessing coefficients blind.
Remove the diagnostic logging before finalizing.

## Known failure patterns (do not repeat)

1. **Mirrored text/asymmetric geometry on a front-facing panel.** A quad strip
   authored with a "natural" left-to-right local-x layout can render backwards
   on screen for a given view/bone orientation. Confirmed once by cropping an
   actual render ("MIKE" showed as "3XIM"). Fix by mirroring the *source*
   pattern (reversed order + reversed sub-indices) rather than the position/
   winding math - touching winding risks flipping face normals and culling
   the geometry entirely. **Always re-render and re-crop after this kind of
   fix**; a placement-only mirror can look "fixed" for symmetric characters
   (M, I) while asymmetric ones (K, E) are still backwards - this happened
   here and needed a second pass to catch.
2. **Oversized/misplaced accessory protrusions (cap brims, etc.).** A
   procedural offset field (e.g. forward reach + downward droop + edge-lift
   curvature) can look fine from one angle and wrong from another - a bill
   that looks reasonable in side profile can still cover the eyes head-on.
   Check both front AND side/three-quarter renders before calling a size fix
   done; a front-on view is disproportionately sensitive to vertical
   attachment height and center curvature, less to forward length.
3. **Deleting "hidden" geometry** (e.g. assuming a full arm exists under a
   sleeve) has left floating body parts. Verify what's actually underneath
   before removing anything.
4. **`renderer.localBounds` from an incompatible mesh coordinate space**
   makes garments invisible/culled. Prefer `updateWhenOffscreen` over
   hand-computed bounds when in doubt.
5. **Same-frame mesh swap + GPU skinning** produces stale preview frames.
   `WardrobePreviewMesh.Bake` (immediate CPU skinning) exists specifically to
   avoid this for preview/portrait captures; gameplay itself still uses
   normal `SkinnedMeshRenderer`.
6. **Recolouring shared materials** instead of per-renderer indexed
   `MaterialPropertyBlock`s tints every wearer of that material, not just the
   one being edited.
7. **Restoring a previously-rejected geometry coefficient.** If a work packet
   records "first attempt was visibly too steep/large and was rejected,"
   don't reintroduce those exact values later without a new reason.

## Tool entry points (prefix `UpIzUpMini.EditorTools.`)

| Method | Effect |
|---|---|
| `Mini166Repair.Audit` | Loads scene, writes audit, no scene save |
| `Mini166Repair.ShoulderBaseline` | Renders the FULL evidence set (defaults, shoulders front/back, tee/jeans, polo/trousers/cap front+side, shorts front/back, both shoe styles) from whatever assets are currently on disk. Does NOT regenerate assets - stale code changes won't show up until `Preview`/`Integrate` runs first. |
| `Mini166Repair.Preview` | REGENERATES garment assets from current code, then renders. Does NOT save the scene. Not read-only. |
| `Mini166Repair.Integrate` | Regenerates assets AND saves the scene. |
| `Mini166Repair.FinalizeVisibility` | Saves Animator/offscreen-skin settings |
| `Mini166Repair.Motion` | Loads scene, samples walk/run/deformation frames; no scene save |
| `Mini166RepairValidation.Run` | Real Play Mode regression harness; backs up/restores the actual PlayerPrefs save |
| `Mini001Build.BuildWindowsPlayer` | Builds `Builds/GrandBayProof/UpIzUpMini.exe` |

All exit the Editor themselves except historical MINI-142-era static methods
(different task) which need `-quit`. One Editor process at a time. Do not use
`-nographics` for any render/capture call - it has produced blank captures.

## Correct order of operations for a geometry/text fix

1. Read the user's exact complaint; inspect the actual current evidence PNGs
   before writing any code (`Logs/Tasks/MINI-166/Repair/*.png`) - crop/zoom
   with a real image tool if the defect is small (a Python+Pillow crop-and-
   enlarge round trip works well here).
2. Claim MINI-166, scoped to the specific defect only.
3. Edit the generator code (`Mini166RepairClothes.cs` or
   `Mini166RepairAccessories.cs`).
4. Compile-check (`-batchmode -nographics -quit`, grep for `error CS`).
5. Run `Mini166Repair.Preview` to regenerate the affected assets.
6. Run `Mini166Repair.ShoulderBaseline` (or equivalent) to re-render.
7. Crop/inspect the SPECIFIC before/after region, not just "did it run."
   If the fix looks wrong or incomplete, iterate steps 3-6 before moving on -
   don't declare a visual fix done from code review alone.
8. Once genuinely confirmed correct: `Mini166Repair.Integrate` (saves scene),
   `Mini166RepairValidation.Run` (real Play Mode pass), `Mini001Build.BuildWindowsPlayer`.
9. Update `Docs/WorkPackets/MINI-166.md`, `Docs/Systems/Characters.md`,
   `Docs/CURRENT.md` with what changed, what was verified, and what was
   explicitly NOT touched.
10. Stage only the files this fix actually changed (garment `.asset` files,
    the one script touched, docs) - never `git add -A` when another task
    (e.g. a concurrent vehicle/map claim) has unrelated dirty files.
11. Commit, release the MINI-166 claim.

## Scope discipline

Fix only the reported defect. If inspection surfaces a second real defect
(e.g. an oversized brim while fixing mirrored text), record it in the work
packet as found-but-not-fixed and let the user decide whether to scope a
follow-up - don't silently expand the current claim to cover it.
