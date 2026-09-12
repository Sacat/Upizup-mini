# Claude house modeling and map repair — complete technical handoff

Prepared 2026-09-12 from the actual scripts, work packets, manifests, Git history and logs. This documents how the existing houses and local map repair were made, plus a reusable workflow for further requested fixes. It does not claim a new map fix or build was performed while writing this guide.

## 1. Start here: scope, state and ownership

Project: `E:\Unity\Up Iz Up Mini`. Do not work in `E:\Unity\Up iz up`, the separate read-only reference project. `E:\Assets` is also reference-only. Unity version is 6000.3.10f1; Blender house production used 5.0.1.

Relevant checkpoints:

- `0121ce1` — MINI-141: stage Blender house and crop approval previews.
- `f5ba411` — MINI-142: integrate approved environment and crop art with local road repair.

These are historical reference points, NOT instructions to reset the current game. Numerous gameplay/character/vehicle changes followed them. Never promote an old review scene over the present live scene.

At guide creation, Claude owns MINI-166 wardrobe/wordmark work, including the live scene. Codex's MINI-142 reservation covers this documentation only. Do not run Unity, import assets, replace the scene or compete for integration while another task owns it. Read current ownership before acting; these notes may be older than the next task.

Use the user's latest specific feedback to choose one bounded house/map repair. Receiving a guide is not authorization to redesign the whole district. Continue an existing applicable task or claim a new bounded task according to project rules; do not repurpose an active wardrobe claim for map changes.

## 2. Required reading and the existing system

Read in this order:

1. `AGENTS.md`, `Docs/CURRENT.md`, current claims in `PROJECT-HANDOFF.md` and relevant `TASKS.md` entries.
2. `Docs/Systems/README.md`, then `Docs/Systems/MapGeneration.md` and `Docs/Systems/BuildAndVerification.md`.
3. `Docs/AI-PRODUCTION-WORKFLOW.md` and `Docs/WORLD-EXPANSION-WORKFLOW.md` completely before terrain/road/anchor work.
4. `Docs/WorkPackets/MINI-141.md` and `Docs/WorkPackets/MINI-142.md` for house/local-repair history.
5. `Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/MANIFEST.json`, its SOURCES/APPROVALS/DISTRICT-PACKET/MIGRATION-ROLE-MAP documents, `Docs/MAP-ANCHORS.json`, `Docs/MAP-SOURCE-NOTES.md`.
6. Relevant visual locks in `Docs/VISUAL-APPROVAL-REGISTER.md`. The house direction is the entry titled Approved Blender house/grass/crop direction; VA-009 numbering is duplicated elsewhere, so use title/date/evidence too.
7. MINI-121/122/123 packets only when investigating the separate spline-road proof.

The system already consists of provenance data, district manifests, stage validators, isolated map-lab scenes, additive integration tools, visual locks and evidence. Do not replace it with an untracked collection of guessed placements.

## 3. Live map versus isolated proofs — essential distinction

| Layer | Purpose/current evidence |
|---|---|
| `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` | Live playable game. Contains migrated map plus later additive repairs/gameplay/manual work. |
| `Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity` | Approved sourced/compressed map-lab foundation, not the current gameplay scene. |
| `Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity` | Separate MINI-121/122 spline/material/junction proof. |
| `Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity` | MINI-123 combined rural spline/bridge proof; no documented live migration. |
| `Assets/UpIzUpMini/Scenes/GrandBayProof_Mini142Review.unity` | Historical additive house/crop/local-road review promoted in MINI-142; NOT safe to promote again over today's game. |

The district manifest currently says `workflowStage: migration`, mapTruth/graybox approved, runtimeStatus `mini_102_evidence_ready_pending_retest`. Some old ledger prose still says graybox; report the discrepancy, don't silently call the map runtime-accepted. During this documentation audit, `Test-MapDistrict.ps1 -Stage migration` returned valid=true, zero errors. That validates declared metadata, not present live driving/collision.

Do not infer that an MB spline in an isolated proof is already in the EXE. Survey active live components/meshes/colliders first. A binary scene-name scan is a lead, not sufficient proof of active geometry.

## 4. Tools and skills actually involved

| Tool | Use |
|---|---|
| PowerShell shell | Inspect repository, launch Blender/Unity, read logs, hashes and files. |
| `rg`, `Get-Content`, Git | Locate real implementations/history, check diffs, checkpoint exact files. |
| Blender Python `bpy` + `mathutils` | Construct real editable house geometry, materials and studio previews. |
| Cycles | Render the Blender candidates; not gameplay lighting. |
| Custom Python JSON exporter | Evaluate/triangulate meshes, consolidate geometry, palette UVs, generate LOD buffers, convert coordinates. |
| Unity Editor C# / AssetDatabase | Import mesh buffers/materials, fit art to existing lots, build colliders/LODGroups, create review scene. |
| Physics raycasts + mesh cross-sections | Survey actual ground/roads and verify the local seam repair. |
| Unity Camera/RenderTexture | Actual in-engine before/after screenshots. |
| PowerShell district tools | Scaffold/validate manifests and export retained OSM-derived map data. |
| Existing MB Road System package | Separate reversible spline proof; not the house tool and not automatically live. |
| Local image viewer | Inspect actual PNGs; reject failed shapes/rendering. |

The repeatable scripts ARE the modeling workflow. No installed map-building SKILL.md has been identified or is required by these sources. Do not invent a named skill or claim it was used. Read any real skills available in your environment before choosing them. Unlike the later wardrobe repair, these houses genuinely were modeled in Blender.

Codex's orchestration used `functions.exec`/`tools.exec_command`, PowerShell and `apply_patch`. Claude should map these capabilities to its own shell/file/image tools; Codex names are not necessarily callable. Default working directories can point to the wrong project: set Mini explicitly. Codex needed sandbox escalation because Mini was outside its writable roots; use your actual permissions and do not bypass rejection.

No Hitem3D credits, new paid services or AI image generation were needed for the house meshes. No claim is made that QGIS was manually operated in the house/local-road pass. QGIS/UTM are possible future tools; the actual map source conversion currently uses the documented local approximation.

Installed executable paths verified during this guide:

- `C:\Program Files\Blender Foundation\Blender 5.0\blender.exe`
- `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`

## 5. Exact house construction in Blender

Source: `Tools/ArtPreview/mini141_preview.py`. It seeds randomness with 141, builds local mesh/material objects, writes editable .blend files and renders outside Assets. It also builds crop samples, so running the whole script is not houses-only.

Primitive helpers:

- `cube`: unit cube scaled to dimensions, scale applied, optional single-segment bevel and Weighted Normal modifier.
- `mesh`: explicit vertices/faces through `from_pydata`.
- `rod`: low-sided tapered cone aligned between endpoints using `to_track_quat('Z','Y')`.
- `ico`: low-subdivision ico sphere for tiny details.
- `mat`: Principled BSDF with colour, roughness and optional metalness.

`house(x,two=False)` builds two reusable archetypes, not 86 individually hand-modeled houses:

- Core width 5.6m, depth 4.7m, 2.75m per storey, foundation height .32m.
- Plaster shell starts above the base; small bevels soften silhouette without subdivision-heavy walls.
- Seafoam single-storey plaster and warm peach two-storey plaster, limewashed trim, concrete, dark blue window panels, painted timber and metal roof materials.
- Two front window assemblies per storey at x offsets +/-1.75: dark panels, side reveals, sill/lintel, mullion and glazing bars.
- Central door: dark recess panel, smaller timber door, raised panels, tiny handle.
- Openings are facade geometry over a shell, NOT Boolean-cut playable rooms or functional doors.
- Two-storey variant adds balcony slab, two supports, rail and 23 balusters.
- Single-storey variant adds veranda canopy and two posts.
- Roof: two explicit pitched planes, ridge .95m above eaves, overhangs; 36 rib positions with a low-sided rod on each slope.
- Fascias, triangular plaster gables, one downpipe and three entry steps finish the exterior.

The key visual method is a coherent facade family with measured proportions and readable large forms first, then economical trim/roof detail. It does not rely on a textured cube pretending to have a modeled roof or doorway.

Prototype render setup: Cycles, 24 samples, denoising, AgX, exposure .6, 1500x1000, broad area key plus sun/world fill, fixed orthographic view. The sample ground/road/sidewalk are presentation context, not proposed live placement. House camera roughly (17,-27,20), target (0,.1,2), ortho scale 23.

Outputs: `Logs/Tasks/MINI-141/01-Houses-Grass-Preview.png`, matching .blend, and `mesh-budget.json`. User accepted the family on 2026-09-06 with all/all are good for now/continue. Approval covers pastel plaster, pitched metal roofs, framed openings/verandas and related vegetation direction; it does not certify final game fit, interiors or mobile performance.

## 6. Export from Blender to Unity

Source: `Tools/ArtPreview/mini142_export.py`. This reuses the original helper/house definitions by executing selected source sections. Avoid casually renaming its source marker strings; it uses them to delimit those sections.

This house pipeline uses custom JSON mesh buffers, NOT an FBX export round trip:

1. Build each archetype at a known origin, collect its objects deterministically, evaluate modifiers with the dependency graph.
2. Calculate loop triangles and per-corner normals, apply object world transforms, subtract export origin (0,1.8,0) for houses.
3. Convert Blender Z-up coordinates to Unity using `(x,z,y)`. This reflection also converts normals and reverses triangle winding to `[base,base+2,base+1]`. Do not swap axes without winding/normal conversion.
4. Collapse modular objects into a single Body part buffer per house. Preserve evaluated hard/smooth corner normals rather than blindly smoothing everything.
5. Assign each source colour a cell in an 8x8 palette. UVs point to cell centres; the resulting 128x128 PNG stores linear colours converted to sRGB.
6. Derive LOD1 through temporary .5 Decimate modifiers and export separate buffers. Remove those temporary modifiers afterward.
7. Write JSON vertices/normals/triangles/UVs, dimensions, LODs, palette and manifest under `Logs/Tasks/MINI-142/Export/`.

Measured production buffers:

| Archetype | LOD0 triangles | LOD1 triangles | Export bounding dimensions X/Y/Z |
|---|---:|---:|---|
| HouseOneStorey | 2282 | 1144 | 6.15 / 4.038132 / 6.155 m |
| HouseTwoStorey | 3426 | 1716 | 6.15 / 6.788132 / 6.14 m |

Bounds include eaves/veranda/steps, not just the wall box. Geometry counts alone do not prove mobile performance. Many Blender objects become one body mesh with one shared palette material in Unity; do not import every rib/baluster as a separate renderer.

## 7. Unity import and fitting to the existing houses

Sources: `Assets/UpIzUpMini/Editor/Mini142ArtIntegration.cs` and `.Apply.cs`. Generated content lives under `Assets/UpIzUpMini/Art/Environment/Mini142/`.

`ImportModels` loads JSON arrays, updates Mesh channels directly, chooses UInt16/UInt32 indices by vertex count, recalculates bounds/tangents, preserves existing asset paths and loads LOD buffers. Palette import is sRGB, 128px, point-filtered, uncompressed and without mipmaps. Shared `Palette.mat` uses `UpIzUpMini/ApprovedArtDiffuse`: opaque Lambert surface shader, Cull Off for roof/leaf planes, full forward shadows. Blender's richer roughness/metalness is not fully reproduced by this simple palette shader. Judge the Unity result, not studio lighting alone.

`PatchHouses` selects only active `Lalay_House_*` and `Highland_House_*` roots with an existing Body renderer. It skips roots already containing ApprovedHouse_MINI142 and skips gameplay-script-bearing residences, except NavMeshModifier. Special safehouses, shops, churches, named roles and imported shanties are not generic replacement targets.

For each selected house:

1. Choose one/two-storey model from the root name.
2. Add `ApprovedHouse_MINI142` under the original root, local Y rotation 180 degrees.
3. Fit full model X/Z bounds into the old Body footprint, so roofs/porches do not newly encroach on roads. Y scale uses old body height divided by 3.05 (one storey) or 5.8 (two).
4. Start at old body bounds.min.y; ray-sample named ground colliders. If ground differs by less than 2m, raise the base to at least ground+.01. This is a local fitting guard, not whole-footprint terrain flattening.
5. Keep original children but disable their renderers/colliders. Add new LOD0/LOD1 renderers and replacement simple collision.
6. LODGroup transitions: .11 and .008 screen-relative height. These are not distances in metres.
7. Main BoxCollider matches the wall volume: 5.6 by 2.75/5.5 by 4.7, correctly centred above foundation. Separate base box: 5.74 by .32 by 4.84.

86 generic exteriors were fitted. This count is not all houses on the map. The doors remain facades; no new playable interiors or room navigation were built. Existing enterable gameplay properties were preserved.

Nonuniform footprint fitting can change proportions. If a specific facade looks squashed, inspect its old Body scale, model dimensions, parent rotation/scale and door-facing direction first. Do not randomly scale every house or move the district to compensate. Any new entrance/interior feature needs explicit scope, collider/doorway tests and its own navigation checks.

## 8. Sparse grass and neighboring crop work

Grass is seven opaque blade triangles per tuft. Production uses seeded random placement near house side/back corners, ground raycasts, road/sidewalk/shore/beach exclusion and obstacle overlap checks. It groups tufts in 20m spatial chunks, disables shadow casting, uses no colliders and culls with LOD threshold .035. Historical integration produced 423 tufts in 50 chunks. Do not replace this with dense full-map alpha grass without measuring overdraw/draw calls.

MINI-142 also integrated crops; do not accidentally rerun that entire pass for a house fix. Carrot and banana exports use separate body/foliage/fruit parts with grounded pivots. Refined green/purple buds later used a shared 1024px albedo/normal bake, separate Fruit_A/Fruit_B and pistils, and LODs. Four growth-stage and attachment tests ran across the existing plot registry; save IDs and crop logic stayed intact. Details belong to MINI-142 packet and `.Crops.cs`.

The exporter defaults can create crop/bud previews too. `--exports-only` exports houses/grass/carrot/banana without the later approval-only bud preview; it is not houses-only. `--cannabis-only` uses the existing palette manifest and is not a map repair command.

## 9. Geographic data and terrain workflow

Stable map ID: `dm-dom-grand-bay-lalay-highland-v1`.

Actual source paths:

- `Tools/World/Export-GrandBayMapTruth.ps1`
- `Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json`
- `Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json`
- `Docs/MAP-ANCHORS.json`
- `Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/MANIFEST.json`

Recorded sources: retained OSM/Overpass roads/waterways/coast, Copernicus GLO30 elevation, user-local annotations/photos. Check source notes and redistribution conditions before any new data import. User photographs supplied architectural reference; do not ship commercial satellite pixels or trace proprietary 3D geometry as game assets.

The current coordinate conversion is a local metric approximation, not a completed UTM/GIS migration. Origin latitude 15.2450638, longitude -61.3181049; source EPSG:4326. The exporter calculates local X using longitude delta times 107500 and Z using latitude delta times 110650. District travel compression is approximately one third. Keep source metric coordinates separate from deliberately compressed gameplay coordinates; never apply compression twice. Working CRS explicitly says UTM 20N migration pending.

`Mini095LalayMapLabSetup` builds the separate map-lab from compact source/height data. `Mini100GrandBayMapMigration` maps approved world pieces and gameplay roles into the canonical game. `Mini011PhaseBSetup.BuildScene()` is a broad legacy full-game generator; it is NOT a safe local repair command after manual/additive work.

The live world uses collider-based mesh ground, not a Unity Terrain component. A name like Copernicus_GLO30_Terrain does not imply Terrain.SampleHeight is valid. GroundHeight uses downward Physics.RaycastAll filtered to authoritative ground names. Never use the unrestricted highest hit near roofs, walls, balconies or sidewalks.

General road/settlement recipe from the established workflow:

1. Preserve licensed source catalogue, render a curated connected phase network.
2. Give links stable IDs and snap genuinely connected endpoints to shared coordinates.
3. Do not automatically invent long gap connectors; only tiny digitizing seams may be automatic. Larger connections need reviewed IDs.
4. Resample road centres against terrain; share canonical graded height between road and terrain corridor.
5. Apply road-class width/grade/sidewalk rules, preserving relief beyond the corridor.
6. Place dense town rows and gentler hillside/farm housing with full footprint clearance. Preserve no-house coast/parcels; do not erase density to make tests easy.
7. Build bridges where retained roads meet retained waterways, with coherent deck widths, symmetric rails and graded ends.
8. Preserve active starting farm and stable inactive future parcel IDs; no giant coloured planning slabs in the player build.
9. Ground jetties on accessible land, cross the shore and visibly extend into water.
10. Validate static network/collision/grade/clearance and then test walking/driving/navigation in the actual game.

Current manifest limits are district-specific: main street 1.5%, secondary 8%, Highland connector 2.5%, inroad 3%, farm spur 3.5%, corridor blend 55m. Do not reuse a single height equation or these numbers indiscriminately for a different district. Consult both manifest and actual generator/runtime evidence if they disagree.

Current declared counts include ten road roots/colliders, four bridge groups/twelve bridge colliders, one active starting parcel/seven inactive future parcels. Declared housing counts cover more than the 86 art replacements. A manifest count is not a fresh scene audit.

## 10. Exact Dog Life local road repair

Source: `Assets/UpIzUpMini/Editor/Mini142ArtIntegration.Road.cs`; round apron helper is in `.Apply.cs`.

The user reported the Lalay road near Dog Life. Survey identified the west join near (-63.293,8.878,-144.473), with Dog Life around X/Z (-38.59,-152.24). Existing legacy ribbons/junction were present; isolated MB spline proof had not replaced them.

Two separate problems required separate geometry treatment:

**Inside wedge at junction:** `RepairRoadPreview` adds a 48-segment radius-4.30m graded round apron. Its plane uses the measured join origin, a small X/Z grade and a .012m lift to avoid flicker. It uses the road material and the same Mesh for visible geometry and MeshCollider. The old JunctionPatch_22917921_23042701 is disabled, retained for rollback. A round cover alone did not solve intersecting height offsets elsewhere.

**Overlapping ribbons at different heights:** `SmoothDogLifeSeams` clones only Road_way_387239000 and Road_Connector_00_way_387239000_way_23042701. It modifies Y only within Z -140..-124 and leaves X/Z routes, terrain, buildings, sidewalks and role placement unchanged.

`WestEdges(z)` finds the actual cross-section of Road_way_23042701 by intersecting its world-space triangle edges with the requested Z. Minimum/maximum X define road edges. No intersection throws an error.

Local measured target grade:

`WestGrade(z) = 8.878 + (z + 144.473) * .01488`

Connector vertices use full blend; the other ribbon fades with SmoothStep across centreline distance 3.3..7.5m. Transform vertices back into the source object's local space, recalculate normals/bounds and assign the resulting mesh to BOTH MeshFilter and MeshCollider. Call Physics.SyncTransforms before collision checks.

These constants belong to this surveyed location. For a new gap, derive new measurements and bounds rather than copying these coordinates into a universal fixer.

**The important failed test:** Review3 sampled an estimated straight line that left the curved road and falsely reported a gap. Review4 sampled real triangle cross-sections. Do not modify terrain because an invalid test path misses the road.

Final `ValidateDogLifeDriveLines` tests three lanes (-1.5,0,+1.5) from Z -143 to -125 in .25m increments, deriving each X from actual road edges. It filters ray hits to road colliders, requires a hit, takes the highest road surface and checks successive heights. 219 samples passed; maximum change was .02808094m per .25m; failure threshold is .045m.

This proves local sampled collision continuity, not all wheel tracks, whole-map grades, bridge approaches or driving feel. Historical `RoadCheck()` has an estimated straight diagnostic line; do not confuse it with the final cross-section validator. The latter is private; a new scoped public validator wrapper may reuse it, but no standalone existing public Run method should be invented in instructions.

## 11. The separate MB spline-road work

Installed embedded package: `Packages/com.barmetler.roadsystem`, recorded version 2.2.1. Inspect installed code/version before changing anything; no package install or migration is required just to fix a local legacy seam.

- MINI-121 copied the approved map-lab, sampled legacy road colliders for Y, retained exact X/Z for way/22917921 and way/23042701, built Bezier roads and kept legacy ribbons disabled in the proof. Source/live hashes stayed unchanged.
- MINI-122 added line-free dark asphalt with the package's 512px normal detail, not its painted-line colour texture. Two RoadAnchors join one MB Intersection; round collision apron and outer noncolliding sidewalk band finish the proof junction.
- MINI-123 extended rural routes without automatic sidewalks, retained a 4.8m dirt farm spur and 6.2m paved treatment, and tested multi-section bridge approaches. Historical complete proof has nine MB roads, 815 road vertices and nine disabled backups.

Bridge lessons: sample centre and both edges, restrict to road/ground colliders, match deck width, overlap both bridge ends in X/Z, blend grade, limit crossfall to the proof's 8-degree cap, and capture both approach directions at driver height. A flat rectangle or overhead screenshot alone is insufficient.

These are proofs, not documented live spline migration. Do not rebuild/promote them over the current scene to repair the wardrobe or a small road seam. Public Build/BuildValidateCapture methods create/rebuild proof assets and scenes; Validate/Capture methods have their own scene assumptions. Read each before use.

## 12. Safe review and historical promotion

MINI-142 first created an immutable backup at `Logs/Tasks/MINI-142/GrandBayProof-Before-MINI142.unity`, then a separate `GrandBayProof_Mini142Review.unity`. It imported assets, fitted eligible houses, added vegetation/crops, repaired the local seam and captured actual Unity views. The user saw the fitted house screenshot and explicitly asked to continue/build.

`ApplyReviewedAndBuild` then:

1. Required the current canonical scene SHA256 to equal the immutable pre-review backup.
2. Loaded source/review and compared 281 script-bearing gameplay object positions by hierarchy path, tolerance .001m. This particular historical check compares positions, not every rotation, component field or asset dependency.
3. Required 86 fitted houses and 14 cannabis plot bindings; reran crop-stage and local-road collision checks.
4. Saved the checked review as canonical and invoked the Windows build.

**Do not run that old promotion today.** The live scene has changed many times since the old backup. Its guard should refuse. Do not edit the guard, replace the immutable backup, or copy the old review over the game to make it pass. Derive a new bounded review from the current scene after obtaining integration ownership, with a new immutable backup and scoped validation.

`Review()` is not a general repair/rebuild button: it imports assets, can modify shared asset dependencies and expects to fit new houses, throwing if zero houses are fitted. Already integrated houses are skipped. Scene isolation alone does not protect shared mutable .asset files; new visual variants should use isolated paths or an explicit dependency backup/reservation.

For a new patch, protect stable role/save IDs and the full relevant transforms/component references, not merely positions. Do not normalize manual placements. Use source names/IDs and small additive tools to open, find, patch only the target and save. Record the patch recipe so future rebuilds can intentionally replay it; do not rely on undocumented scene edits.

## 13. Testing: what ran, what passed, what remains

Historical source evidence:

- `Logs/Tasks/MINI-141/mesh-budget.json` — evaluated geometry counts.
- `Logs/Tasks/MINI-142/Export/manifest.json` — exported dimensions/LOD counts/palette.
- `Logs/Tasks/MINI-142/survey.txt` — live house/road/role measurements.
- `Logs/Tasks/MINI-142/road-after-drive-lines.txt` — 219 local lane samples, max .0281m change per .25m.
- `Logs/Tasks/MINI-142/applied-validation.txt` — 281 protected positions, 86 houses and 14 crop bindings passed.
- `Logs/Tasks/MINI-142-ApplyBuild.log` — CROP STAGE PASS, ROAD COLLISION PASS, LIVE APPLY PASS, BUILD SUCCEEDED.
- Historical Windows build: 397,915,219 bytes total, about 8.46 seconds, applied 2026-09-06. This is not today's latest game build size.

Real Unity pictures under `Logs/Tasks/MINI-142/` include After-Lalay-Houses-REVIEW.png, After-DogLife-Junction-REVIEW.png and crop views. They are camera renders, not footage of somebody driving. The historical player smoke stayed alive for 15 seconds without fatal/exception matches, but logged 100523 repeated inactive CharacterController.Move warnings at uncapped headless speed. It was NOT a clean runtime sign-off. Later actor fixes are separate tasks; re-read current logs instead of assuming this old warning is still present or already proven absent.

Still not certified by that pass: both-direction driving through the local join; full house approach/door/step collision behavior; all bridge approaches; whole-map NPC navigation; mobile frame rate; new playable interiors; perfect inner sidewalk appearance. The old inner sidewalk corner was explicitly left for follow-up.

The guide-creation audit ran only read-only metadata/path/source checks and district migration validation. It did not reopen Unity or rerun the old map integration, because another agent owns the live scene.

## 14. Repeatable acceptance tests for a new map/house fix

Match tests to claims. Do not weaken a valid test to disguise a defect, but first prove the test samples the intended geometry.

**House static checks:** verify model/import axes, winding/normals, bounds, palette sampling, renderer/material count, LOD transitions, original root/role preservation, ground contact across footprint, roof/porch clearance and collider coverage. Check rotated/scaled lots, not only the studio sample. A decorative doorway must not be advertised as enterable.

**Road static checks:** verify active source/collider identity, normals, mesh/collider agreement, endpoint overlap, left/centre/right continuity, maximum local steps/grades, bridge rails and under-road terrain interference. Sample bends by their real path/mesh. An unrestricted ray may hit a roof; a centre-only line misses edge holes; an overhead image can hide a vertical step.

**Whole-map structural checks:** use `Mini100GrandBayMapValidation.Validate` as an existing migration validator, then inspect failures against current accepted changes. It loads the live scene and has historical expectations. Also check manifest connections, active/future parcels, no-build zones, safehouse/shop/mission positions, spawn/fall recovery and actual NavMesh or existing navigation structures.

**Runtime acceptance:** use the real built game. Walk/run both characters, approach new houses, traverse targeted roads in both directions with the actual TMAX/car, test edge lanes/braking/turning, watch companion/patrol/police behavior, interaction prompts and camera occlusion. Record short normal-speed footage when motion is claimed. Static camera screenshots do not prove these behaviors.

**Performance:** inspect a phone-like resolution and measure target-device frame time, draw calls, visible triangles, memory and LOD popping. LODs and small atlases are design measures, not proof of mobile readiness. Preserve district budgets and record measured exceptions.

For visible fixes, keep matched before/after camera position, target, FOV, resolution and lighting. Include overview plus player/driver-height and disputed-detail views. Inspect images for blank output and actual defects; do not only check that PNG files exist.

## 15. Exact tool commands and side effects

Examples are for future authorized work. Run one stage at a time, inspect results, and do not execute mutation examples just because they appear here. Use fresh log/output names to preserve approval evidence. PowerShell variables are session-local.

### Read-only setup and district metadata check

```powershell
Set-Location 'E:\Unity\Up Iz Up Mini'
git status --short
git log -5 --oneline
Get-Content AGENTS.md
Get-Content Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/MANIFEST.json
Get-Process -Name Unity -ErrorAction SilentlyContinue
& .\Tools\World\Test-MapDistrict.ps1 -MapId dm-dom-grand-bay-lalay-highland-v1 -Stage migration
```

This district validator reads declared JSON state; it does not load/drive the game. Requesting runtime_acceptance requires explicit accepted runtime status, not changing a flag to pass. `New-MapDistrict.ps1` creates folders/metadata for a genuinely new map only. `Export-GrandBayMapTruth.ps1 -Offline` still rewrites derived map/anchor/preview outputs using retained OSM; it is NOT a read-only audit. Without Offline it also requests source data from Overpass. Do not refresh source data casually during a local repair.

After claiming a bounded implementation task, run `Tools/AIWorkflow/Invoke-Preflight.ps1` with your agent/task ID. Its parser recognizes only the first Current claim pair; inspect actual concurrent reservations rather than overwriting another task to make it pass. Documentation-only work can verify non-overlap without claiming live integration.

### Blender preview/export — writes evidence/buffers, not live Unity scene

```powershell
$blenderExe = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe'
$housePreview = Start-Process -FilePath $blenderExe -WindowStyle Hidden -PassThru -ArgumentList '--background --python "E:\Unity\Up Iz Up Mini\Tools\ArtPreview\mini141_preview.py"' -RedirectStandardOutput 'E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-preview-stdout.log' -RedirectStandardError 'E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-preview-stderr.log'
$housePreview.Id
```

This starts a separate Blender process; the script resets its working scene. It writes historical preview names, so preserve existing approvals or parameterize a new output location before running a changed candidate. Do not run scene-reset code inside an unsaved user Blender session.

After completion, inspect logs/images and require MINI141_DONE, not just a PID. For an approved export change:

```powershell
$houseExport = Start-Process -FilePath $blenderExe -WindowStyle Hidden -PassThru -ArgumentList '--background --python "E:\Unity\Up Iz Up Mini\Tools\ArtPreview\mini142_export.py" -- --exports-only' -RedirectStandardOutput 'E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-export-stdout.log' -RedirectStandardError 'E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-export-stderr.log'
```

The marker is MINI142_EXPORT_READY. Check manifest/JSON/palette coherence and budgets. Export logs are not proof of imported Unity appearance. The existing export script overwrites its fixed buffers; make isolated candidate outputs for a new style variant.

### Unity survey — no canonical scene save

Only when no other Editor/integrator is using the project:

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
$mapSurvey = Start-Process -FilePath $unityExe -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -quit -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini142ArtIntegration.Survey -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-survey.log"'
```

Survey loads the live scene and writes fixed Before-* evidence/survey.txt. Archive older evidence before reuse. Do NOT use -nographics: Camera.Render requires graphics and has produced crashes or blank captures when disabled. Unlike some wardrobe methods, these historical static methods do not exit themselves; -quit is appropriate. Read each method's lifecycle before copying flags to a Play Mode test.

After completion:

```powershell
Get-Process -Id $mapSurvey.Id -ErrorAction SilentlyContinue
rg -n 'SURVEY PASS|error CS|Exception' Logs/Tasks/MINI-142/claude-survey.log
```

### Whole-map structural validator

```powershell
$mapValidation = Start-Process -FilePath $unityExe -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini100GrandBayMapValidation.Validate -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-map-validation.log"'
```

This validator exits in batch mode. Require MIGRATION VALIDATION PASS and inspect failures; do not confuse metadata validation with this scene validation or either with live navigation.

### Compile and build after authorized changes

```powershell
$mapCompile = Start-Process -FilePath $unityExe -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -nographics -quit -projectPath "E:\Unity\Up Iz Up Mini" -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-compile.log"'
```

No graphics is appropriate only for this pure compile/import check. After it finishes, require no C# compile failures, then run relevant static/runtime/visual checks. Build only after those pass:

```powershell
$mapBuild = Start-Process -FilePath $unityExe -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-142\claude-build.log"'
```

Require BUILD SUCCEEDED after process completion. Output is Builds/GrandBayProof/UpIzUpMini.exe plus adjacent data/DLLs. Validate fresh UpIzUpMini_Data/level0 and Assembly-CSharp.dll when relevant; the EXE stub timestamp alone may remain old. A started process or no immediate log error is not success. Keep users informed, and stop only a test process you launched when necessary, never all Unity/game sessions.

Read the user's actual Player.log for reported runtime problems: company/product are in ProjectSettings/ProjectSettings.asset, historically DefaultCompany/Up Iz Up Mini under LocalLow. Explicit -logFile gives a controlled test log. Do not claim a headless startup proves driving or house collision.

## 16. Reusable house/map operating procedure

Use the existing district state machine: scaffold -> map_truth -> approved_preview -> graybox -> approved_graybox -> migration -> runtime_acceptance -> decorated. A local already-authorized repair does not require repeating every historical approval, but it must preserve approved topology/style and provide replacement evidence for the aspect being changed. A new district or changed layout follows its own gates.

For each request, separate these four layers:

1. **Geography:** source roads/coast/elevation and trust-marked anchors. Keep provenance and source coordinates.
2. **Gameplay layout:** compressed routes, grades, buildable lots, interactions, spawns and progression. Keep stable IDs and role mapping.
3. **Art:** swappable house families, roofs, trims, grass and props fitted to approved layout.
4. **Verification:** measured geometry/collision, fixed images, actual movement and performance. Each claim gets the appropriate evidence.

Do not edit all layers to solve a defect in one. A palette issue does not require terrain generation; a road-height seam does not require relocating shops; a facade improvement does not require changing missions.

Bounded work packet template:

```text
Task ID / integrator / current reservation:
User's exact reported defect and desired result:
Map ID, live scene and targeted stable objects/road IDs:
Current Git checkpoint and unrelated dirty files:
Protected topology, placements, IDs and scene/asset dependencies:
Reference and before evidence:
Measured cause (geometry / collider / grade / import / scale / navigation):
Smallest planned edit and its side effects:
New candidate outputs and immutable backup:
Acceptance thresholds and exact tests:
Before/after cameras and actual runtime route:
Triangle/material/texture/LOD/collider/light budget:
Verification logs, images, build and observed results:
User-approved / rejected / not tested aspects:
Commit, ownership release and next single action:
```

Implementation loop:

1. Read current state/ownership and classify the request; don't guess its location.
2. Survey the actual live source mesh, transforms and collision with fixed cameras.
3. Prove the root cause and preserve before evidence.
4. Reuse the appropriate existing code; parameterize new local values rather than hard-coding old Dog Life coordinates everywhere.
5. Make one isolated candidate; inspect it before integration and reject visibly bad results yourself.
6. Apply through a scoped additive patch with asset/scene backup and gameplay-state checks.
7. Compile, run focused structural/collision tests, inspect matching captures, then perform the requested actual-game test.
8. Build and verify when required for delivery. Preserve evidence and report gaps honestly.
9. Update system ledger/work packet/current state as applicable, stage only owned changes, commit and release only your claim.

For a new district, use `Tools/World/New-MapDistrict.ps1` to create its own identity/folders, never overwrite the existing Lalay manifest. Store sources, approvals and role mapping under Docs/Maps/<map-id>; derived Source/Generated/Staging under the corresponding Assets region. Keep uncertain anchors labeled candidate/artistic instead of quietly treating them as verified.

## 17. Common mistakes and recovery

- **Looks fine in Blender, wrong in Unity:** compare axes/winding, normals, shader/light response, bounds and actual lot scale. Do not judge imported results from studio screenshots.
- **Many draw calls:** use the consolidated Body/LOD buffers and palette; don't retain every Blender trim as an independent renderer.
- **House floats/sinks:** inspect ground filter, parent scale, footprint corners and source base offset. Terrain.SampleHeight is not valid for this collider world.
- **Door appears usable but blocked:** generic doors are facades. A playable entrance needs deliberate interior/door/collider/navigation design.
- **Gap test fails on bend:** prove samples are within actual road cross-sections before changing geometry.
- **Junction renders connected but vehicle snags:** inspect height overlap and collider mesh, not only the visible apron.
- **Road spikes near buildings:** ground sampling probably hit roof/wall/sidewalk. Filter authoritative colliders.
- **Bridge fixed from one side only:** inspect both deck ends/lanes and driver-height views, not just overhead.
- **Proof looks good but live map is still old:** confirm active live road/component names and migration history; proof assets aren't automatically shipped.
- **Old Apply refuses:** expected after newer live edits. Make a fresh scoped review; never bypass or replace its old immutable backup.
- **Review changes shared assets:** reserve or isolate mutable dependencies; a copied scene does not duplicate all meshes/materials.
- **Full builder erases manual work:** stop broad regeneration. Use additive tools and explicit replayable patches.
- **Old CopySerialized mesh code behaves strangely:** MINI-142 historical grass/road updates use it, while later wardrobe work exposed stale Mesh-buffer problems. If regenerated meshes become stale/corrupt, use explicit Mesh channel updates (as ImportParts does), save/reload and inspect; do not assume every historical updater is ideal.
- **No C# errors but blank pictures:** remove -nographics for render jobs and inspect output. A successful process exit is insufficient.
- **Metadata gate passes:** that is declared state, not scene collision or live driving.
- **Smoke log has massive warnings:** report and diagnose the actor/system involved; don't call startup clean or change unrelated AI without evidence.
- **Everything has sidewalks:** Lalay and rural routes have different rules; don't indiscriminately generate Highland/farm sidewalks.
- **New art loses map identity:** retain approved density, coastal exclusions, road alignments and institutions. Improve art within the layout rather than moving the place to suit the model.

## 18. Handoff and immediate next action

The reusable tools and workflow already exist; this guide makes their relationships and limits explicit. Do not install a speculative plugin or rewrite the whole world system just because the latest visible result is disappointing.

When the user reports the next specific map/house defect, first produce a read-only live survey of that location and a bounded fix proposal grounded in those measurements. Continue implementation within the user's authorization once ownership is clear. Existing active wardrobe work must not be overwritten by an old map review or build process.

Copy/paste instruction for Claude:

> Read E:\Unity\Up Iz Up Mini\Docs\CLAUDE-HOUSES-MAP-COMPLETE-HANDOFF-MINI-142.md completely, together with AGENTS.md, current ownership, MapGeneration.md, BuildAndVerification.md and WORLD-EXPANSION-WORKFLOW.md. This explains the actual Blender house construction, custom Unity export/import, local Dog Life road repair, data sources, tests and failure history. Preserve the current live scene, manual placements and other active tasks. Do not rerun old Review/Apply or the full scene builder. For my next specific house/map correction, survey the real live objects first, make one bounded additive fix, inspect before/after and actual runtime evidence, then build/document/checkpoint as appropriate. Do not claim the isolated MB spline proof is already live or that static tests certify driving.
