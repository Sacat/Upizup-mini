# MINI-168 — Geneva construction and verification lesson

Current result (2026-09-15): built in MapLab_GrandBayExpansionCopy.unity, not the live game. User authorized Unity execution after the research preview. Documentation exists to teach Claude the method; no work was delegated to Claude. Keep all later implementation under MINI-168 until a separately authorized integration task.

## Reference to construction

The earlier GENEVA-SOURCE.json and source diagram contain retained OpenStreetMap geometry. The recipe preserves road order and shared junctions, with an artistic roundabout enlargement and compressed football field. Map truth is not an excuse to claim surveyed building or pitch dimensions. Read CLAUDE-CONTINUE.md's research section for URLs and provenance. No fresh satellite pixels, paid model generation, Blender modeling, QGIS session, new plugin or external agent was used in this revision.

Tools actually used: PowerShell for source extraction, ownership/preflight, process launch, logs and hashes; Codex apply_patch for C# source edits; Unity6000.3.10f1 C# Editor API for meshes, transforms, colliders, scene save and fixed-camera rendering; System.Drawing only to draw the source schematic and resize actual renders for inspection; Git for bounded checkpoint. Existing AI-PRODUCTION-WORKFLOW.md and WORLD-EXPANSION-WORKFLOW.md supplied the production rules. A universal Blender skill was not required for this bounded procedural map change.

## Geometry and measurements

Coordinate conversion: longitude origin -61.3181049, latitude origin15.2450638; x=(lon-origin)*107500/3, z=(lat-origin)*110650/3. Unity +X=east,+Z=north,+Y=up. Dimensions below are game metres, intentionally compressed or authored.

The existing bay mesh MBRoad_way_22917921 is clipped at world X250 using per-triangle half-plane clipping. Source asset remains unmodified and original object is disabled in the copy. The replacement retains all original west-side triangle positions. The exact cross-section runs from (250,3.11,-100.25) to (250,2.98,-90.03), centre(250,3.042918,-95.14). The approach's first two vertices use those exact seam endpoints; they do not approximate a join with a visually overlapping box. Original sidewalks, kerb ramps and frontage are similarly clipped at X250 into district-owned meshes so old roadside pieces do not intrude into the roundabout.

Roundabout centre(279.14,-66.62) in X/Z, centreline radius8.5, paved width7,64 angular segments. First/last ribbon frames use the same wraparound tangent to close the seam. Island diameter9.7, top0.24 above ring; grass inset. Bay arrival is at the southwest ring sample; coastal departure at north sample. Both approaches use matching ring elevation3.042918. This is deliberately larger than the source geometry after 1/3 compression.

Grand Bay Road way548578022 retains its northern scope cut Z<=90 but restores the previously omitted eastern tail. The original west section keeps its sampled grade. Tail begins around X170 and descends to the shared coastal junction(231.27,74.67), Y13.68575. Way440101884 supplies the coastal path. Four metres of level approach at junction ends avoid having differently sloped overlapping road ribbons produce a lip. Maximum saved road centreline grade is7.96%, below the8% preview limit; main eastern tail is about6.91%, ring and bay approach0%. Main/coast/bay lanes6.2m wide; campus4.8m. Surfaces have matching MeshColliders.

The original generated terrain only extended far enough for the previous corridor. The first attempt correctly stopped at the coverage guard before claiming success. Retain GenevaTerrainBaseline.asset as the immutable pre-Geneva terrain. Extend its east-edge rows to X340 at roughly2m spacing, only for Z>=-112; reuse the existing edge vertex indices for a welded seam. Do not extrude the entire southern coast into extra land. This added strip is artistic terrain extrapolation from the edge, not fresh DEM sampling. Grade from explicit connected road profiles, leaving road surfaces0.18m above the corridor;5m inner zone and20m transition. Field grading uses its rotated footprint and18m transition. Inside the pad it remains flat; road clearance retains priority within5m of a road.

Geneva field centre(273.2,57), elevation12.88575. Local right axis normalized(.93,0,.36); forward normalized(-.36,0,.93), so it follows the mapped parcel's diagonal. Grass pad42x64; marked football pitch34x56; centre circle radius5; penalty area20x10 and goal area10x4; two goals6wide x2.44high, depth2. White0.12m line beams and0.025m simplified net strands. Mown stripe bands7m long. Three concrete sideline benches with supports. These dimensions/details are artistic game interpretation, not a survey of Geneva's equipment, stands or regulation field dimensions. The OSM polygon describes the sports-centre parcel, not the painted football boundary.

PCSS centroid(106.5267,49.2967) unchanged. Rear wing and roof now at centroid Z-10; side wings unchanged; courtyard opens north toward Grand Bay Road. Campus entrance mesh connects nearest main-road centre to courtyard interior and measures5.42% grade. This is a flip of approximate massing, not a reconstruction of the actual school's architecture.

## Actual failure and inspection lessons

1. PowerShell diagram arithmetic failed when negative numeric arguments arrived as strings. Use typed double parameters and ErrorActionPreference=Stop; command exit0 alone was not evidence.
2. The initial Unity build stopped because terrain did not cover the field. Fix coverage explicitly with a welded grid extension; do not bypass the guard or put the field on a floating plane.
3. The first same-process renders appeared to omit the restored road tail although saved mesh vertices, normals and colliders passed. We treated the image as a failure, retained it under Rejected-Geneva-Pass2, and compared actual rendered objects with collider ray hits. There was no extra blocking renderer at the junction. A separate Unity process rendered the road correctly. This supports stale generated-mesh/render state as the cause; it is not proof of a specific Unity engine defect. Final images must be made by a fresh-process FinalReview, after the build process exits.
4. The fresh render revealed that extruding every east boundary row added unnecessary land south of the bay. Restrict the extension's geographic rows. Trim inherited roadside geometry too; changing the road alone leaves unrelated kerbs in the new carriageway.
5. Flat field geometry needs supporting terrain. Added63 support samples across the playing surface; require ground within0.18m. A good-looking goal does not prove a supported playing area.
6. Endpoint equality alone does not prove a smooth junction. Add level endpoint sections, inspect both road approaches and check actual centreline slope. Full walking/driving feel remains untested.

## Exact repeatable commands

Run from E:\Unity\Up Iz Up Mini, after claiming the Unity session:

```powershell
& .\Tools\AIWorkflow\Invoke-Preflight.ps1 -Agent Codex -TaskId MINI-168
$job = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -quit -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini168Expansion.BuildGeneva -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-168\geneva-build-final.log"'
$job.Id
```

Wait for actual completion; inspect the log for exceptions and return code0. Then launch a NEW process:

```powershell
$review = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -quit -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini168Expansion.FinalReview -logFile "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-168\geneva-final-saved-review.log"'
$review.Id
& .\Tools\World\Test-MapDistrict.ps1 -MapId dm-dom-grand-bay-expansion-v1 -Stage scaffold
```

Do not use -nographics for renders. Do not launch another Editor while one owns the project. Do not run the historical MINI095/full gameplay builders. BuildAndCapture now routes to BuildGeneva; the old first-pass constructor is private. BuildGeneva patches the saved extension and retains houses, source terrain baseline and rollback meshes. Generated mesh/material files stay within this district so shared original assets are protected.

## Evidence and limits

Logs: geneva-survey.log (compile/read-only road survey), geneva-build-pass1-terrain-boundary.log (rejected), geneva-build-pass2.log, geneva-diagnosis.log, geneva-diagnosis2.log, geneva-build-final.log, geneva-final-saved-review.log under Logs/Tasks/MINI-168. Reports under Evidence: GENEVA-BUILD.txt, GENEVA-BAY-SEAM.txt, GENEVA-VALIDATION.txt, GEOMETRY-VALIDATION.txt, SCHOOL-FLIP.txt. Before-Geneva and Rejected-Geneva-Pass2 preserve images. Diagnostic09 is not the final presentation image.

Validation checks finite mesh coordinates on all axes;11 expansion road mesh/collider pairs;8% grade ceiling; school/coast and ring/approach endpoints within0.05m; field support samples; north-facing school opening; road-centre terrain clearance; protected scene and build-settings hashes. No separate Unity Test Framework assemblies were found under Assets; the task uses its project-style Editor validators plus the district scaffold validator. Static tests and renders do not establish walking, driving, NPC navigation, mobile frame rate, every old inherited junction, or final user visual acceptance. Current extension mesh count is about217,480 triangles including repeated houses; no new textures or dynamic lights. Field meshes are deliberately simple; no LOD or draw-call optimization pass yet.

Hash discipline: Evidence/protected-scene-hashes.txt remains the original historical checkpoint. Claude's legitimate MINI-169 live changes mean its old live hash no longer describes current HEAD. This authorized operation captured live/source/build-settings hashes in geneva-operation-start-hashes.txt before editing; validation compares those exact files against this operation's baseline while also preserving the original source hash check. If another authorized task later changes live gameplay, explicitly audit and record a new operation baseline; never silently overwrite history to hide a mismatch.

The next user step is visual review of the updated isolated copy. Gameplay integration and EXE remain out of scope. This document is an instructional production record for Claude, not a request for Claude to start editing.
