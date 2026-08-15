# Up Iz Up Mini — Project Handoff

## Project

- Path: `E:\Unity\Up Iz Up Mini`
- Unity: `6000.3.10f1`
- Reference project: `E:\Unity\Up iz up` — read-only
- Status: MINI-012 built on top of Phase C: real decimated crop plants with 4 growth stages, hillside Montine farm (8-plot grid, terraced), beach/jetty/boat + swimming, NPC idle/patrol animation (T-pose fixed), police reacting to heat, black road, market stalls, farm safehouse, F5/F9 save-load, and retuned movement speeds. Windows build produced, zero errors in a 12s headless run. Not hands-on playtested yet.
- Current owner: None
- Active task: None
- Last verified change: `MINI-000`; `MINI-001` implemented pending confirmation; `MINI-011` Phase C and `MINI-012` built and statically/headlessly verified, pending user playtest.
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
- `GrandBayProof.unity`'s MINI-011 Phase C and MINI-012 content needs a hands-on human playtest via `Builds/GrandBayProof/UpIzUpMini.exe`. Highest-uncertainty items: the running-speed retune (a reasoned fix for foot-sliding, not a confirmed one), swimming, and the F5/F9 save-load round-trip. See the MINI-012 entry for what is and isn't confirmed.

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

