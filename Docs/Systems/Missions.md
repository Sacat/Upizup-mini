# Missions

## Current state

The mission state machine and content generation are shipped and have been through many iterative correctness passes (MINI-053 through MINI-119-era fixes). Every marked objective uses one universal blinking-yellow minimap marker that clamps to the radar edge when distant. A retrospective-completion rule exists (owning/having-already-done something credits the objective instead of forcing a repeat purchase/action), balanced against story-gated content staying genuinely locked.

## Architecture

- `Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs` - the runtime state machine.
- Mission CONTENT itself is serialized by `BuildMissions` inside `Mini011PhaseBSetup.cs` - editing only the saved scene is temporary and gets overwritten on a rebuild, same caution as `MapGeneration.md`.
- Objective kinds (`ObjectiveKind` enum) are generic (`TalkTo`, `ReachArea`, `HarvestCrop`, `SellCrop`, `BuyItem`, `DefeatAllRivals`, `RestAtSafehouse`, etc.) - a data-driven pattern already, each mission is a list of these plus marker positions, not bespoke per-mission code.

## What worked / what didn't

- **(2026-08-21, MINI-109) "Retrospective completion" needed an explicit, consistent rule** - a `BuyItem` objective now credits owning the item outright instead of demanding a repeat purchase. Before this fix, mission-completion logic sometimes disagreed with what the player's actual inventory/progression state already proved true.
- **(historical) Stale mission markers after a map migration**: `Mini100GrandBayMapMigration.RemapMissionObjectives()` baked a marker position BEFORE an NPC was moved to their final migrated position, producing a marker that pointed at the wrong spot. Fixed by reordering (remap objectives AFTER final placement, not before). Lesson: any code that bakes a position into mission data must run strictly after all placement/migration steps that could move the referenced object.

## Open items

- Per `Docs/CLAUDE-HANDOFF-CURRENT.md`'s section 6 (narrative direction, written 2026-08-21 - confirm against `Docs/CURRENT.md`'s top entries whether these have since shipped): Normy's item-favour/Boat Man bridge, Rasta's strain ladder, Dog Life escalation, Brakes' community missions, and cellphone unlock were the planned next narrative beats. Status needs reconfirming - large parts of the roadmap between then and MINI-119 may already be done; check `PROJECT-HANDOFF.md`'s index before assuming any of this is still pending.

## Key files

- `Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs`
- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (`BuildMissions` and related methods)
- `Docs/GAME-DESIGN.md`, `Docs/STORY.md` (narrative source of truth)
