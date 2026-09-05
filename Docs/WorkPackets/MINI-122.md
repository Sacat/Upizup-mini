# MINI-122 — Lalay asphalt and connected MB junction proof

```yaml
task_id: MINI-122
title: Improve Lalay asphalt and connect its two MB spline roads
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: one material/junction proof, focused validation and fixed screenshots
  claude_time: 0
  external_credits: 0
  stop_condition: stop before live migration and wait for the next visual decision
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini122MBRoadLalayUpgrade.cs
  - Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity
  - Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/Staging/MBRoadLalayProof
  - Logs/Tasks/MINI-122
  - Docs/WorkPackets/MINI-121.md
  - Docs/WorkPackets/MINI-122.md
  - Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/APPROVALS.md
  - Docs/VISUAL-APPROVAL-REGISTER.md
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
  - MINI-121 / VA-007 accepted spline and rollback foundation
```

## Intent

Make the accepted Lalay spline proof look more road-like at gameplay distance and ensure its two spline pieces are connected through MB Road System rather than behaving as unrelated meshes.

## References

- VA-007 and MINI-121 evidence.
- MB Road System `Intersection`, `RoadAnchor` and generated-road APIs.
- Included Road007 surface-normal sample from the user's installed MB Road System package; its painted-line color texture is explicitly excluded because the real Grand Bay road has no lines.

## Non-goals

- No live-game migration or EXE build.
- No terrain, mission, NPC or gameplay-anchor changes. The proof-only blue houses obstructing this junction may be removed and the local sidewalk ends may be visually rounded; the source Map Lab remains untouched.
- No change to the accepted centerline, width or rollback design.
- No full road-network conversion.

## Acceptance scorecard

- [x] Both Lalay roads connect to one MB `Intersection` through `RoadAnchor` components.
- [x] The junction is visually covered without a collidable bump.
- [x] The road ends overlap one rounded, upward-normal junction collider and the nearby sidewalk follows the outer curve.
- [x] The user-rejected blue junction obstruction is absent from the proof.
- [x] The asphalt remains line-free and any subtle surface texture imports at no more than 512px with mipmaps.
- [x] Generated road mesh/collision remains within the first-pass budget.
- [x] Fixed overhead, player-height and junction screenshots captured.
- [x] Unity compile and focused validation pass.
- [ ] User visual approval received before vehicle testing/migration.

## Implementation plan

1. Rebuild the accepted MINI-121 proof deterministically.
2. Copy only the included road normal sample into staging and import it at 512px; do not use the painted-line color map.
3. Create one shared line-free Standard asphalt material using a dark weathered base color and subtle normal detail.
4. Connect both road starts to a single two-anchor MB intersection and add a non-colliding visual surface cover.
5. Regenerate/persist road meshes, validate and capture fixed screenshots.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/Tasks/MINI-122/unity-build-validate-capture.log` — pass | No |
| Connected graph | Focused intersection/anchor validation | Pass: 2 connected anchors on one intersection | No |
| Appearance | Fixed screenshots | `Logs/Tasks/MINI-122/LineFreeAsphalt-Lalay-*.png` | Yes |
| Driving feel | Later live vehicle pass | Not attempted | Yes |

## Handoff

- Files changed: `Mini122MBRoadLalayUpgrade.cs`, isolated proof scene/staging assets and this task's documentation/evidence only.
- Decisions made: Lalay uses line-free dark asphalt; only the package's 512px normal detail is used. The lined sample remains available but unused for a possible future secondary road. The two spline ends are pulled back evenly into a rounded apron with one continuous collider; a non-colliding curved sidewalk band cleans the outside edge. Two proof-only blue house masses obstructing the junction were removed without changing the source Map Lab.
- Visual locks added/changed: VA-007 preserved.
- Known limitations: Full network intersections and driving remain later gates.
- Next action: Show the three fixed screenshots and wait for user approval before vehicle testing or live migration.
- Ownership released: Yes; evidence is complete and no live scene was changed.
