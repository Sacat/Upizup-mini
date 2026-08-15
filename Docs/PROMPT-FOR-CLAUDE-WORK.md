# Copy-paste prompt for Claude Work

You are working on **Up Iz Up Mini**, a separate Unity project at:

`E:\Unity\Up Iz Up Mini`

The larger project at `E:\Unity\Up iz up` is **read-only reference**. Do not modify it and do not bulk-copy it.

Before taking any action, completely read:

1. `AGENTS.md`
2. `CLAUDE.md`
3. `PROJECT-HANDOFF.md`
4. `TASKS.md`
5. `DECISIONS.md`
6. `Docs/GAME-DESIGN.md`
7. `Docs/MAP-STRATEGY.md`
8. `Docs/DEFINITION-OF-DONE.md`

Then inspect the clean Unity project and claim task `MINI-001` in `PROJECT-HANDOFF.md`. Do not work on any other task.

Build the **2.5D Grand Bay proof of concept** described under `MINI-001`:

- Use a real 3D scene and a perspective three-quarter follow camera looking down approximately 40–50 degrees.
- Do not create a flat overhead tilemap or old-GTA-style top-down camera.
- Create a narrow Lalay road with simple buildings on both sides.
- Create a dirty path branching toward a Montine farm clearing.
- Add one controllable temporary 3D character with walk and run.
- Add one standing NPC. When the player is close, show a world-space `[ E ] Talk` prompt above the NPC. Pressing E should show a short Dominican-style text line.
- Add one farm plot with a world-space `[ E ] Plant` prompt.
- Prefer a repeatable editor setup script named `Assets/UpIzUpMini/Editor/Mini001SceneSetup.cs` for generated scene content.
- Keep assets primitive/temporary unless an existing asset is deliberately approved and documented.
- Keep the implementation modular and future-mobile-friendly.

Before claiming completion:

1. Run a Unity batch-mode compile.
2. Run any relevant Play Mode check you create.
3. Verify the scene contains the camera, road, buildings on both sides, player, dirty path, NPC interaction, and farm interaction.
4. Record exact files changed and verification results in `PROJECT-HANDOFF.md` and `CHANGELOG.md`.
5. Return `Current owner` and `Active task` to `None`.

If you cannot verify a visual result, state that clearly and leave a precise user play-test request. Do not expand scope without approval.

