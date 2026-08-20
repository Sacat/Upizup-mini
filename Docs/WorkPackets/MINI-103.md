task_id: MINI-103
title: Lalay roadside access, police patrols, yellow objective and skippable intro
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  external_credits: 0
  stop_condition: Implement only the user's six listed corrections, capture evidence, then build.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapValidation.cs
  - Assets/UpIzUpMini/Scripts/UI/GtaMiniMapController.cs
  - Assets/UpIzUpMini/Scripts/Dialogue/OpeningConversationController.cs
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
protected_files:
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat

## Intent

Move gangs and commerce to usable sidewalk frontage, park Boss C's Rover clear and parallel, provide three distinct sidewalk police patrols, display the live mission destination in yellow with a hint, and allow E to advance the opening dialogue.

## Non-goals

No new systems, characters, missions, map topology, paid assets, or unrelated visual changes.

## Acceptance scorecard

- [x] Gang and shops visibly accessible beside Lalay sidewalk
- [x] Rover clear of traffic and aligned to road
- [x] Three police have distinct Lalay sidewalk patrols
- [x] Active objective is yellow on minimap and hint is visible
- [x] E advances opening dialogue without leaving the mission briefing delayed
- [x] Fixed screenshots inspected before Windows build

## Evidence

- `Logs/Tasks/MINI-103/MigrationValidation.log`: pass.
- `Logs/Tasks/MINI-103/Capture-D3D11.log`: pass; fixed screenshots in the same folder.
- `Logs/Tasks/MINI-103/WindowsBuild.log`: success, 387,522,526 bytes.
- `Tools/World/Test-MapDistrict.ps1 -Stage migration`: pass with zero errors.
