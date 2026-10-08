# MINI-180 — Clean Grand Bay road-network preview

```yaml
task_id: MINI-180
title: Audit and preview cleaner road junctions in the Grand Bay expansion
request_owner: User
integrator: Codex
status: implementing
approval_class: C
external_credits: 0
stop_condition: researched, measured, fixed-camera preview of specific road overlap/crowding repairs; no scene implementation before user approval
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini180RoadPreview.cs
  - Docs/WorkPackets/MINI-180.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
```

## Intent

Identify messy, overlapping or over-close road joins in the isolated Grand Bay expansion, then present a neat, connected road preview before changing the playable map or saved expansion scene.

User has additionally authorized a compact walkable beach beside the bay-side roundabout in this expansion copy. It replaces only the local seawall segment necessary for beach access; roads, the roundabout, and the playable scene remain protected.

## References

- User asks for a neat road network and approval preview.
- Public Grand Bay evidence places Geneva Playing Field by PCSS and describes village access through Geneva, Lalay and the coastal network. It informs topology only; no Google imagery/geometry is copied.
- MINI-171/172 road importer/refinement and current expansion scene.

## Non-goals

Do not alter the live playable scene, promotion/build settings, school/field/roundabout locations, or uncommitted Claude MINI-178/179 work. Do not save the preview into the expansion scene.

## Acceptance scorecard

- [ ] Read-only numerical audit finds the exact cluttered joins
- [ ] Preview replaces overlapping road ends with measured single paved junction surfaces
- [ ] Before/after fixed overhead and close junction evidence
- [ ] User approval before saved-scene change

## Evidence

- Read-only full-map audit: 25 road meshes; two at-grade non-parallel crossings require cleanup. Continuous seam pairs and existing junction-patch meshes are excluded from this count.
- Join A: `Road_user_lalay_inland_coastal_connector` / `ExpansionRoad_0`, 49.8 degrees, 0.022 m vertical separation, both 6.2 m wide.
- Join B: `ExpansionRoad_6` / `ExpansionRoad_0`, 88.3 degrees, 0.294 m vertical separation, 4.8 m / 6.2 m wide.
- `Logs/Tasks/MINI-180/ROAD-AUDIT.txt`
- Fixed overhead: `Logs/Tasks/MINI-180/Renders/before-network.png`, `preview-network.png`
- Fixed closeups: `before-Road_user_lalay_inland_coastal_connector-R0.png`, `preview-Road_user_lalay_inland_coastal_connector-R0.png`, `before-R6-R0.png`, `preview-R6-R0.png`

The render uses temporary unsaved road-shaped beveled junction surfaces. The expansion scene and playable scene remain unmodified pending user approval.

## 2026-09-26 expansion promotion and Windows build

The user explicitly changed the scope: the saved Grand Bay expansion is now the active game map. Copied `GrandBayProof_ExpansionImport.unity` into the canonical `GrandBayProof.unity` scene slot, retained the old scene in `Builds/PreExpansionSceneBackup`, and made the canonical scene the sole enabled build scene. The existing road-junction preview geometry remains unsaved and requires a separate decision.

The expansion preview build checked its expansion root, mission system, minimap and playable character. The promoted scene passed static validation after the old single-player assumption in `Mini001SceneValidation` was updated for the Franki/Sacat switcher and its camera handoff. The final Windows build succeeded at `Builds/GrandBayProof/UpIzUpMini.exe` with the promoted scene. A 15-second headless player run stayed alive and logged no exception, but repeated kinematic-body angular-velocity warnings remain; visible walking, driving and mission testing is still owed. Logs: `Logs/mini180-expansion-build-20260926.log`, `Logs/mini180-promoted-scene-validation-final.log`, `Logs/mini180-final-build-20260926.log`, and `Logs/mini180-final-player-smoke.log`.
