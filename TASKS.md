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

Status: **Phase A and Phase B complete, bugfixed once from user testing.**
See `Docs/MINI-011-VISUAL-PLAN.md`, `Docs/ASSET-REGISTER.md`, and the
Phase B / Phase B bugfix entries in `PROJECT-HANDOFF.md`. `GrandBayProof`
now has a sculpted terrain, road, ~30-40 houses (real Shanty Town
structures + hand-built modular houses), vegetation, sea, and Humanoid
player/NPC with basic locomotion animation. Full story now recorded in
`Docs/STORY.md`.

**Phase C scope (next, not started)** — from the user's own request list
after testing Phase B:

- Police, shopkeeper, and crop-buyer NPCs, visible and interactable near
  the Lalay road (per `Docs/STORY.md` Mission 1/2's mentor/market-woman
  and Mission 2's shopkeeper/friendly police).
- HUD: health meter, heat meter (police), stamina meter.
- Numbered crop-selection: keys 1/2/3/4 change which crop is planted.
- Multiple farm plots (6+ per the corrective brief) with light-brown
  (unplanted) vs dark-brown (planted/watered) soil states.
- Crop growth: visible scale-up through growth stages; tomato fruit
  colour green while growing, red when ripe.
- Upgrade `OnGUI` prompts/dialogue to Canvas + TextMeshPro (already
  planned; `com.unity.ugui` is installed).
- Tab/Q character switching, with Strong built as a real second
  controllable character (not just the NPC placeholder) — `CharacterSwitchManager` per the corrective brief, shared money/inventory/heat, independent health/stamina/position.
- Re-confirm the running/animation fix from the Phase B bugfix pass.

Car/driving is explicitly deferred by the user to a later phase (noted,
not scheduled yet).

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

