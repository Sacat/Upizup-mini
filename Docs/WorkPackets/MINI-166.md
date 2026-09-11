# MINI-166 — Fitted selectable outfits

User authorizes implementing the approved wardrobe: Lacostes polo, Mike tee, jeans, denim shorts, trousers, Lacos cap, Mike 90 and Mike 97, fitted for both characters with independent colours. Build from the current repaired, skinned characters. Preserve wave hair, removable headphones, faces, hands, watch and chain placement.

Owner: Codex. Scope: new OutfitWardrobe runtime/data/editor tools and generated outfit meshes/materials; VisualWardrobePanel; CharacterEquipment; SaveLoadSystem; GrandBayProof scene; task and system documentation. No paid assets or external uploads. Budget: local mesh production and focused Unity verification. Existing free wardrobe access continues; no ownership grants.

Acceptance: actual mesh changes in all four clothing slots, distinct shirt/leg/footwear silhouettes, colour choice, Cancel/Apply and per-character save/load, no missing arms or lower legs with shorts, animation pose verification, fixed renders, compile and Windows build. Iterate on visible defects before integration. Existing dirty work preserved. Baseline scene and renderer meshes are checkpointed before integration.

## Checkpoint for Claude — 2026-09-11

Startup smoke: launched the new EXE with `-batchmode -nographics -logFile "E:\Unity\Up Iz Up Mini\Logs\mini166-smoke.log"`; it remained running for 15 seconds, then only that test process was stopped. No Exception/Error/Crash matches in the log. Repeated kinematic-body angular-velocity warnings remain. This is startup evidence, not outfit visual or interaction acceptance.

Incomplete at user's requested EXE/handoff checkpoint. Added OutfitWardrobe selection/tint/capture/restore data model, save fields/hooks, conditional legacy garment bypass, and current-scene mesh audit. No generated outfit geometry, scene attachment or clothing UI integration yet. Earlier acceptance paragraph describes required work, not completed results; no new source mesh/scene backup was created yet because no geometry integration occurred.

Unity Windows build completed successfully: `Logs/mini166-build.log`, marker `MINI-001 BUILD SUCCEEDED`, total packaged size 411,132,245 bytes, 20.55 seconds. Existing compiler warnings remain (VehicleSpawnController unreachable code; CharacterEquipment unused field). Output `Builds/GrandBayProof/UpIzUpMini.exe` with adjacent data files. Full outfit acceptance tests and visual proof remain outstanding. Detailed step-by-step continuation: `Docs/CLAUDE-CONTINUE-MINI-166.md`. Ownership released to None for Claude to claim.
