# MINI-168 — Grand Bay expansion copy

```yaml
task_id: MINI-168
title: Expand the approved Lalay/Highland/By-the-Bay map system across the rest of Grand Bay in an isolated copy
request_owner: User
integrator: Codex
status: implementing
approval_class: C
budget:
  codex_time: one complete map-truth and isolated graybox cycle, with bounded visual correction passes
  claude_time: 0
  external_credits: 0
  stop_condition: stop before gameplay migration; retain the expansion only as a separate map-lab scene and data package
reserved_files:
  - Docs/WorkPackets/MINI-168.md
  - Docs/Maps/dm-dom-grand-bay-expansion-v1/
  - Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-expansion-v1/
  - Assets/UpIzUpMini/Editor/Mini168GrandBayExpansionCopy.cs
  - Assets/UpIzUpMini/Scenes/MapLab_GrandBayExpansionCopy.unity
  - Logs/Tasks/MINI-168/
  - TASKS.md
  - PROJECT-HANDOFF.md
  - CHANGELOG.md
  - DECISIONS.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity
  - Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json
  - Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json
depends_on:
  - MINI-099 / VA-005 approved Lalay, Highland and bay relationships
  - MINI-123 reversible MB Road System proof
```

## Intent

Create a geographically recognizable, compressed 3D map-lab copy that expands the existing Lalay, Highland and church/By-the-Bay area into the remaining Grand Bay road network. The copy must use the same terrain, road, settlement, house-family and landmark language as the accepted area while keeping every live gameplay scene and approved source unchanged.

## References

- Approved source copy: `Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity`.
- Existing licensed catalogue: `Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json` (132 OSM-derived roads, 21 waterways, 20 anchors).
- Existing elevation input: `Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json`.
- Existing geographic rules: `Docs/WORLD-EXPANSION-WORKFLOW.md`, `Docs/MAP-ANCHORS.json`, and `Docs/MAP-STRATEGY.md`.
- Source and licence record: `Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/SOURCES.md`.
- User direction, 2026-09-14: expand to the rest of Grand Bay in a similar style, leave it as a copy, and do not implement it in the game yet.

## Non-goals

- Do not modify, rebuild, or open-save `GrandBayProof.unity`.
- Do not migrate the expansion into gameplay, the build list, missions, NPCs, navigation, save data, or progression.
- Do not change VA-005's accepted Lalay/Highland/bay topology or MINI-123's accepted spline construction.
- Do not ship satellite imagery or unlicensed map pixels.
- Do not treat all 132 source roads as equally important; curate a connected playable hierarchy and retain excluded source features in data.
- Do not decorate before the expanded road/coast/elevation/landmark relationships pass the graybox gate.

## Initial district plan

- New map ID: `dm-dom-grand-bay-expansion-v1`.
- New isolated scene: `MapLab_GrandBayExpansionCopy.unity`.
- Seed the scene from the approved combined spline proof so the finished Lalay/Highland/bay work remains the spatial and visual baseline.
- Extend the connected road hierarchy into the remaining Grand Bay settlement, institutions, residential branches, coast connections and district gateways.
- Use one metre per Unity unit before deliberate global compression; preserve anchor order, road direction, coast/hill relationships and major junction identity.
- Keep paved main roads line-free and approximately 6.2 m where two-vehicle access is needed; use narrower shoulders/no sidewalks on rural and secondary routes unless local evidence supports sidewalks.
- Preserve relief outside graded road and lot corridors. Use collider-aware road grading and explicit bridge approaches.
- Reuse the established Caribbean house families with district density rules, footprint clearance, LOD/culling budgets and stable IDs.

## Acceptance scorecard

- [x] District scaffold validates.
- [ ] Map-truth package records bounds, source dates, licences, trust-marked anchors and retained/excluded source features.
- [ ] Cheap overhead preview shows the existing accepted district and proposed expansion distinctly.
- [ ] User approves or corrects the expansion topology before detailed graybox generation.
- [x] Initial isolated scene copy is generated repeatably from the approved proof without changing its hash; expansion generation remains pending.
- [ ] Roads are connected, collidable, graded and clear of buildings.
- [ ] Coast, waterways, institutions, residential density and gateways remain recognizable at compressed scale.
- [ ] Fixed overview, town, coast and inland player-height images are captured.
- [ ] Geometry/material/collider budgets are recorded.
- [ ] Unity compile and task-specific validation pass.
- [x] Source scene and live gameplay scene hashes remain unchanged at the copy checkpoint.
- [x] Expansion remains absent from the player build at the copy checkpoint.

## Implementation plan

1. Scaffold the new district package and record exact source coverage.
2. Audit the 132-road catalogue against the currently retained phase-one network and classify remaining roads by playable importance.
3. Create a labelled overhead proposal with uncertainty and connection gates visible.
4. After topology review, generate the isolated scene copy through an idempotent Unity editor tool.
5. Extend terrain, spline roads, bridges, lots and landmark massing in bounded sectors.
6. Validate continuity, grades, collision, clearance, source hashes and build exclusion.
7. Capture fixed visual evidence and stop before gameplay migration.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Project ownership | Required preflight | Ownership ready; only unrelated `Packages/packages-lock.json` was already dirty | No |
| Existing source inventory | JSON audit | 132 roads, 21 waterways, 20 anchors; approved spline proof available | No |
| Expanded topology | Labelled overhead comparison | Pending | Yes |
| District scaffold | `Test-MapDistrict.ps1 -Stage scaffold` | PASS, zero errors | No |
| Isolated implementation | Unity scene/hash validator | `Logs/Tasks/MINI-168/copy-build.log` and `copy-validation.log` PASS; protected hashes in `protected-scene-hashes.txt` | No |
| Appearance | Fixed overview and player-height renders | Pending | Yes |
| Gameplay migration | Explicitly excluded | Not attempted | No |

## Handoff

- Files changed: task/claim records, valid district scaffold, idempotent copy/validator tool, and `MapLab_GrandBayExpansionCopy.unity`.
- Decisions made: expand from the approved spline proof as an isolated copy; protect the live and approved source scenes; keep the copy out of build settings.
- Visual locks added/changed: none; VA-005 remains protected.
- Known limitations: exact local names, entrances and some institutional footprints still need user/local verification; OSM coverage is source data rather than automatic gameplay scope.
- Next action: classify the full-road catalogue, define the expansion boundary and sectors, then build the labelled overhead expansion preview before detailed terrain/road generation.
- Ownership released: No; MINI-168 remains active.
