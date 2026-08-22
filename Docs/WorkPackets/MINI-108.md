# MINI-108 — Early mission clarity, waypoint navigation and Lalay cleanup

```yaml
task_id: MINI-108
title: Readable intro, persistent mission guidance, revised Boss J branch and early heat missions
request_owner: User
integrator: Codex
status: complete
approval_class: B
budget:
  external_credits: 0
  stop_condition: Implement requested early-game fixes, validate, capture UI/map/banana/Paro evidence, and build once.
reserved_files:
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scripts/Missions/ObjectiveMarker.cs
  - Assets/UpIzUpMini/Scripts/UI/MissionHUD.cs
  - Assets/UpIzUpMini/Scripts/UI/GtaMiniMapController.cs
  - Assets/UpIzUpMini/Scripts/UI/GameplayHintController.cs
  - Assets/UpIzUpMini/Scripts/UI/CheatCodeController.cs
  - Assets/UpIzUpMini/Scripts/Progression/ProgressionChoiceController.cs
  - Assets/UpIzUpMini/Scripts/Progression/ProgressionManager.cs
  - Assets/UpIzUpMini/Scripts/Progression/ProgressionGate.cs
  - Assets/UpIzUpMini/Scripts/Economy/EconomyManager.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Scripts/Interaction/SafehouseInteractable.cs
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapValidation.cs
  - Assets/UpIzUpMini/Editor/Mini108EarlyMissionValidation.cs
  - Docs/WorkPackets/MINI-108.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - User-approved Sacat/Franki models, chains, accessories and character proportions
  - Approved Lalay road topology and house placement outside explicit bridge/collider obstructions
depends_on:
  - MINI-103
```

## Intent

Make the opening readable, make distant missions navigable, repair the early career branch and heat sequence, improve Boss J/Paro dialogue, remove reported bridge/collider obstructions, and make the banana read as banana fruit.

## Acceptance scorecard

- [x] Opening dialogue has a black transparent background and remains E-skippable.
- [x] Current mission marker pulses and clamps to the minimap edge when distant.
- [x] One-time hints explain yellow mission guidance.
- [x] Six `1` presses complete the current mission.
- [x] Legitimate choice contains the requested frustration dialogue and ultimately rejoins Boss J; K goes directly.
- [x] Early cooldown is split into safehouse rest, clean-police contact and $100 Normy/$20% heat mechanics.
- [x] Paro's first interaction is dialogue-only and his rough clothing reads visually.
- [x] Bridge shanty and reported Boss C-side collider obstruction are removed/neutralized without changing approved roads.
- [x] Temporary banana bunch has curved fruit, a lighter brown stem and five top leaves; user accepted leaving it for now and requested a later Hitem3D replacement.
- [x] Compile, focused validation, generated-scene rebuild, map validation, screenshots, Windows build and smoke pass.
