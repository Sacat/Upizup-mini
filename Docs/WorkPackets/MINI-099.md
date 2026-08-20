# MINI-099 — Highland road simplification and beach-side correction

```yaml
task_id: MINI-099
map_id: dm-dom-grand-bay-lalay-highland-v1
title: Simplify Highland roads, smooth two-vehicle travel, and move beach across from church
request_owner: User
integrator: Codex
status: accepted
approval_class: C
budget:
  codex_time: One bounded map-lab correction and screenshot pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after repeatable map-lab rebuild, road/bridge/house static gates and fresh screenshots; migration is a separate task.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs
  - Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json
  - Assets/UpIzUpMini/Maps/MapLab/**
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - map workflow and handoff records
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
depends_on:
  - MINI-098
  - VA-004
```

## Intent

Keep the accepted Lalay house placement while adding more between-intersection density. Simplify the Highland network into clean connected roads, grade every retained driveable road/bridge for two vehicles, keep the church on ordinary ground, and place sand/stones only across the coastal road toward the water with no houses on it.

## References

- User guideline: `C:\Users\PCSS-PC\AppData\Local\Temp\codex-clipboard-547e1fa7-8fc6-47c2-8224-51dbb002df19.png`.
- MINI-098 user-routed connector and no-house decisions remain source intent.

## Non-goals

- Do not migrate or edit the playable gameplay scene.
- Do not reproduce the reference pixels as shipped map art.
- Do not add final vegetation, drains, traffic or landmark models.

## Acceptance scorecard

- [x] Highland has a simpler, legible connected road network without overlapping connector clutter.
- [x] Every retained driveable road is smooth, collidable and at least two-vehicle graybox width.
- [x] Every road/water crossing has a centered bridge with equal side spacing.
- [x] Church and church-side land are not sand.
- [x] Sand/stones exist only across the coastal road toward the water.
- [x] No houses overlap sand or the eastern coastal exclusion.
- [x] Both Lalay sides remain dense, with increased total infill.
- [x] Updated overview, Lalay, Highland and bay screenshots produced.
- [x] Playable scene remained untouched.

## Handoff

- Files changed: repeatable map-lab builder, compact map data, generated map-lab scene, district records.
- Decisions made: nine-road phase network; paved roads share a 6.2 m width; Highland connector grades are capped at 4.5%/5.5%; Lalay remains dense; Highland uses 18 regular/two-storey/apartment placeholders; eight farm parcel IDs exist, with only plot 1 active.
- Visual locks added/changed: VA-005 records the accepted corrected graybox and migration permission.
- Known limitations: Static screenshots cannot prove vehicle steering feel; that is the runtime migration gate.
- Next action: MINI-100 rollback-safe gameplay migration.
- Ownership released: Yes after checkpoint.
