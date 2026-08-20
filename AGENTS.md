# Up Iz Up Mini — Agent Rules

These rules apply to Codex, Claude Work/Claude Code, and any other coding agent.

## Project boundaries

1. The only writable game project for Mini work is `E:\Unity\Up Iz Up Mini`.
2. Treat `E:\Unity\Up iz up` and `E:\Assets` as read-only reference sources. Never edit, move, rename, or delete anything there while working on Mini.
3. Do not bulk-copy the main project. Import only assets that are required, technically suitable, and reasonably licensed.
4. Never claim the whole game or a feature is finished without the required verification evidence.

## Required startup sequence

1. Read this file completely.
2. Read `Docs/CURRENT.md`, the `### Current claim` block in `PROJECT-HANDOFF.md`, the active/relevant task entries in `PROJECT-HANDOFF.md` and `TASKS.md`, and relevant files in `Docs/`. The full handoff is the audit history; do not load all completed entries when a targeted search is sufficient.
   For production, visual, asset, world, character, animation, vehicle, or QA work, also read `Docs/AI-PRODUCTION-WORKFLOW.md` and create a bounded work packet from `Docs/WORK-PACKET-TEMPLATE.md`.
3. Confirm `Current owner` is `None` or already assigned to you for the active task.
4. Claim exactly one task ID in `PROJECT-HANDOFF.md` before editing.
5. Record the files or scene that the task reserves.

## Ownership and conflicts

- Only one agent may edit Unity scenes, prefabs, project settings, packages, input settings, or rendering configuration at a time.
- Other agents may work only on explicitly non-overlapping scripts or documentation.
- Never edit the same `.unity`, `.prefab`, `.asset`, or `.meta` file concurrently.
- Prefer repeatable editor setup scripts over hand-editing serialized scene YAML.
- If ownership is unclear, stop and leave a blocker in `PROJECT-HANDOFF.md`.

## Engineering standards

- Build a 2.5D game using real 3D terrain, buildings, characters, vegetation, and vehicles.
- Use a perspective three-quarter camera; do not turn the game into a flat overhead tilemap.
- Keep systems modular and data-driven. Avoid hard-coding mission content into unrelated controllers.
- Keep mobile performance in mind: URP, small texture budgets, LODs, pooled NPCs, limited dynamic lights, and bounded simulation distances.
- Preserve geographic anchors and Grand Bay identity while compressing travel distances.
- Do not add online services, telemetry, purchases, accounts, or network features without explicit user permission.
- Do not publish, upload, or share local assets or project data.

## Verification

For every implementation task:

1. Run a Unity batch-mode compile.
2. Run available Edit Mode or Play Mode tests.
3. Run the task-specific acceptance checks.
4. Record exact commands/log paths and results.
5. Update `PROJECT-HANDOFF.md`, `CHANGELOG.md`, and `DECISIONS.md` when applicable.
6. Commit a checkpoint when Git is configured.
7. Release ownership by returning `Current owner` to `None`.

If a visual result cannot be inspected, say so explicitly and request a user play-test or screenshot. Do not substitute assumptions for visual confirmation.

User-approved appearance and placement recorded in `Docs/VISUAL-APPROVAL-REGISTER.md` must not be silently recalculated, regenerated, normalized, or replaced. Changing a visual lock requires explicit user approval and replacement evidence.
