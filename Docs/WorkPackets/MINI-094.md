# MINI-094 — Grand Bay Map Truth and Lalay-to-beach approval preview

```yaml
task_id: MINI-094
title: Grand Bay Map Truth and Lalay-to-beach approval preview
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: One bounded source-data and overhead-preview pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after a fixed overhead preview and anchor review are ready; do not alter the playable scene before user approval.
reserved_files:
  - Docs/WorkPackets/MINI-094.md
  - Docs/MAP-ANCHORS.json
  - Docs/MAP-SOURCE-NOTES.md
  - DECISIONS.md
  - Tools/World/Export-GrandBayMapTruth.ps1
  - Logs/Tasks/MINI-094/**
  - TASKS.md
  - PROJECT-HANDOFF.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
depends_on:
  - MINI-085 current-build safety checkpoint
```

## Intent

Create a defensible geographic foundation for the first Grand Bay district and a cheap overhead approval preview covering Lalay main street toward the beach/jetty, nearby side lanes, civic/market anchors, and the future Highland planting connection. Preserve one Unity unit as one metre and the documented Grand Bay local origin.

## References

- User references: `E:\Assets\GrandBayReference\Maps\Grandbay entire.jpg`, the supplied Lalay aerial photographs, and the supplied island heightmap.
- Existing in-game reference: current 320-metre procedural `GrandBayProof` world, used only as a gameplay baseline.
- Geographic source: OpenStreetMap raw geodata under ODbL, retrieved through Overpass API on 2026-08-20; Government of Dominica institution listings and user-owned reference images are supporting references.

## Non-goals

- Do not rebuild or modify `GrandBayProof.unity`.
- Do not change missions, characters, vehicles, combat, materials, prefabs, packages, or project settings.
- Do not trace or ship Google imagery.
- Do not invent exact Highland, Lalay-boundary, Village Council, or jetty coordinates where source truth is absent.
- Do not spend Hitem3D or Asset Store credits.

## Acceptance scorecard

- [x] Source and licence metadata recorded.
- [x] Stable local origin and coordinate conversion recorded.
- [x] OSM-backed roads, waterways/coast information, and named anchors summarized.
- [x] Unknown or artistic anchors explicitly marked unverified/approximate.
- [x] Fixed overhead planning preview produced at 1600x1000.
- [x] Mobile-distance/readability reviewed at preview scale.
- [x] Working gameplay scene remains unchanged.
- [ ] User approves or corrects the proposed Lalay-to-beach layout before scene integration.

## Implementation plan

1. Export a narrow OSM source snapshot in memory and transform relevant features into a compact, documented map-truth file.
2. Merge only defensible institution coordinates; distinguish OSM-verified, official-but-locally-unverified, and artistic placeholders.
3. Render a separate overhead planning image with roads, water, major anchors, and uncertainty labels.
4. Present the preview to the user. Stop before Unity world replacement.

Rollback point: this packet creates documentation, tooling, and evidence only. Deleting the MINI-094 files returns the project to the previous gameplay checkpoint.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Sources recorded | Metadata review | `Docs/MAP-SOURCE-NOTES.md`; OSM timestamp `2026-08-20T15:17:46Z` | No |
| Coordinate conversion | Scripted export | `Docs/MAP-ANCHORS.json`; 8 anchors with local metre coordinates | No |
| Layout preview | Fixed overhead render | `Logs/Tasks/MINI-094/LalayToBeach-Overhead.png` (1600x1000) | Yes |
| Gameplay preserved | Git diff check for scenes/builders | No `.unity`, prefab, builder, package, or settings diff | No |

## Handoff

- Files changed: Work packet, map anchors/source notes, reusable exporter/renderer, decision/task/handoff records.
- Decisions made: Highland replaces Montine as the first remote planting district (`D-009`); exact Highland and jetty pins remain unverified.
- Visual locks added/changed: None.
- Known limitations: Public map data does not establish resident-defined Lalay/Montine boundaries or every current landmark entrance.
- Next action: User approves/corrects the overhead preview, then a separate packet builds the map-lab graybox.
- Ownership released: Yes; evidence is ready for the user's Class C decision.
