# Up Iz Up Mini — current Claude development handoff

Updated 2026-08-28, superseding the 2026-08-21 version below wherever they conflict. **Everything in sections 5 and 6 about MINI-109 through MINI-116 is DONE** — that entire sequence shipped between the previous update and now (see `Docs/CURRENT.md`'s top entries and `PROJECT-HANDOFF.md`'s matching MINI-119 follow-up entries for the full record, or `git log --grep=MINI-119` for every individual commit). The current real state and the actual next task are in the new section 0 immediately below. The rest of this file (sections 1-4, 7-9) is still accurate as general project truth and workflow; only the "what's done / what's next" narrative in 5-6 and the stop condition in 10 are stale. This is the compact operational truth Claude should read first. `PROJECT-HANDOFF.md` remains searchable audit history; it is not a substitute for this current brief.

## 0. Actual current state and next task (2026-08-28 - read this before sections 5/6/10)

SuperMoto riding (mount/ride/wheelie with real hand-foot IK/pillion) is fully shipped and user-confirmed via real exe play. On top of that, in the same continuation, this session shipped: NPC ragdoll-on-hit (Police/Villager/Gang - fatal fades after 3s lying down, non-fatal recovers after 2s and walks); a Koss bike purchasable at the Car Dealer for $2,500 (reuses the proven SuperMoto wiring via a new shared `WireSuperMotoInstance` helper); 7 more Villager NPCs and a corrected Lalay/Highland `AreaNameDisplay` zone boundary (final: Lalay = Dog Life block to Car Dealer, Highland = bridge to farm/safehouse cluster, both verified via direct `ResolveArea` calls against real landmarks); a farm-hedge containment fix (a villager was standing inside the hedge, confirmed via a new geometry check, moved outside); a wheelie back-clip damping fix (measured the position/pitch math was NOT the cause - it's the authored wheelie-overlay clip - dialed its weight down while mounted on the SuperMoto specifically); and `AutoMountSuperMotoOnSpawn` turned back off per the user's own request now that the feature is proven (dev bike spawn itself stays on).

**Next task, NOT yet started**: combat mechanics. User: "Can we start working on the combat mechanics maybe fighting first and then shooting after?" Follow-up narrowed the very first target to **fixing the existing bugs** (warped punch pose, over-generous hit detection - both previously reported, never triaged into a task) rather than new combos, per the user's own explicit choice. The user is also deciding whether to buy a paid fighting/boxing animation pack; recommended a small real-mocap unarmed/boxing pack over a free one (free packs are the most common source of exactly the warped-pose symptom already on record) - no purchase made, needs the user's approval and their own pick of a specific asset before any credits/money are spent.

**Immediate blocker**: none of this session's MINI-119 follow-up work has been hands-on playtested by the user yet (only their own live direction mid-session, e.g. the three zone-boundary corrections). Do not start the combat task until that playtest happens and comes back clean, per this project's own standing "one compound change, then playtest" discipline - the same discipline that governed MINI-109 through MINI-116 below.

## 1. Project identity and hard boundaries

- Writable project: `E:\Unity\Up Iz Up Mini`
- Unity version: `6000.3.10f1`
- Full-size reference project: `E:\Unity\Up iz up` — read-only
- External asset library: `E:\Assets` — read-only; do not import unverified packages wholesale
- Canonical generated gameplay scene: `Assets/UpIzUpMini/Scenes/GrandBayProof.unity`
- Canonical scene generator: `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs`
- Windows build: `Builds/GrandBayProof/UpIzUpMini.exe`
- Current map ID: `dm-dom-grand-bay-lalay-highland-v1`
- Internal compatibility IDs such as `BossK`, `MontineFarm` and `land_montine` must remain unchanged. Player-facing language uses `Boss J` and `Highland`.
- The working tree contains valid, uncommitted user/Codex work from MINI-104 through MINI-108. Never reset, clean, checkout, normalize or absorb unrelated dirty files.

## 2. Required Claude startup sequence

Read completely, in this order:

1. `.agents/skills/upizup-mini-production/SKILL.md`
2. `AGENTS.md`
3. `Docs/CURRENT.md`
4. this file
5. `Docs/AI-PRODUCTION-WORKFLOW.md`
6. `Docs/WorkPackets/MINI-109.md`
7. the `### Current claim` block and MINI-108/MINI-109 entries in `PROJECT-HANDOFF.md`
8. relevant sections of `TASKS.md`, `Docs/STORY.md`, `Docs/GAME-DESIGN.md` and `Docs/DIALECT-LEXICON.md`
9. `Docs/WORLD-EXPANSION-WORKFLOW.md` only if touching terrain, roads, anchors, lots, buildings or map migration
10. `Docs/CHARACTER-PRODUCTION-WORKFLOW.md` only if touching characters, garments, accessories, rigging, LODs or motion

Then:

1. Confirm `Current owner` is `None`.
2. Run `Tools/AIWorkflow/Invoke-Preflight.ps1 -Agent Claude -TaskId MINI-109`.
3. Claim exactly `MINI-109`; reserve only the files actually needed.
4. Reproduce the reported bugs before editing.
5. Implement the smallest bounded packet, validate, capture evidence, build once, update handoff files and release ownership.

Do not start `MINI-110` in the same pass. The user wants to playtest between compound changes.

## 3. Game vision

Up Iz Up Mini is a compact modern 2.5D/three-quarter 3D Caribbean farming-and-crime progression game set in Grand Bay, Dominica. Sacat and Franki are two school dropouts trying to build a farming business. Legal produce pays slowly; higher-risk weed work brings money, police heat, exploitative bosses, territorial gangs, Guadeloupe opportunities and eventually Roseau expansion.

The game should feel specifically Grand Bay:

- Lalay is the narrow, slightly inclined main street with dense homes and roadside commerce.
- Highland is the first farming district, reached by a dirt inroad from Lalay.
- The bay, church, sand/stone shore, jetty, Boat Man and boat form the coastal story area.
- Road topology and geographic anchors are sourced/compressed; buildings are stylized, swappable game art.
- The audience is primarily future mobile players, even while PC keyboard controls remain the current test surface.

Presentation target:

- stylized low-poly 3D, not a flat top-down tile game;
- perspective three-quarter follow camera;
- readable warm Caribbean colour, corrugated roofs, vegetation and modest concrete houses;
- text-first Dominican dialogue, used naturally rather than as a caricature;
- stable systems that allow later assets, maps, missions and characters to be swapped without rewriting core logic.

## 4. Current playable foundations — improve, do not restart

### Characters

- Sacat and Franki are switchable protagonists with separate vitals/positions and shared economy/progression.
- Sacat is the smarter business character; Franki is stronger/faster with more stamina.
- Locomotion, jump, interaction, combat, vehicles and companions already exist.
- User-approved Sacat and Boss C two-piece chain placements are visual locks. Never recalculate or replace their transforms.
- MINI-107 modular Sacat work is paused. The original 100k rig animates correctly; the 25k/12k/4.5k LODs fail motion deformation and must not enter gameplay.

### Farming and economy

- Crops, stages, watering, harvesting, cloning, inventory, plot ownership and an automated farmhand loop exist.
- Current crop IDs include `tomato`, `banana`, `carrot`, `bushers`, `black_sugar`, `purple`, `blue_cheese`, `purple_black`, `sugar_cheese` and `purple_cheese` where defined.
- One Highland plot starts active; additional plots are progression purchases.
- Legal and illegal sellers, apparel/accessory purchases, food, pharmacy, land and vehicles exist.

### Missions and dialogue

- The mission state machine is `Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs`.
- The actual mission content is serialized by `BuildMissions` inside `Mini011PhaseBSetup.cs`; editing only the saved scene is temporary and will be overwritten.
- MINI-108 added a readable transparent black dialogue/mission panel, E fast-forward for the introduction, K/L early route choice, safehouse rest, clean-police/Normy mission and `111111` current-mission skip.
- Every marked objective uses a blinking yellow minimap marker that clamps to the edge when far away. Yellow always means current mission.

### NPCs, heat, gangs and combat

- Boss J is the first exploitative weed employer; internal ID remains `BossK`.
- Boss C releases higher strains and has the approved chain/Range Rover presentation.
- Normy is a crooked contact, not a normal police officer.
- Paro is the rough street buyer and only starts buying after the introductory talk.
- Brakes is the priest by the church.
- Rasta is the strain/farming mentor.
- Dog Life is the rival gang; Not Ah Word is the player's future crew.
- Police patrol/chase/search/recovery, heat, reinforcement, timed melee contact, hit reactions and death/safehouse recovery exist.
- Combat animation quality is still placeholder. Do not expand to a large combat tree before replacing the warped clip and proving one clean punch/hit/knockdown loop.

### Vehicles, phone and Guadeloupe

- TMAX riding/wheelie/pillion, Range Rover driving/passengers and radio exist.
- `CellPhoneController` already uses Q after the player owns `phone_basic`; it calls the other protagonist and recruited crew, and can fill empty Range Rover seats.
- `GuadeloupeTrade` already supports a first NPC courier and later sends the inactive protagonist away, locks switching, hides the character, shows a remaining time through its API and returns the character later.
- The next story pass should reuse these foundations, not duplicate them.

## 5. Verified current state

MINI-108 passed:

- canonical generated-scene rebuild;
- focused early-mission validation;
- Lalay/Highland map-lab validation;
- migrated Grand Bay map validation;
- Windows build;
- 12-second built-player headless startup with no fatal errors.

Evidence is under `Logs/Tasks/MINI-108/`. The latest EXE still needs hands-on end-to-end mission playtesting. A compile does not prove dialogue timing, mission feel, walking, riding, driving, crowd movement or combat.

The temporary procedural banana remains intentionally frozen even though it looks odd. The user will later replace it through Hitem3D. Do not spend time polishing it unless asked.

## 6. User's newly approved story/progression direction

### Immediate repair — MINI-109

Follow `Docs/WorkPackets/MINI-109.md` exactly:

- Normy must take the Clean Face mission payment once and complete it; no stuck repeated service loop.
- Delivering harvested Black Sugar to Boss J must actually complete.
- Rasta's current location, not his old map position, must drive the Ital mission marker.
- Define retrospective completion versus progression locking consistently.

### Next narrative bridge — MINI-110

This comes only after the user accepts MINI-109.

1. Before the Boat Man chapter, Normy asks the player to bring requested food and pharmacy items. This should use item IDs and inventory truth, not just visiting shops.
2. Normy then gives uncertain street information: somebody may be taking the boys' crop/stuff, but he does not know who; he suggests checking the Boat Man about Gardey Zafeh in Guadeloupe.
3. First Boat Man meeting needs a real introduction, approximately:
   - `Boat Man: I see allu on allu hustle. Dat is a good ting.`
   - `Boat Man: When allu want go up and make some euro, talk to me.`
   - `Sacat/Franki: So you could check some business by Gardey in Gwada?`
   - `Boat Man: Awright. I will introduce allu to the scene up dere one time.`
4. Seed earlier dialogue about a Grand Bay gang before the Gardey reveal; do not reveal Dog Life without buildup.
5. On a protagonist courier run, the inactive protagonist must physically disappear and remain unswitchable while away. A visible HUD/phone timer must show the return countdown. The current `GuadeloupeTrade` already provides the lock, hide and `SecondsRemaining`; connect UI and mission truth rather than recreating travel.
6. Normy's favour mission belongs before the Boat Man/Gardey mission.

### Rasta strain ladder — MINI-111

Rasta should stop asking for tomatoes. His chapter becomes the production/strain school:

1. grow/harvest 3 Bushers;
2. later, 3 Black Sugar;
3. later, 3 Purple;
4. unlock and prove Blue Cheese;
5. teach the first mixed strain, Purple Sugar (if the implementation retains the legacy `purple_black` ID, document the player-facing rename/migration);
6. Sugar Cheese;
7. Purple Cheese.

Each tier must be more valuable and story-gated. The player should not see or buy locked strains early. Use data definitions and mission requirements rather than duplicating crop-specific logic in NPC code.

Only after the boys hold all core strains and meaningful stock should the story say they are winning the Grand Bay market and open the Guadeloupe opportunity. Guadeloupe remains before Roseau expansion.

### Dog Life escalation — MINI-112

- Dog Life becomes jealous as the boys gain strains, stock and Grand Bay market share.
- The story first builds the player's crew, then sends them to attack/confront Dog Life.
- Defeated/disabled Dog Life pool members should become available again after roughly 100 seconds when the player has left the scene.
- Dog Life fighters must stop chasing when the player is too far, return to their block and resume their block behavior.
- A small group may occasionally leave the block and walk down Lalay together, within mobile simulation limits.
- Do not have the whole pool permanently active. Reuse and extend `RivalGangSpawner` and current combat/respawn systems.

### World and navigation cleanup — MINI-113

- Smooth bumps where secondary road ribbons intersect Lalay and other driveable roads.
- Keep the accepted road topology; grade surfaces/corridors together so there are no floating or doubled collision lips.
- Pull the farm privacy hedge fully off the Highland road while keeping all plots screened and the dirt-road entrance clear.
- Add minimap markers for owned/usable vehicles.
- Spawn purchased/current vehicles in road-safe spaces, aligned with road direction and never blocking traffic.
- Use the map-lab -> validation -> fixed screenshots -> migration workflow. Map changes are Class C when topology/approved placement changes, Class B when only repairing collision/grade within an approved corridor.

### Brakes/priest missions — MINI-114

- Brakes does not accept personal payment.
- If the player offers money, he can explain that church collection is separate from his help.
- Give him small community errands to obtain supplies/items.
- A completed blessing restores health to 100%; do not sell blessings as an ordinary shop service.
- Keep the stronger Gardey/strain-related content locked until the relevant story stage.

### Cellphone progression — MINI-115

- Add a mission that awards/unlocks `phone_basic` and teaches the phone.
- The first use is calling the other protagonist back from farming.
- Later reputation unlocks calling recruited gang members for backup.
- The UI should hint the feature only when unlocked.
- Keep calls routed through the existing input abstraction and define a future touch button; Q remains the PC test key.

### TMAX production repair — MINI-116

- The current TMAX can look see-through and its build payload is far too large for mobile.
- Diagnose normals/backface holes, missing geometry, materials, texture import and GLB payload separately. Do not merely add double-sided materials if the mesh is actually broken.
- Current audit: the TMAX source contributed about 172 MB to a Windows build despite modest visible geometry. Target a production copy with LODs, texture atlas/compression, simple colliders and preserved riding anchors.
- This is a visual/performance packet requiring before/after screenshots, short riding capture and build-size evidence.

## 7. Workflow Claude must use for every kind of work

### Universal bounded loop

`intent -> references -> task packet -> preflight/claim -> reproduce/audit -> cheap preview -> user approval when needed -> one-owner implementation -> compile/validate -> screenshot or motion capture -> user accept/revise -> visual lock -> handoff/release`

Use one feature packet at a time. Do not “improve nearby things” outside the packet.

### Code and mission workflow

1. Find the source of truth and existing extension seam.
2. Write a focused validator that first reproduces the reported defect.
3. Keep content/data separate from generic controllers where practical.
4. Preserve save-facing IDs and add migrations only when unavoidable.
5. Rebuild the generated scene after compound edits, not after every tiny text change.
6. Run focused validation, standing regressions, build and smoke.
7. Ask the user to play the exact mission path; automated checks cannot prove feel.

### Map/world workflow

Read `Docs/WORLD-EXPANSION-WORKFLOW.md`. Every district has a manifest, licensed sources, stable anchors, approval history and progression-safe parcels. Never ship satellite imagery. Build/repair in Map Lab, validate roads/colliders/grades/clearance, show overhead and player-height screenshots, then migrate with rollback. Require live walking and two-way vehicle testing.

### Character/wardrobe/accessory workflow

Read `Docs/CHARACTER-PRODUCTION-WORKFLOW.md`. Hitem3D creates candidates; Blender owns topology, UVs, skeleton, weights, bind pose and export. Approve front/back/side/three-quarter reference views before spending credits. Use one canonical full-finger Humanoid body per wardrobe family. Clothes are separate skinned geometry using the same skeleton/bind pose; hide covered body regions. Accessories use named per-character attachment profiles. Approved manual transforms win over calculations.

The manifest system is already present under `Docs/CharacterPipeline/System/` and `Tools/CharacterPipeline/`. Do not integrate the failed MINI-107 LOD motion proof.

### Asset workflow

1. Check `Docs/ASSET-REGISTER.md` before importing.
2. Record source, license, purpose, triangle/material/texture budget and derivative steps.
3. Use already owned/free acceptable assets first.
4. Never import an entire legacy package just because one mesh looks useful.
5. Stage in isolation, inspect pivots/normals/materials/colliders/scale, optimize, create LODs and only then integrate.
6. Paid generation or purchases require user approval before credits/money are spent.

### Animation/motion workflow

- A valid Humanoid avatar is only a static gate.
- Use owned Starter Assets/Human Basic Motions clips where suitable.
- Capture idle, walk, run, jump and required action at normal speed.
- Bike work uses seat/grip/peg targets, per-rider fit profiles, full-finger baked grip poses and one shared lean source.
- Combat uses timed contact, facing/LOS and one hit per swing before combos/ragdolls/shooting.

### Mobile workflow

- Keep new controls behind `GameInput` or another explicit facade with a touch equivalent.
- Use LODs, <=4 skin weights, 1024 atlases for ordinary assets, pooling, culling and bounded update distances.
- The project currently uses Built-in RP and legacy Input Manager despite older URP aspirations. Do not migrate rendering/input inside a content task.
- Android build/device profiling is a separate approval packet.

## 8. Tools, skills, apps and live control

Use the best available capability, but keep one Unity integrator:

- filesystem/code tools for search, patching, diffs, validators and batch builds;
- the Up Iz Up Mini production skill as the project procedure;
- Unity Editor for canonical scene rebuild, inspector confirmation and runtime testing;
- Blender for topology, UV, LOD, weights, garments and exports;
- Hitem3D only after an approved asset card/reference sheet;
- browser control only for signed-in Hitem3D/asset research explicitly within scope;
- computer/live control for Unity or Blender inspection when batch tools cannot show the issue;
- screenshots for static appearance and short screen recordings for movement, riding, driving, combat, NPC pathing and UI timing.

The user authorizes visual inspection/live control for development, but that does not authorize purchases, uploads, destructive cleanup, package migration or external publishing. If an important visible choice is uncertain, show the recommended preview and ask.

## 9. Verification and evidence contract

Every completed packet records:

- exact files changed;
- exact Unity/editor methods and logs;
- focused validator result;
- standing regression result;
- build result and executable path;
- screenshots in `Logs/Tasks/MINI-###/` with stable names;
- a short motion capture for any movement claim;
- mobile/content budgets when assets are touched;
- user approval status and remaining hands-on check;
- updated `Docs/CURRENT.md`, work packet, `PROJECT-HANDOFF.md`, `TASKS.md`, `CHANGELOG.md`, plus decisions/assets/visual locks when applicable;
- released ownership.

Useful existing editor entry points:

- `UpIzUpMini.EditorTools.Mini011PhaseBSetup.BuildScene`
- `UpIzUpMini.EditorTools.Mini100GrandBayMapValidation.Validate`
- `UpIzUpMini.EditorTools.Mini100GrandBayMapValidation.Capture`
- `UpIzUpMini.Editor.Mini095LalayMapLabSetup.BuildValidateCapture`
- `UpIzUpMini.Editor.Mini108EarlyMissionValidation.Validate`
- `UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer`

Never report a visual/motion system as accepted merely because it compiles.

## 10. Stop condition for the next Claude session (superseded 2026-08-28 - see section 0)

This section's original MINI-109 stop condition is done; kept below only as a historical example of the pattern to follow, not a live instruction.

The current stop condition: do not start the combat bug-fix pass until the user has hands-on playtested this session's full MINI-119 follow-up chain (ragdoll, Koss, villagers, zone boundary, wheelie damping, auto-mount off) and reported back. When that task does start, reproduce the warped-punch-pose and over-generous-hit-detection bugs first, exactly as this project's own history shows every real fix in this task started from reproduction, not a guess - see any `Mini119*.cs` diagnostic tool from this session for the pattern (measure/reproduce via Unity batch mode against the real scene, THEN fix, THEN re-verify the same way).

Original MINI-109 condition, for reference: implement only MINI-109, stop after its evidence and build are ready, ask the user to test Clean Face through one Normy payment, Black Sugar delivery to Boss J, the Rasta mission marker, and whether previously owned/completed requirements advance fairly. Do not start the Boat Man, Rasta ladder, Dog Life, road, priest, phone or TMAX packets until that feedback returns. (All of this has since happened - see section 0.)
