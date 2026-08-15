# Up Iz Up Mini — Task Board

## Done (pending user confirmation)

### MINI-001 — 2.5D Grand Bay proof of concept

Goal: Create one playable scene proving the visual and control direction.

Status: Implemented and statically validated by Claude on 2026-08-15. Batch-mode compile and scene-wiring validation both pass. Runtime/visual behavior (movement feel, prompts, dialogue, console-clean Play mode) has **not** been manually play-tested yet — headless automated Play-mode verification hit an unrelated Unity Editor Search-module bug in this environment. See the MINI-001 entry in `PROJECT-HANDOFF.md` for full detail and the requested manual check.

Acceptance criteria:

- Perspective three-quarter follow camera, approximately 40–50 degrees downward.
- Simple 3D player capsule or approved temporary character can walk and run.
- A narrow Lalay road runs between simple buildings on both sides.
- A visibly dirty path branches from the village road toward a Montine farm clearing.
- One standing NPC displays a world-space `[ E ] Talk` prompt when approached.
- Pressing E displays one short Dominican-style text line.
- One farm plot displays a world-space `[ E ] Plant` prompt.
- No flat top-down tilemap presentation.
- Scene runs without console errors and passes a batch-mode compile.

## In progress

### MINI-011 — Grand Bay production vertical slice (corrective rebuild)

Goal: Replace MINI-001's primitive-geometry proof scene with a real visual
and gameplay vertical slice — Shanty Town-style village art, hand-authored
Grand Bay-shaped terrain, two switchable named boys (Smart/Strong), NPCs,
police, a complete tomato mission loop, and a working HUD. Full brief
recorded verbatim in the MINI-011 change entry in `PROJECT-HANDOFF.md`
(too long to duplicate here). This single request effectively supersedes
the separate scope of `MINI-004` through `MINI-009` below — those entries
stay as a scope reference but MINI-011 is being tracked as one corrective
initiative broken into internal Phases A-D with hard visual gates between
them, per the brief's own instructions.

Status: **Phase A, B, and C all built.** See `Docs/MINI-011-VISUAL-PLAN.md`,
`Docs/ASSET-REGISTER.md`, and the Phase B/bugfix/Phase C entries in
`PROJECT-HANDOFF.md`. `GrandBayProof` now has: sculpted terrain, road,
~30-40 houses (real Shanty Town structures + hand-built modular houses,
now with collision), vegetation, sea; two controllable Humanoid characters
(Smart/Strong, Tab to switch, companion follows when inactive); 4 NPCs
(Villager/Police/Shopkeeper/Buyer); 6 farm plots with a full
plant→water→grow→harvest state machine and light/dark soil states;
1-4 crop selection (Tomato/Banana/Carrot/Bushers); a HUD (health/stamina/
heat/money/crop/character-name); Esc pause menu with mouse+keyboard
navigation; mouse-look camera (horizontal + vertical). Full story recorded
in `Docs/STORY.md`.

Compiles clean, scene builder runs clean, static validation passes, and a
Windows build runs with **zero console errors in a 10-second headless run**
of the actual compiled game (economy/NPCs/plots/characters/HUD all
actually initializing, not just constructed). **None of it has been
hands-on playtested by a human yet** — that's the explicit next step, not
assumed done.

**Known gaps, not yet addressed:**

- No real "buy seeds" transaction — planting just uses whichever crop is
  selected via 1-4.
- Only tomato's grow-colour (green→red) was deliberately tuned; other
  crops use reasonable placeholder colours.
- `FollowController` (companion AI) is direct-steering, not NavMesh —
  can cut corners/snag on obstacles in tight spots.
- `OnGUI` interaction prompts still not upgraded to Canvas + TextMeshPro
  (the pause menu/HUD use legacy UGUI Text now; prompts are the one
  remaining OnGUI surface).
- Car/driving explicitly deferred by the user to a later phase.

## Ready

### MINI-002 — Map-anchor data

Goal: Record verified Grand Bay and Dominica coordinate anchors in a data file without yet building the full island.

### MINI-003 — Core farming loop

Goal: Plant, water, grow, harvest, inventory, and sell tomatoes in the proof scene.

## Later

- `MINI-004`: Two playable boys and switching.
- `MINI-005`: Standing and pooled walking residents.
- `MINI-006`: Heat meter, police patrol, chase, and maximum-heat reinforcement.
- `MINI-007`: Pickup or bike enter/exit/driving slice.
- `MINI-008`: Banana, carrot, Bushers, Black Sugar, and Purple progression.
- `MINI-009`: Four-to-six-mission Grand Bay chapter.
- `MINI-010`: Save/load, Windows build, and Android performance pass.

