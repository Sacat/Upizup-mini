# MINI-101 — Runtime map correction: clear Lalay and flatten Highland

```yaml
task_id: MINI-101
title: Clear Lalay shops and rebuild the Highland connection/farm pad
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: One bounded corrective map/build/validation pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after repeatable world/migration fixes, fixed screenshots, validators and one Windows build; live driving feel remains the user gate.
reserved_files:
  - map-lab builder/data/scene
  - Docs/MAP-ANCHORS.json artistic first-farm correction
  - migration/validation builders and generated-scene navigation joins
  - generated gameplay scene and coordination records
protected_files:
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
  - approved manual character/accessory profiles
depends_on:
  - MINI-100
  - rollback checkpoint 32758a1
```

## Intent

Keep both Lalay traffic lanes clear; place shops/sellers on grounded roadside lots that replace nearby house placeholders; make Lalay a mild continuous incline; join Lalay and Highland without a cliff/gap; place the gameplay farm, safehouse and planting plots together on a flat off-road Highland pad.

## References

- User screenshots: `codex-clipboard-111577a0-ec75-401e-96cc-eecfaa3d85d6.png` and `codex-clipboard-c896fdf3-7f1e-4d64-898f-4eb55c5a4a18.png`.
- Existing approved topology: `VA-005`; this task is the user's runtime correction to vertical grading and gameplay-role placement.

## Non-goals

- No final shop/house art, new assets, missions, controls, characters, vehicle tuning or paid generation.
- Do not alter user-approved chains/equipment or unrelated dirty files.

## Acceptance scorecard

- [x] Shop/seller bounds clear the paved road and sidewalks and have roadside space.
- [x] Lalay road, sidewalks, nearby houses and shops use one grounded grade.
- [x] Highland connector endpoints share elevation with Lalay/Highland roads and the approach is gentle.
- [x] Farm plots and safehouse sit on a flat pad clear of the road.
- [x] Canonical map-lab and gameplay rebuilds pass static/map validators. Legacy Edit-Mode NavMesh route remains partial; a road-following runtime join chain was added and is explicitly a user-test item.
- [x] Fixed Lalay, connector and farm screenshots exist.
- [x] Windows EXE builds; user walking/driving acceptance remains owed.

## Implementation plan

1. Preserve `32758a1` as the pre-correction rollback point.
2. Make connected road grading use shared endpoint heights and a wider terrain blend.
3. Move the artistic first-farm anchor off the inroad and flatten its bounded pad.
4. Replace generic road-centre migration with deterministic roadside role lots and remove only conflicting placeholder houses.
5. Rebuild once, validate, capture, visually inspect, then build the EXE.

## Evidence

- Map validation: `MigrationValidation-Final.log` — pass.
- Static scene wiring: `StaticValidation.log` — pass.
- Screenshots: `MINI-101-LalayShops-1280x720.png`, `MINI-101-HighlandConnection-1280x720.png`, `MINI-101-HighlandFarm-1280x720.png`, `MINI-101-Overview-1600x1000.png`.
- Windows build: `WindowsBuild.log` — success, updated scene data at 2026-08-20 16:27 local.
- Known gate: `NavMeshValidation-5.log` remains `PathPartial` in Edit Mode. The generated scene includes short bidirectional links following the approved road centreline; confirm companion/NPC traversal in the EXE.
