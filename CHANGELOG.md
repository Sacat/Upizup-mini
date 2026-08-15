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

