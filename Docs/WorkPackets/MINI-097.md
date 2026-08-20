# MINI-097 — Lalay connectivity, bay and density correction

```yaml
task_id: MINI-097
map_id: dm-dom-grand-bay-lalay-highland-v1
title: Correct graybox roads, bridges, bay land use and Lalay density
request_owner: User
integrator: Codex
status: awaiting_user_visual_review
approval_class: C
budget:
  codex_time: One bounded map-lab correction and screenshot pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after repeatable map-lab rebuild, static passability validation, and fresh overview/Lalay/Highland/bay screenshots; do not migrate gameplay.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs
  - Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json
  - Assets/UpIzUpMini/Maps/MapLab/**
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - Tools/World/Export-GrandBayMapTruth.ps1
  - Docs/MAP-ANCHORS.json
  - Docs/MAP-SOURCE-NOTES.md
  - Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/**
  - workflow and handoff records
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
depends_on:
  - MINI-095
  - MINI-096
  - VA-004
```

## Intent

Make the approved phase-one crop read as a coherent drivable district: connect small road gaps, bridge water crossings, remove the temporary Highland highlight, reserve later map exits behind boundaries, keep the lower bay/jetty open except for the mapped church, and make Lalay visibly denser with the same Shanty Town assets already used by the main game.

## References

- User inspection crop: `C:\Users\PCSS-PC\AppData\Local\Temp\codex-clipboard-9ae7647e-88ee-455d-8e85-d5ec34a8c9c6.png`.
- User aerial/satellite references are land-use/density references only; pixels/footprints are not copied.
- Grand Bay Catholic Church: OSM way `392195638`, cross-checked against public place/church records; anchor already exists in map truth.

## Non-goals

- Do not alter the playable scene, missions, NPCs, vehicles or save data.
- Do not trace satellite building footprints.
- Do not build later-version regions outside the phase-one crop.
- Do not call driving accepted from screenshots/static colliders.

## Acceptance scorecard

- [x] Small in-crop road gaps connect without inventing broad new roads.
- [x] Every detected road/water crossing in the crop has visible, collidable bridge treatment.
- [x] Phase-one exits are blocked for later expansion.
- [x] Pink Highland outline is removed from the scene.
- [x] Lower bay/jetty house exclusion contains only the mapped church/jetty.
- [x] Lalay uses denser Shanty Town houses while preserving road clearance.
- [x] Road and bridge collider checks pass.
- [x] Four fixed screenshots produced for user inspection.
- [x] Playable scene remains untouched.

## Handoff

- Files changed: map-lab builder and generated scene; four new materials; map workflow records.
- Decisions made: Connect only small driveable-road gaps within `7.5 m`; add collidable decks and rails at detected in-crop water crossings; use the existing mapped church anchor; exclude houses from the lower bay strip; use imported Shanty Town houses for most Lalay massing; keep later exits visibly blocked plus invisible perimeter colliders.
- Visual locks added/changed: None until user accepts screenshots.
- Verification: Unity build/validate/capture passed with `172` road roots/colliders, `5` bridges (`15` colliders), `15` blocked exits and `95` house roots. Evidence: `Logs/Tasks/MINI-097/MapLab-*.png`.
- Known limitations: Static checks cannot prove vehicle feel; church is a graybox landmark until a licensed model is approved; current material count is 43 and should be atlased before mobile migration.
- Next action: User accepts/revises screenshots.
- Ownership released: Yes; graybox remains pending user approval.
