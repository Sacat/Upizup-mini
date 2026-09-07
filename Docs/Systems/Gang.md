# Gang

Added 2026-08-29 at the user's explicit request ("there should also be a gang system") - previously folded into `NPCsAndAI.md`, split out since gangs (territory, recruitment, rivalry, reputation) are a substantial, distinct gameplay system in their own right.

## Current state

Two gang-shaped systems exist: **Dog Life** (the rival gang controlling the Lalay block, hostile, pooled/distance-activated) and **Not Ah Word** (the player's own recruitable crew, paid pool members plus a couple of standalone respect-gated recruits). Both are shipped and have been through several correctness passes. A group-walk feature (Dog Life members occasionally leaving the block to walk down the road together) had a real, just-fixed ground-height bug.

## Architecture

- `Assets/UpIzUpMini/Scripts/Interaction/RivalGangSpawner.cs` - Dog Life's pool. Activates/deactivates by distance from a fixed `blockCentre` (hysteresis: `enterDistance`=26m to activate, `exitDistance`=40m to deactivate, so it doesn't flicker at the boundary), checked only against `CharacterSwitchManager.Instance.Active.root` - i.e. whichever character the player currently controls, NOT both simultaneously. Per-member respawn cooldown (`respawnCooldownSeconds`, default 100s) tracked via `NpcCombatHealth.LastDefeatedAt`, not just "has the GameObject been reactivated." The "group walk" feature temporarily swaps a member's `PatrolNPC` waypoints to send them down the road for `groupWalkDurationSeconds`, then restores their original waypoints.
- `Assets/UpIzUpMini/Scripts/Character/GangMemberController.cs` - Not Ah Word's own per-member behaviour (guard position, etc.).
- `Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs` - melee for faction-affiliated NPCs (`Allegiance` enum, e.g. `DogLife`), separate from the player's own `SimpleMeleeCombat`/`MeleeMoveLibrary` (see `Combat.md`) - NPCs don't use the combo chain, they use their own single-hit `MeleeAttackProfile`.
- Built by `Mini011PhaseBSetup.cs`: `BuildDogLifeGang` (Dog Life) and the Not Ah Word roster builder (search `BuildNotAhWordRoster`).
- Ragdoll-on-hit (`Combat.md`'s `NpcRagdoll`) is wired onto both Dog Life and Not Ah Word members via the same `NpcCombatHealth` extension every other combat-capable NPC role uses.

## What worked / what didn't

- **2026-09-07 MINI-143:** first activation bug, not a moved block: RivalGangSpawner resets an authored inactive member BEFORE Awake. ResetForRespawn assigned default homeScale=(0,0,0), then Awake captured that zero. NpcCombatHealth now shares one guarded EnsureInitialized between Awake and reset, capturing original pose/scale once. Test the real ReactivateEligibleMembers path on a never-activated member WITHOUT pre-calling Awake; prior MINI112 manual Awake masked it. Positive nonunit scale, health, visibility, defeat cooldown and repeated respawn pass. Four real-scene members restored at original roadside coordinates with empty chest box-overlap results; screenshot Logs/Tasks/MINI-143/DogLife-Visible.png. Normal patrol/block-entry playtest remains; no house/anchor relocation.

- **(2026-08-30) Dog Life's cooldown could be correct while respawn still failed afterward (MINI-130).** `RivalGangSpawner` correctly found members whose 100-second cooldown had elapsed, but `NpcCombatHealth.ResetForRespawn` threw on stale/uninitialized cached renderer data while the pooled object was inactive. Because the exception happened before `member.SetActive(true)`, the gang stayed absent and the same failure repeated every frame. Refreshing shared combat references at the respawn boundary fixes the actual post-cooldown reactivation path; the focused test now forces the old cache failure and passes.

- **(2026-08-29) Real bug found and fixed: the group-walk feature's destination waypoint never re-sampled ground height.** `patrol.SetWaypoints(new[] { start, start + roadDirection * groupWalkDistance })` kept `start`'s own Y unchanged for the new position 24m away - if that spot sits near a building at a different elevation than the block itself, the member's waypoint Y is simply wrong for what's actually there. User's report: "i only see someone floating in the air on the building opposite side" - exactly this symptom. Fixed by ground-snapping the destination via `Physics.Raycast` (this scene has no Unity `Terrain`, same lesson as `MapGeneration.md` - ground height must be raycast, not `Terrain.SampleHeight`).
- **(2026-08-29) Confirmed, not assumed, that Dog Life's own block position was never touched this session** - compared the exact same 4 members' coordinates from a survey done at the very start of this session (before any villager/zone work) against a fresh read just now: byte-for-byte identical. The user's "maybe you changed where the block is" suspicion was reasonable to ask given how much map work happened this session, but the evidence ruled it out cleanly - the floating-member report was the group-walk bug above, not a moved block.
- **(historical, MINI-058) A pool list with no `[SerializeField]` built up correctly in-memory during the same Editor session that ran `BuildScene`, but held nothing at all once the scene was actually saved and reloaded** - Dog Life would never have appeared in a real build. Caught by validation, not left for the user to find. Lesson (same as `BuildAndVerification.md`'s own theme): an Editor-session-only correct state can silently not survive a save/reload - always verify against a reloaded scene, not just the same live session.
- **(historical, MINI-112) Per-member respawn cooldown needed to check `NpcCombatHealth.LastDefeatedAt`, not just "is the GameObject active"** - simply reactivating a GameObject undid a defeat instantly regardless of how little time had passed, until this was fixed to check real elapsed time.

## Open items

- Dog Life escalation/roaming behaviour beyond the current group-walk feature was planned (MINI-112-era, see `Docs/CLAUDE-HANDOFF-CURRENT.md`) - status of anything beyond what's documented here not reconfirmed.
- The group-walk fix ground-snaps the destination but doesn't prevent the destination from landing ON a building's roof if the walk path happens to cross one - only removes the literal "floating" symptom. If a member ends up standing on a roof (not floating, but still visually wrong), the next fix would be constraining `roadDirection`/`groupWalkDistance` to a path known clear of buildings, not just ground-snapping wherever it lands.

## Key files

- `Assets/UpIzUpMini/Scripts/Interaction/RivalGangSpawner.cs`
- `Assets/UpIzUpMini/Scripts/Character/GangMemberController.cs`
- `Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs`
- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (`BuildDogLifeGang`, `BuildNotAhWordRoster`)
