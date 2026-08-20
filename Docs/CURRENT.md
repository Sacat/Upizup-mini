# Up Iz Up Mini — Current State

Read this at the start of every task. `PROJECT-HANDOFF.md` remains the full audit trail; search it for the active task and the systems you will touch rather than loading all completed history into every session.

## Project

- Path: `E:\Unity\Up Iz Up Mini`
- Unity: `6000.3.10f1`
- Full-size reference project: `E:\Unity\Up iz up` (read-only)
- Current production state: work through `MINI-084` is preserved by the deliberate baseline commit `b367fc5` on `codex/mini-085-baseline-20260820`. MINI-085 compiled, rebuilt, validated, built, smoke-tested, and inspected the live opening; most later motion/feel remains not hands-on accepted.
- Current task/owner: authoritative only in the `### Current claim` block of `PROJECT-HANDOFF.md`.
- Last known committed checkpoint shown by Git during MINI-084 audit: `42efdf3` (`MINI-050`).

## What exists

- Switchable Sacat and Franki, locomotion/jump/stamina/health, inventory/economy/save-load.
- Farming, crop progression/cloning, plots/land, shops/apparel/accessories, food/pharmacy.
- Missions, dialogue/dialect foundation, bosses, reputation, gangs, police/heat, safehouses, Guadeloupe abstraction.
- TMAX riding/wheelie/pillion and Range Rover driving/passengers, radio, interaction prompts.
- NPC patrol/navigation foundations, melee/hit reaction foundations, recruitment/following.
- Fixed-camera editor snapshots, many feature validators, Windows builds, and headless built-player smoke tests.

## Sources of truth

- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` generates `GrandBayProof.unity`. Manual scene edits are overwritten.
- The current world is a 320m procedural prototype, not measured Grand Bay: sinusoidal road, algorithmic houses, straight Montine spur, synthetic coast/jetty.
- `Docs/MAP-ANCHORS.json` does not exist. Candidate research under `E:\Assets\GrandBayReference` is read-only and remains user-unverified.
- Locomotion uses the authored StarterAssets controller and `MotionSpeed`; do not regenerate it casually.
- `HumanoidAnimationManager` is the reusable action-layer foundation.
- Bike seating already uses `VehicleSeat`, `VehicleRider`, `BikeRiderAnimation`, and Humanoid IK. Improve profiles/clips/gates rather than starting over.

## Highest risks

1. Visual acceptance: MINI-085 preserved the prior working tree and verified the build, but no appearance is user-locked. The active-player name is missing from the live HUD and the opening camera can be blocked by the safehouse roof.
2. Visual debt: many changes compile or pass harnesses but have not been watched in real Play Mode. Static screenshots cannot prove animation, combat, riding, driving, NPC movement, or UI timing.
3. Map fidelity: existing Lalay-to-beach layout has no defensible geographic source data.
4. Mobile architecture: Built-in RP, legacy Input Manager, no touch/safe-area layer, no Android build gate.
5. Build size: latest audited Windows build was about 388.6 MB; textures about 280.6 MB. TMAX source contributed about 172.2 MB and Range Rover about 43.5 MB.
6. Combat: current attack uses a sword-like clip and immediate large overlap sphere, so air hits and warped-looking punches are expected.
7. Character modularity: current clothes mostly recolor existing meshes; matching height does not make rig/bone scale or garment fit identical.

## Workflow now in force

- Read `Docs/AI-PRODUCTION-WORKFLOW.md`.
- Create one bounded packet from `Docs/WORK-PACKET-TEMPLATE.md`.
- Run `Tools/AIWorkflow/Invoke-Preflight.ps1`.
- Parallelize read-only audits and non-overlapping files only.
- One integrator owns Unity scenes, prefabs, packages, settings, imports, generated world data, and final visual integration.
- Record accepted appearance/placement in `Docs/VISUAL-APPROVAL-REGISTER.md`.
- Do not spend Hitem3D credits before the reference/asset card is approved.

## Recommended next sequence

1. Make one small packet for the missing active-player name and obstructed opening camera; then ask the user to play the current build and accept/reject movement and presentation.
2. Collect and verify Lalay-to-beach map anchors, then build a separate map-lab graybox without destroying the working gameplay scene.
3. Prepare one Hitem3D character reference card. No paid generation until the user approves the reference.
4. Integrator: introduce an input facade and asset-size/performance report before touch UI or more vehicles.
5. Preserve the current gameplay scene until the separate map-lab layout is accepted.
6. Prove one canonical modular body + one shirt/short/shoe set before generating a wardrobe.
7. Compress vehicle payloads, then improve per-character bike fit, mount/dismount sequencing, and proper riding clips.
8. Fix combat contact timing and one polished unarmed hit/knockdown loop before combos, ragdolls, or shooting.
9. Isolated URP proof and first Android device build are separate approval tasks.

## Update rule

Keep this file short. Update facts and priorities at task completion; put detailed evidence, command logs, and historical narrative in the task entry inside `PROJECT-HANDOFF.md`.
