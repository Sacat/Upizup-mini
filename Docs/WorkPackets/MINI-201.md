# MINI-201: One road look, smooth junctions, roads level with the ground

```yaml
task_id: MINI-201
title: Unify road material, remove junction steps, sit roads on the ground, smooth bumps (live GrandBayProof)
request_owner: User ("fix the roads ... where the roads intersect, some of the roads are too high, should be smooth with the ground ... same colors and textures ... all roads sort of flat so the bike rides without bumps")
designer: Claude cloud session (no Unity): survey + height solve + renders from the live scene file
integrator: local machine (Unity): run the preview tool, inspect, then apply to live
status: tool + data ready, NOT yet run in Unity
protected: Assets/UpIzUpMini/Scenes/GrandBayProof.unity is only touched by "Apply To Live", which backs it up first
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini201RoadSmooth.cs
  - Docs/WorkPackets/MINI-201/*
  - Tools/RoadPipeline/mini201_road_smooth/*
  - Assets/UpIzUpMini/Art/Environment/Mini201/* (created by the tool)
```

## What was measured (live scene, read with UnityPy; every number from the real meshes)

27 active road meshes + 4 bridge decks + `MINI168_Expansion/ExpansionTerrain` (the only active ground).
Map: `MINI-201/renders/live_road_map_materials_and_steps.png`.

| Problem | Before |
|---|---|
| Road materials | 3 different: `ExpansionAsphalt` (14 pieces, grey 0.16), `MainRoad` (5, 0.17), `SideRoad` (7, lighter 0.28) + `DirtTrack` farm spur |
| Junction steps (two road surfaces over the same spot) | up to 2.07 m (backstreet × way_254679575); 0.3–0.84 m at ExpansionRoad_0 × roads 1/2/4/5/6 and × the Lalay coastal connector; 0.67 m ExpansionRoad_8 × junction patch 1 |
| Stacked duplicate road | ExpansionRoad_1 runs on top of the Lalay coastal connector for ~60 m, 0.6 m higher |
| Road floating | RoadJunctionPatch_1 0.69 m, coastal connector 0.69 m, Dog Life junction 0.68 m above ground |
| Road buried | Road_way_254679575: 68% under terrain (median −0.27 m) |
| Terrain poking through roads | 5.6% of road surface samples, up to 2.6 m |
| Bumps (grade change per 0.5 m) | up to 5.8% (Road_way_387239000), 5.6% (Highland inroad) |

## The fix (computed outside Unity, applied by the tool)

`Tools/RoadPipeline/mini201_road_smooth/solve_roads.py` solves one sparse least-squares height field for all road vertices:
- smoothness along each road centre line (arc-length second difference) and level cross-sections;
- every road vertex lying on another road's surface gets that surface's height (junctions share one height);
- roads stay where they are unless buried (>5 cm under) or floating (>15 cm over); then they go to ground + 3 cm;
- Lalay roads with sidewalks/kerbs (way_22917921 trim, way_23042701, Dog Life junction, connectors) are held;
- roads within 8 m of any building/stall/fence/NPC are held at their current height;
- bridge decks are never changed; roads meet them.

Terrain is then fitted: cut where it would poke through a road, raised only where a road edge would float >12 cm,
**never raised within 6 m of a building/prop/NPC**, and never changed in the gully under a bridge.
Duplicate road triangles that lie on top of a higher-priority road are dropped (no z-fighting with one material).
X/Z of every vertex is unchanged.

| Check | After |
|---|---|
| Road materials | 1 (`ExpansionAsphalt.mat`) on all 27 road objects (farm spur included) |
| Worst junction step | 6.4 cm (ExpansionRoad_0 × 1), all others ≤ 5.5 cm |
| Terrain through roads | 0.15% of samples (near two Lalay houses where the ground is protected) |
| Buried roads | none |
| Worst bump | 4.9% (Road_way_180962530 at its junction), ≤ 4.4% elsewhere, most roads ≤ 2% |
| Objects whose ground moved > 15 cm | none; 2 objects are ground-followed (Market_CLOTHES −3 cm, FutureExitBarrier_01 −5 cm) |
| Covered duplicate road triangles dropped | ~70; `Road_Connector_00_way_387239000_way_23042701` fully covered → disabled |

Before/after renders: `MINI-201/renders/before_after_*.png` (Blender, from the extracted live geometry, not Unity).
Full numbers: `analysis_before.txt`, `analysis_after.txt`.

## Run it on the local machine

```
git fetch origin cloud/sacat-legs
git checkout origin/cloud/sacat-legs -- Assets/UpIzUpMini/Editor/Mini201RoadSmooth.cs Docs/WorkPackets/MINI-201 Docs/WorkPackets/MINI-201.md Tools/RoadPipeline
```
Then in Unity (or batch):
1. **Up Iz Up Mini → MINI-201 → Preview On Copy (safe)**. It copies the live scene to `GrandBayProof_RoadFix201.unity`, applies, saves the copy, writes
   `Logs/Tasks/MINI-201/APPLY-LOG.txt`, `VALIDATION-copy.txt` and `Renders/before_*.png` / `after_*.png`.
   Batch: `Unity.exe -batchmode -quit -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini201RoadSmooth.PreviewOnCopy -logFile Logs/mini201-preview.log`
   (omit `-nographics`, the renders need a GPU).
2. Open the copy, drive the bike over the junctions listed above in both directions.
3. If approved: **MINI-201 → Apply To Live GrandBayProof**. It first copies the scene to `Builds/PreMini201SceneBackup/GrandBayProof.unity`.

Safety built into the tool: every road and the terrain are found by full hierarchy path before anything changes; every vertex's
recorded world X/Z/Y must match the scene (2 cm) or the run aborts without saving (stale data). Original mesh assets are not
overwritten; new meshes go to `Assets/UpIzUpMini/Art/Environment/Mini201/`.

## Not verified (no Unity in the cloud)
- C# compile (no compiler available in the sandbox; written against APIs already used in this project).
- Unity renders and real riding. The validation file measures drive-line steps (pass < 6 cm per 0.25 m) and terrain above road (< 0.5%).
- Sidewalks/kerbs/frontages beside the held Lalay roads: those roads move ≤ 12 cm, check the kerb joins visually.
- Navigation/AI waypoints that store absolute Y near moved roads (largest road moves: Road_way_254679575 1.5 m, ExpansionRoad_5 1.3 m, backstreet 1.2 m).
