# Up Iz Up Mini — Current State

## MINI-119 complete, awaiting playtest (2026-08-22)

Claude fixed a full batch of post-playtest reports in one pass (see `Docs/WorkPackets/MINI-119.md` for the full writeup). Dialogue truncation on the boss NPCs traced to a background-panel height cap (330px, not hold duration - the user explicitly corrected an initial wrong diagnosis of that), raised to 620px. The Boat Man's most common repeat line ("nothing to load") now rotates instead of always repeating. A villager's stale, pre-migration patrol route (pointed at the old map's mountain) fixed generically for every patrol-enabled NPC placed via `PlaceRoadsideNpc`, not just this one case. Rasta now sells Blue Cheese seed directly ($650/3 seeds) strictly after his own teaching mission unlocks it, themed on the user's own suggested line. Blue Cheese/Purple Sugar's plant-selection number keys swapped to match Rasta's real teaching order (7=Blue Cheese, 8=Purple Sugar); added a minimap marker for the breeding station; extended per-crop hints to every Rasta-taught base strain, not just the three hybrids. War Story (M16) now requires a real `DefeatAllRivals` objective (every Dog Life member in the block knocked out), not just walking in. The bike's `motorTorque` raised past MINI-118's mass-parity value (which had only preserved the OLD power-to-weight ratio, not actually made hills easier); added a slope-proportional hill-climb assist and a yaw-spin damping assist that only clips the portion of spin above a real threshold, so a hedge/ledge hit still spins - just far less wildly - while ordinary steering yaw is untouched. Cheat `000000` now also grants Normy reputation, matching every other faction. A new focused validator, every standing validator (058/065/109/110/111/112/113), and the MINI-065 real-physics drop test all re-run clean; Windows build succeeded. Manual road-intersection/farm-hedge adjustment instructions were given directly to the user in chat per their own explicit request, not implemented as code. Ownership released. Not yet hands-on playtested.

## MINI-118 complete, awaiting playtest (2026-08-22)

Claude made the TMAX substantially heavier (220kg -> 480kg) to fix "bumps too high on any bump" - confirmed via code reading first that the wheelie (kinematic) and lean/stability (`ForceMode.Acceleration`) systems are already mass-independent by design, so this couldn't touch them. Suspension spring/damper are formula-derived from mass so sag/damping feel stays identical; a fixed-size bump now imparts a proportionally smaller velocity change (Δv = impulse/mass) - the actual physics of "harder to launch." Scaled motor/brake torque by the same ratio since those ARE real mass-dependent forces. Found and compensated a real, measured side effect (wheelie pitch dropped from 85.9deg baseline to 77.4deg; raised `wheelieRiseRate` and re-verified at 89.6deg). Added a second, independent safety net per the user's own suggestion: extra downward force once genuinely airborne for a sustained period, off during an actual wheelie. All vehicle validators pass except one pre-existing, unrelated issue (`Mini069GangWarValidation` - NPC positioning) flagged separately. Windows build and smoke clean. Ownership released. Not yet driven by a human.

## MINI-113 complete, awaiting playtest (2026-08-22)

Claude cleaned up world/navigation issues. Measured every road-junction pair first: only the Highland inroad/farm-spur junction had a real bump (0.286m, fixed to 0.000m). Found a more serious bug along the way: `BikeHomePoint` (the bike's save/load return point) was orphaned 303.52m from the migrated safehouse - fixed to 7.34m, a real save/load correctness bug, not cosmetic. Also corrected an earlier wrong measurement of the farm hedge (a Unity Collider API unreliable on non-convex meshes gave false "on the road" readings; real vertex measurement showed the hedge was always genuinely clear, 0.92-15.21m). Added minimap markers for owned vehicles. All standing validators, Windows build, and headless smoke pass. Ownership released. Not yet hands-on driven/walked.

## MINI-117 complete, awaiting playtest (2026-08-22)

Claude fixed the `Mini058FactionsValidation` failure found during MINI-112 - two real bugs, not one. First, the validator itself never initialized `MissionSystem`, so the recruiter's own mission-reached gate always refused regardless of anything else (the real recruiter path was never actually broken). Fixing that exposed a second, genuine bug: `GangMemberInteractable`'s Chevy recruitment path checked a combined mission+reputation gate before its own more specific reputation check, so that second check's clearer message could never fire - split so each check does its own job. Also added three in-game hints (per the user's direct follow-up) explaining the breeding-station process and plant-selection number key for each hybrid crop (Purple Sugar=7, Sugar Cheese=9, Purple Cheese=0). All standing validators pass, Windows build and a 12s headless smoke test clean. Ownership released. **Five rounds deep without a playtest**: MINI-109 through MINI-112 and now MINI-117 have all shipped back-to-back per the user's own repeated choice to skip the playtest gate each time.

## MINI-112 complete, awaiting playtest (2026-08-22)

Claude built Dog Life's escalation slice. Found and fixed a real bug: `FactionBrawler` and `PatrolNPC` both drove the same `CharacterController` with no coordination, so a chasing/fighting Dog Life member had ambient wandering and combat steering fighting each other, and nothing ever walked them back to the block afterward. A new `FactionBrawler.IsEngaged` flag fixes this - `PatrolNPC` now yields while engaged, which delivers "stop chasing, return to block, resume block behaviour" without new steering code. Added a real ~100s respawn cooldown (`NpcCombatHealth.LastDefeatedAt`, was previously instant-reset on reactivation), a jealousy dialogue line reusing the existing `CropUnlocked` condition, and an occasional two-member group walk down the road. New focused validator, Windows build, and a 12s headless smoke test all pass. Ownership released. **New blocker found, NOT this task's fault**: `Mini058FactionsValidation` now fails on a pre-existing `GangMemberInteractable` reputation-gate issue in a file this packet never touched - needs its own investigation. **Four rounds deep without a playtest**: MINI-109 through MINI-112 have all shipped back-to-back per the user's own repeated choice to skip the playtest gate each time.

## MINI-111 complete, awaiting playtest (2026-08-22)

Claude built Rasta's full strain-mentorship ladder. M13 now asks for 3 Bushers instead of tomatoes; six new missions (M13C2-M13C7) follow immediately, each teaching one tier (Black Sugar -> Purple -> Blue Cheese -> Purple Sugar hybrid -> Sugar Cheese -> Purple Cheese) and unlocking that crop the moment the mission becomes current, via a new `Mission.unlocksCropId` field. This is additive to the pre-existing Boss-exploitation unlock path (`BlackSugarUnlocked` etc., still independently verified working) - and it's the FIRST route to any advanced strain for Legitimate Farmer players, who never see the WeedRoute-only M9W-M11W missions. A real bug was found and fixed along the way: the scene builder's manual mission-copy code had no line for the new field, so it would have silently unlocked nothing at all in the built game despite looking correct in source - caught by the new focused validator. Standing regressions (MINI-108/109/110), Windows build, and a 12s headless smoke test all pass. Evidence under `Logs/Tasks/MINI-111/`. Ownership released. **Three rounds deep without a playtest**: MINI-109, MINI-110 and MINI-111 have all shipped back-to-back per the user's own repeated choice to skip the playtest gate each time - all three now await a single combined human playtest before `MINI-112` (Dog Life escalation) begins.

## MINI-110 complete, awaiting playtest (2026-08-22)

Claude added Normy's item favour (mission M13B, "Small Ting") between M13 and M14 - a new `ObjectiveKind.DeliverItem` requires actually holding a bought `food_bakes` and `pill_energy`, not just visiting the shops. M14's briefing now carries Normy's uncertain street-info hint pointing at the Boat Man/Gardey Zafeh. The Boat Man's first-meeting dialogue is the approved four-line introduction. `HUDController` gained a courier-timer readout, visible only while a Guadeloupe trip is active, reading `GuadeloupeTrade`'s existing lock/hide/timer API (untouched). Found and fixed a real bug along the way: the map migration's marker remapper had no case for `DeliverItem` and would have left Normy's M13B marker stale after migration - same class of bug as MINI-109's Rasta fix. Focused validator, standing regressions (including MINI-109's own), Windows build, and a 12s headless smoke test all pass. Evidence under `Logs/Tasks/MINI-110/`, including a forced-state HUD screenshot. Ownership released. **Note**: the user chose to proceed to MINI-110 without first playtesting MINI-109 - both rounds' fixes now await a human playtest: Clean Face -> Black Sugar delivery -> Rasta (MINI-109), then Normy's favour -> Boat Man intro -> a real Guadeloupe courier run (MINI-110), before `MINI-111` begins.

## MINI-109 complete, awaiting playtest (2026-08-22)

Claude fixed all three MINI-109 defects, each with a distinct confirmed root cause: Normy's Clean Face payment shared the ambient bribe's zero-heat gate (fixed with a separate idempotent mission-payment branch); Boss J's Black Sugar sale gate checked only Bushers count, not any held illegal crop (fixed with `HasAnySellableIllegalStock`); Rasta's M13 marker was baked in by `Mini100GrandBayMapMigration.RemapMissionObjectives()` BEFORE he was moved to his final migrated position (fixed by reordering). Also added a retrospective-completion rule for `BuyItem` objectives (owning an item outright now credits the objective instead of demanding a repeat purchase). Focused validator, standing mission/map regressions, Windows build, and a 12s headless smoke test all pass. Evidence and screenshots are under `Logs/Tasks/MINI-109/`. Ownership released. The user still needs to hands-on playtest: Clean Face -> Black Sugar delivery -> Rasta, per `Docs/CLAUDE-HANDOFF-CURRENT.md` section 10, before `MINI-110` begins.

## Claude handoff — MINI-109 ready (2026-08-21)

The current operational handoff is `Docs/CLAUDE-HANDOFF-CURRENT.md`; the copy-paste startup prompt is `Docs/NEXT-CHATGPT-PLUS-HANDOFF.md`. Claude's first bounded packet is `Docs/WorkPackets/MINI-109.md`: fix the stuck Clean Face/Normy transaction, Black Sugar delivery to Boss J and Rasta's stale mission marker, then stop for the user's playtest. The larger approved sequence—Normy favour/Boat Man timer, Rasta strain ladder, Dog Life escalation, road/hedge/vehicle marker cleanup, Brakes missions, phone unlock and TMAX optimization—is documented but must not be implemented as one uncontrolled batch.

## Current gameplay packet — MINI-108 (2026-08-21)

MINI-108 rebuilds the early playable flow while MINI-107 remains paused. Every marked mission now uses one universal blinking yellow minimap objective that clamps to the radar edge when distant. The opening dialogue has a dynamically sized transparent black background. The early choice now lets K go directly to Boss J or L play a short increasingly frustrating legal-farming branch before both rejoin Boss J. The broken cooldown was replaced by a Highland-safehouse rest mission and a clean-face mission requiring two different regular officers plus Normy's $100/20%-heat service. Map/mission/focused gates, the Windows build and a 12-second built-player headless startup pass. Hands-on UI timing and full mission progression remain for the user. The temporary procedural banana is frozen; replace it later with an approved Hitem3D production asset.

## Paused character work — MINI-107 (2026-08-21)

MINI-107 is paused at the user's request while they playtest the game. The reusable character manifest/workflow/tooling is in place. The approved 25k/12k/4.5k Sacat LODs pass static checks but fail motion deformation; they are not approved for playable integration. The original 100K Sacat rig animates correctly. Resume from `Docs/WorkPackets/MINI-107.md` and repair LOD topology/weight transfer before any character swap.

Read this at the start of every task. `PROJECT-HANDOFF.md` remains the full audit trail; search it for the active task and the systems you will touch rather than loading all completed history into every session.

## Project

- Path: `E:\Unity\Up Iz Up Mini`
- Unity: `6000.3.10f1`
- Full-size reference project: `E:\Unity\Up iz up` (read-only)
- Current production state: systems 1–3 are integrated. `MINI-108` adds universal distant objective guidance, readable dynamic dialogue, the revised Boss J branch, safehouse/clean-police/Normy missions, Paro gating/appearance, curved bananas with a brown stalk, bridge clearance and the Boss C-side collider repair. Static/map/focused gates pass; hands-on acceptance remains.
- Current task/owner: authoritative only in the `### Current claim` block of `PROJECT-HANDOFF.md`.
- Last known good gameplay checkpoint: `3adb5c0` (`MINI-093` police melee retaliation; focused validation passed, no new Windows build).

## What exists

- Switchable Sacat and Franki, locomotion/jump/stamina/health, inventory/economy/save-load.
- Farming, crop progression/cloning, plots/land, shops/apparel/accessories, food/pharmacy.
- Missions, dialogue/dialect foundation, bosses, reputation, gangs, police/heat, safehouses, Guadeloupe abstraction.
- TMAX riding/wheelie/pillion and Range Rover driving/passengers, radio, interaction prompts.
- NPC patrol/navigation foundations, melee/hit reaction foundations, recruitment/following.
- Fixed-camera editor snapshots, many feature validators, Windows builds, and headless built-player smoke tests.

## Sources of truth

- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` generates `GrandBayProof.unity`. Manual scene edits are overwritten.
- `GrandBayProof.unity` now uses the approved sourced/compressed Lalay-to-Highland phase-one world: ten connected collidable road ribbons including the south Backstreet, four bridge groups, dense Lalay housing, 18 Highland homes (including five small apartments), a church/sand/jetty coast, one gameplay farm and seven inactive future parcel anchors.
- `Docs/MAP-ANCHORS.json` and compact Unity map/height data now exist. `VA-004` locks the source road/street target and `VA-005` locks the corrected migration graybox; the untouched map-lab and commit `aed8854` are the rollback backups.
- `Docs/WORLD-EXPANSION-WORKFLOW.md` is mandatory for all map work. The first live manifest is `dm-dom-grand-bay-lalay-highland-v1`, currently at `graybox`; it cannot advance until a corrected player-height Lalay view is approved.
- Locomotion uses the authored StarterAssets controller and `MotionSpeed`; do not regenerate it casually.
- `HumanoidAnimationManager` is the reusable action-layer foundation.
- Bike seating already uses `VehicleSeat`, `VehicleRider`, `BikeRiderAnimation`, and Humanoid IK. Improve profiles/clips/gates rather than starting over.

## Highest risks

1. Visual acceptance: Sacat's two-piece chain placement is locked as `VA-002`; its visibility/swing while walking still needs the user's playtest. Most other movement and appearance remain unapproved.
2. Visual debt: many changes compile or pass harnesses but have not been watched in real Play Mode. Static screenshots cannot prove animation, combat, riding, driving, NPC movement, or UI timing.
3. Map runtime risk: the migration passes static/map gates, but the legacy Edit-Mode NavMesh path check remains partial. A road-following navigation-link chain is present; the user still needs to walk, drive and bring a companion from Lalay to Highland to accept it.
4. Mobile architecture: Built-in RP, legacy Input Manager, no touch/safe-area layer, no Android build gate.
5. Build size: latest audited Windows build was about 388.6 MB; textures about 280.6 MB. TMAX source contributed about 172.2 MB and Range Rover about 43.5 MB.
6. Combat visuals: contact is now timed/forward/LOS checked, but the current sword-like placeholder clip can still look warped and needs a later approved animation replacement.
7. Character modularity: current clothes mostly recolor existing meshes; matching height does not make rig/bone scale or garment fit identical.

## Workflow now in force

- Read `Docs/AI-PRODUCTION-WORKFLOW.md`.
- Create one bounded packet from `Docs/WORK-PACKET-TEMPLATE.md`.
- Run `Tools/AIWorkflow/Invoke-Preflight.ps1`.
- Parallelize read-only audits and non-overlapping files only.
- One integrator owns Unity scenes, prefabs, packages, settings, imports, generated world data, and final visual integration.
- Record accepted appearance/placement in `Docs/VISUAL-APPROVAL-REGISTER.md`.
- For map work, scaffold/validate a stable district with `New-MapDistrict.ps1` and `Test-MapDistrict.ps1`; never skip its current gate.
- Do not spend Hitem3D credits before the reference/asset card is approved.

## Recommended next sequence

1. Claude implements only MINI-109, captures Normy/Boss J/Rasta evidence, builds once and returns for the user's mission playtest.
2. After acceptance, MINI-110 adds Normy's item favour and the Boat Man/Gardey narrative bridge using the existing character-away and timer foundation.
3. MINI-111 builds Rasta's data-driven strain/hybrid ladder; MINI-112 then escalates Dog Life and crew conflict.
4. MINI-113 repairs road-intersection bumps, farm-hedge clearance, vehicle minimap blips and road-safe vehicle spawns through the map workflow.
5. MINI-114 adds Brakes community/blessing missions; MINI-115 adds the phone unlock/tutorial over the existing Q-call system.
6. MINI-116 diagnoses the see-through TMAX and compresses its oversized build payload before more vehicle art is added.
7. Resume MINI-107 only when the user requests character production again; repair animated LOD deformation before any playable swap.
8. Preserve later combat, Android and render/input migrations as separate approval packets.

## Update rule

Keep this file short. Update facts and priorities at task completion; put detailed evidence, command logs, and historical narrative in the task entry inside `PROJECT-HANDOFF.md`.
