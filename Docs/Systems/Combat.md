# Combat

## Current state

Data-driven since MINI-120 (2026-08-29). The player's own melee is a real 4-hit combo chain (Jab -> Hook -> Right Hook -> Finisher, looping) reading from a static move table, not hardcoded fields. The Finisher step uses the real multi-strike "Punch Combo" clip (MINI-AST-124) as the payoff move instead of one more single punch. **First user playtest reported "only seeing one punch"** - the reset window (1.0s) may have been too tight for the user's actual test-press cadence; widened to 1.5s and a real Debug.Log added to `Attack()` (`MINI-120 COMBO: fired step...`) so the next test can confirm from the actual `Player.log` rather than another guess. Not yet re-confirmed by the user as of this entry. NPC-side ragdoll-on-hit shipped in the same MINI-119 follow-up session for Police/Villager/Gang roles. Player-side ragdoll-on-defeat does not exist yet. Shooting mechanics do not exist yet - explicitly sequenced by the user as "fighting first, then shooting after," and shooting must not start before the fighting pass is fully accepted.

## Architecture

- `Assets/UpIzUpMini/Scripts/Combat/MeleeMoveLibrary.cs` - the data table. `MeleeMoveLibrary.Chain` is a static array of `ComboMove` structs (id, clip, damage, windup/active/recovery timing, reach/radius/arc/bodyRadiusBonus). Adding a new combo step or a new attack entirely means adding one array entry here, not touching the driver component.
- `Assets/UpIzUpMini/Scripts/Combat/SimpleMeleeCombat.cs` - the driver. Tracks `_comboStep` (advances one per landed Attack press, wraps after the chain length, resets to 0 if the player doesn't follow up within `comboResetSeconds`). Reads everything else (damage, timing, contact geometry) from the current `ComboMove`.
- `Assets/UpIzUpMini/Scripts/Combat/MeleeContactResolver.cs` - shared geometry: `MeleeAttackProfile` (per-move reach/radius/arc/bodyRadiusBonus data) and `IsInsideForwardContact`/`TryFindNearest` (the actual hit-test). Used by the player, `PoliceOfficer`, `FactionBrawler`, and `CompanionCombatAssist` - each can carry its own profile values.
- `Assets/UpIzUpMini/Scripts/Combat/NpcRagdoll.cs` - generic ragdoll wrapper (works on any Humanoid Animator, not Sacat-specific despite reusing `SacatRagdollBuilder`). `Ragdoll(impactVelocity)` goes dynamic with a one-time velocity match; `Recover()` reverses it.
- `Assets/UpIzUpMini/Scripts/Combat/NpcCombatHealth.cs` - MINI-038's health/hit-reaction/knockdown system, extended in MINI-119 to branch on whether an `NpcRagdoll` is present. With one: every hit ragdolls; fatal hits lie `fatalLieSeconds` (3s) then fade; non-fatal hits lie `nonFatalLieSeconds` (2s) then recover and resume walking. Without one (shopkeepers/dealers/mission NPCs - none currently get an `NpcRagdoll`): falls back to the original animation-only hit-reaction/knockdown behaviour, unchanged.
- Shared Animator Controller: `Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller`. Every combo move's clip is baked as a state on this ONE shared controller (all characters use it) via `HumanoidAnimationLayerBuilder.EnsureActionLayers`, driven by `Mini011PhaseBSetup.GetSharedActionEntries()`. **This is the actual thing that plays at runtime** - see the "what didn't work" entry below before assuming a clip swap took effect.
- Animation source: real Mixamo motion capture, the user's own free account (`Docs/ASSET-REGISTER.md` MINI-AST-122/123/124), not the previously-owned Kevin Iglesias "Human Melee Animations FREE" pack (that pack has zero unarmed clips - every "Attack" entry in it is a weapon swing).

## What worked / what didn't

- **(2026-08-29) Fixing a clip only on `HumanoidAnimationManager.actions` did NOT work.** That per-character serialized data is only read for `clip.length` (blend-out timing) and `Animator.HasState` existence checks - `Animator.CrossFadeInFixedTime(id, ...)` actually plays a STATE BY NAME/HASH on the shared `StarterAssetsThirdPerson.controller` asset, and that state's baked `Motion` is a completely separate thing that the scene-side ActionEntry patch never touches. **What worked**: call `HumanoidAnimationLayerBuilder.EnsureActionLayers(controller, GetSharedActionEntries())` again directly against the real controller asset - `EnsureState`'s own documented behaviour re-authors an existing state's Motion in place. Confirmed via direct before/after read of the state's `.motion.name`, not assumed. See `Mini120PatchAnimatorController.cs`.
- **(2026-08-29) The "warped punch" root cause was a missing animation, not a tunable bug.** The already-owned free pack had zero bare-fist clips; the game was reusing a one-handed WEAPON-swing clip (`HumanM@Attack1H01_R.fbx`) for punching with no weapon in hand. Confirmed visually via `Mini120RenderPunchPose.cs` (renders a character posed by a clip via `AnimationClip.SampleAnimation`, no Play Mode needed) - the old clip visibly rotated the whole torso sideways mid-swing (a sword follow-through).
- **(2026-08-29) A free, hand-keyframed Asset Store animation pack ("Fighter Pack Bundle FREE") looked cartoonish** - user's own words, "looks weird, like cartoon movements." Real motion capture (Mixamo, free with an account) looked correct on the first try. Lesson: for combat/character animation specifically, prefer real mocap sources over generic free packs even when both are free.
- **(2026-08-29) The over-generous hit detection root cause, measured not guessed**: a hardcoded `+0.35f` "body radius" forgiveness bonus in `MeleeContactResolver.IsInsideForwardContact`, stacked on a `reach=1.65` tuned for a lunging weapon swing, registered hits up to 2.3m away and 0.6m to either side. Exposed as `MeleeAttackProfile.bodyRadiusBonus` (a real field, optional constructor param defaulting to the old 0.35f so `PoliceOfficer`/`FactionBrawler`/`CompanionCombatAssist` keep their existing feel unless explicitly retuned). New combo moves use ~0.15 and reach 0.95-1.05 - re-measured max reach 1.3-1.4m.
- **(2026-08-29) Verifying multi-press combo cycling via Editor batch mode did NOT work** - `UnityEngine.Time.time` does not advance outside Play mode, so a scripted second `Attack()` call is blocked forever by the real cooldown gate (`Time.time < nextHit`). The combo-advance arithmetic itself (`(step+1) % length`) was verified structurally; the actual in-game multi-press behaviour needed the user's real hands-on test.
- **Not yet tried**: trimming a long raw Mixamo clip (e.g. a punch clip padded with several seconds of idle/bounce before the actual strike) via `ModelImporter.clipAnimations` frame ranges. Came up while investigating "Hook.fbx" (turned out to actually be a boxing idle loop per rendered sampling, though the user separately confirmed watching a real hook strike in Mixamo's own preview when downloading manually - worth resolving which is true before adding more moves from raw Mixamo downloads without the user watching each one first).

## Open items

- Combo chain is now 4 steps (Jab/Hook/RightHook/Finisher) with a wider 1.5s reset window and real Debug.Log diagnostics in `Attack()` - awaiting the user's re-test after the first attempt only showed one punch.
- Bonus multi-strike clip downloaded but still unused: `Mixamo_ComboPunch8.fbx` (8-hit) - a candidate for an even bigger finisher/rare special move later.
- Player-side ragdoll-on-defeat: not built. Only NPCs (Police/Villager/Gang) ragdoll currently.
- Shooting mechanics: not started, and must not start before the fighting pass is fully accepted per the user's own explicit sequencing.
- Input remap requested but not yet done: this doesn't touch Combat directly, see `Vehicles.md`'s open items for the E/Q mount/wheelie remap.

## Key files

- `Assets/UpIzUpMini/Scripts/Combat/MeleeMoveLibrary.cs`
- `Assets/UpIzUpMini/Scripts/Combat/SimpleMeleeCombat.cs`
- `Assets/UpIzUpMini/Scripts/Combat/MeleeContactResolver.cs`
- `Assets/UpIzUpMini/Scripts/Combat/NpcRagdoll.cs`
- `Assets/UpIzUpMini/Scripts/Combat/NpcCombatHealth.cs`
- `Assets/UpIzUpMini/Scripts/Interaction/PoliceOfficer.cs`, `Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs`, `Assets/UpIzUpMini/Scripts/Character/CompanionCombatAssist.cs` (other `MeleeAttackProfile` consumers)
- `Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller` (shared Animator Controller - the actual runtime source of truth for which clip plays)
- `Assets/UpIzUpMini/Editor/HumanoidAnimationLayerBuilder.cs` (bakes action states onto the shared controller)
- `Assets/Mixamo/Animations/` (Jab/Hook/RightHook/PunchCombo4/ComboPunch8 source clips)
