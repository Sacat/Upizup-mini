# Claude Work Instructions — Up Iz Up Mini

You are a bounded implementation agent for the Unity project at `E:\Unity\Up Iz Up Mini`.

Before doing anything, read:

1. `AGENTS.md`
2. `Docs/CURRENT.md`
3. `Docs/Systems/README.md` — the system-ledger router (added 2026-08-29, at the user's request for the project to route by system rather than reload full history every time). Classify which system(s) the actual task touches (combat, vehicles, map generation, missions, dialogue, economy, characters, NPCs/AI, or the build/verification tooling itself) and read only those files under `Docs/Systems/` — each carries its own "what worked / what didn't" ledger, current architecture, and open items, and is meant to replace loading all of `PROJECT-HANDOFF.md`'s history for a task that only touches one or two systems.
4. The `### Current claim` block in `PROJECT-HANDOFF.md` (now an index of one-line pointers into the system files — the detail itself lives in `Docs/Systems/`, not here) and any active/relevant task entries
5. Active/relevant entries in `TASKS.md`
6. `Docs/GAME-DESIGN.md`
7. `Docs/MAP-STRATEGY.md`
8. `Docs/DEFINITION-OF-DONE.md`
9. `Docs/AI-PRODUCTION-WORKFLOW.md`
10. `Docs/VISUAL-APPROVAL-REGISTER.md`

Follow the ownership protocol exactly. Claim one `MINI-###` task, reserve its files, implement only that task, verify it, document it, and release ownership. When the task is done, update the matched `Docs/Systems/*.md` file(s) (architecture changes, what worked/didn't, new open items) before adding the one-line pointer entry in `PROJECT-HANDOFF.md` — the system file is where detail belongs now, not the master log.

The project at `E:\Unity\Up iz up` is the larger reference game. You may inspect it, but you may not modify it. Do not copy its `Library`, `Packages`, `ProjectSettings`, full `Assets` directory, or unfinished scripts into Mini. Reuse only deliberately selected concepts or assets after recording the source and reason.

The YouTube reference demonstrates a useful AI-assisted Unity workflow, but its flat 2D top-down presentation is not the Mini’s target. The Mini uses a modern 2.5D presentation: a perspective three-quarter camera over a real 3D Grand Bay environment.

Do not begin broad implementation from an open-ended request. Convert the request into a task with acceptance criteria first. If a task would change core architecture or exceed its reserved files, document the proposal and stop for approval.

Use `Docs/WORK-PACKET-TEMPLATE.md` for new tasks. Treat user-approved visual locks as protected. Parallel work is encouraged for read-only investigation and non-overlapping code, but one integrator alone owns scenes, prefabs, packages, project settings, generated world data, and final visual integration.
