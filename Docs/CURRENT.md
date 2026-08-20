# Up Iz Up Mini — Current State

Read this at the start of every task. `PROJECT-HANDOFF.md` remains the full audit trail; search it for the active task and the systems you will touch rather than loading all completed history into every session.

## Project

- Path: `E:\Unity\Up Iz Up Mini`
- Unity: `6000.3.10f1`
- Full-size reference project: `E:\Unity\Up iz up` (read-only)
- Current production state: systems 1–3 are integrated and user-tested on `codex/mini-085-baseline-20260820`. `MINI-095` adds an accepted, separate Lalay/Highland map-lab with an OSM-backed road network and Copernicus terrain; the playable generated scene is deliberately unchanged until the next migration task.
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
- The current world is a 320m procedural prototype, not measured Grand Bay: sinusoidal road, algorithmic houses, straight Montine spur, synthetic coast/jetty.
- `Docs/MAP-ANCHORS.json` and compact Unity map/height data now exist. `VA-004` locks the approved OSM road network and Lalay street scale; the map-lab remains separate from gameplay.
- `Docs/WORLD-EXPANSION-WORKFLOW.md` is mandatory for all map work. The first live manifest is `dm-dom-grand-bay-lalay-highland-v1`, currently at `graybox`; it cannot advance until a corrected player-height Lalay view is approved.
- Locomotion uses the authored StarterAssets controller and `MotionSpeed`; do not regenerate it casually.
- `HumanoidAnimationManager` is the reusable action-layer foundation.
- Bike seating already uses `VehicleSeat`, `VehicleRider`, `BikeRiderAnimation`, and Humanoid IK. Improve profiles/clips/gates rather than starting over.

## Highest risks

1. Visual acceptance: Sacat's two-piece chain placement is locked as `VA-002`; its visibility/swing while walking still needs the user's playtest. Most other movement and appearance remain unapproved.
2. Visual debt: many changes compile or pass harnesses but have not been watched in real Play Mode. Static screenshots cannot prove animation, combat, riding, driving, NPC movement, or UI timing.
3. Map migration risk: the new sourced map-lab is accepted, but the old playable generated scene still uses its 320m procedural world until a rollback-safe migration task replaces it.
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

1. User checks the updated chain while walking/running, then accepts or reports only concrete fit/swing changes.
2. Migrate the accepted `VA-004` Lalay/Highland map-lab into the generated gameplay world in a separate task, preserving all missions, NPCs, vehicles, farms and safehouses.
3. Prepare one Hitem3D character reference card. No paid generation until the user approves the reference.
4. Integrator: introduce an input facade and asset-size/performance report before touch UI or more vehicles.
5. Preserve the current gameplay scene until the separate map-lab layout is accepted.
6. Prove one canonical modular body + one shirt/short/shoe set before generating a wardrobe.
7. Compress vehicle payloads, then improve per-character bike fit, mount/dismount sequencing, and proper riding clips.
8. Fix combat contact timing and one polished unarmed hit/knockdown loop before combos, ragdolls, or shooting.
9. Isolated URP proof and first Android device build are separate approval tasks.

## Update rule

Keep this file short. Update facts and priorities at task completion; put detailed evidence, command logs, and historical narrative in the task entry inside `PROJECT-HANDOFF.md`.
