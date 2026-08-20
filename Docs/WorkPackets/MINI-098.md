# MINI-098 — User-corrected connector and Lalay housing rows

```yaml
task_id: MINI-098
map_id: dm-dom-grand-bay-lalay-highland-v1
title: Add the user-drawn connector and correct Lalay/coastal housing rules
request_owner: User
integrator: Codex
status: awaiting_user_visual_review
approval_class: C
budget:
  codex_time: One bounded map-lab correction and screenshot pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after repeatable map-lab rebuild, static validation, and fresh overview/Lalay/bay screenshots; do not migrate gameplay.
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
  - MINI-097
  - VA-004
```

## Intent

Convert the user's purple annotation into a normal driveable connector from the west/inland junction to the coastal road, make houses read as continuous rows on both sides of Lalay, keep the user-marked eastern coastal strip house-free, and treat the cleared jetty-side bay as a sand-and-stone shoreline.

## References

- User annotation: `C:\Users\PCSS-PC\Pictures\Map.jpg`.
- Purple pixels are a routing instruction only and must not render in the scene.
- Black-on-white labels specify housing density/exclusion rules, not artwork to import.

## Non-goals

- Do not migrate or edit the playable gameplay scene.
- Do not trace or ship the underlying satellite/overview pixels.
- Do not decorate the connector with final buildings or vegetation.

## Acceptance scorecard

- [x] User-drawn connector joins both existing roads as one collidable driveable route.
- [x] Purple annotation is absent from the generated scene.
- [x] Lalay has close housing rows on both sides with road clearance.
- [x] Eastern coastal no-house strip contains no house roots.
- [x] Existing bay/church/jetty exclusion remains intact.
- [x] Cleared jetty-side bay reads as sand and stones without blocking the road or jetty.
- [x] Four updated screenshots produced for user inspection.
- [x] Playable scene remains untouched.

## Handoff

- Files changed: map truth JSON, repeatable map-lab builder/generated scene, two shoreline materials, and workflow records.
- Decisions made: Store the user route as `user/lalay_inland_coastal_connector`; keep the annotation itself out of Unity; sample each Lalay road independently; place houses every 4.2 m before clearance filtering; require at least 30 buildings on each side (measured Side A 34, Side B 48); keep regular one-/two-storey houses dominant (59) and shanties a minority (23); use a terrain-following non-colliding sand grid with 26 non-colliding stones.
- Visual locks added/changed: None until user accepts screenshots.
- Verification: Unity build/validate/capture passed with 181 road roots/colliders, 5 bridge groups, 21 blocked exits, and 82 Lalay house roots. Evidence: `Logs/Tasks/MINI-098/MapLab-*.png`.
- Known limitations: Static evidence cannot prove driving feel; the church and regular houses are still graybox art.
- Next action: User accepts or marks corrections on the new overview.
- Ownership released: Yes; graybox remains pending user approval.
