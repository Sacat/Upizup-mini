# NPCs & AI

## Current state

Patrol/navigation, hit reaction, recruitment/following, police heat/chase/search/recovery, and gang territory behaviour all exist and are shipped. NPC ragdoll-on-hit (MINI-119 follow-up) now covers Police, Villager, and Gang-member roles - see `Combat.md` for the ragdoll mechanism itself, this file covers the AI/behaviour layer that decides when/how an NPC acts, not the hit-resolution math.

## Architecture

- `PatrolNPC` - generic waypoint patrol for ordinary NPCs (villagers, ambient roles).
- `PoliceOfficer` - patrol + chase + heat-reactive escalation, steers around buildings (an upgrade over plain `PatrolNPC`'s direct-line movement).
- `RivalGangSpawner` / `GangMemberController` - Dog Life's distance-pooled territorial presence and the player's own Not Ah Word recruit roster; defeated/disabled pool members become available again after a cooldown once the player has left the scene.
- `TownNPCInteractable` + `NpcRole` enum - the role classification (`Villager`, `Police`, `FarmShop`, `CarDealer`, etc.) that both `Combat.md`'s ragdoll-eligibility gating and `MapGeneration.md`'s placement logic read from.
- `NpcCombatHealth` (see `Combat.md`) - the hit/health/knockdown state every combat-capable NPC carries.

## What worked / what didn't

- **(historical) NavMesh-based steering (MINI-052) was tried for companion/gang-member following and reverted back to direct-line steering** after a hill-glitching report - direct-line movement, layered onto the existing `CharacterController` scripts, was what actually worked for this project's terrain, not a full NavMeshAgent swap.
- **(2026-08-28) Extending ragdoll-on-hit to Villager-role NPCs required a genuine gap-fill, not a tweak** - Villagers previously had NO `NpcCombatHealth` component at all (confirmed via grep before assuming otherwise); only Police and Gang members did. Added via the same additive-patch pattern as `MapGeneration.md`'s ledger (a tool that finds already-placed NPCs in the live scene and attaches what's missing, rather than a full scene rebuild).
- **(2026-08-28) `FindObjectsByType` without `FindObjectsInactive.Include` silently misses pooled/inactive NPCs** - the Not Ah Word gang roster is pooled/inactive-until-recruited by design, so a first pass of an additive NPC-patch tool found only 1 of 4 gang members before this was caught and fixed.

## Open items

- Dog Life escalation/respawn/roaming behaviour was planned (MINI-112-era) - status not reconfirmed since; check `PROJECT-HANDOFF.md`'s index.
- None else specifically logged as of this system file's creation (2026-08-29).

## Key files

- `Assets/UpIzUpMini/Scripts/Interaction/PatrolNPC.cs`, `PoliceOfficer.cs`, `TownNPCInteractable.cs`
- `Assets/UpIzUpMini/Scripts/Interaction/RivalGangSpawner.cs`
- `Assets/UpIzUpMini/Scripts/Character/GangMemberController.cs`
- `Assets/UpIzUpMini/Scripts/Combat/NpcCombatHealth.cs`, `NpcRagdoll.cs` (shared with `Combat.md`)
