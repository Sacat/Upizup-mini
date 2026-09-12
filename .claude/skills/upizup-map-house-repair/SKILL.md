---
name: upizup-map-house-repair
description: Fix and extend Up Iz Up Mini's live map, roads, and generic house exteriors (MINI-141/142) - Blender-modeled houses exported via custom JSON buffers, fitted to existing lots, plus local road/junction seam repairs. Use for house placement/fit defects, road seams/grades, junction geometry and district manifest questions.
---

# Up Iz Up Mini — Map & House Repair Skill

## Purpose

Fix a specific, bounded map or house defect on the LIVE scene without
promoting stale review scenes, re-running whole-map generators, or eroding
approved geography/gameplay layout.

Full detail, exact commands and failure history:
`Docs/CLAUDE-HOUSES-MAP-COMPLETE-HANDOFF-MINI-142.md`, `Docs/WorkPackets/MINI-141.md`
and `Docs/WorkPackets/MINI-142.md`. Read only what the current defect needs.

## Live map vs. isolated proofs — check this first

| Scene | Status |
|---|---|
| `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` | THE live playable game. |
| `Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity` | Approved map-lab foundation, NOT gameplay. |
| `Assets/UpIzUpMini/Scenes/MapLab_MBRoad_Lalay*Proof.unity` | Separate MB Road System spline proofs (package `com.barmetler.roadsystem`). Not documented as migrated to the live scene - verify with a live-object survey before assuming otherwise. |
| `Assets/UpIzUpMini/Scenes/GrandBayProof_Mini142Review.unity` | Historical review scene. NOT safe to promote over the current game - it predates many later changes. |

A scene-name match in a search is a lead, not proof something is live. Survey
the actual active components/meshes/colliders in `GrandBayProof.unity` first.

## Four layers - touch only the one the defect is actually in

1. **Geography** - source roads/coast/elevation, trust-marked anchors
   (`Docs/MAP-ANCHORS.json`, `Docs/Maps/<map-id>/MANIFEST.json`). Has real
   provenance/licensing constraints; don't refresh source data casually.
2. **Gameplay layout** - compressed routes, grades, lots, spawns, stable
   role/parcel IDs. Changing this needs its own authorization.
3. **Art** - house families, roofs, trims, grass/crop props fitted to the
   approved layout. Most house/prop-shape defects live here.
4. **Verification** - measured geometry/collision, fixed camera comparisons,
   actual movement/performance evidence.

A palette/material issue does not require terrain regeneration. A road-height
seam does not require relocating shops. A facade complaint does not require
touching missions or parcels. Diagnose which layer before editing.

## House pipeline (see also `upizup-blender-modeling` skill)

Houses are Blender-modeled, then exported through a custom JSON mesh-buffer
pipeline - NOT an FBX round trip:

- `Tools/ArtPreview/mini141_preview.py` - builds house archetypes + crop
  samples, renders Cycles previews, writes an editable `.blend`.
- `Tools/ArtPreview/mini142_export.py` - evaluates modifiers, triangulates,
  converts Blender `(x,y,z)` to Unity `(x,z,y)` (reflection - remember to also
  reverse triangle winding and convert normals when doing this yourself),
  bakes a shared 8x8-cell 128x128 palette texture, writes LOD0/LOD1 JSON
  buffers under `Logs/Tasks/MINI-142/Export/`.
- `Assets/UpIzUpMini/Editor/Mini142ArtIntegration.cs` / `.Apply.cs` - imports
  those JSON buffers into real Unity `Mesh` assets, then `PatchHouses` fits
  the model to an EXISTING generic house root's footprint (non-uniform X/Z
  scale to the old Body bounds, Y scale to old-height/reference-height,
  ground-snaps within a small tolerance, builds simple box colliders and an
  LODGroup), while explicitly skipping special/scripted/named buildings.

Reference dimensions (not mandatory for new buildings, just a coherence
baseline): 5.6m wide x 4.7m deep core, 2.75m per storey, .32m foundation,
.95m ridge above eaves. One-storey = seafoam + veranda; two-storey = peach +
balcony. Doors/windows are facade detail over a shell, not playable openings.

## Road/junction repair pattern

Source: `Assets/UpIzUpMini/Editor/Mini142ArtIntegration.Road.cs` (the actual
Dog Life junction/seam repair - the concrete worked example).

1. **Survey the real cross-section**, not an estimated straight line between
   two points - an early attempt here used a straight-line gap test and
   falsely reported a fixed gap because the sampled line had left the actual
   curved road. Intersect the road mesh's real world-space triangle edges at
   each sampled Z to get true left/right edges.
2. Separate the two distinct problems that "a bad junction" usually is:
   - a wedge/gap AT the junction (fixed with a small graded round "apron"
     mesh reusing the road material, doubled as both visible mesh and
     MeshCollider), and
   - overlapping ribbons at DIFFERENT HEIGHTS approaching it (fixed by
     re-lofting only the affected road objects' Y values against a measured
     local grade line, blending across a small centerline-distance band with
     SmoothStep, leaving X/Z/terrain/buildings untouched).
3. Derive a local measured grade equation from real surveyed points for THIS
   location - do not reuse another junction's constants as a universal
   fixer.
4. After any mesh edit, recompute normals/bounds, assign to BOTH MeshFilter
   and MeshCollider, and call `Physics.SyncTransforms()` before any
   collision-based validation.
5. Validate with real multi-lane, real cross-section sampling (e.g. 3 lanes x
   fine Z steps, deriving X from actual road edges, filtering hits to road
   colliders, checking max height delta between adjacent samples against a
   real threshold) - not a single centerline ray and not an unfiltered
   raycast that can hit a roof or wall instead of the road.

## Ground/height rules specific to this project

- The live world uses COLLIDER-based ground, not a Unity Terrain component.
  `Terrain.SampleHeight` is not valid here even if an object is named
  "...Terrain".
- Ground/ height sampling must filter to authoritative ground collider names.
  An unrestricted downward raycast near a house/road can hit a roof, wall,
  sidewalk or balcony instead of the ground and silently produce nonsense
  heights.

## Common mistakes (do not repeat)

- Judging an imported house purely from the Blender studio render - axes,
  winding, normals and the shader/light response can all look different
  once in Unity's palette shader and gameplay lighting. Judge the Unity
  result.
- Importing every Blender trim/rib/baluster as its own renderer instead of
  consolidating into the shared Body/LOD buffer - inflates draw calls.
- Assuming a decorative modeled doorway is an enterable interior.
- Fixing a "gap" that a straight-line/unfiltered test only appears to show,
  without first proving the test samples the real geometry.
- A junction that renders visually connected but still snags a vehicle -
  check the COLLIDER mesh and height overlap, not just the visible apron.
- Fixing a bridge/seam from only one approach direction or one camera angle.
- Promoting an old review/backup scene over the current live scene, or
  bypassing an `Apply`-style guard's checksum/backup check to force it to
  pass - if it refuses, the live scene has moved on; build a NEW bounded
  review with a new backup instead.
- Running a broad legacy scene builder (e.g. a whole-game `BuildScene()`) to
  fix one house or one road seam - always use the additive, scoped tool.
- Treating a district-manifest/metadata validator pass as proof of live
  scene collision or drivability - it validates declared JSON state only.

## Verification tiers - use the one that matches the claim

1. **Static house/road checks**: import axes/winding/normals, bounds,
   palette/material/LOD counts, collider coverage, footprint clearance on a
   ROTATED/SCALED lot (not just the studio sample).
2. **Road/junction checks**: real cross-section sampling (see above), left/
   center/right continuity, max local grade step, bridge rail/deck
   continuity.
3. **Whole-map structural**: `Mini100GrandBayMapValidation.Validate` (loads
   the live scene) plus a manual read of the district manifest for parcel/
   no-build/role expectations.
4. **Runtime acceptance**: the actual built EXE - walk/drive through the
   fixed area in both directions, both characters if relevant, watch for
   NPC/police pathing and camera occlusion. Static screenshots do not
   substitute for this when motion/driving is the actual claim.
5. **Performance**: measured draw calls/triangles/frame time at a
   phone-like resolution, not just "the polycount is low."

## Order of operations for a bounded map/house fix

1. Read the user's exact defect report and locate the real live object(s)
   involved (name/ID, not a guess) via a read-only survey.
2. Claim the relevant task ID in `PROJECT-HANDOFF.md`, scoped to that
   location/object only; check what's already dirty/owned before touching
   the scene.
3. Capture BEFORE evidence (fixed camera position/target/FOV) if the fix is
   visual.
4. Identify which of the four layers (geography/layout/art/verification)
   actually contains the defect.
5. Make the smallest reversible additive patch via existing tools; avoid
   hand-editing scene YAML.
6. Compile, run the matching static/structural check, inspect the AFTER
   evidence at the same camera settings, iterate if wrong.
7. Run the runtime/actual-game check appropriate to the claim.
8. Build only when required for delivery; verify BUILD SUCCEEDED and
   inspect payload freshness (e.g. `Assembly-CSharp.dll`, `level0` timestamps),
   not just the launcher stub's mtime.
9. Update the MapGeneration/BuildAndVerification system ledger and the
   relevant work packet with exact measurements, logs and what was NOT
   certified.
10. Stage only owned files, commit, release the claim.
