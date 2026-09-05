# MINI-121 — MB Road System Lalay proof

```yaml
task_id: MINI-121
title: Reconstruct Lalay with MB Road System without touching the approved road
request_owner: User
integrator: Codex
status: accepted
approval_class: C
budget:
  codex_time: one bounded Lalay proof, validation, and fixed screenshots
  claude_time: 0
  external_credits: 0
  stop_condition: stop before live-scene migration and wait for visual approval
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini121MBRoadLalayProof.cs
  - Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity
  - Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/Staging/MBRoadLalayProof
  - Logs/Tasks/MINI-121
  - Docs/WorkPackets/MINI-121.md
  - Docs/Systems/MapGeneration.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json
  - Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json
  - Packages/com.barmetler.roadsystem
depends_on:
  - dm-dom-grand-bay-lalay-highland-v1 approved graybox route
```

## Intent

Show the same Lalay road alignment rebuilt with MB Road System splines and generated collision, while retaining the approved road as an untouched rollback source.

## References

- Existing route: `Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json`, Lalay IDs `way/22917921` and `way/23042701`.
- Existing elevations and surroundings: `Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity`.
- Tool: installed embedded package `Packages/com.barmetler.roadsystem`, version 2.2.1.
- Geographic source/license: existing district manifest and OSM-derived route data; tool provenance is the user's Unity Asset Store download.

## Non-goals

- Do not touch or rebuild the live `GrandBayProof.unity` scene.
- Do not alter buildings, sidewalks, shops, NPCs, missions, terrain, bridges, or anchors.
- Do not delete or overwrite the approved Map Lab road.
- Do not migrate the proof until the user accepts screenshots.

## Acceptance scorecard

- [x] The proof follows the existing Lalay X/Z route and sampled road height.
- [x] The generated road has MB Road System components and collision.
- [x] The original road remains available as a disabled backup in the proof copy and untouched in the source Map Lab.
- [x] Fixed overhead and player-height screenshots captured.
- [x] Mobile-conscious mesh counts recorded.
- [x] Unity compile and focused validation pass.
- [x] User approval received before further proof development.

## Implementation plan

1. Copy the approved Map Lab scene through Unity's scene API.
2. Sample exact Lalay heights from its current road colliders.
3. Convert both Lalay polylines into MB Road System Bezier roads using one narrow 6.2m two-lane source mesh.
4. Move the copied legacy Lalay render/collision objects into a disabled backup root only in the proof scene.
5. Validate geometry, components, collision, endpoints and source-scene protection.
6. Capture fixed evidence and stop for user approval.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/Tasks/MINI-121/unity-build-validate-capture.log` PASS | No |
| Same route | Focused endpoint/height validation | Two MB roads, two retained backups, exact X/Z endpoints, 478 generated vertices; PASS | No |
| Appearance | Fixed overhead and player-height captures | `Logs/Tasks/MINI-121/Legacy-*` and `MBRoad-*` | Yes |
| Driving feel | Live drive in proof/live migration | Not attempted | Yes |

## Handoff

- Files changed: one editor proof builder, one isolated proof scene, one source mesh and two generated mesh assets, this packet and handoff ledgers.
- Decisions made: Proof scene only; legacy road retained; no live migration. The approved 6.2m width and existing material were retained so this gate evaluates route/system quality without mixing in a texture redesign.
- Visual locks added/changed: VA-007 locks the MB spline foundation, exact Lalay route and retained rollback approach; final asphalt and junction appearance remain editable.
- Known limitations: MB Road intersections and live vehicle feel remain outside this first proof.
- Next action: MINI-122 improves the proof asphalt and connects the two spline roads through a road-system junction, then returns for another screenshot decision. Live migration remains forbidden until that later gate and vehicle testing.
- Ownership released: Yes; no current scene owner while awaiting the user's visual decision.
