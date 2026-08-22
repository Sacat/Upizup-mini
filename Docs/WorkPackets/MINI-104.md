task_id: MINI-104
title: Highland mission target, sea boat, chase jumping and progression locks
request_owner: User
integrator: Codex
status: implementing
approval_class: B
budget:
  external_credits: 0
  stop_condition: Implement only the six corrections in the user request and produce fixed evidence before build.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapValidation.cs
  - Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs
  - Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
  - Assets/UpIzUpMini/Scripts/Navigation/NpcObstacleJumpMotor.cs
  - Assets/UpIzUpMini/Scripts/Interaction/PoliceOfficer.cs
  - Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity

## Intent

Put the jetty boat in water, repair M1 Highland completion/marker/distance at the safehouse-farm entrance, add low-obstacle chase jumps for police and gangs, make Brakes' shirt visibly white, and hide/lock Gardey Zafeh until Black Sugar progression.

## Non-goals

Do not relocate Highland itself until the user manually identifies the intended geographic area. Do not change map roads, missions beyond the broken target/gate, or unrelated character looks.

## Acceptance scorecard

- [ ] Boat visibly floats beyond the sand edge in sea
- [ ] M1 Highland objective uses the real migrated safehouse/farm destination
- [ ] Yellow world/minimap marker and HUD distance use that same point
- [ ] Chasing police and gang fighters can jump low obstacles
- [ ] Brakes has a visibly white shirt
- [ ] Gardey Zafeh prompt/action stays hidden until Black Sugar is unlocked
- [ ] Lalay sidewalks and immediate house frontage have continuous level colliders
- [ ] Lalay houses, shops, sellers and other roadside NPCs stand on the raised frontage instead of intersecting it
- [ ] No Lalay shop, seller, boss, police officer or gang member intersects a residential building footprint
- [ ] M16 Dog Life block marker uses the migrated sidewalk block instead of the old map coordinate
- [ ] A Boat Man talk objective restores his visual/interactability at the jetty
