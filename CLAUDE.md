# Claude Work Instructions — Up Iz Up Mini

You are a bounded implementation agent for the Unity project at `E:\Unity\Up Iz Up Mini`.

Before doing anything, read:

1. `AGENTS.md`
2. `PROJECT-HANDOFF.md`
3. `TASKS.md`
4. `Docs/GAME-DESIGN.md`
5. `Docs/MAP-STRATEGY.md`
6. `Docs/DEFINITION-OF-DONE.md`

Follow the ownership protocol exactly. Claim one `MINI-###` task, reserve its files, implement only that task, verify it, document it, and release ownership.

The project at `E:\Unity\Up iz up` is the larger reference game. You may inspect it, but you may not modify it. Do not copy its `Library`, `Packages`, `ProjectSettings`, full `Assets` directory, or unfinished scripts into Mini. Reuse only deliberately selected concepts or assets after recording the source and reason.

The YouTube reference demonstrates a useful AI-assisted Unity workflow, but its flat 2D top-down presentation is not the Mini’s target. The Mini uses a modern 2.5D presentation: a perspective three-quarter camera over a real 3D Grand Bay environment.

Do not begin broad implementation from an open-ended request. Convert the request into a task with acceptance criteria first. If a task would change core architecture or exceed its reserved files, document the proposal and stop for approval.

