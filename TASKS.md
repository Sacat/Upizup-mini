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

Blocked: `Docs/MAP-ANCHORS.json` still doesn't exist. Every terrain/road pass so far is an approximation of reference photos, not measured. Needs real coordinates, an OSM export, or annotated aerial imagery from the user before this can move.

### MINI-003 — Core farming loop

Goal: Plant, water, grow, harvest, inventory, and sell tomatoes in the proof scene.

## Done (pending user confirmation)

### MINI-049 — Cheat code: C#0W@

Goal: type `C#0W@` in-game for $100,000, every strain/route unlocked, and
invincibility + unlimited stamina. Built and verified with a real
validation harness (near-miss rejection, rolling-buffer match with junk
typed first, exact effects, and a genuine damage-blocking proof, not just
a flag check) - see the MINI-049 entry in `PROJECT-HANDOFF.md`. Deliberately
not documented in the H-controls overlay. Not yet hands-on typed into the
running build. Scoped to health/stamina only - does not affect police heat
generation/detection.

### MINI-048 — Blue Cheese + Sugar Cheese + Purple Cheese

Goal: a new base strain (Blue Cheese, boss-granted like Black Sugar/Purple)
plus two new hybrids bred from it (Sugar Cheese = Black Sugar x Blue
Cheese, Purple Cheese = Purple x Blue Cheese - confirmed with the user
rather than guessed). `CropBreedingStation` generalised from one fixed
recipe to a list so all three hybrids share one bench. Built and verified
with a real validation harness (caught and fixed a real bug in the test
itself, not the game code) - see the MINI-048 entry in
`PROJECT-HANDOFF.md`. Crop-selection hotkeys are now at 10 slots (1-9, 0) -
flagged as a real UX ceiling worth a proper menu eventually. Not yet
hands-on playtested.

### MINI-047 — Interbreeding: Purple Black

Goal: combine one harvested Purple + one harvested Black Sugar at a new
breeding station to produce Purple Black seed (orange+purple two-tone
buds), gated behind a later progression unlock than either parent strain.
Built and verified with a real validation harness, including a regression
check proving every earlier crop's single-colour look is unaffected - see
the MINI-047 entry in `PROJECT-HANDOFF.md`. Not yet hands-on playtested or
visually rendered.

### MINI-046 — Stronger together: companion auto-assist and damage bonus

Goal: the following boy automatically fights nearby police once eligible,
and the player's own punch deals 1.5x damage while the companion is close,
on his feet, and free to help. Built and verified with a real validation
harness (caught and fixed two real Physics.OverlapSphere edit-mode testing
limitations along the way, not game bugs) - see the MINI-046 entry in
`PROJECT-HANDOFF.md`. "Or gang" from the user's original ask has no gang
NPCs yet - that's the separate rival-gang task. Not yet hands-on playtested.

### MINI-045 — Chain swing physics

Goal: the gold chain accessory dangles/swings instead of sitting perfectly
rigid on the bone it's already attached to (MINI-022). A damped spring, not
IK - IK doesn't apply to a passive necklace. Built and verified with a real
physics-integration validation harness - see the MINI-045 entry in
`PROJECT-HANDOFF.md`. Not yet hands-on playtested; stiffness/damping/clamp
are first-pass numbers.

### MINI-044 — Farmhand assignment is a real later unlock

Goal: sending the other boy to farm now requires having made the M8 career
choice first (previously only the *tutorial hint* for it was delayed, per
MINI-030 - the mechanic itself was always available). Built and verified
by Claude on 2026-08-16 - see the MINI-044 entry in `PROJECT-HANDOFF.md`.
Not yet hands-on playtested.

### MINI-043 — Guadeloupe: NPC courier first, character unlock after

Goal: the first Guadeloupe run sends an NPC courier (no playable character
touched); completing it unlocks sending an actual boy from then on. Built
and verified by Claude on 2026-08-16, including a real state-machine
validation harness proving both stages - see the MINI-043 entry in
`PROJECT-HANDOFF.md`. Not yet hands-on playtested.

### MINI-042 — Buyable Lalay house

Goal: a second, purchasable safehouse in town, gated on the already-sold
(but previously inert) `prop_safehouse` item. Built and verified by Claude
on 2026-08-16 - see the MINI-042 entry in `PROJECT-HANDOFF.md`. Not yet
hands-on playtested.

### MINI-041 — Reinforcement officers no longer disappear

Goal: fix the reinforcement-spawn/despawn bug diagnosed in MINI-040 -
added hysteresis (separate, lower despawn thresholds) and a walk-off
coroutine instead of an instant SetActive(false). Built and verified,
including a real state-machine validation harness proving the specific
case that was broken (a small heat dip must not undo a spawn) - see the
MINI-041 entry in `PROJECT-HANDOFF.md`. The walk-off *movement* itself is
unverified beyond "doesn't throw" (coroutines don't tick outside Play
mode) - needs a human to watch one leave.

### MINI-040 — Cell phone to call your partner

Goal: buy a phone, press C to instantly bring the inactive boy to you,
including pulling him off farm work. Built and verified by Claude on
2026-08-16 - see the MINI-040 entry in `PROJECT-HANDOFF.md`. Also
diagnosed (but deliberately did not fix, per the user's own instruction to
do this task first) the reinforcement-officers-disappear bug reported after
MINI-039 - root cause and a likely fix are recorded there for next time.
Not yet hands-on playtested.

### MINI-039 — Tier-1 economy/mission tuning batch

Goal: death fee to the health center, expensive strain seeds, the M3 land
mission pointed at the Land Office instead of the stale Farm Shop marker,
corrected Black Sugar (orange)/Purple (purple) bud colours, and police
confiscating weed near the plantation on proximity, not just a blind timer.
Built and verified by Claude on 2026-08-16 - see the MINI-039 entry in
`PROJECT-HANDOFF.md`, including a real lesson about where crop data actually
lives (`Mini011PhaseBSetup.CropSpecs`, not the generated `.asset` files -
edits to the asset directly get silently reverted on the next scene build).
Not yet hands-on playtested.

### MINI-038 — NPC hit reactions and knockdown

Goal: Police receiving punches stagger on non-fatal hits and fall/lie down
(held, not vanished) on the hit that drops them, using the MINI-031
animation manager. Built and verified by Claude on 2026-08-16, including a
real state-machine validation harness (`Mini038CombatValidation`), not just
"compiled and ran without exceptions." See the MINI-038 entry in
`PROJECT-HANDOFF.md`. Not yet visually confirmed - the fall pose itself
hasn't been rendered or watched live, only proven correct at the
state/layer-weight level.

### MINI-031 — Reusable Humanoid Animation Manager (foundation)

Goal: Replace ad-hoc per-script Animator wiring with a reusable, data-driven
action-animation layer any Humanoid character can use, so eating, fighting,
shooting, riding/driving, entering/exiting vehicles, seated driving, and
wardrobe/accessory poses can be added later as data entries instead of new
animation plumbing each time.

Status: Built and verified by Claude on 2026-08-16. See the MINI-031 entry
in `PROJECT-HANDOFF.md` for full detail. Scope was deliberately bounded to
what the project has real content for today (locomotion + melee) — the
architecture is generic, but eating/shooting/vehicle *content* is explicitly
out of scope here and tracked as its own later tasks below.

## Later

- `MINI-032`: Modular wardrobe + accessory IK/physics (chain jiggle, etc.) — attachment already exists (MINI-022 `CharacterEquipment`), this adds swing/sway physics and separate garment geometry instead of material recolor.
- `MINI-033`: Bike — first vehicle, uses the MINI-031 FullBodyOverride layer for a seated pose; simplest vehicle (no wheel/engine physics needed).
- `MINI-034`: Car — buy/enter/exit/seated driving, real vehicle physics; depends on MINI-033's enter/exit pattern.
- `MINI-035`: Rival gang faction — reuses `PoliceOfficer`'s chase/avoidance as a template; "stronger together"/companion auto-assist in a fight.
- `MINI-036`: Shooting — aim/ammo/weapon-hold; needs a real animation clip (none imported yet) and mobile-friendly aim input; heat +100 on landing a hit once this exists.
- `MINI-037`: Economy/mission tuning batch — death fee to health center, police confiscate weed near the plantation, expensive seed-strain seller NPC, land purchase via a land office/survey flow instead of the shop, cell phone to recall companion, buyable Lalay house, farmhand-for-hire and Guadeloupe-NPC-first as later unlocks, Black Sugar/Purple/Purple-Black bud recoloring.
- `MINI-004`: Two playable boys and switching.
- `MINI-005`: Standing and pooled walking residents.
- `MINI-006`: Heat meter, police patrol, chase, and maximum-heat reinforcement.
- `MINI-007`: Pickup or bike enter/exit/driving slice.
- `MINI-008`: Banana, carrot, Bushers, Black Sugar, and Purple progression.
- `MINI-009`: Four-to-six-mission Grand Bay chapter.
- `MINI-010`: Save/load, Windows build, and Android performance pass.

