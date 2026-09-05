# MINI-123 — Rural Highland MB Road proof

```yaml
task_id: MINI-123
title: Extend the reversible spline-road proof to every non-Lalay phase-one road without sidewalks
request_owner: User
integrator: Codex
status: implementing
approval_class: C
budget:
  external_credits: 0
  stop_condition: stop before live migration and show fixed Highland screenshots
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini123MBRoadHighlandProof.cs
  - Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity
  - Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/Staging/MBRoadHighlandProof
  - Logs/Tasks/MINI-123
  - map workflow and handoff documentation
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity
  - Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json
depends_on:
  - MINI-121 / VA-007 accepted spline foundation
  - MINI-122 line-free rounded Lalay proof
```

## Intent

Continue the road-tool evaluation across the complete phase-one network while permanently separating town and rural road treatments: Lalay may have sidewalks; every non-Lalay road must not.

## Scope

1. Rebuild MINI-122 deterministically, then copy it to a new combined proof scene.
2. Convert every remaining active phase-one road to an MB spline using its approved collider height, X/Z route, road width and surface class.
3. Retain every original converted road disabled as rollback.
4. Generate no sidewalk, kerb or frontage objects outside Lalay.
5. Preserve dirt treatment for the Highland farm spur and line-free paved treatment elsewhere.
6. Capture fixed Highland player-height and complete-network overhead evidence.

## Non-goals

- No live-game migration or EXE build.
- No change to Highland terrain, houses, farms, dirt farm spur, missions or gameplay anchors.
- No sidewalks anywhere outside Lalay.

## Acceptance scorecard

- [x] Every retained non-Lalay route and sampled grade retained.
- [x] Line-free paved surface and matching collider generated.
- [x] Every replaced non-Lalay road retained inactive for rollback.
- [x] Zero non-Lalay sidewalk, kerb or frontage objects generated.
- [x] Source and live scenes remain unchanged.
- [x] Compile, focused validation, fixed screenshots and map-district validation pass.
- [ ] User visual approval required before migration.

## Current evidence (2026-08-29)

- The first Highland route is now a line-free, collider-backed MB Road with no generated sidewalks; its legacy road remains disabled for rollback.
- The original bridge-to-Lalay grass break is covered by a graded, road-width transition mesh. It samples the centre and both road edges, banks toward the higher terrain side with an 8-degree maximum crossfall, overlaps the bridge deck, blends into the Lalay road, and has matching mesh collision.
- Both bridge ends now use multi-section approach transitions rather than flat rectangles. Each transition overlaps the driveable deck, matches the deck width, grades into its approach road, samples only the authoritative terrain/road colliders, and carries matching collision. Separate driver-height captures verify the Lalay and Highland directions.
- Focused validation passes for the current Highland proof: one MB road, zero generated sidewalks, one rollback road and a line-free material.
- Fixed evidence includes `MBRoad-Highland-Lalay-DriveLine-1280x720.png`, which verifies from driving height that the centre path contains road rather than exposed grass.
- The remaining seven mapped routes plus the measured `way/387239000`–`way/23042701` gap connector are now converted. The proof contains nine MB roads, 815 road-mesh vertices, nine disabled rollback roads, zero generated sidewalks, line-free paved surfaces and one 4.8m dirt farm spur. No live-scene migration has occurred.
- Complete-network evidence: `MBRoad-Complete-Remaining-Network-Overhead-1280x720.png`. Map-district `approved_graybox` validation remains valid with zero errors.
