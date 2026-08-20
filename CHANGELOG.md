# Up Iz Up Mini — Changelog

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
