# MINI-100 — Migrate approved Grand Bay map into gameplay

```yaml
task_id: MINI-100
map_id: dm-dom-grand-bay-lalay-highland-v1
title: Rollback-safe migration of VA-005 into GrandBayProof
request_owner: User
integrator: Codex
status: evidence_ready_waiting_user_runtime
approval_class: C
budget:
  codex_time: One bounded environment migration, regression gate and evidence pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after generated-scene migration, static regressions and fixed screenshots; hands-on walking/driving remains the runtime acceptance gate.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - migration and coordination records
protected_files:
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json
  - Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
depends_on:
  - MINI-099
  - VA-005
  - rollback commit aed8854
```

## Intent

Replace the old synthetic test environment in the generated playable scene with the accepted Lalay/Highland map while preserving players, missions, NPCs, vehicles, farming, shops, progression and save IDs. Keep the isolated map-lab and Git checkpoint as backups.

## Non-goals

- No final art replacement, URP/input migration, combat expansion or new paid assets.
- Do not change the approved map-lab source/scene.
- Do not claim walking/driving feel without a user runtime test.

## Acceptance scorecard

- [x] Authoritative generated scene applies migration repeatably.
- [x] Old test environment is absent/disabled; accepted map environment is present.
- [x] Stable gameplay role IDs remain present and are mapped to new anchors.
- [x] Players/safehouse, starting farm, mission NPCs, police, vehicles and jetty remain present and grounded; the Sacat-to-farm NavMesh route is complete.
- [x] Existing static validators/regressions pass.
- [x] Fixed overview and gameplay-distance screenshots exist.
- [x] Isolated map-lab and rollback commit remain unchanged.
- [ ] User walking/driving acceptance remains explicitly owed.

## Evidence

- Rebuild: `Logs/Tasks/MINI-100/BuildScene-2.log`
- Migration gate: `Logs/Tasks/MINI-100/MigrationValidation-2.log`
- Scene wiring: `Logs/Tasks/MINI-100/StaticSceneValidation.log`
- Navigation: `Logs/Tasks/MINI-100/NavMeshValidation.log` (complete 8-corner Sacat-to-Highland route; safehouse footprint excluded)
- Windows build: `Logs/Tasks/MINI-100/WindowsBuild.log` (`Builds/GrandBayProof/UpIzUpMini.exe`, succeeded)
- Screenshots: `Logs/Tasks/MINI-100/MINI-100-*.png`
