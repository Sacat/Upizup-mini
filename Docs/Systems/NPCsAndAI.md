# NPCs & AI

Gang-specific behaviour (Dog Life, Not Ah Word, territory/recruitment) moved out to its own [Gang.md](Gang.md) on 2026-08-29, per the user's explicit request for a dedicated gang system ledger. This file now covers general/ambient NPC AI and Police specifically.

## Current state

Patrol/navigation, hit reaction, and police heat/chase/search/recovery all exist and are shipped. NPC ragdoll-on-hit (MINI-119 follow-up) now covers Police, Villager, and Gang-member roles - see `Combat.md` for the ragdoll mechanism itself and `Gang.md` for gang-specific AI, this file covers the general AI/behaviour layer that decides when/how an ordinary or police NPC acts.

## Architecture

- `PatrolNPC` - generic waypoint patrol for ordinary NPCs (villagers, ambient roles).
- `PoliceOfficer` - patrol + chase + heat-reactive escalation, steers around buildings (an upgrade over plain `PatrolNPC`'s direct-line movement).
- `TownNPCInteractable` + `NpcRole` enum - the role classification (`Villager`, `Police`, `FarmShop`, `CarDealer`, etc.) that both `Combat.md`'s ragdoll-eligibility gating and `MapGeneration.md`'s placement logic read from.
- `NpcCombatHealth` (see `Combat.md`) - the hit/health/knockdown state every combat-capable NPC carries.

## What worked / what didn't

- **(historical) NavMesh-based steering (MINI-052) was tried for companion following and reverted back to direct-line steering** after a hill-glitching report - direct-line movement, layered onto the existing `CharacterController` scripts, was what actually worked for this project's terrain, not a full NavMeshAgent swap.
- **(2026-08-28) Extending ragdoll-on-hit to Villager-role NPCs required a genuine gap-fill, not a tweak** - Villagers previously had NO `NpcCombatHealth` component at all (confirmed via grep before assuming otherwise); only Police and Gang members did. Added via the same additive-patch pattern as `MapGeneration.md`'s ledger (a tool that finds already-placed NPCs in the live scene and attaches what's missing, rather than a full scene rebuild).
- **(2026-08-28) `FindObjectsByType` without `FindObjectsInactive.Include` silently misses pooled/inactive NPCs** - a lesson that also applies to `Gang.md`'s pooled rosters (Not Ah Word/Dog Life), see that file for the specific incident.

## Open items

- None specifically logged as of this system file's creation (2026-08-29) beyond what's now tracked in `Gang.md`.

## Key files

- `Assets/UpIzUpMini/Scripts/Interaction/PatrolNPC.cs`, `PoliceOfficer.cs`, `TownNPCInteractable.cs`
- `Assets/UpIzUpMini/Scripts/Combat/NpcCombatHealth.cs`, `NpcRagdoll.cs` (shared with `Combat.md`)
- See `Gang.md` for `RivalGangSpawner.cs`, `GangMemberController.cs`, `FactionBrawler.cs`
