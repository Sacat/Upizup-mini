# Up Iz Up Mini — Project Handoff

## Project

- Path: `E:\Unity\Up Iz Up Mini`
- Unity: `6000.3.10f1`
- Reference project: `E:\Unity\Up iz up` — read-only
- Status: MINI-018 built. Animation root cause found and fixed (see MINI-016). See Docs/CODEX-HANDOFF.md for continuation context. Fixed the real T-pose cause (NPC models were Generic rigs, not Humanoid - all 9 now verified valid humanoid), fixed running by measuring the clips' authored speed (1.90/3.80 m/s), fixed floating fruit, added Space jump, seed/cloning economy, a fake-brand shop, H controls, street signs, fading area names, and renamed the protagonists to Franki and Sacat with dialogue from the user's real supplied conversation. Windows build produced, zero errors in a 12s headless run. **Missions are still not built** - see MINI-013 known issues. Not hands-on playtested yet.
- Current owner: None
- Active task: None
- Last verified change: `MINI-000`; `MINI-001` implemented pending confirmation; `MINI-011` Phase C, `MINI-012`, `MINI-013` built and statically/headlessly verified, pending user playtest.
- Last known good commit: `9f80953`

## Ownership protocol

Before editing, set:

```yaml
current_owner: Codex | Claude | User
active_task: MINI-###
claimed_at: ISO-8601 timestamp
reserved_files:
  - exact/path
```

### Current claim

```yaml
current_owner: None
active_task: None
```

After verification, append a change entry, update the verification results, and return the owner and active task to `None`.

## Current blockers

- The precise Grand Bay map anchors must be copied into `Docs/MAP-ANCHORS.json` from verified research/user references (still not done — `MINI-002`).
- `GrandBayProof.unity` needs a hands-on human playtest via `Builds/GrandBayProof/UpIzUpMini.exe`. Highest-uncertainty items now: whether NPCs actually animate after the rig conversion, running feel at the measured speeds, jump, and the shop/seed loop.
- **Missions are not implemented.** The user asked for full missions (planting/selling/police/bosses/buying land). The underlying systems exist (economy, shop, seeds, heat, land item) but there is no mission/objective state machine, no boss NPCs, and buying land has no gameplay effect yet. This needs its own task.

## Change record

### MINI-000 — Project and coordination scaffold

- Date: 2026-08-15
- Owner: Codex
- Request: Create the separate Mini project structure and a handoff strategy for Claude Work.
- Implementation: Created a clean Unity 6 project and the coordination/design documentation scaffold.
- Files changed: Coordination and documentation files at the project root and under `Docs/` and `Coordination/`.
- Scene/prefab changes: None.
- Verification: Unity project reports editor version `6000.3.10f1`; documentation structure validated.
- Known issues: Gameplay and assets are intentionally not implemented yet.
- Next action: Claude or Codex claims `MINI-001` and builds the camera/world proof of concept.

### MINI-001 — 2.5D Grand Bay proof of concept

- Date: 2026-08-15
- Owner: Claude
- Request: Build the MINI-001 vertical slice per `TASKS.md` acceptance criteria (three-quarter follow camera, walk/run capsule, Lalay road with buildings, dirty Montine farm path/clearing, NPC talk prompt, farm plot plant prompt).
- Acceptance criteria: see `TASKS.md` MINI-001.
- Implementation:
  - Built the scene entirely through a repeatable editor script (`Mini001SceneSetup.cs`) rather than hand-edited scene YAML, per D-004: ground plane, a narrow `RoadSurface` box flanked by 8 simple two-part buildings (wall + roof cube), a `DirtyPath` box branching off the road toward a `FarmClearing`/`FarmPlot`, one `NPC_Villager` capsule, one `Player` capsule, a directional light, and a `MainCamera`.
  - `PlayerController` (CharacterController-based, camera-relative walk/run, Left Shift to run) and `ThirdPersonFollowCamera` (fixed-yaw offset, LookAt-based, ~44° downward look angle from distance=7/height=6.8) satisfy the movement/camera criteria.
  - `IInteractable` / `InteractableBase` / `NPCInteractable` / `FarmPlotInteractable` / `InteractionDetector` implement the range-detection, "[ E ] Talk" / "[ E ] Plant" prompts, and one-line NPC dialogue on interact.
  - Prompts are rendered with legacy IMGUI (`OnGUI` + `Camera.WorldToScreenPoint`), not a UGUI World Space canvas, because this project has no `com.unity.ugui`/TextMeshPro package installed yet. They track world positions correctly but are not literally Canvas-based; a future UI pass should swap in Canvas + TMP once those packages are deliberately added.
  - `FarmPlotInteractable` only marks the plot and shows a feedback line — the real plant/water/grow/harvest loop is explicitly MINI-003 scope, not reimplemented here.
- Files changed:
  - `Assets/UpIzUpMini/Scripts/Interaction/IInteractable.cs`
  - `Assets/UpIzUpMini/Scripts/Interaction/InteractableBase.cs`
  - `Assets/UpIzUpMini/Scripts/Interaction/NPCInteractable.cs`
  - `Assets/UpIzUpMini/Scripts/Interaction/FarmPlotInteractable.cs`
  - `Assets/UpIzUpMini/Scripts/Interaction/InteractionDetector.cs`
  - `Assets/UpIzUpMini/Scripts/Character/PlayerController.cs`
  - `Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs` (namespace `UpIzUpMini.Cameras`, not `.Camera` — collides with `UnityEngine.Camera` otherwise)
  - `Assets/UpIzUpMini/Editor/Mini001SceneSetup.cs`
  - `Assets/UpIzUpMini/Editor/Mini001SmokeTest.cs`
  - `Assets/UpIzUpMini/Editor/Mini001SceneValidation.cs`
  - `Assets/UpIzUpMini/Art/Materials/*.mat` (8 generated materials)
- Scene/prefab changes: Created `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (generated by `Mini001SceneSetup.BuildScene`, added to Build Settings).
- Verification commands:
  - `Unity.exe -batchmode -nographics -quit -projectPath "E:\Unity\Up Iz Up Mini" -logFile Logs\mini001-compile.log` (compile check, no scene) → exit 0, no `error CS`.
  - `Unity.exe -batchmode -nographics -quit -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini001SceneSetup.BuildScene -logFile Logs\mini001-buildscene.log` → exit 0, scene saved.
  - `Unity.exe -batchmode -nographics -quit -projectPath "E:\Unity\Up Iz Up Mini" -logFile Logs\mini001-compile2.log` (final compile check with all files) → exit 0, no `error CS`.
  - `Unity.exe -batchmode -nographics -quit -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini001SceneValidation.Run -logFile Logs\mini001-validation.log` → exit 0, `MINI-001 STATIC VALIDATION PASS`.
- Verification results:
  - Batch-mode compile: clean, twice, no errors.
  - Static scene validation (no missing scripts anywhere in the hierarchy; `Player` has `CharacterController`/`PlayerController`/`InteractionDetector`; `MainCamera` has `Camera`/`ThirdPersonFollowCamera` targeting the Player at a 44.3° look angle, inside the 40-50° range; at least one `NPCInteractable` and one `FarmPlotInteractable` present): **PASS**.
  - Headless Play-mode smoke test (`Mini001SmokeTest.cs`): attempted twice (with and without `-nographics`), both times hung and had to be force-killed. Root cause confirmed from the log both times: `UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase()` throws `ArgumentOutOfRangeException` inside `SearchInit.IndexationOnStartup`, triggered when Play mode is entered via `-batchmode -executeMethod` in this environment. This is an Editor Search-module issue unrelated to the GrandBayProof scene or scripts (it fires before any of our `MonoBehaviour`s run). **Not resolved** — treat as an environment limitation, not a code defect.
  - No automated Edit/Play Mode tests exist — `com.unity.test-framework` is not installed in this project yet (`Assets/UpIzUpMini/Tests/` is still empty scaffolding from MINI-000).
- Known issues:
  - **Runtime/visual behavior is not yet play-tested.** Movement feel, prompt readability, dialogue display, and "no console errors while actually running" are confirmed only by static code/wiring review and the static validation pass above, not by an actual Play-mode run. Per `AGENTS.md`, this must not be assumed — **a manual Play-mode check in the Unity Editor GUI is requested**: open the project, open `Assets/UpIzUpMini/Scenes/GrandBayProof.unity`, press Play, walk with WASD/arrows (Shift to run) up the road, approach the NPC and press E, approach the farm plot and press E, and confirm the Console shows no errors.
  - Prompts use `OnGUI`, not a UGUI World Space canvas (see Implementation notes above) — visually functional but not the eventual production UI approach.
  - Buildings, road, and farm path are untextured primitive blocks (flat color materials) — placeholder geometry proving layout/camera/interaction only, not final art.
  - Headless Play-mode automated verification is blocked in this environment by the Search-module bug described above; retry once Unity/Editor is patched or the Search index has been built once via the interactive Editor.
- Build follow-up (same day): Added `Mini001Build.cs` and produced a Windows standalone player at `Builds/GrandBayProof/UpIzUpMini.exe` (mirrors the reference project's `Builds/PlayableAlpha/UpIzUp.exe` convention; `Builds/` is gitignored, not committed). Ran it headlessly (`-batchmode -nographics`, 6 seconds, log at `Logs/mini001-player-run.log`) — this sidesteps the Editor Search-module bug entirely since standalone players don't go through Editor Play mode. Result: **zero console errors**, confirming `Awake`/`Update`/`LateUpdate`/gravity/`CharacterController`/interaction range-scan all ran cleanly every frame. This is real evidence, not a substitute for looking at it: `-nographics` means nothing was rendered, and the player never walked into range of the NPC or farm plot, so the `OnGUI` prompt-box rendering path specifically was not exercised by this run.
- Next action: User (or next agent) either double-clicks `Builds/GrandBayProof/UpIzUpMini.exe` directly, or opens `GrandBayProof.unity` in the Editor and presses Play, to do one real visual check: walk with WASD/arrows (Shift to run), approach the NPC and press E, approach the farm plot and press E, confirm the `[ E ] Talk`/`[ E ] Plant` prompts and dialogue line look right, and check the Console/log for errors during that walk. Once confirmed, update this entry's "Known issues" to drop the play-test caveat and set MINI-001 to fully verified before starting `MINI-002`/`MINI-003`.

### MINI-011 — Grand Bay production vertical slice (corrective rebuild), Phase A

- Date: 2026-08-15
- Owner: Claude
- Request: A large corrective-rebuild brief from the user (full text preserved in session transcript, not duplicated here) rejecting MINI-001's primitive geometry and asking for: Shanty Town-style village art, hand-authored Grand Bay-shaped terrain, Smart/Strong as two named switchable protagonists, ambient NPCs/vendor/buyer/police, a complete tomato Mission 1 loop with HUD, and five named Unity Asset Store packages, organized into Phases A-D with hard visual gates. This single request's scope subsumes `MINI-004` through `MINI-009`.
- Acceptance criteria: see the full brief (session transcript) and `TASKS.md` MINI-011 summary; not restated here given length.
- Implementation (Phase A only):
  - Audited `E:\Assets` (read-only reference library) instead of the five named Asset Store URLs, none of which exist locally and none of which I can acquire myself (no Unity account/Asset Store login capability, and downloading/accepting terms on the user's behalf is outside what I can do without explicit per-action permission). User chose to use already-owned local packs instead.
  - Found `Arteria3d Shanty Town` + `Shanty Town 2` (corrugated-roof shanty architecture, props, and 9 bundled rigged characters + a soldier) as a much stronger single-style match than the fantasy/medieval packs also present locally (Adventurer/Fighter/Warriors & Commoner), plus `Arteria3d Tropical Island Foliage Pack` and `Tropical Nature Pack` for vegetation.
  - Confirmed the user-supplied heightmap (`heightmapper-*.png/.raw` in `E:\Assets\GrandBayReference\Maps\`) is independently documented, in that reference pack's own `Terrain_File_Assessment.md`, as not a valid Unity heightmap (wrong dimensions, unknown byte layout, no recorded geographic bounds/licence) — not usable as a direct import regardless of this task. User chose to hand-author an approximate terrain instead of blocking on sourcing real elevation data.
  - Wrote `Docs/MINI-011-VISUAL-PLAN.md` (art direction, asset picks, scene blockout, terrain approach, performance budget, risks) and `Docs/ASSET-REGISTER.md` (candidate packs with explicit licence-verification caveats — none of this is Asset Store-entitlement-verified by me).
  - Did not touch any scene, script, or ProjectSettings file this pass — Phase A is documentation/audit only, per the brief's own instruction to stop at the visual-plan gate before production work.
- Files changed: `Docs/MINI-011-VISUAL-PLAN.md` (new), `Docs/ASSET-REGISTER.md` (new), `TASKS.md`, `PROJECT-HANDOFF.md`.
- Scene/prefab changes: None.
- Verification commands: None applicable — no code/scene changes this pass.
- Verification results: N/A.
- Known issues:
  - Every asset pack named in `Docs/ASSET-REGISTER.md` has unverified Asset Store licence/entitlement status from my side — user should confirm before Phase B imports them.
  - Shanty Town 2's characters are an old multi-format export with frame-range (not per-clip) animations; Humanoid-avatar/retargeting compatibility for the Smart/Strong switching requirement is unconfirmed and is the first thing to test in Phase B.
  - I have no tool capable of capturing Unity Editor Game-view or desktop screenshots, so every screenshot-based "Gate" in the brief cannot be self-verified by me; builds + user visual confirmation will substitute, as already established on MINI-001.
- Next action: awaiting user go/no-go on the Phase A plan before Phase B (environment/terrain/camera rebuild, starting with the character-rig compatibility test) is claimed.

### MINI-011 — Unity Asset Store acquisition pass (Phase A continued)

- Date: 2026-08-15
- Owner: Claude
- Request: User granted explicit browser permission and asked to acquire the five specific free Asset Store packages from the corrective-rebuild brief.
- Implementation: Used Claude in Chrome (user's real logged-in browser, user selected which of two connected browsers and signed into their Unity ID themselves — I never entered credentials) to open each of the five package pages and click "Add to My Assets," accepting the Standard Unity Asset Store EULA per item with the user's explicit in-chat permission. Verified all five landed in the account via `assetstore.unity.com/account/assets` (purchase date Aug 15, 2026). While there, discovered three more relevant packages already added to the account on Aug 14, 2026 (before this session) — `Demo City By Versatile Studio (Mobile Friendly)`, `Human Basic Motions FREE`, `Human Melee Animations FREE` — plus several older owned packages (Unity's own `Starter Assets - ThirdPerson | URP`, `Robot Kyle | URP`, `Vehicle Physics Pro - Community Edition`, `Rocks FREE pack`, `Grass Flowers Pack Free`). Updated `Docs/ASSET-REGISTER.md` and `Docs/MINI-011-VISUAL-PLAN.md` to reflect this — the asset plan now favors Demo City + Low Poly Character Pack + Human Basic Motions FREE over the local Arteria3d Shanty Town fallback, which removes the legacy-rig/animation-format risk flagged in the prior entry.
- Files changed: `Docs/ASSET-REGISTER.md`, `Docs/MINI-011-VISUAL-PLAN.md`, `PROJECT-HANDOFF.md`.
- Scene/prefab changes: None. No files were added to `Assets/` in the Mini project.
- Verification commands/results: Confirmed via `assetstore.unity.com/account/assets` page text — all 5 requested packages plus the 3 bonus packages listed with Aug 2026 purchase dates.
- Known issues: Acquisition (adding to the Unity account) is complete, but pulling any of these into the actual Mini project's `Assets/` folder requires the Unity Editor's Package Manager "My Assets" download+import flow — a native GUI action. I tried the Asset Store website's "Open in Unity" deep-link button once (for Low Poly Character Pack) to see if it completes the download automatically; I have no way to confirm what it did, since it hands off to Unity Hub (already running on this machine before the click) and I have no tool that can see or drive native desktop application windows. This is the concrete next blocker before Phase B geometry work.
- Next action: user completes the Editor-side download+import (Package Manager → My Assets → Download/Import for each of the 8 packages), or confirms another way to get them into `Assets/`; then Phase B can be claimed starting with a Humanoid-avatar rig test on Low Poly Character Pack.

### MINI-011 — Package import + Humanoid rig test (Phase A wrap-up)

- Date: 2026-08-15
- Owner: Claude
- Request: User said they'd downloaded the assets. Opening the Editor GUI myself confirmed nothing had landed in `Assets/` yet — the Package Manager GUI is a native window I can't drive. Found the actual `.unitypackage` files already sitting in the OS-level Asset Store cache (`%APPDATA%\Unity\Asset Store-5.x\...`), which meant CLI import was possible after all.
- Implementation:
  - Confirmed no Unity process still had the project open/locked, then imported all 8 packages one at a time via `Unity.exe -batchmode -nographics -quit -importPackage "<path>.unitypackage"`: Low Poly Character Pack, Human Basic Motions FREE, Low Poly Environment - Nature Free, Low Poly Tropical Beach, POLYGON Starter Pack, Cartoon Farm Crops, Demo City (Mobile Friendly), Human Melee Animations FREE.
  - Human Basic Motions FREE's bundled demo script needed `UnityEngine.UI` (Button/Text), which wasn't available — added `com.unity.ugui` to `Packages/manifest.json` (a standard Unity registry package, not an Asset Store item, so no account/licence question). This is also the package the project needs anyway for the Canvas/TextMeshPro HUD called for in the brief, replacing MINI-001's `OnGUI` prompts.
  - Ran a full-project batch-mode compile after all imports: clean, 0 `error CS`.
  - Wrote a one-off diagnostic (`Mini011RigTest.cs`) to resolve the biggest flagged risk: set Low Poly Character Pack's `male01_1.fbx`/`male02_1.fbx` importers to Humanoid animation type and checked the resulting `Avatar.isValid`/`isHuman`. **Both pass.** This unblocks building Smart/Strong on this pack with standard Mecanim retargeting.
- Files changed: `Assets/Floreswa/**`, `Assets/Kevin Iglesias/**`, `Assets/Polytope Studio/**`, `Assets/Aquaset/**`, `Assets/Synty/**`, `Assets/Cartoon_Farm_Crops/**`, `Assets/Versatile Studio Assets/**`, `Assets/Standard Assets/**` (dependency), `Packages/manifest.json` (+com.unity.ugui), `Assets/UpIzUpMini/Editor/Mini011RigTest.cs`, `Docs/ASSET-REGISTER.md`.
- Scene/prefab changes: None yet — `GrandBayProof.unity` is untouched. This was import + verification only, not Phase B geometry work.
- Verification commands: see `Logs/import-01` through `import-08-*.log`, `Logs/ugui-resolve.log`, `Logs/final-compile-check.log`, `Logs/rigtest.log`.
- Verification results: All 8 imports exit 0. Full-project compile exit 0, 0 `error CS`. Rig test: `RIGTEST OVERALL: PASS`.
- Known issues: `Assets/` is now 757MB and unorganized (assets sit in their default publisher-named folders, not yet moved/curated under `Assets/UpIzUpMini/`). Nothing has been visually inspected yet — I still have no screenshot capability; a look at these assets in the Editor is worth doing before Phase B geometry work commits to any of them stylistically.
- Next action: awaiting user direction — either a visual gut-check of the imported packages, or go-ahead to claim Phase B (terrain + environment + camera rebuild) directly.

### MINI-011 — Phase B investigation: rendered asset snapshots, buildings blocker found

- Date: 2026-08-15
- Owner: Claude
- Request: User gave the go-ahead for Phase B, added new standing project context (mobile/all-platforms priority, per-phase exe test workflow, Dominica-first world framing with a Guadeloupe 3x sea-trade abstraction) — recorded in `DECISIONS.md` D-006/D-007/D-008 and this session's persistent memory.
- Implementation: Before committing to Demo City as the building source, built `Mini011AssetSnapshot.cs` — a batch-mode (no `-nographics`, so real rendering works) tool that instantiates prefabs and renders an actual `Camera.Render()` frame to PNG, sidestepping both the earlier Editor Play-mode bug and the desktop-screenshot window-focus problem. Rendered actual samples instead of guessing from prefab names:
  - Demo City's `small_house_*`/`mid_house_*` prefabs: flat gray/white concrete cubes with black roller-shutter doors — a modern/industrial city kit, not a village. **Rejected.**
  - POLYGON Starter Pack's "Building" prefabs: generic industrial/sci-fi kit (pipes, beams, ladders, background silhouettes). **Rejected** as a building source (still fine as a general low-poly style reference for props).
  - Low Poly Character Pack's male01/male02: clean, visually distinct, both valid Humanoid avatars. **Confirmed good** for Smart/Strong.
- Files changed: `Assets/UpIzUpMini/Editor/Mini011AssetSnapshot.cs`, `Docs/ASSET-REGISTER.md`, `DECISIONS.md`.
- Scene/prefab changes: None persisted — snapshots render into a throwaway in-memory scene (`NewScene`, never saved), `GrandBayProof.unity` untouched.
- Verification: `Logs/snapshot-democity.log`, `Logs/snapshot-characters.log`, images at `Logs/Snapshots/demo-city-houses.png` and `Logs/Snapshots/character-pack-sample.png`.
- Known issues: **No confirmed building/environment source for the Caribbean shanty-village look yet.** Real options: (a) the local `E:\Assets` Arteria3d Shanty Town packs (visual style is a strong match per the original audit, but Asset Store entitlement is still unverified by me — user has not explicitly confirmed ownership of these specific packs the way they did for the 5 Store-acquired ones), (b) hand-built modular houses (wall/roof/door/window kit), which the corrective brief explicitly sanctions ("may be agent-built modular geometry, but they must look like finished houses, not scaled cubes"), or (c) sourcing a new pack. Not decided yet — asked the user.
- Next action: user picks a building-source direction; then Phase B (terrain/road/building placement/camera) gets claimed and built for real.

### MINI-011 — Phase B build: real Grand Bay environment

- Date: 2026-08-15
- Owner: Claude
- Request: User confirmed the hybrid building strategy and said to move to the actual build, using Google Maps/reference images as a general style guide (not surveyed geometry).
- Implementation:
  - `Mini011PhaseBSetup.cs` (new) rebuilds `GrandBayProof.unity` end to end: a sculpted `Terrain` (coast → flat village shelf → hills, approximate, inspired by `Grandbay entire.jpg`'s general shape per `Docs/MAP-STRATEGY.md`/the terrain assessment doc — not surveyed data), a textured grass `TerrainLayer` (Polytope Studio's ground texture), a visible `Sea` plane on the coastal side (per user: don't forget the sea — visual placeholder only, Guadeloupe sea-trade per D-007 is not wired up yet), a multi-segment bending road textured with Shanty Town's `road.jpg`, ~30-40 densely-packed structures along both sides (mix of real Shanty Town shanty meshes and hand-built modular houses — proper pitched gable roofs via two angled boxes meeting at a ridge, inset door/window panels, varied Caribbean wall colours, occasional 2-storey variants), scattered Shanty Town props (barrels/clothesline/fence/tyres), vegetation (Aquaset palms coastal side, Polytope fruit trees inland), and the Montine dirty farm path/clearing/plot (now textured with Shanty Town's `tyretracks.jpg`).
  - Replaced both the player and NPC capsules with real Low Poly Character Pack Humanoid models (`male01_1`/`male02_1`), each carrying an `Animator`.
  - Built a small Idle/Walk/Run `BlendTree` `AnimatorController` (`Assets/UpIzUpMini/Art/PlayerLocomotion.controller`) from Human Basic Motions FREE's male clips (already Humanoid-rigged, confirmed via `.meta`, so Mecanim retargeting onto the Floreswa avatar works with no extra setup). `PlayerController.cs` now drives an `Animator.SetFloat("Speed", …)` blend based on `CurrentSpeed`/`IsRunning`.
  - **Found and fixed a real bug via the new rendering-based visual-check workflow, not guesswork:** Aquaset's `PalmTree` prefabs are wired to their `Materials/URP/` variant by default; this project has no URP package, so they rendered solid magenta. The pack ships a `Materials/Built-In/` sibling for every material — added `SwapUrpMaterialsForBuiltIn()` to reassign by filename convention on instantiation. Confirmed fixed by re-rendering (palms are green in the after-shot).
  - Iterated on house density twice using the same rendering workflow: first pass was far too sparse (large empty gaps, ~14 structures total) against the brief's "close to both sides of the road" requirement; resampled the road polyline to ~11m spacing for placement (independent of the coarser road-mesh polyline) to roughly triple density, confirmed via a second render.
  - Extended `Mini011AssetSnapshot.cs` with scene-view snapshot modes (player-eye view, wide overview, mid-distance overview) used throughout this pass to catch the above two problems before building an exe, rather than after.
- Files changed: `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (new), `Assets/UpIzUpMini/Editor/Mini011AssetSnapshot.cs` (extended), `Assets/UpIzUpMini/Scripts/Character/PlayerController.cs` (Animator hookup), `Assets/UpIzUpMini/Art/PlayerLocomotion.controller` (new), `Assets/UpIzUpMini/Art/GrassGround.terrainlayer` (new), `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (rebuilt).
- Scene/prefab changes: `GrandBayProof.unity` fully rebuilt per above. `Mini001SceneSetup.cs`'s output is superseded but the script itself is untouched (MINI-001 history preserved per the corrective brief's instruction).
- Verification commands: `Logs/phaseb-compile*.log` (batch-mode compiles, clean throughout), `Logs/phaseb-buildscene*.log` (scene builder runs, no exceptions), `Logs/snapshot-*.log` + `Logs/Snapshots/*.png` (visual checks — player-eye view, wide overview showing the palm-tree bug, mid overview showing both the bug-fix and the density fix), `Logs/phaseb-build-exe.log` (Windows Player build), `Logs/phaseb-player-run.log` (headless player run).
- Verification results: all compiles clean (0 `error CS`). Scene builder runs clean, no exceptions logged. Windows build succeeded (~146MB). Headless player run: zero console errors across ~8 seconds of the real compiled game. Visual checks performed and iterated on using actual rendered frames, not assumptions — this is the first MINI-011 pass with real look-and-feel evidence, not just asset-level snapshots.
- Known issues/limitations (stated plainly, not glossed over):
  - Terrain shape is algorithmic (coast/shelf/hills approximation), not matched against the actual Grand Bay aerial/map references in any measured way — "looks generally like a tropical coastal village," not a surveyed recreation. Road path is procedural (sine-wave bend), not traced from the reference images.
  - No mission, HUD, NPC talk/plant prompts upgraded yet — MINI-001's `OnGUI` interaction system is still wired in underneath (untouched), Canvas/TextMeshPro upgrade is Phase C per the corrective brief.
  - Smart/Strong character switching is not built yet — only one controllable character (visually upgraded, but still one boy, no `CharacterSwitchManager`). Also Phase C+.
  - Two-storey/tall building variant only rotates through one procedural shape; more visual variety would help before this is called "done."
  - Have not personally watched it move/animate in real time — the rendered snapshots are static frames from batch mode; the headless run confirms no errors but not animation quality. That's the ask for this build.
- Next action: user tests the launched build (movement, camera, animation quality, whether it reads as "Grand Bay" enough) and gives feedback before Phase C (people/prompts/mission/HUD) is claimed.

### MINI-011 — Phase B bugfix pass (from user's hands-on test)

- Date: 2026-08-15
- Owner: Claude
- Request: User tested the Phase B build and reported, with visual inspection requested for all: upside-down roofs, prompt text too small, house density still too low, Shanty Town models too small, one white/untextured shanty house, character can't run, and asked for Smart-darker/Strong-lighter skin tones. Also supplied the full game plot (saved to `Docs/STORY.md`) and a future-extensibility note about shops/accessories/vehicles/property (saved to memory, not implemented now).
- Implementation (each fix verified by rendering the actual scene again, not assumed):
  - **Roofs upside-down:** `BuildGableRoof`'s slope rotation had the wrong sign — the ridge (should be the high edge) was ending up low, producing a valley/trough silhouette instead of a peak. Fixed (`-side * pitchDeg` instead of `side * pitchDeg`) and confirmed via render: roofs now show a correct peaked gable.
  - **White/untextured shanty house:** traced to `ShantyVariants` including 16/18/20, which have no matching `Materials/shantyN.mat` in the source pack (only 1-14 do — confirmed by inspecting the pack's Materials folder). Restricted the variant list to 1-14.
  - **Shanty models too small:** applied a `2.1x` uniform scale on instantiation (source meshes import undersized relative to the ~2m-tall Humanoid characters).
  - **Not enough house density:** tightened road-polyline resampling for house placement from ~11m to ~7m spacing, widened the road-clearance-adjusted lateral band, reduced how often a gap/tall-building slot is skipped. Confirmed via a second wide-overview render: continuous house frontage along the road, not scattered dots.
  - **Prompt/text too small:** `InteractionDetector`'s `OnGUI` styles used a fixed pixel font size, which reads tiny on a high-resolution display and would be worse on a phone/tablet. Rewrote to scale against a 1080-tall reference resolution — this also directly serves the mobile-first requirement, not just this bug report.
  - **Character can't run:** found and fixed a real staleness bug: `BuildAnimatorController` cached the generated controller across rebuilds via `if (existing != null) return existing`, so later script edits to the blend tree wouldn't necessarily take effect. Changed to always delete and rebuild fresh. Also increased the animation blend response rate. Root cause of the *original* complaint is not fully certain (no missing-clip warnings were logged on the first-ever build either), so this needs a second confirmation from the user — noted as still open below.
  - **Character skin tone:** found the character pack's material slot is literally named `skin` (confirmed by probing the prefab's renderers, not guessed). Cloned it per-instance (editing the shared material would have recoloured every character using that prefab) — the controllable character (Smart) is darker, the NPC placeholder (standing in for Strong until Phase C's switching system exists) is lighter.
  - Saved `Docs/STORY.md` (full plot, verbatim) and a memory note on future shop/accessory/vehicle/property extensibility — informs how the economy data model gets designed in a later phase, not implemented now.
- Files changed: `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs`, `Assets/UpIzUpMini/Scripts/Character/PlayerController.cs`, `Assets/UpIzUpMini/Scripts/Interaction/InteractionDetector.cs`, `Assets/UpIzUpMini/Editor/Mini011MaterialProbe.cs` (new, kept as a small reusable diagnostic), `Docs/STORY.md` (new), `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (rebuilt).
- Verification commands/results: `Logs/bugfix-compile.log` (clean), `Logs/bugfix-buildscene.log` (clean), `Logs/snapshot-mid3.log` + `Logs/Snapshots/grandbay-overview-mid.png` and `grandbay-overview-wide.png` (visually confirm roofs/density/scale/no-white-house fixes), `Logs/bugfix-build-exe.log` (Windows build succeeded), `Logs/bugfix-player-run.log` (zero errors across ~8s headless run).
- Known issues / still open:
  - **Running fix is not independently confirmed** — the caching bug was real and is fixed, but I don't have certainty that was the *entire* original cause. Needs the user's re-test.
  - Tab/character-switching was not tested by the user yet and doesn't exist as a real system yet (only one controllable character) — Phase C.
  - Police, shopkeeper, and crop-buyer NPCs; health/heat/stamina meters; numbered (1-4) crop-selection; before/after soil colour states across multiple plots; crop growth-stage scaling and green-to-red fruit colour — none of this exists yet. This is the clearly-scoped Phase C list now (see `TASKS.md`).
  - Car/driving explicitly deferred by the user to a later phase.
- Next action: user re-tests this build; then Phase C (NPCs, HUD meters, crop selection/growth, Tab switching + Strong as a real second character) gets claimed.

### MINI-011 — Pause menu + mouse-look

- Date: 2026-08-15
- Owner: Claude
- Request: User confirmed the bugfix pass works, asked for Esc-to-menu with Q-to-quit/navigable options, and mouse-look camera control. Noted running still feels weird (deferred by user) and asked me to keep visually inspecting my own work.
- Implementation:
  - `PauseMenuController.cs` (new): Esc toggles a pause panel (`Time.timeScale` 0/1, cursor unlocked+visible while paused, locked+hidden while playing). While paused, Q quits (`EditorApplication.isPlaying = false` in-Editor, `Application.Quit()` in a build). Resume/Quit are real UGUI `Button`s — mouse-clickable, and keyboard-navigable for free via Unity's default `Selectable` navigation once one is selected (`EventSystem.SetSelectedGameObject(resumeButton)` on pause).
  - `ThirdPersonFollowCamera.cs`: added mouse-look — horizontal orbit driven by `Input.GetAxis("Mouse X")`, but only while `Cursor.lockState == CursorLockMode.Locked`, so opening the pause menu doesn't spin the camera from residual mouse delta. Downward look angle is unaffected (still ~44°); `PlayerController` already moves camera-relative, so WASD direction follows wherever the mouse has oriented the camera, which is the intended combined scheme.
  - `Mini011PhaseBSetup.cs`: builds the `EventSystem` (+`StandaloneInputModule`, matching the project's legacy Input Manager setup) and a Screen Space - Overlay `Canvas` with the pause panel/buttons using legacy `UnityEngine.UI.Text` and Unity 6's built-in `LegacyRuntime.ttf` (not `Arial.ttf` — that constant was removed in Unity 6 and threw an `ArgumentException` on the first attempt; fixed and reconfirmed via a rebuild).
  - Extended `Mini011AssetSnapshot.cs` with a pause-menu snapshot mode to visually confirm the UI layout before shipping the build — caught a real bug in the diagnostic itself along the way (`GameObject.Find` doesn't search inactive objects, so the initially-hidden panel wasn't found; fixed by looking it up via the canvas's transform instead).
- Files changed: `Assets/UpIzUpMini/Scripts/UI/PauseMenuController.cs` (new), `Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs`, `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs`, `Assets/UpIzUpMini/Editor/Mini011AssetSnapshot.cs`, `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (rebuilt).
- Verification: compile clean; scene builder hit one real runtime exception (`Arial.ttf` removed in Unity 6) caught and fixed, then rebuilt clean; rendered the pause menu (title + both buttons legible, correctly positioned — full-screen dim overlay didn't show in this particular render because verifying an Overlay-mode canvas requires temporarily switching it to Camera mode for the offline snapshot technique to capture it at all, which appears to affect its compositing in this static single-frame test; the shipped scene keeps the canvas in Overlay mode, unaffected by the snapshot script since scene changes there aren't saved). Windows build succeeded, zero errors in an 8s headless run, launched for the user.
- Known issues:
  - **Mouse-look and pause-menu interaction (clicking buttons, Q-to-quit, keyboard nav) have not been confirmed by an actual human interacting with the compiled game** — Update()-driven input logic can't be meaningfully exercised from batch mode. This is exactly what the launched build is for.
  - The pause panel's full-screen dark overlay wasn't visually confirmed in the offline snapshot for the technical reason above; worth a quick look in the real build.
  - Running "feels weird" is explicitly deferred by the user — not addressed in this pass.
- Next action: user tests Esc/mouse-look/Q-quit in the running build.

### MINI-011 — Phase C: NPCs, HUD, farming loop, crop selection, character switching + deferred fixes

- Date: 2026-08-15
- Owner: Claude
- Request: User confirmed the pause menu/mouse-look pass worked, then asked for the full Phase C list from `TASKS.md` (police/shopkeeper/buyer NPCs, health/heat/stamina meters, 1-4 crop selection, multi-plot soil states, crop growth visuals) plus, separately mid-turn, a standing note about designing the economy for future shop/accessory purchases (saved to memory, not implemented). After Phase C, asked for two more fixes: vertical (pitch) mouse-look, and Shanty Town buildings having no collision (player walked through them).
- Implementation:
  - **Economy** (`Scripts/Economy/`): `CropDefinition` (ScriptableObject — id/name/price/illegal flag/growth-visual colours, deliberately generic per the future-extensibility note rather than a tomato-only type), `EconomyManager` (shared money + inventory + heat, matching Docs/STORY.md's "share money, inventory, heat"), `CropSelectionController` (keys 1-4 pick the active crop). 4 crop assets created: Tomato, Banana, Carrot, Bushers (the last flagged `isIllegal`, so selling it adds heat — the brief's own heat-source requirement).
  - **Farming** (`Scripts/Farming/FarmPlot.cs`): replaces `FarmPlotInteractable` with a real state machine (Empty → PlantedDry → Growing → Ripe → back to Empty). Soil is light brown while empty, dark brown once planted — matches the user's explicit request. Crop visual scales up and tints from unripe to ripe colour over the final third of growth (3 distinguishable visual stages: tiny/green, mid-size/green, full-size/ripe-colour). 6 plots built in a 3x2 grid in the farm clearing (the brief's minimum), not the single plot MINI-001 had.
  - **NPCs** (`Scripts/Interaction/TownNPCInteractable.cs`): one role-based interactable (`Villager`/`Police`/`Shopkeeper`/`Buyer`) rather than four near-duplicate classes. Police's line reacts to current heat level. Buyer sells the entire shared inventory via `EconomyManager.TrySellAll`. 4 NPCs placed along the road using 3 different Floreswa body variants so they're visually distinct from each other and from Smart/Strong.
  - **Characters**: `CharacterVitals` (per-character health/stamina, matching Docs/STORY.md's "retain separate health, stamina"), `FollowController` (simple direct-steering companion AI for whichever boy isn't controlled — not a NavMesh agent, so it can cut corners around obstacles; acceptable for this pass), `CharacterSwitchManager` (Tab swaps control between Smart and Strong: toggles `PlayerController.IsControlled`/`InteractionDetector.enabled`/`FollowController.FollowingEnabled`, retargets the camera). Strong now exists as a real second controllable character (lighter skin per the user's tone request) rather than the NPC placeholder from the earlier bugfix pass — the ambient villager NPC moved to a third Floreswa body variant so it doesn't overlap Strong's identity.
  - **HUD** (`Scripts/UI/HUDController.cs`): health/stamina/heat bars (`Image.Type.Filled`), money, active character name, current crop selection — built into a second Screen Space - Overlay canvas alongside the pause menu's.
  - **Deferred fixes, done after Phase C per the user's explicit ordering:**
    - `ThirdPersonFollowCamera` rewritten from fixed-height/distance to a proper spherical orbit (yaw + pitch, both mouse-driven while the cursor is locked, pitch clamped 20-75°) so the player can look up and down, not just side to side. Kept a field-initializer default (`_pitch = 44f`) rather than only setting it in `Awake()`, specifically so `Mini001SceneValidation`'s look-angle check (which runs without entering Play mode) keeps working.
    - Shanty Town instances never had a `Collider` — FBX-imported meshes don't get one automatically the way primitives do, so the player walked straight through every shanty structure. Added `AddBoundsCollider()`: computes each instance's actual rendered bounds and adds a matching `BoxCollider` in local space (scale-safe). Hand-built houses already had colliders (from their primitive walls) and were unaffected. Small scattered props (barrels/tyres/etc.) deliberately left uncollided — the user's complaint was specifically about buildings.
  - Updated the old `Mini001SceneValidation.cs` to recognize both the pre- and post-Phase-C interactable types, so it doesn't silently report a false failure now that `NPCInteractable`/`FarmPlotInteractable` have been superseded by `TownNPCInteractable`/`FarmPlot` in the built scene (MINI-001's own history/scripts are untouched otherwise).
- Files changed: `Assets/UpIzUpMini/Scripts/Economy/**` (new), `Assets/UpIzUpMini/Scripts/Farming/FarmPlot.cs` (new), `Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs` (new), `Assets/UpIzUpMini/Scripts/Character/CharacterVitals.cs`, `CharacterSwitchManager.cs`, `FollowController.cs` (new), `PlayerController.cs` (IsControlled gate + stamina hookup), `Assets/UpIzUpMini/Scripts/UI/HUDController.cs` (new), `Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs` (spherical orbit), `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (major expansion), `Assets/UpIzUpMini/Editor/Mini001SceneValidation.cs`, `Assets/UpIzUpMini/Data/Crops/*.asset` (new), `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (rebuilt).
- Verification: compile clean on first attempt for the full Phase C addition (a genuinely large change). Scene builder ran clean (no exceptions) on the first try. Rendered three real views before shipping: a wide/mid overview (village layout still intact), a gameplay-HUD render (confirmed all three meters, money, and — importantly — **both Smart and Strong standing together, visually distinct**, not assumed), consistent with the corrective brief's "clearly different clothing colours, body silhouettes" requirement. Ran `Mini001SceneValidation` after updating it for the new component types: **PASS**, including the camera look-angle check (44°, within the 40-50° range, confirming the deferred vertical-look rewrite didn't regress it). Windows build succeeded; headless run of the actual compiled game: **zero console errors** across 10 seconds with every new system (economy, 6 farm plots, 4 NPCs, both characters, switch manager, HUD) actually initializing via real `Awake()`/`Start()`/`Update()` calls, not just constructed statically.
- Known issues (stated plainly):
  - **None of the interactive behavior has been confirmed by an actual human** — crop selection, planting/watering/harvesting, talking to each NPC role, selling, Tab-switching, the companion actually following, vertical mouse-look, and walking into a (now solid) shanty building all need a real playtest. Static/headless verification confirms things construct and run without exceptions, not that they feel or play correctly.
  - `FollowController` uses direct steering, not pathfinding — the companion can get stuck on/cut through obstacles in tight spots. Acceptable for this pass, worth a NavMesh upgrade later if it's a problem in practice.
  - Only tomato has its growth colours deliberately tuned (green→red per the user's explicit ask); banana/carrot/Bushers use reasonable but less-considered placeholder colours.
  - Shopkeeper doesn't have a real "buy seeds" transaction yet — planting just uses whichever crop is selected via 1-4, no seed-purchase gate. Matches the brief's Mission 1 loosely but simplifies it; worth a look before calling this "done."
  - HUD is a first pass (basic bars/labels, no icons/polish); layout was eyeballed from one render, not fine-tuned.
- Next action: user playtests everything above by hand and reports back.

### MINI-012 — Realistic crops, hillside farm, coast, NPC stance, save/load

- Date: 2026-08-15
- Owner: Claude
- Request: After playtesting Phase C the user reported: NPCs standing with arms stretched out (T-pose), Strong too pale and bearded (both boys are 18), plantation should be in the hills (Montine), crops unrealistic (suggested reusing the larger game's), skybox making the money label unreadable, farm plots wildly placed, wanted a jetty/boat/sand and swimming, shop area visible with its NPC, black Lalay road, watered soil going darker, seeing actual plants rather than a spreading green circle, police movement tied to heat, awkward running fixed, save/load, and a safehouse near the farm.
- Implementation:
  - **Realistic crops (the significant piece).** Read `E:\Unity\Up iz up\AI-COLLABORATION-HANDOFF.md` and mined the larger project. Its `Assets/Imported Plants/` tomato and weed models are photogrammetry scans: **2,000,000 triangles each, 183MB, one submesh, no growth stages** — measured with a probe, not assumed. That's ~13x a whole mobile scene budget for one plant, and it's why the larger game imported but never actually referenced them. Rather than drop the idea or ship 12M triangles of farm, wrote `MeshDecimator.cs` (vertex-clustering decimation: grid the bounds, collapse each cell's vertices to their average, rebuild triangles, drop degenerates) and `Mini012CropAssetBuilder.cs`. Result: **3,259 and 5,561 triangles (0.16% / 0.28% kept)**, verified by render to still read as a real tomato plant with fruit and a real cannabis plant. Source FBXs deleted after conversion — 366MB in, 282KB out.
  - `CropStageVisual.cs` + rewritten `FarmPlot.cs`: 4 discrete growth stages using the real plant mesh (scaling through seedling → grown), with separate fruit spheres that ripen green → red. Fruit is separate because the scan is a single submesh, so tinting it would have recoloured the foliage too. Soil colours now copied exactly from the larger game's `CropPatch.cs` (`DrySoilColor` / `WateredSoilColor`) so watering visibly darkens the plot and the two games match.
  - **Farm moved to the hills** (+X is the rising side per `BuildTerrain`; it was previously on the coastal side) and reorganised into a tidy 4x2 grid of 8 plots aligned to the farm's own axes. Added `FlattenTerrainArea()` to carve a level terrace, without which flat plot geometry would float on the hillside. The access track now stops at the plantation edge instead of running through it, the tilled ground is sized/rotated to the grid, and vegetation is kept out of the farm radius (a tree was growing up through the plots).
  - **Coast**: sand beach placed on the actual measured waterline, a timber jetty on posts running out over the water, and a moored boat. `SwimmingController.cs` floats characters at the surface instead of sinking, so the sea is enterable.
  - **NPC stance fixed**: NPCs were instantiated with no `RuntimeAnimatorController`, so they rendered the model's authored T-pose. They now receive the same locomotion controller as the players. Verified by sampling the idle clip in-editor and rendering (animators don't evaluate outside Play mode, so a plain render would have proven nothing).
  - `PatrolNPC.cs`: villager and police walk between waypoints with correct walk animation; police speed up and switch to the run blend once heat passes its threshold.
  - **Characters read as 18**: `HideFacialHair()` makes the beard/moustache/goatee material slots transparent per-instance (they're separate slots on one shared skinned renderer, so they can't be deleted, and editing the shared material would strip beards from every character). Strong's skin corrected from a near-white tone to a mid-brown.
  - **Awkward running diagnosed**: walk/run speeds (3.2 / 6.5 m/s) far exceeded the authored stride of the Human Basic Motions clips, so the feet skated. Retuned to 1.9 / 4.4 (and the companion to 2.1). This is a plausible, reasoned fix but **not confirmed** — it needs the user's eye.
  - **Market area**: stall canopy, table and crates beside the road with the shopkeeper and buyer at it; house placement now leaves that frontage clear (the stall was previously buried in a house). **Black Lalay road.** HUD money/crop moved onto a dark backing panel so they're readable against bright sky. Farm safehouse beside the plantation.
  - `SaveLoadSystem.cs`: F5 saves, F9 loads — money, heat, inventory, every plot's crop/state/growth timer, both characters' positions, and the active character. Uses PlayerPrefs+JSON rather than a file so it works identically on Windows, Android and WebGL (WebGL has no ordinary filesystem) — relevant to D-008.
- Files changed: `Assets/UpIzUpMini/Editor/MeshDecimator.cs`, `Mini012CropAssetBuilder.cs`, `Mini012MeshProbe.cs` (new); `Scripts/Farming/CropStageVisual.cs`, `Scripts/Character/SwimmingController.cs`, `Scripts/Interaction/PatrolNPC.cs`, `Scripts/SaveLoadSystem.cs` (new); `Scripts/Farming/FarmPlot.cs`, `Scripts/Character/PlayerController.cs`, `FollowController.cs`, `CharacterSwitchManager.cs`, `Scripts/Economy/EconomyManager.cs`, `Editor/Mini011PhaseBSetup.cs`, `Editor/Mini011AssetSnapshot.cs`; `Art/CropMeshes/*.asset` (new); `Scenes/GrandBayProof.unity`; `Docs/ASSET-REGISTER.md`.
- Verification: compiles clean throughout. Scene builder clean. Rendered and inspected five views before shipping — decimated crops (real plant shape confirmed), the farm (tidy grid, red ripe vs green young fruit, darker watered soil, safehouse, no tree through the plots), the coast (sand/jetty/boat), and NPC stance twice (natural arms-down pose; second pass confirming the market stall no longer clips a house). Windows build succeeded; headless run of the compiled game: zero console errors over 12 seconds.
- Known issues (honest):
  - **Nothing here is hands-on playtested.** Swimming, save/load round-trip, police heat reaction, the running fix, and planting the real crops all need the user at the controls. Static render + clean headless run is not the same as it playing correctly.
  - The running-speed retune is a *reasoned* fix for foot-sliding, not a confirmed one; if it still looks wrong the next step is inspecting the clips' actual root velocity rather than guessing again.
  - Decimated crops have no UVs/textures — they're flat-shaded solid colour. Silhouette is right, surface detail isn't. Source scans' own provenance/licence isn't recorded in the larger project either; worth confirming before any release.
  - Not done from the request: NPC jumping, and porting the larger game's dialogue content (only its soil colours and crop-stage structure were reused).
  - Swimming is float-at-surface only — no swim animation, so the character plays walk/idle while in water.
- Next action: user playtests; the running feel and swimming are the two most likely to need another pass.

### MINI-013 — T-pose root cause, running fix, jump, seeds/shop, Franki & Sacat

- Date: 2026-08-15
- Owner: Claude
- Request: User reported NPCs still walking in T-pose, tomato fruit floating in mid-air, and running still bad. Asked for: seed/cloning like the larger game, spacebar jump, missions (planting/selling/police/bosses/land), a shop with fake-brand goods, characters renamed to Franki and Sacat, H for controls, street signs, fading area names, and Dominican dialogue based on a supplied real conversation. Also asked to record a YouTube workflow link.
- Implementation:
  - **T-pose root cause found and fixed (this was a real diagnosis, not a retry).** Checked `animationType` in every character model's `.meta`: the two player models are `3` (Humanoid) but **all four NPC models were `2` (Generic)**. Humanoid clips cannot retarget onto a Generic rig, so NPCs fell back to their bind pose regardless of the animator controller I attached last pass. `Mini013RigAndAnimAudit.ForceHumanoidRigs` converts all 9 models to Humanoid and re-verifies each avatar (`isValid`/`isHuman`) — all 9 now report `valid humanoid`.
  - **Running fixed with measurement, not guesswork.** `MeasureLocomotionSpeeds` reads the clips' own root motion: Walk01 is authored at exactly **1.90 m/s**, Run01 at **3.80**, Sprint at 5.69. My previous run speed of 4.4 was ~16% faster than the animation's stride, which is precisely what makes feet skate. Set to 1.90/3.80 exactly.
  - **Floating fruit fixed.** The decimated scan is centred on its own origin, so half the plant sat below the soil and the fruit (positioned against full plant height) ended up above its visible top. Mesh is now offset so its base rests at y=0, and fruit sits inside the plant's real height band. Verified by render — fruit now hangs on the plant.
  - **Jump**: Space jumps (`v = sqrt(2gh)`), with a Jump state + Grounded transition added to the generated animator controller.
  - **Seeds/cloning**: planting now consumes a seed; harvesting returns 3 crops **and 2 seeds**, so one purchase can be cloned into a bigger farm (the larger game's behaviour). `StarterInventory` grants a few legal seeds at start so the player isn't blocked; illegal strains are deliberately excluded (story-gated).
  - **Shop**: `ShopItemDefinition` is one generic purchasable type covering seeds/clothing/footwear/accessories/vehicles/boats/property/land, so future stock is data rather than code. Stocked with fictional near-miss brands per the user — Mike Cap, Mike Air Kicks, Lacostes Polo, Gold Chain — plus seeds, a Montine land plot and a fishing pirogue. Opened by talking to the shopkeeper. (Caught and fixed a double-purchase bug in the panel while writing it: `TryPurchase` was being called twice per keypress.)
  - **Franki and Sacat** replace the Smart/Strong placeholder names throughout.
  - **H** controls overlay, **street signs** (LALAY, MONTINE, MONTINE FARM), and a **fading area-name banner** that only re-triggers when the area actually changes.
  - **Dialogue** rewritten from the user's real supplied conversation and the patterns documented in the new `Docs/DIALOGUE-REFERENCE.md` (`yea wii`, `mn`, `nuh`, `facts`, `irie`, `allu`, `doe`, `di`). NPCs cycle lines so repeat talks vary. Used sparingly per AGENTS.md's warning against stereotype.
  - YouTube workflow link recorded in `Docs/DIALOGUE-REFERENCE.md` with the user's note.
- Files changed: `Editor/Mini013RigAndAnimAudit.cs` (new); `Scripts/Economy/ShopItemDefinition.cs`, `StarterInventory.cs`, `Scripts/UI/ShopPanelController.cs`, `ControlsPanelController.cs`, `AreaNameDisplay.cs` (new); `Scripts/Economy/EconomyManager.cs`, `Scripts/Farming/FarmPlot.cs`, `Scripts/Character/PlayerController.cs`, `Scripts/Interaction/TownNPCInteractable.cs`, `Scripts/UI/HUDController.cs`, `Editor/Mini011PhaseBSetup.cs`; `Data/Shop/*.asset` (new); all 9 `Floreswa/Models/*.fbx.meta` (rig type); `Scenes/GrandBayProof.unity`; `Docs/DIALOGUE-REFERENCE.md` (new).
- Verification: compiles clean; all 9 avatars verified valid humanoid; animation speeds measured and logged; scene builder clean; farm re-rendered confirming fruit attached to plants and the Montine sign present; Windows build succeeded; **zero console errors in a 12s headless run**.
- Known issues (honest):
  - **The T-pose fix is verified at the asset level (all 9 avatars valid humanoid), not yet seen animating in a real playtest.** That's the single most important thing for the user to check.
  - **Missions are NOT built.** The user asked for full missions (planting/selling/police/bosses/buying land). What exists is the economy, shop, seeds, land *item*, police heat reaction — the systems missions would sit on — but no mission/objective state machine, no boss NPCs, no land ownership actually changing gameplay. Buying "Montine Land Plot" currently only marks it owned. This is the largest outstanding gap and needs its own task.
  - Shop items other than seeds have no visual effect yet — clothing/chains/shoes don't appear on the characters, vehicles/boats/houses aren't drivable or enterable. They're economy entries only.
  - Jump animation transition timing is untested by hand and may need tuning.
  - The YouTube workflow video is recorded but not reviewed.
- Next action: user playtests — priority checks are NPC animation (T-pose gone?), running feel, jump, and the shop/seed loop. Then missions should be scoped as their own task.

### MINI-014 — StarterAssets/Mixamo animations, GTA-style missions, split shops

- Date: 2026-08-15
- Owner: Claude
- Request: Prioritise farm land and switching animations to the Mixamo ones in the larger project, then build the missions previously skipped, GTA-style with arrows and bit-by-bit instructions, keeping the farm shop and apparel shop separate.
- Implementation:
  - **Animations replaced with the larger project's set.** Copied the 8 locomotion/jump clips from the larger project's `StarterAssets/ThirdPersonController/Character/Animations` (Idle, Walk_N, Run_N, Run_S, Jump, InAir, the two Land clips — ~5MB total). `Mini014AnimationImport` forces them Humanoid, sets loop flags per clip, and re-measures them. **Critically, these are in-place clips with no root motion** (measured planar speed ~0.00 m/s), so unlike the previous Kevin Iglesias set their correct speed cannot be read from the clip itself. Read the tuned values straight from the larger project's `ThirdPersonController.cs` instead: `MoveSpeed 2.0`, `SprintSpeed 5.335`, `JumpHeight 1.2`, `Gravity -15`. Applied all four. The animator's `Speed` parameter is now driven in **real m/s** with blend thresholds at 0 / 2.0 / 5.335 — the same convention StarterAssets uses for these exact clips. That is the actual fix for running looking wrong, rather than another speed guess.
  - `PlayerController`, `FollowController` and `PatrolNPC` all updated to the same m/s convention; the companion now breaks into a run when it falls well behind instead of only ever walking.
  - **Missions** (`Scripts/Missions/`): `MissionSystem` runs one objective at a time with a short explicit instruction, an optional world marker, and progress counts. Gameplay reports events *in* (`Notify`/`NotifyCount` from planting, watering, harvesting, selling, buying seeds, talking, switching, reaching an area) rather than the mission system polling — so missions stay decoupled from farming/economy internals. Two missions built: **M1 "A Start in Montine"** (8 objectives: Tab-switch tutorial, find the Farm Shop, buy tomato seeds, follow the track to Montine, plant, water, harvest 3, sell to the buyer — $60) and **M2 "Look Sharp"** (find the clothes shop, grow/harvest 6 more, sell — $40). Mirrors `Docs/STORY.md` Mission 1.
  - `ObjectiveMarker`: bobbing, spinning gold arrow plus a ground ring that follows the current objective and hides once the player is close enough that it would obscure the target. `MissionHUD`: persistent objective card with live **distance in metres**, plus a large fading banner for briefings and completions.
  - **Shops split in two, as asked.** `ShopPanelController` is no longer a singleton — each shopfront owns its own panel, title and stock, and each shopkeeper NPC holds a reference to its own shop. `NpcRole.Shopkeeper` became `FarmShop` and `ApparelShop`. Farm Shop sells seeds and the Montine land plot; the separate Clothes Shop sells Mike Cap, Mike Air Kicks, Lacostes Polo, Adibas Shorts, Pumba Runners, Ray-Bam Shades, Rollex Watch and a Gold Chain (all deliberately fictional near-miss brands). Each stall now carries a readable shopfront sign.
- Files changed: `Editor/Mini014AnimationImport.cs`, `Scripts/Missions/MissionSystem.cs`, `Scripts/Missions/ObjectiveMarker.cs`, `Scripts/UI/MissionHUD.cs` (new); `Art/Animations/*.fbx` (new, from the larger project); `Scripts/Character/PlayerController.cs`, `FollowController.cs`, `CharacterSwitchManager.cs`, `Scripts/Interaction/PatrolNPC.cs`, `TownNPCInteractable.cs`, `Scripts/Farming/FarmPlot.cs`, `Scripts/Economy/EconomyManager.cs`, `Scripts/UI/ShopPanelController.cs`, `Editor/Mini011PhaseBSetup.cs`, `Editor/Mini011AssetSnapshot.cs`; `Scenes/GrandBayProof.unity`.
- Verification: compiles clean throughout. Clips verified Humanoid and re-measured. Scene builder clean. Rendered and inspected the market three times — caught and fixed a mirrored shopfront sign (TextMesh reads from its -Z side) and an objective arrow resting at origin. Final render confirms a readable "FARM SHOP" sign, gold arrow + ground ring on the objective, and all 7 characters posed naturally with the new idle. Windows build succeeded; **zero console errors in a 12s headless run**.
- Known issues (honest):
  - **Not playtested by hand.** The animation swap is the whole point of this pass and cannot be confirmed from static renders — that is the single thing to check first.
  - Missions cover the farming loop and the two shops. **Police/boss/land-ownership missions are still not built** — police react to heat and land is purchasable, but no mission uses them and buying land still has no gameplay effect.
  - Apparel purchases still do not appear on the characters — economy entries only.
  - `MissionSystem` state is not saved by `SaveLoadSystem` yet, so F9 restores money/crops/positions but restarts mission progress.
  - Objectives are generated in the scene builder rather than authored as assets; fine for two missions, worth moving to ScriptableObjects as the count grows.
- Next action: user playtests — animation feel first, then run Mission 1 end to end and check the arrow/instructions read clearly.

### MINI-015 — Closing the outstanding gaps: boss/police/land missions, wearable apparel, mission save/load

- Date: 2026-08-15
- Owner: Claude
- Request: "do what is still not done" — finish the three items flagged as outstanding at the end of MINI-014.
- Implementation:
  - **Apparel now actually appears on the characters.** `CharacterEquipment` attaches purchased items to Humanoid bones via `Animator.GetBoneTransform` — which only works because every model was converted to a Humanoid rig back in MINI-013, so no per-model bone names are needed. Cap and shades mount to `Head`, the gold chain to `Chest`, the watch to `LeftLowerArm`. It subscribes to `EconomyManager.OnChanged`, so an item appears the moment it's bought. Geometry is generated primitives (there are no clothing assets in the project); it is deliberately easy to swap for real meshes later.
  - **Buying land now does something.** `LockedFarmPlot` gates three extra plots behind the `land_montine` purchase: until it's bought they are fenced off and their `FarmPlot` component is disabled, which removes them from the interaction registry so no `[E] Plant` prompt appears on land you do not own. Buying the land drops the fences and enables them. Verified by render — the three fenced plots sit clearly behind the main 8.
  - **Three new missions**, continuing the GTA-style one-instruction-at-a-time flow:
    - **M3 "More Land"** — buy the Montine Land Plot from the Farm Shop, then plant on the new land. Uses a new `BuyItem` objective kind.
    - **M4 "The Offer"** — Boss K (new `NpcRole.Boss`, standing near the Montine turnoff away from the market, per `Docs/STORY.md` Mission 5) gives Bushers seed on first talk; then plant, grow, harvest and sell it — with the sale raising police heat.
    - **M5 "Cool Down"** — a new `EscapeHeat` objective that requires heat to actually be raised above 35 and then cooled below 8 before completing, so it cannot be skipped by simply never committing a crime; then talk to the officer while clean.
  - **Mission progress is now saved.** `SaveLoadSystem` persists mission index, objective index and per-objective progress, plus (previously missing) seed counts and owned shop items. `EconomyManager.CaptureExtras`/extended `LoadState` handle the economy half.
- Files changed: `Scripts/Character/CharacterEquipment.cs`, `Scripts/Farming/LockedFarmPlot.cs` (new); `Scripts/Missions/MissionSystem.cs` (BuyItem/EscapeHeat kinds, save/load state), `Scripts/SaveLoadSystem.cs`, `Scripts/Economy/EconomyManager.cs`, `Scripts/Interaction/TownNPCInteractable.cs` (Boss role), `Scripts/UI/ShopPanelController.cs`, `Editor/Mini011PhaseBSetup.cs`; `Scenes/GrandBayProof.unity`.
- Verification: compiles clean (one brace-structure slip introduced while editing `MissionSystem.Update` was caught and fixed before it reached a build). Scene builder clean. Farm re-rendered: 8 usable plots plus 3 clearly fenced locked plots, ripe red fruit on the plants, MONTINE FARM sign readable. Windows build succeeded; **zero console errors in a 14s headless run**.
- Known issues (honest):
  - **Still not playtested by hand.** Everything here is verified by compile, scene render and a clean headless run — not by playing it. The apparel bone offsets in particular were positioned by reasoning about Humanoid proportions, not by looking at a character wearing them, so the cap/chain placement may well need nudging.
  - Clothing and footwear (`shirt_lacos`, `shorts_adibas`, `shoes_mike`, `shoes_pumba`) still have no visual — they would need to replace body materials or swap meshes rather than attach to a bone, which is a larger change than the accessory attachment used here.
  - Vehicles and boats remain economy entries only; nothing is drivable.
  - `EscapeHeat` relies on heat decaying over time; there is no active police pursuit, so "escaping" currently means waiting rather than being chased.
  - Missions are still generated in the scene builder rather than authored as assets. With five missions this is close to the point where moving them to ScriptableObjects would pay off.
- Next action: user playtests. Best single run to exercise the new work: buy a cap and chain from the Clothes Shop (check they appear), then follow M3 → M4 → M5 to hit the land, boss and police content, and press F5/F9 partway to confirm mission progress survives.

### MINI-016 — Root-caused the animation problem; real character models, R cloning, windowed mode

- Date: 2026-08-15
- Owner: Claude
- Request: Fix walking/running ("spaghetti legs") as the essential item and explain why it works in the larger project but not here; add R-key plant cloning for +2 seeds; windowed/minimisable mode; stamina should drop the player to a walk rather than stopping them; police reinforcements at high heat; and write a proper handoff so Codex can continue. User explicitly approved copying the original Franki/Sacat character assets from the larger project.
- Implementation:
  - **Root cause found by measurement, and it answers the user's question directly.** `Mini016AvatarDiagnostic` reports bone *counts*, not just avatar validity — which matters because the previous "avatar valid = true" check was passing while the animation was still broken. Result: the Floreswa models map **23 human bones over a 39-bone skeleton**; the larger project's `Mainchar`/`Strong` map **52 over 67–72**. The Floreswa rigs were authored **Generic** and force-converted to Humanoid in MINI-013, so Unity auto-estimated a rest pose from a model never authored in a T-pose. Retargeting the shared clips against that sparse, mis-estimated avatar is what distorted the limbs. That is precisely why the same clips look right in the larger project, which uses models authored *for* Humanoid.
  - Copied `Mainchar.fbx` (Franki) and `Strong.fbx` (Sacat) plus their materials/textures into `Assets/UpIzUpMini/Art/Characters/` with the user's approval, imported them Humanoid, and verified the 52-bone avatars. Scene builder now uses them; skin-tint and facial-hair hiding are no longer applied to the protagonists since these ship their own authored materials.
  - **Verified visually**, which is the real test: sampled the walk clip mid-stride and rendered it. Clean leg bend, correct arm swing, no distortion — versus the previous distorted result.
  - **Texture budget**: the copied maps are 4K PNGs (~380MB, some normals 30MB each), far past the mobile target in D-008. Removed 8 duplicate `" 1.png"` variants (50MB) and added `Mini016TextureBudget` capping imports (normals 512, colour 1024, compressed, mipmapped). Runtime/mobile cost is now sane; the repo still carries full-size sources, flagged in the handoff.
  - **R cloning**: `FarmPlot.Clone()` takes cuttings from a ripe plant for +2 seed without harvesting it, surfaced as a second world prompt line and wired to R in `InteractionDetector`.
  - **Windowed mode**: `WindowModeController` starts windowed (1600×900) and toggles borderless fullscreen on F11 / Alt+Enter — borderless rather than exclusive specifically so minimising and alt-tab behave normally.
  - **Stamina no longer stops the player.** Exhaustion now only removes the *run* option; walking always remains available.
  - **Police reinforcements**: `PoliceReinforcementSpawner` pools officers and activates one at heat ≥ 55 and a second at ≥ 85, positioned behind the active character so they arrive rather than materialise in view. Despawns as heat falls.
  - Controls overlay updated for R, F11 and the stamina behaviour.
  - **`Docs/CODEX-HANDOFF.md`** written as requested: the avatar root cause with the bone-count table, why locomotion speeds are fixed constants (the clips are in-place with no root motion, so speeds come from the larger project's `ThirdPersonController.cs`), the fact that the scene is *generated* and hand edits get overwritten, the crop-decimation rationale, the verification workflow that actually works here (Play Mode hangs in batch mode; animators do not evaluate outside Play mode; Overlay canvases do not render to a RenderTexture), current state, and a prioritised list of outstanding work.
- Files changed: `Editor/Mini016AvatarDiagnostic.cs`, `Mini016CharacterImport.cs`, `Mini016TextureBudget.cs`, `Scripts/UI/WindowModeController.cs`, `Scripts/Interaction/PoliceReinforcementSpawner.cs`, `Docs/CODEX-HANDOFF.md` (new); `Art/Characters/*` (new, approved copy); `Scripts/Farming/FarmPlot.cs`, `Scripts/Interaction/InteractionDetector.cs`, `Scripts/Character/PlayerController.cs`, `Scripts/UI/ControlsPanelController.cs`, `Editor/Mini011PhaseBSetup.cs`, `Editor/Mini011AssetSnapshot.cs`; `Scenes/GrandBayProof.unity`.
- Verification: compiles clean. Avatar bone counts measured before and after. Walk pose rendered and inspected — the decisive check. Texture caps applied to 49 textures. Scene builder clean. Windows build succeeded; **zero console errors in a 14s headless run**.
- Known issues (honest):
  - **Not playtested by hand.** The walk *pose* is verified from a rendered frame; motion in real time is not. This is the thing to check first.
  - **Not done from this request** (carried into `Docs/CODEX-HANDOFF.md` §7): dedicated "Land and Surveys" location and a car dealer for vehicles/boats — land is still sold via the farm shop; the Guadeloupe $500 → 3× sea trade; shirts/shorts/shoes as wearables (only cap/shades/chain/watch attach to bones); tighter/multiple building colliders; harder police missions actually using the new reinforcements; and vehicles/driving, which the user deferred to a later pass.
  - `Art/Characters/` adds ~380MB of 4K source PNGs to the repository. Import settings cap the runtime cost, but the repo weight is real — worth re-encoding or Git LFS.
  - Police reinforcements are spawned and positioned but do not pursue; `EscapeHeat` still means waiting for decay.
- Next action: user playtests movement first. If the walk/run now looks right, the next most valuable work is the shop split (Land and Surveys, car dealer) and tighter colliders, then vehicles.

### MINI-017 — Character scale mismatch, tighter colliders, Land/Dealer shops, Guadeloupe run

- Date: 2026-08-15
- Owner: Claude
- Request: User sent a video showing the character walking oddly and noted the protagonist is much smaller than the other characters, and asked for the remaining outstanding items.
- Implementation:
  - **Scale mismatch measured, and it is the inverse of how it appears.** `Mini017ScaleProbe` measured real world-space heights: Franki 1.974m and Sacat 1.934m (correct, and matching the 2m `CharacterController` capsule), against Floreswa NPCs at **2.73–2.83m**. The protagonists are not undersized — the NPCs were roughly 45% oversized, i.e. nearly 9 feet tall, and were also bursting out of the 2m capsule that actually moves through the world. That mismatch is very likely a contributor to the odd-looking movement as well, since a model taller than its collision capsule cannot have its feet line up with the ground.
  - `Mini017NpcScaleFix` normalises all nine Floreswa models to 1.85m via `ModelImporter.globalScale`, folding into the existing import scale rather than overwriting it. Fixed at import so every use is correct, rather than scaling instances at each call site. Re-measured after: all NPCs now 1.850m. Verified visually against the stalls and buildings.
  - **Building colliders tightened.** `AddBoundsCollider` previously fitted a single box to the whole instance's bounds, so shanty structures with overhanging roofs and lean-tos blocked the player metres from the actual walls. Now each renderer gets its own `BoxCollider` from its local mesh bounds, trimmed 8% horizontally so eaves and thin trim don't push the player off the wall face.
  - **Land and vehicle sales split into their own locations**, as asked. New `NpcRole` values `LandOffice`, `CarDealer` and `BoatMan`. Land no longer sells from the farm shop: a **"Land and Surveys"** office sells the Montine plot, a Hillside survey lot and a Montine safehouse deed; a **"Car Dealer"** sells a Scrambler bike, a Pickup van and the Fishing pirogue. Vehicle prices ($1,800–$3,200) are deliberately set well above early-game income so they remain a later purchase, per the user's direction. Both have their own stall and sign.
  - **Guadeloupe run implemented** (`GuadeloupeTrade`), matching DECISIONS.md D-007 and Docs/STORY.md Chapter Five: talk to the boat man at the end of the jetty, pay the captain **$500**, and the character who is *not* currently controlled sails with the whole crop inventory and returns after ~90s with **3×** its local value. While away that character is deactivated and `CharacterSwitchManager.SetLocked` prevents switching to them — so the player genuinely gives up their second body for the duration. No Guadeloupe map, route or evasion detail is modelled.
- Files changed: `Editor/Mini017ScaleProbe.cs`, `Mini017NpcScaleFix.cs`, `Scripts/Economy/GuadeloupeTrade.cs` (new); `Scripts/Character/CharacterSwitchManager.cs` (lock support), `Scripts/Interaction/TownNPCInteractable.cs`, `Editor/Mini011PhaseBSetup.cs`; all nine `Floreswa/Models/*.fbx.meta` (import scale); `Data/Shop/*.asset`; `Scenes/GrandBayProof.unity`.
- Verification: compiles clean. Heights measured before and after (2.73–2.83m → 1.850m). Scene builder clean. Market re-rendered — NPCs now sit at believable height against the stalls and buildings. Windows build succeeded; **zero console errors in a 14s headless run**.
- Known issues (honest):
  - **The video could not be viewed** — there is no ffmpeg on this machine and no video decoding available, so the walk problem was diagnosed from the user's written description plus measurement, not from the footage. The scale mismatch is confirmed and fixed; **whether that fully accounts for the "walks weird" complaint is not established.** If it still looks wrong after this, the next thing to check is the animator blend thresholds against actual movement speed, and whether the `Jump`/`Grounded` transitions are firing during normal walking.
  - Nothing here is playtested by hand.
  - Vehicles and boats are purchasable but **not drivable** — no vehicle controller, physics or road mechanics yet. That was explicitly deferred by the user to a later pass and remains the largest outstanding feature.
  - Shirts/shorts/shoes still have no visual (bone attachment only covers cap/shades/chain/watch).
  - The Guadeloupe trip length (90s) and 3× multiplier are first-pass values and unbalanced against the rest of the economy.
  - Police reinforcements spawn with heat but still do not pursue.
- Next action: user playtests movement and scale first. If movement still looks wrong, that needs a focused pass with a frame-by-frame comparison against the larger project rather than more inference.

### MINI-018 — The actual walking fix: use the larger project's authored animator controller

- Date: 2026-08-15
- Owner: Claude
- Request: Walking/running still looked wrong after MINI-016/017. User's read — "the original bones and mappings with the mixamo was the best... get it fixed like the original just scale down" — was correct and pointed at the remaining difference. Also: an NPC walking through houses, and a building sitting on the Lalay/Montine junction.
- Implementation:
  - **Root cause of the remaining problem: I was generating my own animator controller instead of using the larger project's.** By MINI-016 the models and clips matched the big game, but the *controller* did not. Reading the big game's `ThirdPersonController.cs` showed it sets a **`MotionSpeed`** parameter (`_animator.SetFloat(_animIDMotionSpeed, inputMagnitude)`) that scales clip playback rate. My generated 1D blend tree had no such parameter, so clips always played at a fixed rate regardless of how fast the character actually moved — the feet could never agree with the ground, no matter how well the speeds were tuned.
  - Copied `StarterAssetsThirdPerson.controller` from the larger project and used it directly (`LoadLocomotionController`, falling back to the generated one only if the asset is missing).
  - **Caught a GUID trap:** the controller references clips by GUID, and the animation FBXs had been copied in MINI-014 *without* their `.meta` files, so Unity had assigned fresh GUIDs — every reference would have silently resolved to nothing, giving a controller that loads fine and plays nothing. Copied the original `.meta` files to restore the GUIDs, then wrote `Mini018ControllerVerify` to prove it rather than assume: **states=4, withMotion=4, missingMotion=0**, and it printed the real thresholds — `Idle @ 0, Walk_N @ 2, Run_N @ 6` — plus all five parameters (Speed, Jump, Grounded, FreeFall, MotionSpeed).
  - Rewrote `PlayerController` to mirror the big game's `Move()`: smoothed speed toward target at `SpeedChangeRate` (10) rather than snapping, a separate smoothed `_animationBlend`, and `MotionSpeed` set from whether there is input. Jump/Grounded/FreeFall now driven the same way.
  - `FollowController` and `PatrolNPC` also set `MotionSpeed` and `Grounded`, or they would have kept the old skating for the companion and NPCs.
  - **NPC walking through buildings**: patrolling NPCs now move via a `CharacterController` (`SimpleMove`) instead of writing `transform.position` directly, so building colliders actually stop them.
  - **Building on the Montine junction removed**: house placement now skips a 14m radius around the mid-road turnoff where the farm track leaves the Lalay road.
- Files changed: `Editor/Mini018ControllerVerify.cs` (new); `Art/Animations/StarterAssetsThirdPerson.controller` + original `*.anim.fbx.meta` (copied, GUID-matched); `Scripts/Character/PlayerController.cs` (rewritten around the authored controller), `FollowController.cs`, `Scripts/Interaction/PatrolNPC.cs`, `Editor/Mini011PhaseBSetup.cs`; `Scenes/GrandBayProof.unity`.
- Verification: compiles clean. Scene builder logs `using authored StarterAssetsThirdPerson controller`. Controller verified with zero unresolved clips. Walk pose rendered — clean stride, correct leg bend and arm swing. Windows build succeeded; **zero console errors in a 14s headless run**.
- Known issues (honest):
  - **Still not confirmed in motion.** A static pose renders correctly and the controller now matches the original exactly, but the complaint is about movement over time, which I cannot observe. This is the third attempt at this problem; the difference now is that the *whole* chain — models, clips, controller, parameters and driving code — matches the larger project rather than being reconstructed. If it is still wrong, the remaining suspects are the camera-relative turn rate fighting the animation, or `Camera.main` lookup cost per frame.
  - Vehicles remain purchasable but not drivable.
  - Shirts/shorts/shoes still have no visual.
  - Police reinforcements spawn but do not pursue.
- Next action: user playtests movement specifically. If still wrong, screenshots at a few points during a walk cycle would tell me more than a video I cannot decode.

## Required change-entry format






```text
### MINI-### — Short title

- Date:
- Owner:
- Request:
- Acceptance criteria:
- Implementation:
- Files changed:
- Scene/prefab changes:
- Verification commands:
- Verification results:
- Known issues:
- Next action:
```

