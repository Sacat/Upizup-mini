---
name: upizup-mini-production
description: Plan, build, inspect, or hand off work for the Up Iz Up Mini Unity project, including world building, Hitem3D assets, characters, animation, vehicles, gameplay, visual QA, and Codex-Claude coordination. Do not use for the separate full-size Up Iz Up project.
---

# Up Iz Up Mini production

Work only in `E:\Unity\Up Iz Up Mini`. Treat `E:\Unity\Up iz up` and `E:\Assets` as read-only references.

Before acting:

1. Read `AGENTS.md`, `Docs/CURRENT.md`, the current claim and active/relevant task entries in `PROJECT-HANDOFF.md` and `TASKS.md`, and `Docs/AI-PRODUCTION-WORKFLOW.md` completely.
2. Read the relevant design/reference documents named by the workflow. Use the full handoff as searchable audit history instead of loading unrelated completed tasks.
3. Inspect the current owner and dirty files. Do not overwrite or silently absorb another agent's work.
4. Convert the request into one bounded work packet using `Docs/WORK-PACKET-TEMPLATE.md` before editing.
5. Claim one task and reserve exact files. Only one agent may integrate scenes, prefabs, packages, project settings, or shared generated assets.

Use the workflow's approval class:

- Class A: invisible/reversible code or documentation; proceed after normal checks.
- Class B: visible but easily reversible; produce fixed-camera evidence and request approval before calling it visually final.
- Class C: identity, map layout, character appearance, paid generation, destructive replacement, or an already approved visual; obtain user approval before spending credits or integrating the change.

For visual work, follow this loop:

`reference -> specification -> cheap preview -> user decision -> implementation -> fixed evidence -> user acceptance -> visual lock`

Never treat a compile, numeric test, or bind-pose render as proof that motion or appearance is good. Use screenshots for static appearance and a short live capture for movement, camera feel, combat, riding, or driving. State explicitly when hands-on confirmation is still owed.

Prefer parallel agents for read-only research, codebase exploration, validation, log analysis, and independent asset review. Use one integrator for all Unity scene/prefab/package/settings work. Do not run Codex and Claude as competing implementers on the same feature.

Use Hitem3D as a candidate generator, not as production truth. Do not spend credits until the reference sheet, intended use, scale, triangle/texture budget, required views, and acceptance criteria are approved. Every generated asset must pass Blender cleanup, provenance recording, Unity import validation, mobile budgets, and visual approval.

Preserve user-approved manual placements and looks recorded in `Docs/VISUAL-APPROVAL-REGISTER.md`. Do not normalize, recalculate, regenerate, or replace them without explicit approval.

Finish by running proportional verification, updating the handoff/change records, listing exact evidence and limitations, and releasing ownership. Never commit unrelated dirty files.
