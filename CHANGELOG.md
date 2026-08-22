# Up Iz Up Mini — Changelog

## 2026-08-22 — MINI-119

- Dialogue truncation ("not showing full dialogue" on the boss NPCs): root cause was a background-panel height cap (330px in `MissionHUD.ResizeBanner`), not hold duration - long text rendered past the dark backing with nothing behind it, illegible against the game world except in the middle, exactly the reported symptom. Raised the cap to 620px.
- Boat Man's most common repeat line ("nothing to load") now rotates through 3 lines instead of always the exact same sentence.
- Fixed a villager's stale, pre-migration patrol route (was pointed at the old map's terrain) - `PlaceRoadsideNpc` now resets `PatrolNPC` waypoints after repositioning, generically, for every patrol-enabled NPC it places.
- Rasta now sells Blue Cheese seed directly ($650 for 3 seeds) once his own teaching mission actually unlocks it, with a themed line; never advertised early, never re-offered while already stocked.
- Blue Cheese/Purple Sugar's plant-selection number keys swapped (7=Blue Cheese, 8=Purple Sugar) to match Rasta's real teaching order; added a minimap marker for the breeding station; extended per-crop hints to every Rasta-taught base strain (Black Sugar/Purple/Blue Cheese), not just the three hybrids.
- War Story (M16) now requires a real `DefeatAllRivals` objective (every Dog Life member in the block knocked out) before laying low - previously winnable by just walking in.
- Bike: `motorTorque` raised past MINI-118's mass-parity value (which only preserved the old power-to-weight ratio, not actually stronger); new slope-proportional hill-climb assist (0 on flat ground, full strength on a real incline/ledge under the rear wheel); new yaw-spin assist that damps only the portion of yaw angular velocity above a real-world-tuned threshold, so a hedge/ledge hit still spins - just far less wildly - while ordinary steering-induced yaw is untouched.
- Cheat `000000` now also sets Normy reputation to 100, matching every other faction it already unlocks.
- New focused validator (`Mini119BatchFixValidation`) covers every item above with a real gameplay-state assertion; full vehicle+scene rebuild pipeline, every standing validator (058/065/109/110/111/112/113), and the MINI-065 real-physics drop test all re-run clean. Windows build succeeded.
- Manual road-intersection/farm-hedge adjustment instructions given directly to the user, per their own request to do that part by hand - not implemented as code this round.

## 2026-08-22 — MINI-118

- Bike mass increased ~2.18x (220kg -> 480kg, deliberately above real-world TMAX weight) so ordinary bumps impart a proportionally smaller velocity change and stop launching the bike. The suspension spring/damper are formula-derived from mass, so sag depth and damping ratio stay identical - only stiffness/damping scale up to match.
- The wheelie mechanic (fully kinematic, `MoveRotation`-driven) and the lean/stability corrections (`ForceMode.Acceleration`, mass-independent by Unity's own definition) needed no changes at all - confirmed by code reading, not assumed.
- `motorTorque`/`brakeTorque`/`wheelieRearTorqueBoost` scaled by the same mass ratio, since these ARE real mass-dependent drive/brake forces, so acceleration/braking/wheelie-forward-creep don't feel weaker as an unintended side effect.
- Measured a real, if minor, side effect: real wheelie pitch dropped from an 85.9deg baseline to 77.4deg (still passing the drop test's 76.6deg minimum, but with far less margin) - compensated by raising `wheelieRiseRate`, re-verified at 89.6deg.
- Added a second, independent safety net per the user's own follow-up suggestion: extra downward "air gravity" once the bike has been genuinely airborne (both wheels off the ground) for a sustained period, gated off during an actual wheelie so it can never fight the intentional lift.
- All standing vehicle/faction/map validators pass except one unrelated, pre-existing issue (`Mini069GangWarValidation`, an NPC-position check untouched by this task) flagged separately. Windows build and headless smoke clean.

## 2026-08-22 — MINI-113

- Fixed the one real road-ribbon bump: measured every road-pair rather than guessing, found the Highland inroad/farm-spur junction had a 0.286m vertical step (right at the existing static check's own tolerance ceiling), fixed it to 0.000m with a localized post-migration mesh blend.
- Found and fixed a real, more serious bug: `BikeHomePoint` (the bike's save/load return point) was never included in the map migration's tracked-and-repositioned objects, and was measured 303.52m from the real, migrated safehouse - fixed to 7.34m.
- Investigated the farm-hedge/road overlap and corrected an earlier wrong measurement: `Collider.ClosestPoint` is unreliable on non-convex road MeshColliders and was falsely reporting 0.00m clearance; re-measured with real mesh vertices and found the hedge was never actually on the road (0.92m-15.21m genuine clearance). A defensive no-op safety net was kept anyway.
- Added minimap markers for owned/usable vehicles, attached at purchase time.
- New focused validator, all standing regressions (MINI-058/100/108/109/112) pass, Windows build and headless smoke clean.

## 2026-08-22 — MINI-117

- Fixed the `Mini058FactionsValidation` failure found during MINI-112's regression pass: the validator never initialized `MissionSystem`, so the recruiter's own `IsMissionReached("M15")` gate always refused regardless of anything else - a validator gap, not a real gameplay bug.
- Found and fixed a second, real bug while fixing the first: `GangMemberInteractable`'s Chevy recruitment path checked a combined mission+reputation gate before its own, more specific reputation check - meaning that second check's clearer refusal message could never actually fire. Split the checks so each does its own job.
- Added three new in-game hints (`GameplayHintController`) explaining the breeding-station process for each hybrid crop and its plant-selection number key (Purple Sugar = 7, Sugar Cheese = 9, Purple Cheese = 0), per the user's direct request.
- All standing validators (MINI-108 through MINI-112, plus MINI-058 and MINI-100) pass. Windows build succeeded; 12-second headless smoke test clean.

## 2026-08-22 — MINI-112

- Found and fixed a real steering-contention bug: `FactionBrawler` and `PatrolNPC` both drove the same `CharacterController` every frame with no coordination, so a Dog Life member mid-chase/fight was fought over by two movement systems at once. `PatrolNPC` now yields while `FactionBrawler.IsEngaged`, which also delivers "stop chasing, return to block, resume block behaviour" for free - once a fight/chase ends, the member simply resumes its own ambient waypoint wander.
- Added a real respawn cooldown: defeated Dog Life members previously reset instantly the moment their GameObject was reactivated, however little time had passed. `NpcCombatHealth.LastDefeatedAt` plus a `RivalGangSpawner`-side ~100-second check now gates each member's reactivation individually.
- Added a jealousy dialogue line for Dog Life, gated on the existing `CropUnlocked` condition (already true via either the Boss-exploitation path or MINI-111's Rasta ladder) - no new dialogue-condition machinery needed.
- Added an occasional small-group walk: up to two active, uninvolved (`!IsEngaged`, not knocked out) members periodically walk further down the road on a temporary waypoint swap, then return to their normal spot.
- New focused validator (`Mini112DogLifeEscalationValidation`). Windows build and a 12-second headless smoke test pass.
- **Found but left unfixed, deliberately out of scope**: `Mini058FactionsValidation` now fails on a `GangMemberInteractable` reputation-gate line, in a file this packet doesn't touch or reserve - flagged as a new blocker for a future task rather than absorbed here.
- Hands-on playtesting of a real Dog Life confrontation remains explicitly requested before MINI-113 begins.

## 2026-08-22 — MINI-111

- Rasta stops asking for tomatoes: M13's harvest objective now asks for 3 Bushers, tier one of a new production/strain school.
- Added six new Rasta missions (M13C2-M13C7), immediately after M13, teaching Black Sugar -> Purple -> Blue Cheese -> Purple Sugar (the `purple_black` hybrid, renamed player-facing) -> Sugar Cheese -> Purple Cheese in order. Each crop is locked until its own mission becomes current, then unlocks automatically via a new `Mission.unlocksCropId` field.
- New `ProgressionManager.RastaTaught*` flags OR into the existing `IsCropUnlocked` checks - additive to the pre-existing Boss-exploitation-stage unlock path, not a replacement. This gives the Legitimate Farmer career path (which never sees the WeedRoute-only M9W-M11W missions) its first-ever route to any strain past the basics.
- Found and fixed a real bug along the way: the scene builder's manual field-by-field copy from the in-memory mission list to the built `MissionSystem` component had no line for the new `unlocksCropId` field, so every mission's unlock would have silently baked in as empty - caught by the new focused validator before it shipped broken.
- New focused validator (`Mini111RastaLadderValidation`), confirming per-tier story-gating and that the pre-existing Boss-exploitation path still independently works. Standing regressions (MINI-108/109/110), a Windows build, and a 12-second headless smoke test all pass. Evidence under `Logs/Tasks/MINI-111/`.
- Hands-on playtesting on both career paths remains explicitly requested before MINI-112 begins.

## 2026-08-22 — MINI-110

- Added Normy's item favour ("Small Ting", mission M13B) between M13 and M14: a new `ObjectiveKind.DeliverItem` requires actually holding a bought `food_bakes` and `pill_energy` (item-ID/inventory truth via a new `EconomyManager.TrySpendConsumable`), not just visiting the shops.
- M14's briefing now carries Normy's uncertain street-info hint (somebody may be taking the boys' stock, he doesn't know who, check the Boat Man about Gardey Zafeh), reusing the existing next-mission-briefing banner.
- Rewrote the Boat Man's first-meeting dialogue to the approved four-line introduction exchange, replacing the old generic one-liner.
- Added a HUD courier-timer readout, shown only while a Guadeloupe trip is active, naming the away character and a live countdown - connects to `GuadeloupeTrade`'s existing lock/hide/timer API rather than recreating travel.
- Found and fixed a real bug along the way: the map migration's mission-marker remapper had no case for `DeliverItem`, which would have left M13B's Normy marker stale after migration - the same class of bug MINI-109 fixed for Rasta.
- New focused validator (`Mini110NarrativeBridgeValidation`). Standing regressions (including MINI-109's own validator), a Windows build, and a 12-second headless smoke test all pass. Evidence and screenshots under `Logs/Tasks/MINI-110/`.
- Hands-on playtesting (Normy's favour -> Boat Man intro -> a real Guadeloupe courier run) remains explicitly requested before MINI-111 begins.

## 2026-08-22 — MINI-109

- Fixed Clean Face (M5B) getting permanently stuck at Normy: the mission's BribeNormy objective previously had no path except Normy's ambient heat-service bribe, which refuses to do anything once heat is at/near zero - exactly the state M5A ("go rest and cool down") leaves the player in. The mission payment is now a separate, idempotent transaction that doesn't touch the ambient cooldown/heat gate.
- Fixed Black Sugar delivery to Boss J (M10W) sometimes not advancing: the interact-time gate only checked the player's Bushers count (hardcoded), not any of Boss J's actually-sellable illegal crops, so a player holding only harvested Black Sugar never triggered a sale attempt at all. Now checks for any sellable illegal stock actually held.
- Fixed M13's Rasta marker pointing at his old location: `Mini100GrandBayMapMigration.ApplyToOpenScene` moved Rasta (and Sacat/Franki) to their final migrated positions AFTER `RemapMissionObjectives()` had already baked in their pre-move positions. Reordered so remapping runs last, against final positions.
- Added a consistent retrospective-completion rule: a `BuyItem` objective now credits immediately if the player already owns the item outright, instead of forcing a repeat purchase `EconomyManager.TryPurchase` would refuse anyway. Every other objective kind keeps today's progression-lock behaviour.
- New focused validator (`Mini109MissionRepairValidation`) reproduces all three defects and proves the retrospective-completion rule against the real built scene. Standing mission/map regressions, a Windows build, and a 12-second headless built-player smoke test all pass. Evidence and screenshots under `Logs/Tasks/MINI-109/`.
- Hands-on mission playtesting (Clean Face -> Black Sugar delivery -> Rasta) remains explicitly requested from the user before MINI-110 begins.

## 2026-08-21 — MINI-109 handoff preparation

- Added a compact, detailed Claude handoff covering the game vision, current architecture, generated-scene truth, completed systems, visual locks, map/character/asset pipelines, tool/live-control rules and verification contract.
- Replaced the outdated next-agent prompt with a ready-to-paste Claude startup prompt.
- Created the approved bounded MINI-109 packet for the stuck Normy objective, Black Sugar delivery and stale Rasta marker.
- Split the larger requested progression into proposed MINI-110 through MINI-116 packets so the user can playtest between changes.
- Updated current story/design truth; no gameplay code, scenes, assets or build were changed.

## 2026-08-21 — MINI-108

- Added a universal blinking yellow objective that remains on the minimap edge for every distant marked mission, plus a one-time navigation hint.
- Added a dynamically sized transparent black mission/dialogue background and retained E fast-forward for the opening exchange.
- Rebuilt the early route: K goes directly to Boss J; L runs an increasingly frustrating legal-farming loop before rejoining Boss J.
- Replaced the flawed cooldown with Highland safehouse rest, two distinct clean-police conversations, and Normy's $100 service removing 20% heat.
- Added `111111` mission-skip testing, renamed M6 to `Stock Up`, made Paro's first meeting dialogue-only, improved Boss J delivery dialogue, built curved bananas with a brown hanging stalk, protected bridge approaches and tightened the Boss C-side shanty collider.
- Map-lab, migrated map and focused MINI-108 validations pass. The Windows build succeeds and its 12-second headless startup contains no errors; hands-on runtime acceptance remains. The user froze the temporary banana for later Hitem3D replacement.

## 2026-08-21 — MINI-106

- Created independent 25k, 12k and 4.5k mobile LOD candidates for the approved Sacat modular base.
- Preserved the 101-bone Humanoid/finger hierarchy, one material, zero unweighted vertices and four maximum skin influences.
- Added an isolated Unity three-level LODGroup prefab and corrected the Blender/Unity 90-degree visual-axis difference after fixed screenshots rejected two lying-down attempts.
- Unity validation and upright static comparison pass; playable replacement, animation deformation and Android device profiling remain gated.

## 2026-08-21 — MINI-105

- Produced and locally rigged the user-approved Sacat modular base with full separate finger chains.
- Added an isolated Unity Humanoid import package, external 2K texture, Standard material and proof prefab; playable Sacat and all locked accessories remain unchanged.
- Unity validation passed: valid Humanoid, 30/30 finger joints, one skinned mesh/material, four maximum skin influences and fixed front/back screenshots.
- The 100k-triangle source remains a high-quality LOD0 candidate; mobile LOD generation and an animation-deformation proof are required before gameplay integration.

## 2026-08-20 — MINI-102

- Moved Backstreet to the user-marked south side of Lalay, connected both ends to the main road, and removed the accidental northern route.
- Established deliberate Lalay placements for shops, Boss J, Normy, police, Dog Life, Not Ah Word, Boss C's Rover, the car dealer and Paro; added Brakes in white at the church and kept Boat Man/boat at the jetty.
- Added a GTA-style rotating minimap with progression-aware blips and a transparent red/blue wanted overlay at 50%+ heat.
- Replaced remaining player-facing Montine terminology with Highland while retaining legacy save IDs.
- Static map/scene validation and fixed visual evidence pass; hands-on driving, NPC-motion and minimap-feel acceptance remain owed.

## 2026-08-20 — MINI-100

- Migrated the approved VA-005 Lalay/Highland map into the generated playable `GrandBayProof` scene and removed the old synthetic terrain/road/coast roots.
- Preserved gameplay-facing object names and systems, relocated players, safehouses, farming, NPCs, vehicles and the Guadeloupe boat role, and updated respawn and plantation-risk positions.
- Retained the separate approved map-lab and commit `aed8854` as rollback backups.
- Added a reusable migration validator and fixed evidence cameras. Migration, scene wiring, NavMesh and Windows build checks pass; user walking/driving acceptance remains owed.

## 2026-08-20 — MINI-098

- Added the user-drawn inland-to-coastal connector as stable, collidable map data rather than a rendered annotation.
- Corrected Lalay sampling and built 82 dense roadside buildings: 59 regular one-/two-storey houses and 23 shanties.
- Enforced the eastern coastal/lower-bay no-house areas and added a terrain-following sand-and-stone bay treatment.
- Rebuilt and validated the separate map-lab; the playable gameplay scene remains unchanged pending visual approval.

## 2026-08-15 — MINI-000

- Created the clean Unity 6 Mini project.
- Added agent coordination, game-design, map-strategy, task, and verification documentation.
- No gameplay or asset import is claimed complete.

## 2026-08-15 — MINI-001

- Built `GrandBayProof.unity` via a repeatable editor script: Lalay road with 8 simple buildings, a dirty Montine farm path/clearing/plot, one NPC, one player capsule, a three-quarter follow camera (~44° downward), and a directional light.
- Added `PlayerController` (walk/run), `ThirdPersonFollowCamera`, and an interaction system (`IInteractable`, `InteractableBase`, `NPCInteractable`, `FarmPlotInteractable`, `InteractionDetector`) with OnGUI world-tracked "[ E ] Talk" / "[ E ] Plant" prompts.
- Batch-mode compile and a custom static scene-wiring validation both pass. Headless Play-mode verification is blocked by an unrelated Unity Editor Search-module bug in this environment (see `PROJECT-HANDOFF.md`); a manual Play-mode check is requested before this is called fully verified.
- Added a Windows standalone build (`Mini001Build.cs` → `Builds/GrandBayProof/UpIzUpMini.exe`, not committed — gitignored). A headless run of the built player confirmed zero console errors across the core Update loop, sidestepping the Editor bug above; visual/prompt confirmation is still pending a manual look.

## 2026-08-15 — MINI-011 (Phase A)

- Corrective-rebuild request received rejecting MINI-001's primitive-geometry visuals; scoped as `MINI-011`, superseding `MINI-004`-`MINI-009`.
- Audited `E:\Assets` (read-only) instead of the five requested Asset Store URLs (none present locally, none of which I can acquire without account access). Selected `Arteria3d Shanty Town`/`Shanty Town 2` + tropical foliage packs as a coherent local substitute — see `Docs/ASSET-REGISTER.md`.
- Confirmed the supplied heightmap is independently documented as not Unity-importable; terrain will be hand-authored instead, per user decision.
- Wrote `Docs/MINI-011-VISUAL-PLAN.md` (Phase A deliverable) and stopped at its gate for user go/no-go before any production scene/script work begins.
- Acquired the 5 requested free packages to the user's Unity account via browser, plus discovered 3 already-owned relevant packages (Demo City Mobile Friendly, Human Basic Motions FREE, Human Melee Animations FREE).
- Imported all 8 packages into `Assets/` via CLI (`-importPackage` against the user's downloaded Asset Store cache, since the Package Manager GUI isn't drivable). Added `com.unity.ugui` to `Packages/manifest.json`. Full-project compile clean. Confirmed Low Poly Character Pack rigs convert to valid Humanoid avatars, resolving the biggest risk flagged for Smart/Strong switching. `GrandBayProof.unity` itself is still untouched — Phase B production work has not started.
- Rendered actual samples of candidate building packs before committing (new capability: `Camera.Render()` to PNG from batch mode). Demo City and POLYGON Starter Pack both turned out to be generic modern/industrial kits, not Caribbean village style — rejected. Added Arteria3d Shanty Town's textured shanty structures/props (genuine corrugated-tin look, confirmed by render); Shanty Town 2's buildings turned out unfixably untextured (source pack has no real texture files for them) and were removed.
- **Phase B built:** `GrandBayProof.unity` rebuilt with a sculpted terrain (coast/village-shelf/hills), textured grass, a visible sea, a bending road, ~30-40 densely-placed houses (real Shanty Town structures + hand-built modular houses with proper pitched roofs, doors, windows), vegetation, and the farm path/clearing. Player and NPC are now real Humanoid characters with basic Idle/Walk/Run animation, not capsules. Found and fixed a real bug via the new render-based visual-check workflow (Aquaset's palm trees were wired to URP materials and rendered magenta on this Built-in RP project — swapped to the pack's own Built-In material variants) and a density problem (first pass was too sparse) before shipping a build. Windows build produced, zero errors in a headless run, launched for the user to test.
- **Phase B bugfix pass** from user hands-on testing: fixed inverted roof pitch (was rendering as a valley, not a gable — sign error in the roof rotation), removed Shanty Town variants 16/18/20 which have no matching material in the source pack (the "white shanty house" bug), scaled Shanty Town meshes up 2.1x (imported undersized), roughly doubled house density again, rescaled `OnGUI` prompt/feedback text against a 1080-tall reference (was fixed-pixel-size, unreadably small — also a mobile-readiness fix), fixed an `AnimatorController` caching bug that could mask animation fixes across rebuilds, and gave the two characters distinct skin tones (darker for the controllable character/Smart, lighter for the NPC placeholder/Strong) via a per-instance material clone of the pack's `skin` material slot. Full story (`Docs/STORY.md`) and a future shop/accessories/vehicles/property extensibility note (memory) recorded from the user's latest messages. Phase C scope (NPCs, HUD meters, crop selection/growth, Tab switching) is now clearly listed in `TASKS.md`.
- **Pause menu + mouse-look:** Esc opens a Resume/Quit pause menu (mouse-clickable UGUI buttons, keyboard-navigable, Q quits while paused); `ThirdPersonFollowCamera` now orbits horizontally under mouse control while the cursor is locked. Hit and fixed a real Unity 6 API break along the way (`Resources.GetBuiltinResource<Font>("Arial.ttf")` throws — renamed to `LegacyRuntime.ttf`). Verified via a render of the pause menu (caught and fixed a `GameObject.Find`-doesn't-search-inactive-objects bug in the verification script itself) plus a clean headless run before shipping the build.
- **Phase C:** economy layer (`CropDefinition`/`EconomyManager`/`CropSelectionController` — shared money/inventory/heat, 1-4 crop selection), a real `FarmPlot` state machine across 6 plots (light/dark soil, 3-stage growth, green→red tomato colour), 4 role-based NPCs (Villager/Police/Shopkeeper/Buyer), a HUD (health/stamina/heat/money/crop/name), and a real second controllable character — Strong, lighter-skinned, switchable via Tab (`CharacterSwitchManager`/`FollowController`/`CharacterVitals`, independent health/stamina, shared economy). Then, per the user's explicit ordering, two deferred fixes: `ThirdPersonFollowCamera` rewritten as a proper spherical orbit so mouse-look now covers vertical pitch too, and Shanty Town buildings (which never had a `Collider` — FBX imports don't get one automatically) now block the player via bounds-fitted `BoxCollider`s. Compile clean on the first attempt for the whole addition; scene builder, static validation (updated for the new component types), and a real Windows build with a **zero-error 10-second headless run** all passed before shipping. Nothing in Phase C has been hands-on playtested yet.
## 2026-08-15 - MINI-023

- Added slower police pursuit with officer stamina, exhaustion stops and recovery.
- Added mission failure at zero health and safehouse respawning/restoration.
- Restored Sacat then Franki naming and added a persistent name/full inventory HUD.
- Added Boss K and vagrant weed routes, black-market clothing resale, and 50/30 heat rules.
- Restricted police proximity heat to carrying weed or weed seeds.
- Added E companion crop orders and two post-weed missions.
- Rebuilt the generated scene; compile, Windows build and player initialization pass.
## 2026-08-15 - MINI-029

- Added branching legitimate-farming versus weed-route progression.
- Added four faction reputations, Boss K exploitation/withheld payments, Boss M/Boss P, locked Black Sugar/Purple, land searches/sabotage, and Grand Bay-gated Guadeloupe access before future Roseau expansion.

## 2026-08-20 — MINI-084

- Added a repo-scoped Up Iz Up Mini production skill shared through `.agents/skills`.
- Added the canonical Codex/Claude production loop, work-packet template, visual approval/lock register, compact current-state handoff, and a read-only ownership/dirty-tree preflight check.
- Documented the low-budget Map Truth, Hitem3D/Blender modular-character, bike IK, combat, NPC, visual QA, and mobile-first gates.
- Audited the current world, character/animation/vehicle, and mobile/QA foundations without changing gameplay, scenes, prefabs, packages, or project settings.
- Did not commit because the working tree already contained a large unrelated uncommitted MINI-052–083 batch; absorbing it into this documentation checkpoint would be unsafe.

## 2026-08-20 — MINI-085

- Preserved the complete current project state on `codex/mini-085-baseline-20260820` at checkpoint `b367fc5` before testing.
- Passed Unity compile, generated-scene rebuild, TMAX/Rover/faction/gang/chain validators, a fresh Windows build, and a clean 15-second headless player smoke.
- Generated current scene/vehicle evidence and inspected the 1280x720 build live: H tutorial, Tab switching/objective progression, inventory, E prompts, camera orbit, basic movement, and both hero idle poses work.
- Recorded two concrete opening issues for the next small fix: the active player name is absent from the HUD and the safehouse roof can obstruct the initial camera. No visual appearance was locked without user approval.

## 2026-08-20 — MINI-086 chain completion

- Captured Sacat's user-approved `GoldChain18k` transform into a permanent placement profile and visual lock `VA-001`.
- Added an immediate post-purchase equipment refresh so a wearable appears on the protagonist who bought it without relying only on event subscription order.
- Kept the cleaned real chain on Boss C and removed Boss J's chain at the generated-scene source.
- Added a transaction/distribution validator proving Sacat's purchase equips only Sacat, Boss C has the real chain, and Boss J has none.
- Rebuilt and statically validated GrandBayProof, produced a fresh Windows build, and completed a clean 12-second headless player smoke.

## 2026-08-20 — MINI-089 dialogue wording

- Replaced the generic locked-progression response with the user's exact line: `Keep doing your ting. I'll maybe organize you when you build up ur self`.
- Added a focused locked-Boss-C validator, rebuilt the generated scene, and produced a successful Windows build.

## 2026-08-20 — MINI-090 input and interaction foundation

- Added one named-action input facade over the unchanged Legacy Input Manager, with injectable virtual movement, look, and button state for future mobile controls.
- Migrated core on-foot movement, run, jump, camera look, interaction, cloning, farmhand assignment, character switching, tutorial, and melee while preserving every existing PC key.
- Passed focused validation, canonical scene rebuild, Windows build, and a 15-second headless player smoke.

## 2026-08-20 — MINI-091 NPC and companion intelligence

- Added stable trailing formations and stop/resume hysteresis for the inactive hero and recruited followers.
- Added bounded, allocation-free nearby-character separation plus blocked wait/retry behavior while retaining terrain-friendly direct companion steering.
- Applied dynamic safety and staggered timing to patrol NPCs.
- Added explicit police Patrol, Chase, Search, Recover, and Down states with line-of-sight and last-known-position searching while preserving police stamina.
- Preserved and validated the existing last-rival escape, defeated-rival despawn, and distance-pool respawn lifecycle.

## 2026-08-20 — MINI-092 combat contact truth

- Added one reusable windup/active/recovery timeline and forward fist-path resolver for player, companion, and gang attacks.
- Removed immediate omnidirectional proximity damage: targets must remain in front, inside the short swept path, and unobstructed when the active moment arrives.
- Enforced one hit per swing, recovery/cooldown, and a seven-stamina player cost.
- Corrected max-heat escalation so it applies when police are struck, not unrelated rivals carrying the same health component.
- Passed focused contact validation, companion and gang regressions, canonical rebuild, Windows build, and a 15-second headless smoke.

## 2026-08-20 — MINI-093 police melee retaliation

- Chasing police now punch at close range through the shared windup/active/recovery contact system and apply 12 player damage once per valid swing.
- Behind/out-of-contact/dead targets are rejected, and the 1.1-second cooldown prevents rapid repeated damage.
- Damage flows through `CharacterVitals`, so zero health continues into the existing mission-failure and safehouse-respawn system.
- Passed saved-scene default checks, deterministic damage/death validation, and the MINI-092 combat regression without rebuilding the scene or Windows player.
# MINI-094 — Grand Bay map-truth preview

- Added a reusable licensed OSM/Overpass map export and overhead rendering tool.
- Added the first coordinate-based `MAP-ANCHORS.json` with local metre conversion and explicit verification states.
- Produced a 1600x1000 Lalay-to-beach planning screenshot without modifying the working gameplay scene.
- Recorded Highland as the first remote planting district; its exact turnoff remains pending local confirmation.

## 2026-08-20 — MINI-095 Lalay/Highland map lab

- Built a separate repeatable Unity map-lab from 130 OSM/user-confirmed road lines and a licensed Copernicus GLO-30 terrain crop.
- Added the explicit Lalay-to-Highland inroad, first active Highland farm, three reserved future parcels, coastal jetty, waterways, landmarks, grey Lalay sidewalks, and 69 close low-poly house masses.
- Smoothed Lalay into a bump-free maximum 1.5% continuous grade with a wide yard transition while retaining stronger Highland relief; densified road meshes so secondary roads follow terrain and reject house overlap.
- Recorded the user's approved road network and original Lalay street target as `VA-004`; the playable `GrandBayProof` scene remains untouched pending a separate migration task.

## 2026-08-20 — MINI-096 reusable world expansion

- Added a mandatory gated workflow for every future Dominica district, island or unrelated map.
- Added reusable district packet and manifest templates plus safe scaffold/validation commands.
- Registered Lalay/Highland as `dm-dom-grand-bay-lalay-highland-v1` with sources, bounds, anchors, grading, connections, passability, approvals, rejected evidence, budgets and migration state.
- Proved a fresh scaffold validates, incomplete map truth is rejected, existing districts cannot be overwritten, and Lalay cannot advance past graybox until its corrected player-height view is approved.
- No Unity scene, prefab, package, project setting or production map data changed.
## 2026-08-20 — MINI-099 accepted map graybox

- Curated the phase-one Grand Bay world to nine connected, collidable roads and four centered two-vehicle bridges.
- Corrected road/terrain conformity, Lalay and Highland grades, coastal sand/no-house land use and jetty reach.
- Built 115 Lalay houses and 18 Highland buildings; registered one active and seven inactive farm parcels.
- Added `VA-005`, advanced the district to `approved_graybox`, and documented the reusable Caribbean map recipe.

## 2026-08-20 — MINI-101 runtime map correction

- Moved seven Lalay shops and their sellers into deterministic roadside lots, replacing only conflicting placeholder houses and keeping the paved road clear.
- Regraded the Lalay/Highland connector and farm spur as one shared gentle corridor; flattened and relocated the active farm, safehouse and breeding station off the road.
- Added repeatable road-join, shop-clearance and farm-level validators plus fixed map screenshots.
- Added a road-following runtime navigation-link chain because the legacy Edit-Mode Sacat-to-farm NavMesh check still reports a partial route.
- Rebuilt `Builds/GrandBayProof/UpIzUpMini.exe`; runtime driving and companion navigation remain the user acceptance gate.
# 2026-08-21 — MINI-107 (paused)

- Added the reusable Up Iz Up Mini character manifest schema, Sacat manifest, production workflow, generic Blender LOD tool, PowerShell runner and isolated Unity motion-proof builder.
- Confirmed the original 100K Sacat rig animates correctly with the owned Humanoid clips.
- Rejected the current decimated mobile LODs for motion because joint deformation tears; no playable character or gameplay scene was changed.
