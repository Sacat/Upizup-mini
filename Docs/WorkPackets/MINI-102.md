# MINI-102 — Highland terminology and Lalay runtime clearance

```yaml
task_id: MINI-102
title: Correct Highland/Lalay roles, south Backstreet, safehouses and GTA-style minimap
request_owner: User
integrator: Codex
status: complete_pending_user_runtime_acceptance
approval_class: C
budget:
  codex_time: One bounded correction, documentation, evidence and Windows build pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after the corrected generated scene, fixed screenshots, GTA-style minimap, handoff prompt and Windows build.
reserved_files:
  - authoritative world/scene builders and validators
  - Assets/UpIzUpMini/Editor/Mini052NavMeshValidation.cs
  - Grand Bay map data, scenes and district records
  - player-facing mission/dialogue/shop text using Montine
  - Assets/UpIzUpMini/Scripts/UI/GtaMiniMapController.cs
  - current design/handoff records
protected_files:
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
  - approved manual character/accessory profiles
depends_on:
  - MINI-101
  - checkpoint 9ed0e8d
```

## Intent

Highland is the only player-facing name for the first farming district. Lalay's paved road remains clear and mission roles occupy deliberate districts. Boss J, Normy and police walk short Lalay beats; both gangs retain distinct Lalay blocks; Paro remains reachable; Brakes, a white-clothed priest, stands by the church; the boat man and boat remain at the jetty. The incorrect north-side Backstreet is removed and a smooth two-vehicle Backstreet follows the user's purple south-side reference. A GTA-style minimap shows the live world, role blips and a transparent red/blue wanted overlay at 50%+ heat.

## References

- User screenshots: `codex-clipboard-e37e59cb-50b6-4412-aa9b-b1f2c720291b.png` through `codex-clipboard-70ed3934-78e3-4919-b63e-3c6073ed3e4d.png`.
- Existing approved map topology: `VA-005`; this packet applies runtime correction evidence without replacing geographic provenance.

## Non-goals

- No new paid assets, character changes, combat changes, packages, render-pipeline changes or mobile control art.
- Keep save-facing item ID `land_montine` as a legacy compatibility key while hiding that obsolete name from the player.

## Acceptance scorecard

- [x] No player-facing game text says Montine; it says Highland.
- [x] Shop stalls, sellers, bosses and other stationary interactables clear the Lalay travel lane.
- [x] Starting respawn is the Highland safehouse; Lalay safehouse is a detailed two-storey locked property.
- [x] Boss C's Range Rover is parallel and outside the travel lane.
- [x] Lalay procedural houses have lightweight doors/windows.
- [x] Backstreet is continuous, gently graded, collidable and clear of buildings.
- [x] Backstreet is south of Lalay as marked; no accidental northern duplicate remains.
- [x] Boss J, Normy and police walk short clear Lalay beats; gang blocks remain distinct.
- [x] Brakes is interactable beside the church in full white; Paro, boat man and boat are present.
- [x] GTA-style minimap follows the active character and shows a transparent police overlay from 50% heat.
- [x] One Highland farm starts active and multiple future parcels remain progression locked.
- [x] Fixed screenshots, validators and Windows build succeed; live driving/NPC acceptance remains owed.

## Implementation plan

1. Add Backstreet to compact map data and rebuild the separate map-lab.
2. Strengthen deterministic lot clearance and roadside placement in the migration builder.
3. Replace the exposed Lalay shelter with a two-storey purchasable house and set Highland as default respawn.
4. Replace player-facing Montine strings while preserving legacy save IDs.
5. Validate, capture, update handoff documentation, commit and build the EXE.

## Evidence

Static/map validation passes under `Logs/Tasks/MINI-102/`. Fixed evidence includes `MINI-102-Overview-1600x1000.png`, `MINI-102-Backstreet-1280x720.png`, `MINI-102-LalayShops-1280x720.png`, `MINI-102-HighlandConnection-1280x720.png`, `MINI-102-HighlandFarm-1280x720.png`, `MINI-102-Church-Brakes-1280x720.png`, `MINI-102-Jetty-BoatRoute-1280x720.png`, `MINI-102-DogLifeBlock-1280x720.png`, `MINI-102-BossCBlock-Rover-1280x720.png`, and `MINI-102-Paro-1280x720.png`. Release gates: `MigrationValidation-Release.log`, `StaticSceneValidation-Final.log`, `NavMeshValidation-Final-2.log`, `MiniMapHeatValidation-2.log`, and `WindowsBuild.log`. The built EXE was launched and inspected at runtime; the minimap rendered in the lower-left and Player.log contained no exceptions.
