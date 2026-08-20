# MINI-095 — Lalay/Highland Unity map-lab graybox

```yaml
task_id: MINI-095
title: Lalay/Highland Unity map-lab graybox
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: One bounded data-to-graybox integration and screenshot pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after a separate Unity map-lab scene compiles, validates, and produces overhead plus gameplay-distance screenshots; do not migrate the working gameplay scene.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs
  - Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json
  - Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json
  - Assets/UpIzUpMini/Maps/MapLab/**
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - Tools/World/Export-GrandBayMapTruth.ps1
  - Tools/World/Build-GrandBayDem.py
  - Docs/WorkPackets/MINI-095.md
  - Docs/MAP-ANCHORS.json
  - Docs/MAP-SOURCE-NOTES.md
  - Docs/VISUAL-APPROVAL-REGISTER.md
  - Docs/CURRENT.md
  - DECISIONS.md
  - TASKS.md
  - PROJECT-HANDOFF.md
  - CHANGELOG.md
  - Logs/Tasks/MINI-095/**
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
depends_on:
  - MINI-094 map-truth preview
  - User-approved Highland and Lalay annotations supplied 2026-08-20
```

## Intent

Build a separate low-cost Unity graybox showing the approved Lalay-to-bay relationship: the full straight credit-union street is Lalay; it is a narrow two-lane road with grey paved sidewalks; the bay/coast end is relatively flat and the inland direction rises uphill; Highland occupies the user-drawn area and contains the first farm; the story jetty sits beside the coastal road on the Highland side.

## References

- User annotated map: `C:\Users\PCSS-PC\AppData\Local\Temp\codex-clipboard-29972d0d-6c9e-4c2f-9223-3b67cb6b9ef3.png`.
- User annotated satellite reference: `C:\Users\PCSS-PC\Pictures\Grandbay map information.jpg`.
- Existing Lalay aerial references supplied earlier in the project.
- Geographic source: MINI-094 OSM/Overpass extract under ODbL and `Docs/MAP-ANCHORS.json`.

## Non-goals

- Do not modify or rebuild the working `GrandBayProof` scene.
- Do not copy, trace, import, or ship satellite imagery.
- Do not add final houses, vegetation, gameplay, NavMesh, missions, characters, vehicles, or expensive assets yet.
- Do not claim 30-metre DSM data reproduces drainage, walls, steps, or road grading exactly; retain a documented smoothing layer for playability.
- Do not spend Hitem3D or Asset Store credits.

## Acceptance scorecard

- [x] Separate repeatable Unity map-lab scene builds from compact map data.
- [x] OSM road hierarchy and coastline are recognizable at compressed game scale.
- [x] Lalay reads as one narrow two-lane road with grey concrete sidewalks.
- [x] Coast end is flatter and inland Lalay uses a smooth, bump-free maximum 1.5% grade.
- [x] Terrain height is sampled from a documented Copernicus GLO-30 crop rather than the earlier directional height formula.
- [x] Highland zone, first farm, Up Iz Up block, Dog Life block, car dealer, schools, farmers co-op, and jetty are visibly labelled/blocked.
- [x] Original low-poly house massing appears tightly packed on both sides of Lalay, with short setbacks and narrow gaps/yards, without tracing satellite footprints.
- [x] Fixed overhead and gameplay-distance screenshots produced.
- [x] Scene object/mesh/material counts recorded for mobile planning.
- [x] User approved the MINI-094 road network and original Lalay street target; working-scene migration remains a separate task.

## Implementation plan

1. Extend the map exporter to produce compact real-metre polylines and approved user zones.
2. Build `MapLab_LalayHighland.unity` through one repeatable editor method using 3:1 geographic travel compression while keeping road/building dimensions playable.
3. Add stylized terrain slope, road/sidewalk ribbons, coastline/sea, graybox landmark lots, first Highland farm, jetty, and original house massing.
4. Add fixed overhead and two gameplay-distance cameras; render screenshots.
5. Compile, validate protected files remain unchanged, and stop for the user's visual decision.

Rollback point: delete the new MINI-095 scene/data/editor files; no working gameplay content is replaced.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | PASS — `Logs/Tasks/MINI-095/BuildValidateCapture-SmoothLalay.log` | No |
| Scene reproducible | Editor build + saved-scene validator | PASS — 130 roads, 69 house roots | No |
| Appearance | Fixed overhead and gameplay-distance screenshots | `Logs/Tasks/MINI-095/MapLab-*.png`; approved source targets recorded as VA-004; rejected intermediate excluded | Final art remains editable |
| Mobile content | Static object/mesh/material count | PASS — 316 mesh renderers, 25 shared materials | No |
| Working scene protected | Git diff check | PASS — no diff to gameplay scene/builder | No |

## Handoff

- Files changed: Map data/DEM, repeatable map-lab builder, separate map-lab scene/materials, exporter and workflow records.
- Decisions made: User approved OSM road network, original Lalay street look, Highland first-farm relationship, and a smooth bump-free Lalay road.
- Visual locks added/changed: `VA-004`.
- Known limitations: Satellite is reference-only; houses are original massing rather than traced footprints; landmark outlines are placeholders; this scene is not yet the playable gameplay scene.
- Next action: Plan the generated-gameplay-scene migration as its own rollback-safe task.
- Ownership released: Yes.
