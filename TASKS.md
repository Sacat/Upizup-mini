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

### MINI-064/065 round 4 — wheel-disc sync, explicit tap-to-wheelie, dealer spawn/stock fixes

Goal: fix the four issues from the user's round-3 test feedback. (1)
Wheel-disc overlay slightly out of sync with the real tyre - traced to
a bad radius-fit probe range (0.02-0.08m catches hub/disc-brake
clutter); corrected to the empirically-clean 0.09-0.12m band plus a
small cosmetic padding. (2) Wheelie triggered too easily off plain
throttle - redesigned as an explicit button (E, tap not hold) with a
decaying "sustain" value that requires a rhythm of repeated taps to
hold a wheelie, verified with a NEW real-physics test
(`Mini065TmaxDropTest`, using `Physics.Simulate()` in Edit Mode) that
actually drives the bike and confirms progressive lift to ~35 degrees
without flipping. (3) Bike spawned "in the store" after purchase -
real root cause found: the buyer faces the dealer NPC while shopping,
and the dealer always faces the road, so the player's own forward
pointed backward into the shop; fixed by spawning relative to the
dealer's own facing direction instead. (4) Dealer stock restricted to
just the TMAX, per "the option should be only the bike and no other
vehicles" - removed the flavour-only Scrambler Bike/Pickup Van/Fishing
Pirogue placeholder entries. All changes verified via the extended
`Mini065TmaxValidation` harness (PASS, including a new dealer-stock-
count-of-1 check) and the new `Mini065TmaxDropTest` physics harness
(PASS - drop settles with zero bounce, drive test travels 40m with no
auto-wheelie, wheelie-tap test lifts to 35.5 degrees without flipping).
See the MINI-064/065 round 4 entry in `PROJECT-HANDOFF.md`.

### MINI-064/065 round 5 — real wheel X-alignment fix, hold-to-wheelie redesign, roll-bug root cause found and fixed

Goal: fix the user's round-4 test feedback. (1) Rear wheel disc still
out of alignment with the real mesh from behind - measured directly in
Blender and found a genuine 0.109m off-centre rear wheel in the source
scan (not noise); fixed by decoupling physics (WheelColliders stay
symmetric at X=0, for clean non-yaw-inducing physics) from visuals
(disc overlays now use the real measured offset), confirmed via
rendered side and rear axle-marker checks. (2) Tap-to-sustain wheelie
wasn't landing ("the wheelie isnt working now when i press E") -
replaced with a simpler hold-to-lift scheme: holding E climbs the
wheelie target progressively toward the cap, releasing drops it back
down, and a quick release-and-reapply naturally gives the "tapping
helps balance" feel without a separate system. (3) A real roll bug
("it gigs leans more to the left") - root-caused via a new roll-
specific measurement in `Mini065TmaxDropTest` to `ApplyStability`
using a pitch-tilted `transform.forward` as both its roll-error and
torque axis, which leaks into yaw once pitch is significant (exactly
what a wheelie is); fixed by flattening the axis onto the horizontal
plane. Two real regressions were hit and reverted on the way (removing
the separate `ApplyUprightAssist` stand broke general driving outright;
removing it AND boosting `ApplyStability`'s gains caused a 180-degree
flip) - the working fix keeps `ApplyUprightAssist` fully active
alongside the newly axis-fixed `ApplyStability`. (4) Cornering visual
lean increased (32→40 degrees, cosmetic only) per "the lean while
riding should be a little more." All changes verified via seven real
`Physics.Simulate()`-based `Mini065TmaxDropTest` iterations (final
PASS: settles upright, drives without auto-wheelie, lifts progressively
on hold, drops on release, roll fully recovers - peak transient roll
25 degrees during the most aggressive possible hold, settling to
0.1 degrees within 2s), plus `Mini065TmaxValidation` and
`Mini001SceneValidation` PASS. See the MINI-064/065 round 5 entry in
`PROJECT-HANDOFF.md`.

### MINI-064/065 round 6 — wheelie raised to 80 degrees, faster rise, airborne steering, steep-wheelie roll drift fixed

Goal: fix the user's round-5 feedback ("make it go up alil bit faster
and it goes to the right alittle") and the follow-up request to raise
the cap to 80 degrees with steering while airborne. (1) Rise rate
raised 30->45->60 deg/s across two asks. (2) Cap raised 38->80 degrees.
(3) The rightward drift was a second instance of the same axis-purity
bug already fixed once in round 5's `ApplyStability` - this time
`ApplyWheelie`'s own pitch torque was using the raw (roll-sensitive)
`transform.right`; fixed the same way, by flattening the axis first.
(4) Added real airborne steering (`ApplyWheelieAirControl`) - steering
previously did nothing at all while wheelieing, since the normal yaw
assist requires the front wheel grounded. (5) The "goes left sometimes"
symptom at the new 80-degree cap was measurably worse than round 5's
lower cap (58.7deg peak vs 25-27) - two further fixes were tried and
real-tested before landing on one that worked: trying to "lock" a
heading via world-Y torque made it worse near-vertical (yaw and roll
stop being separate axes that close to 90 degrees pitch); boosting the
existing torque-based roll correction's gain made it dramatically
worse (93.6deg peak, briefly tipped over - classic PD-instability from
an unbounded torque integrator). The fix that actually worked: add the
extra roll authority to the existing Slerp-based `ApplyUprightAssist`
instead, which is bounded to [0,1] per step and geometrically cannot
overshoot. Final measured peak transient roll: 20.6 degrees at the new
80-degree/60deg-per-second wheelie - actually LOWER than round 5's
25-27 degrees at the old, gentler 38-degree cap. Verified via four real
`Mini065TmaxDropTest` (`Physics.Simulate()`) iterations, including the
two reverted regressions, plus `Mini065TmaxValidation` and
`Mini001SceneValidation` PASS. See the MINI-064/065 round 6 entry in
`PROJECT-HANDOFF.md`.

### MINI-064/065 round 7 — real visual-desync fix, plus a live in-Editor wheelie tuner

Goal: fix the round-6 screenshot's rear-wheel misalignment during a
wheelie, and respond to the user's own correct diagnosis that the
automated headless test can't be trusted to catch everything ("your
tests are flawed... put the wheelie controls as sliders into unity and
i will adjust them"). (1) Real bug found and fixed: the cosmetic
cornering-lean rotation shares the same transform as both wheel discs
and the body mesh, and had no awareness of the wheelie's own much
larger physics-real pitch - composing the two doesn't commute and
visibly skews the whole visual body away from the physics. Fixed by
fading the cosmetic lean to zero as the wheelie deepens (no effect on
ordinary riding). (2) Acknowledged a real gap in the test methodology:
`Mini065TmaxDropTest` drives the bike with exact synthetic inputs, not
real `Input.GetAxis` timing/decay - Unity's default keyboard-axis
smoothing plausibly explains both "hardly goes up" (slower real
throttle ramp) and "goes left sometimes" (decayed steering-axis residue
feeding the new airborne-steering torque from round 6, which the
all-or-nothing synthetic test never exercised). Widened that torque's
input deadzone (0.05->0.25) as a reasoned mitigation, and - per the
user's own explicit request - built `TmaxWheelieTuner.cs`, a real-time
in-Editor slider panel (test-scene-only, never touching the real
gameplay prefab) exposing every wheelie/stability tunable plus a live
speed/target-angle/real-pitch/real-roll readout, so the user can tune
by feel in real Play Mode instead of another blind numeric
guess-and-recompile cycle. Verified via full rebuild pipeline plus
`Mini065TmaxDropTest`/`Mini065TmaxValidation`/`Mini001SceneValidation`
all PASS (unchanged numbers from round 6, as expected - the real fixes
here are cosmetic or only engage on inputs the synthetic test can't
produce, so a clean pass confirms no regression, not that the reported
symptoms are gone). See the MINI-064/065 round 7 entry in
`PROJECT-HANDOFF.md`.

### MINI-064/065 round 8 — weight-bias sliders (centre of mass)

Goal: per the user's explicit request ("sliders for the weight of the
bike as well like back, front, left and right"), add live-tunable
centre-of-mass offsets to the wheelie tuner. Implemented as two
offsets from the already-measured baseline COM - front<->back and
left<->right - applied every FixedUpdate so slider drags take effect
immediately in Play Mode, the physically correct way to model a rider
shifting their weight (shifting back genuinely makes a real
motorcycle's front come up easier). Added to `TmaxWheelieTuner.cs` as
two `DirectionalSlider` rows labelled with the user's own named
directions at each end. Verified via full rebuild pipeline plus
`Mini065TmaxDropTest`/`Mini065TmaxValidation`/`Mini001SceneValidation`
all PASS (unchanged from round 7, as expected - both offsets default
to 0). See the MINI-064/065 round 8 entry in `PROJECT-HANDOFF.md`.

### MINI-064/065 round 9 — weight sliders were unreachable, not missing

Goal: fix "where are the weight controls i asked you for." The round-8
weight sliders were genuinely present (confirmed in the scene file
before touching anything) but buried below twelve other sliders in a
fixed-position, non-scrolling panel that had grown taller than most
Game views - not missing, just unreachable. Rewrote the tuner from
manual Rect math to a proper GUILayout scroll view bounded to the
actual screen height, and moved the weight-bias section to the very
top of the panel so it's the first thing visible with zero scrolling.
Pure UI change, no physics/gameplay code touched - verified via full
rebuild pipeline, all three regression checks PASS (unchanged numbers,
as expected), plus a direct grep of the rebuilt scene confirming the
component is actually serialized in. See the MINI-064/065 round 9
entry in `PROJECT-HANDOFF.md`.

### MINI-064/065 round 10 — debug force override, tunable lift authority, pinned live status, precise numeric entry

Goal: fix "the bike does not wheelie even if I play in all the
sliders... maybe we should have invisible holders to bring it up and
hold it upright that can be adjusted" plus "let the bike flip over if
it has to i will fix it with the slider" and "make sure i can see the
sliders in the ui". Found a strong candidate root cause: the wheelie's
own upward torque was clamped to a hardcoded constant (16) with NO
slider anywhere controlling it - no matter how far any other slider was
pushed, the actual applied torque could never exceed that invisible
ceiling. Replaced with tunable `wheelieLiftClamp`/`wheelieRecoverClamp`
sliders (defaults unchanged, so nothing regresses silently). Also
added: a direct "Debug Force" override (`ApplyDebugForcedPitch`) that
bypasses every gate the normal wheelie has, for isolating "can the rig
move at all" from "is the gating wrong"; a pinned, always-visible
status box (outside the scrollable area) showing live speed/eligible-
reason/held/target/real-pitch/real-roll so a SETTING (the angle cap,
always reads whatever it's set to) can't be mistaken for a LIVE
reading again; numeric text-field entry next to every slider for exact
values; and an explanation (not a silent implementation) of why "270
degrees" isn't representable - pitch is measured via asin(forward.y),
mathematically bounded to +-90. Verified via full rebuild pipeline plus
`Mini065TmaxDropTest`/`Mini065TmaxValidation`/`Mini001SceneValidation`
all PASS, unchanged from round 9 (every new capability is opt-in/
defaults-preserving). See the MINI-064/065 round 10 entry in
`PROJECT-HANDOFF.md`.

### MINI-064/065 round 11 — researched real wheelie physics, redesigned to real rear-wheel torque

Goal: fix "the wheelie says the bike goes up 89 but it doesnt actually
go up" and "research online... compare with yours". Real web research
(UNSW Physclips wheelie-physics writeup, multiple Unity Discussions
threads) found: (1) real wheelies are driven by rear-wheel traction
torque acting at ground level, whose lever arm to the centre of mass
is what produces the lift - not an abstract torque applied to the
chassis; (2) multiple independent Unity developers report that naive
chassis AddTorque/AddForceAtPosition "doesn't work as expected" for
this exact problem; (3) the recommended architecture is the OPPOSITE
of what this project was doing - real rear motor torque as the lift
mechanism, with pitch angle only as a governor/limiter, not the
primary lift source. Redesigned accordingly: added
`wheelieRearTorqueBoost`, real extra motorTorque on the rear
WheelCollider (new tuner slider, try-this-first), with the old PD
pitch correction demoted to a secondary assist/governor. Also: (a)
`IsWheelieing` changed from the controller's setpoint to the real
`!frontWheel.isGrounded` signal, per "it should start reading when the
front tire is off the ground"; (b) `BikeMassKg` reduced 280->220,
removing a ~60kg "future rider" padding that was real, unaccounted
weight working against every wheelie attempt, per "do it without the
character's weight in mind". Verified via full rebuild pipeline plus
`Mini065TmaxDropTest`/`Mini065TmaxValidation`/`Mini001SceneValidation`
all PASS - honest regression noted: peak transient roll rose from
~20.6 to 32.2 degrees (still under the test's 35-degree threshold,
fully recovers), a real trade-off of the new physically-grounded
torque, not free. See the MINI-064/065 round 11 entry in
`PROJECT-HANDOFF.md`.

### MINI-064/065 round 12 — simplified to one "invisible hydraulic" slider, after a real caught regression

Goal: respond to "this is stressing me can we just have an invisible
hydraullic to bring the bike up to make it appear like it is
wheelieing. that sounds easy" plus "i hope the bike stays straight and
not turn." First attempt collapsed round 11's multi-part lift system
into one plain symmetric PD actuator - `Mini065TmaxDropTest` caught
this as a real regression before it was reported as done: peak roll
rose to 91.8 degrees (worse than round 10's 20.6 at the same settings),
and a second attempt at lower strength/more damping made it worse
again (103.5, no longer recovering). The asymmetric behaviour removed
in that first attempt - gentle while climbing, much stronger while
correcting overshoot - was real tested safety margin, not incidental
complexity. Fixed by restoring round 10's proven asymmetric shape but
collapsed onto ONE exposed slider (`wheelieHydraulicStrength`) whose
torque ceiling scales directly with the slider value instead of being
a separate fixed number - giving genuine one-slider simplicity without
losing the safety margin. Removed four separate fields/sliders
(rear torque boost, lift clamp, recover clamp, hold strength) down to
one. Verified via three real drop-test iterations (two failed, caught
and reverted; one passing) plus full rebuild pipeline and
`Mini065TmaxValidation`/`Mini001SceneValidation` PASS - final peak
transient roll 8.6 degrees, the best result of any round so far. See
the MINI-064/065 round 12 entry in `PROJECT-HANDOFF.md`.

### MINI-064/065 round 13 — real physical anti-roll "trike" outriggers, after two real regressions caught and fixed

Goal: fix "the bike keeps moving right when wheelieing" per the user's
own suggested design - "two invisible wheels at the back and one
invisible wheel at the front so it will appear as a bike but it will
be a trike only when you press E to wheelie", extended to "should
still stay on while the bike is up." Two real regressions were hit and
caught by the drop test before either was reported as working: (1)
real extra WheelCollider components, toggled via `.enabled` at
runtime, broke the vehicle's basic drop/settle physics even though the
toggle only ever wrote false->false while parked; (2) switching to
moving the WheelColliders' Y position instead of toggling `.enabled`
made no difference - the exact same failure occurred whether parked
3m or 0.5m away, proving the issue wasn't about distance/suspension at
all but WheelCollider's own contribution to Unity's automatic inertia
tensor calculation, independent of ground contact. Fixed by not using
Collider components for this at all: two plain, physics-inert
Transform anchors, with a new raycast + `Rigidbody.AddForceAtPosition`
spring (`ApplyStabilizerSpring`) providing real anti-roll force at a
real contact point only while active - genuine physics without
touching the Rigidbody's own mass/inertia setup. Active whenever the
player wants a wheelie and is eligible, the wheelie target is above
zero, OR the front wheel is genuinely off the ground (the last
condition directly answers "should stay on while the bike is up").
Verified via four real drop-test iterations (two failed and reverted)
plus full rebuild pipeline and
`Mini065TmaxValidation`/`Mini001SceneValidation` PASS - critically,
Phase 1/2 numbers are back to the EXACT proven pre-round-13 baseline,
proving the raycast approach is genuinely inert when not active;
wheelie hold test peak roll 8.6 degrees (matching the best result so
far), fully recovering. See the MINI-064/065 round 13 entry in
`PROJECT-HANDOFF.md`.

### MINI-064/065 round 14 — the wheelie never actually worked, and the test was the reason I didn't know

Root cause of roughly ten rounds of false progress: the drop test
recorded `bike.WheelieAngle`, which returns the controller's own
setpoint - a number the controller assigns to itself every step,
whether or not the Rigidbody moved. It reported "reached 80 degrees,
PASS" while the bike physically pitched ~7 degrees and the front wheel
never left the ground. Every wheelie PASS from round 3 onward was
worthless, and the user's repeated "it doesn't actually go up" was
right every time. Fixed the test first (real `CurrentPitchAngle`, real
front-wheel airborne step count, raycast ground clearance for BOTH the
collider and the visible wheel, plus a pitch-independent roll
measurement replacing another copy of the round-5 axis bug), let it
fail honestly, then fixed the mechanic: `ApplyWheelie` now DIRECTLY
DRIVES the pose (yaw x pitch, no roll term anywhere, so sideways lean
is mathematically impossible) rotating about the rear contact patch so
the front genuinely rises, with the upright/roll stabilisers standing
down while it runs. Two further bugs found by reading the trace rather
than trusting the verdict: an early return froze the target while
airborne, and rear-wheel grounded-chatter flickered eligibility and
stalled pitch at 66.7 degrees (fixed by splitting "can start" from
"can sustain"). Also: wheelie from 1 km/h, extra rear motor torque,
wider outriggers, angle slider to 360. Result on measurements that now
mean something: REAL pitch 90.0 degrees, front wheel airborne 95/100
steps, collider clearance 1.706m, visible wheel clearance 1.699m, roll
0.0 degrees throughout, recovering within 2s. See the MINI-064/065
round 14 entry in `PROJECT-HANDOFF.md`.

### MINI-083 - normalized Sacat/Franki to the same height as every NPC (height-only, by the user's own choice)

User: "I want all characters the same size for the accessories to b the
same placement so in the future swapping shirts etc would be seamless for
the eco system."

Measured every character model first: the entire Floreswa NPC/boss/gang/
police cast is already uniformly 1.85m tall; the two hero models (Sacat
1.9739m, Franki 1.9337m) are a different, taller family that don't even
match each other. Also found the accessory-placement problem has a second,
deeper cause already documented in `CharacterEquipment.cs` (MINI-067) - the
player's and Boss C's rigs bake wildly different BONE scales, not just
overall height, which is why separate `BossChain*` constants and
`NormaliseAccessoryScale()` exist. Presented three real options to the user
(height-only / full rig unification / just formalize the existing per-rig
constants) rather than guessing at scope - user chose **height-only**.
Sacat and Franki are now scaled 0.94/0.96 at instantiation to land on
exactly 1.85m, matching everyone else - verified directly off the rebuilt
scene's renderer bounds. Deliberately left untouched: the mains'
`CharacterController` capsule size and the deeper rig-family bone-scale gap
- both out of the scope the user chose. All seven standing validations/
drop/drive tests re-run PASS. See the MINI-083 entry in
`PROJECT-HANDOFF.md`. Not yet hands-on playtested - nobody has looked at
the ~4-6% shrink yet, and per-character accessory tuning will likely still
be needed for new items (expected, not a regression, given the scope
chosen).

### MINI-082 - shop panels fade shut on walking away instead of needing E, and shrunk

User: "I still have to press E to close the shops box I should just walk
and it fades and make it smaller or more dynamic. like the farmer, clothes,
car dealer etc."

`ShopPanelController.Open()` now takes the opening NPC's transform as an
anchor; walking more than 4.5m away fades the panel shut on its own via a
new `CanvasGroup` (E/Esc still work too, now animated the same way instead
of an instant close). Panel size shrunk 900x640 -> 620x460 across all seven
shop-style panels that share this one controller (Farm Shop, Clothes Shop,
Land and Surveys, Car Dealer, Food Shop, Pharmacy, Black Market resale) - a
true content-sized dynamic panel was not attempted, flagged as a possible
later pass. Verified via a throwaway diagnostic reading every shop panel's
wired size/CanvasGroup/range straight off the rebuilt scene - all 7 correct.
See the MINI-082 entry in `PROJECT-HANDOFF.md`. Not yet hands-on playtested
- the fade timing/distance is a first guess, not seen live.

### MINI-081 - Gardey Zafeh reveal waits for his return, Jadame risk on Guadeloupe sales, drug-mission police intervention, a reusable FollowNpc mechanic, quieter radio

User: give the Gardey Zafeh information only after he comes back from the
trip (was previously handed over the instant he was paid and sent away);
missions should be locked/one-links-to-next (checked - already true, single
active mission, no skip-ahead); add following an NPC walking or driving as
a mission mechanic; police should sometimes intervene on drug-mission
deliveries; Guadeloupe sells should sometimes go bad and return less money
because of Jadame (Guadeloupe police); lower the radio music.

Gardey Zafeh: `TownNPCInteractable` now defers `RevealDogLife()` and the
actual reveal line to the moment he becomes interactable again after his
trip, not the moment of payment - also banners it via `MissionSystem.Alert`
so the player doesn't need to guess he's back. Guadeloupe: `GuadeloupeTrade`
now rolls a 20% chance per completed trip of only keeping half the cargo
value, with a "Jadame" flavor line, on both the NPC-courier and
character-courier paths. Drug missions: mission-tagged illegal sales (the
existing +50 heat bonus, `M9W`/`M10W` only - `M11W` has no sale objective to
hook, flagged not fixed) now also roll a 35% chance of a further +25 heat,
reliably crossing the reinforcement spawn threshold so an officer actually
converges on the player, not just an ambient patrol that happens to be
close. FollowNpc: a new, generic `ObjectiveKind.FollowNpc` tracks sustained
proximity to a live, moving NPC transform (walking or driving) rather than
a fixed point - built and tested in isolation, but deliberately NOT wired
into any mission's content yet, since which NPC/story-beat it belongs to is
a content decision for the user to specify next. Radio: `AudioSource.volume`
on the in-car radio was never set (defaulted to 1.0) - now 0.45, confirmed
on the rebuilt prefab. All seven standing validations/drop/drive tests
re-run PASS (none of this round's changes touch vehicle physics). See the
MINI-081 entry in `PROJECT-HANDOFF.md` for full detail. Not yet hands-on
playtested.

### MINI-080 - chain-purchase visibility bug fixed (system-wide principle), bounce, hide-interaction-while-riding, test-vehicle removal, two missions

User: the manually-placed chain doesn't show up on purchase, fix it and apply
"use what I manually placed and save it" as a system-wide principle; bike
still bounces too much on ramps/bumps, lower it a lot; disable world
interaction while riding a vehicle; remove the parked test bike/Range Rover
and the dev tuning UI until testing resumes (keep the code); add more
missions; also search for the cheapest pillion rider animation (research
only) and note the user's own $4.99 scooter animation lead, deferred to
2026-08-22.

Root cause of the chain bug: `CharacterEquipment.OnEnable()` only subscribed
to `EconomyManager.OnChanged` if `EconomyManager.Instance` already existed at
that exact moment - an unguaranteed Awake/OnEnable ordering race, not a
placement problem. The user's hand-tuned chain constants were confirmed
byte-for-byte unchanged. Fixed with a `_subscribed` guard retried from both
`OnEnable` and `Start` (the latter always runs after every `Awake` in the
scene). The identical bug pattern found and fixed the same way in
`LockedFarmPlot.cs`. Bike bounce: found a second contributor beyond earlier
suspension tuning - the "Trike Stabilizer" spring was firing on ordinary
bumps, not just wheelies; added a 0.12s debounce, and retuned
damping/travel (travel corrected to 0.17 after an initial 0.14 cut broke
`Mini065TmaxValidation`'s ground-clearance check). Test vehicles/tuner
removed behind toggles (`IncludeParkedTestVehicles`, `IncludeDevTuner`),
flip back to `true` to restore. Two validations (`Mini069GangWarValidation`,
`Mini067ChainValidation`) updated to match the deliberate removal rather
than reverted against it. M17/M18 missions added. All seven standing
validations/drop/drive tests re-run PASS after these changes. See the
MINI-080 entry in `PROJECT-HANDOFF.md` for full detail. Not yet hands-on
playtested - the chain fix especially still needs a human to buy a chain
and look.

### MINI-079 - pillion clip trim, bike/rider lean unification, "side character wont board" bug fixed

User: use the latter part of cheer2 for the pillion (skip the cheering
lead-in, fix his head being too far back); the leaning animation should
freeze together since bodies come off the bike at full lean and the bike
leans more than the riders; the side character still wont get into the
test vehicle by the safehouse. Added startTimeSeconds to
HumanoidAnimationManager.BeginSustainedAction, threaded through
VehicleSeat/VehicleRider, pillion seat now starts 3.6s into the 6.00s
cheer02_Loop clip. Real cause of the lean mismatch: riders were capped
to 15deg earlier this session but the BIKE BODY's own maxVisualLean was
left at 42.36 - an oversight, since the user's original sentence named
the bike too. Capped to 15 as well, written explicitly (same serialized-
prefab trap as before), confirmed by reading it back off the rebuilt
prefab. The board-bug was real and found by comparison with a WORKING
sibling feature: TryBoardOtherMainCharacter checked
PlayerController.IsControlled, which CharacterSwitchManager sets false
for ANY inactive character regardless of whether they're riding
anything - unconditionally blocking the feature. BikeInteractable's own
BoardPillion (which boards this same character onto the bike, and
works) checks VehicleRider.IsMounted instead - mirrored that. Also
caught and removed a second bug in the same area before it shipped:
DropOtherMain was setting IsControlled=true on the still-inactive
character, which would have made two characters read keyboard input at
once. Full rebuild pipeline run, all clean, all five validations plus
drop/drive tests PASS. Not yet seen in Play Mode - the 3.6s clip offset
is a best estimate against documented clip length, not frame-measured.
See the MINI-079 entry in PROJECT-HANDOFF.md.

### MINI-078 - found the real cause of "not hearing the music"; radio announces the song

User: "not hearing the music as well when i press M should show the song
playing as well." Found a genuine structural bug, not a tuning miss:
Mini071RoverVehiclePrep added CarRadioController (which carries
[RequireComponent(typeof(CarInteractable))]) BEFORE adding CarInteractable
itself. Unity's RequireComponent auto-adds a missing dependency THE
INSTANT it is needed, synchronously - so a blank, default CarInteractable
got created right then. The real, fully-wired CarInteractable added
moments later became a SECOND instance on the same object, silently
allowed since CarInteractable carried no [DisallowMultipleComponent].
CarRadioController.Awake()'s GetComponent grabbed whichever instance came
first - the blank one, whose HasDriver can never become true since
nothing ever calls Enter() on it - so pressing M never even reached the
toggle logic. Fixed by reordering (CarInteractable now added and wired
first), explicitly wiring the radio's car field via SerializedObject
rather than relying on GetComponent alone, and adding
[DisallowMultipleComponent] to CarInteractable so this whole CLASS of bug
fails loudly in future instead of silently duplicating a mount/drive/
passenger system again. Verified concretely: a throwaway script counted
GetComponents<CarInteractable>() on the rebuilt prefab - confirmed 1, was
silently 2 before. Radio now also announces itself via the existing
mission Alert banner - "RADIO ON / [song]" and "RADIO OFF" - rather than
a new UI element. All standing validations and drop/drive tests still
PASS. Not yet confirmed by a human pressing M and hearing it, but the
software-side wiring is now concretely verified correct. See the MINI-078
entry in PROJECT-HANDOFF.md.

### MINI-077 - lean sync, pillion hand placement, wheelie min speed, suspension, Range Rover AWD, car boarding fallback

Large batch. Pillion now mirrors the DRIVER's own counter-pitch/roll
treatment verbatim (identical numbers at the identical instant = "bend in
sync" per the user, and matches their own diagnosis that both riders
share one mocap pack) - fixes "leaning way too back" during a wheelie,
which he never had any correction for before. Pillion hand IK targets
moved from his own hip out to the driver's waist depth, a static
repositioning (not bone-tracking) so his hands read as reaching the
driver's body. Wheelie min speed 1->8km/h, written explicitly via
SerializedObject in WireController since a changed C# default does not
touch an already-serialized prefab (same trap hit twice before this
session). Bike suspension retuned for hard IMPACTS specifically (less
travel, more damping, added wheel rotational damping) - distinct from an
earlier fix that only handled settling at rest. Range Rover switched to
AWD (the honest fix - a real Range Rover is 4WD - and the effective one,
roughly doubling available traction on a grade) plus more torque;
verified with real numbers, not assumed: 84.68m/5s at 120.6km/h versus
58.4m/~83km/h before, same drive test. CarInteractable now boards the
OTHER main character too (polled continuously while driving, since the
user's own wording described an ongoing condition, not a one-time check
at entry) - caught and fixed a real seat-double-booking risk along the
way by switching occupancy checks from a recruit-only list to a
transform childCount check. If the car is full, the other character
auto-mounts the bike (as driver, if in range) via a new public
BikeInteractable.TryMountFor() - explicitly NOT autonomous AI
follow-pathing, which does not exist in this project; the player Tab-
switches and rides it themselves. Full rebuild pipeline run in order, all
clean. Drop test PASS (85.9deg real pitch), drive test PASS, all five
standing validations PASS. Nothing in this batch has been seen in Play
Mode. See the MINI-077 entry in PROJECT-HANDOFF.md.

### MINI-076 - fixed a real "cannot mount either vehicle" bug

User reported neither main character could get on the TNAX or the Range
Rover. Traced by measuring the actual built scene (not guessing): the
parked bike and the parked, driveable Rover added this session sit only
5.6m apart, and mountRange(3.2)+doorRange(2.6)=5.8m meant their mount
zones genuinely overlapped in a narrow strip - one F press there could
fire both BikeInteractable and CarInteractable mounting on the same
character in the same frame, leaving neither visibly working. Fixed two
ways: a general IsControlled mutual-exclusion guard in both
TryMountByKey()/TryEnter() (structurally correct regardless of vehicle
spacing, not just a patch for this one overlap), and moved the parked
Rover from x=-3.4 to x=-9.0, re-measured at 11.20m separation, safely
past the 5.8m sum. All five regression validations PASS. Not yet
confirmed by an actual human pressing F in a running game - this is a
real, measured bug and a principled fix, but still unproven in Play
Mode. See the MINI-076 entry in PROJECT-HANDOFF.md.

### MINI-075 - car rider visibility workaround

User played the build: driver sits ON TOP of the Range Rover instead of
inside, and passengers were not visibly boarding either. Instructed a
pragmatic temporary fix rather than a real one right now: "make the
characters disappear by the vehicle... illusion that they are in it for
now", explicitly "dont destroy the system just leave it for later days",
and "dont forget the recruits as well". Added hideRidersInsteadOfPosing
(default true) to CarInteractable - disables every Renderer under a
character (not the GameObject itself, so animation/IK components keep
running harmlessly) right after they are parented into a seat, restores
them right before they leave. Driver and passengers both covered; since
recruits board through the same FillEmptySeats() method whether boarding
at entry or summoned by phone mid-drive, one change covers both paths.
The underlying seat-position bug itself was NOT investigated or touched,
per explicit instruction - the real seating/parenting/IK system from
MINI-071/073 is untouched underneath, and the toggle is written so
flipping real seating back on later is a one-line change. All five
regression validations PASS. Not yet seen in Play Mode. See the MINI-075
entry in PROJECT-HANDOFF.md.

### MINI-074 - in-car radio + a real mission tail (M12-M16)

Follow-up to MINI-073, same session. Added a licensed in-car radio to the
Range Rover: the user's own track (found in their Downloads, verified it
actually imported with real audio data - 2:57, stereo, 44.1kHz - not just
copied and assumed), toggled with M, driver-only, 2D/non-spatial, force-
stopped the instant nobody is driving so it can never keep looping in the
world after exit. Then, since budget allowed, added the mission tail that
MINI-073 had scoped but not written: M12 "Round the Village" (Normy + Food
+ Pharmacy shopkeepers), M13 "Ital and Elders" (Rasta), M14 "Ason Ki Move"
(the Boat Man, now reachable by BOTH career paths, not just WeedRoute),
M15 "Not Ah Word" (the gang recruiter), M16 "War Story" (the hardest -
walks the player into Dog Life's block, where FactionBrawler's own
proximity fight takes over, then EscapeHeat as the comedown). All five use
existing ObjectiveKind values only, appended as Undecided-required so both
paths reach them. Caught and fixed a real bug before shipping: the first
draft pointed Normy's marker at a generic patrol spot and guessed at
Food/Pharmacy's road indices - checked against their actual BuildNpc
positions (4/5/11) and corrected. All five regression validations PASS.
Scene rebuilds clean. Second EXE of the day built with both included. Still
not done: the ProgressionManager unlock-sequence redesign, a real
"defeat the gang" objective kind (M16 uses a ReachArea proxy), the estate
garage as a real vehicle home point, and a dedicated validation for the
new mission content itself. See the MINI-074 entry in PROJECT-HANDOFF.md.

### MINI-073 - large mixed batch (PARTIAL - see PROJECT-HANDOFF.md for full detail)

One big multi-part request under a tightening token budget, deliberately
triaged (with the user's explicit permission) rather than left half-done
everywhere. DONE and validated: interaction feedback box shrunk and now
fades by WALKED DISTANCE rather than a flat timer (corrected mid-task
after the user clarified "fade as walk motion is done"); mission
objective card auto-grows to fit its text instead of truncating (a
wiring gap was caught and fixed within the same pass, not shipped
broken); gang roster renamed Chevy->Zoomy, Reds->Deluxe, Ju->Draco,
Skeng->Rio (two stale name references in Mini058FactionsValidation
caught and fixed); Rasta Mentor renamed "Rasta" with a green "liberation
shirt"; the other main character auto-boards the bike's pillion seat
(wiring from MINI-066 that existed but was never exercised); recruited
gang members auto-board the Range Rover's three passenger seats, and
also board mid-drive if summoned by phone while the player is already
driving; a new two-story "Lalay Estate" with a garage sized to the real
Range Rover's measured footprint, $8,500, reusing the same
SafehouseInteractable rest features as the farm safehouse, verified
clear of neighbouring buildings by an actual distance check; Food and
Pharmacy items now stash in a proper inventory instead of being used
instantly on purchase, with a new category-tabbed Inventory panel (I key)
matching the user's own "main button, suboptions, weapons later" UX
spec - the same real heal/stamina effects as before, just deferred to
when the player actually uses the item. All five listed validations
PASS after this pass; scene rebuilds clean. NOT DONE, not started at
all: missions for Normy/Boatman/Not-Ah-Word/Rasta/shopkeepers; the
unlock-sequence redesign; wiring the estate garage as an actual vehicle
home/respawn point. See the MINI-073 entry in PROJECT-HANDOFF.md for the
full breakdown and a concrete suggested approach for the mission work.

### MINI-071 / MINI-072 - driveable Range Rover, bike camera and lean

Made the Range Rover driveable at the user's chosen "minimal" scope, sold
at the real 2026 base MSRP ($113,300) as "Range Rova", with the bike
renamed "TNAX 560" - display names only, ids unchanged so no save loses a
purchase. Wrote CarController/CarInteractable fresh rather than
generalising the bike, which carries wheelie/lean/IK machinery a car has
no use for and whose tuning took three tasks to settle; the shared layer
is a later refactor toward a known shape. Car entry uses the DoorSeated
style added speculatively back in MINI-066 - you must be at the driver's
door, not just near the car. Physics rig placed from measured bounds:
wheelbase 3.07m vs a real 2.997m. Two real faults caught by testing: the
first drive test failed honestly at 0.00m travelled because Edit Mode
never calls Awake/FixedUpdate, and lowering the centre of mass then
changed nothing because a serialized prefab keeps the value it was built
with - the retest returned bit-identical numbers, which is a far better
smell than a number merely looking wrong. Rebuilding the prefab cut
corner lean from 39.6 to 33.9 degrees. MINI-072: camera orbit is locked
while riding (panning was not just unwanted, it left the camera pointing
off into the world while the bike drove out of shot) and rider lean is
clamped to 15 degrees - a clamp, not a smaller follow factor, so ordinary
cornering still tracks one-for-one. Drive test PASS, bike drop test PASS,
three validations PASS. Visible wheels do not spin: the source is one
merged mesh. See the MINI-071/072 entry in PROJECT-HANDOFF.md.

### MINI-070 - Boss C's black SUV becomes a real Range Rover

Replaced the box-and-cylinder placeholder outside Boss C with the user's
downloaded Range Rover. Third asset in a row to arrive unusable the same
way - 1,984,875 triangles and two 8192x8192 textures, 62MB - so the same
treatment: weld, decimate and resize in headless Blender before Unity
sees it, down to 20k tris / 2048 maps / 5.6MB. More generous than the
chain's 8k/1024 because a car fills far more screen. The prefab builder
measures rather than assumes: the model arrives unit-normalised so scale
is derived from real Range Rover length, and WHICH axis counts as length
is derived too, since the glTF Z-up to Y-up conversion decides whether
it lands on X or Z and guessing wrong would scale the car by its width.
Origin sits at the tyres so placement needs no height fudge, and it gets
one BoxCollider rather than a 20k-tri mesh collider. Also dropped a
placeholder quirk: the old stand-in faced `right` while building its
body along local X, i.e. modelled sideways relative to its own forward.
Checked by rendering it in the built scene - correct scale beside the
characters, on its wheels, clear of the building. Validation PASS plus
four regression validations PASS. It is set dressing, not driveable.
See the MINI-070 entry in PROJECT-HANDOFF.md.

### MINI-069 - Not Ah Word vs Dog Life, phone regroup, bike controls

Added FactionBrawler: one component driving BOTH gangs via a Side enum,
so "doglife should fight back as well" is the same code with the side
flipped rather than a second implementation. Reuses the existing punch
animation per the user. Gated on ProgressionManager.DogLifeRevealed,
which the Gardey Zafeh reading already sets - no new progression state
invented. The character the player is driving is skipped, since he
punches manually with F. Chase uses two ranges on purpose (engage 8m,
pursue 22m) - with one range a chase ends the moment the target steps
back. Both sides stand down once the enemy is down to one man standing.
Kept separate from CompanionCombatAssist, which is about police and adds
full heat per swing - a gang scrap should not call the police on you.
Controls: wheelie R->E, mount/dismount F, and the look keys deleted
rather than rebound since nothing read them. F is also the punch key, so
mounting needed a one-frame guard or you swing every time you get on.
Caught a real trap: changing a C# field default does NOT change an
existing prefab, so the bike kept R until the scheme was written onto
the prefab explicitly. Phone call moved C->Q and now summons recruits
too, forcing them back to Follow so a guard does not walk straight back
to his post. Chevy moved beside the recruiter. Validation PASS plus four
regression validations PASS. The brawl itself is unverified - Play Mode
does not tick in batch. See the MINI-069 entry in PROJECT-HANDOFF.md.

### MINI-067 / MINI-068 — "Gucci Law" 18k chain, and the bike's home spot

Brought the user's own gold chain model in and put it on the player and
Boss C, priced 6000, renamed "Gucci Law" (item id stays chain_gold so
existing saves keep the purchase). The source was NOT game-ready:
1,995,768 triangles and two 8192x8192 textures, ~60MB, for a necklace -
welded, decimated to 8k tris and resized to 1024 maps in headless
Blender first, 60MB down to 2.0MB, with a validation check on the
triangle budget so a re-export that skips that step fails loudly rather
than quietly shipping a two-million-poly chain. Hit the bone-axis trap
again: placement was in chest-bone local space and AccessorySwing
copied the bone's rotation outright, but this rig's bone rest
orientations are not world-aligned - already documented on Boss C's
necklace, and a symmetric ring of spheres hid it where a real model
does not. Both now place in the CHARACTER's frame. Also fixed three
faults in my own render harness that each looked like a placement bug:
new renderers are absent from the culling data (so Camera.Render skips
them), the camera aimed 0.2m above the actual chest bone, and the
companion stands close enough that a chest-height camera sits inside
her. For MINI-068 the bike now returns to a BikeHomePoint outside the
farm safehouse on every load - deliberately reversing the
save-last-position behaviour from two tasks ago, at the user's request,
with the saved fields still written so it can be restored - and buying
reuses the bike already in the world instead of spawning a second one.
Validation PASS. The user then fitted the chain by hand (faster than my
render sweeps) and it now looks right on both characters; the player's
numbers are baked (forward 0.044, up 0.383, side -0.032, tilt -25.6).
Hand-placing surfaced two more bugs: bone scale is inherited so one
prefab came out 0.308m on the player and 0.068m on Boss C, and the fix
for that was itself wrong at first because it measured the world-aligned
Renderer.bounds X extent, which on a rotated chain is its thickness -
now measured from the mesh's local bounds instead. Boss C turned out to
be on a different rig entirely (Blender metarig vs mixamorig), so he
gets his own constants (forward 0.195, up 0.356, tilt -6.1), read back
off his hand placement and baked, then proven by re-running the scene
build so his chain regenerates from the constants rather than living
only in the saved scene. Closed. See the MINI-067/MINI-068 entry in
PROJECT-HANDOFF.md.

### MINI-066 follow-up 5 — wheelie pose as a partial blend

The user could not get the rider's angle to sync with the bike: "the
character angles further back when wheelieing". Diagnosed as a
structural conflict rather than a tuning shortfall - the pack's wheelie
clip (cheer01) is bolt upright, measured at +1.7 degrees torso vs -59.8
for riding, so playing it outright imposes a ~61 degree back-lean that
is authored into the clip and answers to no numeric offset. The angle
had two owners and only one was tunable. Fixed by adding a second
full-body override layer, FullBodyBlend, stacked above
FullBodyOverride: the riding pose is now held at full weight right
through the wheelie and the wheelie pose plays on the layer above at an
adjustable weight, so the weight is a genuine ride-pose -> wheelie-pose
blend and the bike's live angle keeps authority. Note that lowering the
weight of FullBodyOverride itself does NOT work - poses crossfade
within that layer, so it blends towards the base standing idle instead,
putting the rider's feet off the pegs. New "WHEELIE pose blend" slider
(default 0.45), included in the PRINT dump, ramped per-frame so it
retunes mid-wheelie without restarting the clip. Also replaced a false
doc claim: BikeRiderAnimation said Mini066RiderValidation cross-checked
its pose ids, but no such file existed - that check now genuinely
exists in Mini065TmaxValidation. Verified: validation PASS including
the new layer/state checks (confirmed non-vacuous against the
controller asset), drop test PASS (90.0 degrees real pitch, 1.962m
front clearance, 1.7 degree roll recovered). No EXE. The blend default
is an estimate for the user to dial, and the visual result still needs
their eyes on a running frame. See the MINI-066 follow-up 5 entry in
PROJECT-HANDOFF.md.

### MINI-066 follow-up 4 — tuned seating baked, stop/rest/pull-away, 1.15x bike, EXE

Baked the user's own Play-Mode-tuned rider seating as defaults
(seated 0.11/-0.01/0.32, wheelie 0/-0.40/0.28, pitch 22). Added the
stop sequence they read off the pack's preview video, using its own
"<from>_<to>" transition clips: coming to rest plays
MOTOIdle02->MOTOIdle01, sitting still ~2.5s plays MOTOIdle01->Idle
(non-looping, so it holds its last frame), pulling away plays
MOTOIdle01->MOTOIdle02 - all fired on moving/stopped EDGES, since
holding a transition clip would freeze the rider mid-move. Scaled the
bike 1.15x on the prefab root (mesh + colliders + markers together)
while compensating the rider's parent scale so he keeps the size the
game already has him. Measured both characters rather than guessing -
Sacat 1.827m vs Franki 1.767m, only 3.3% apart, left distinct since IK
adapts reach and Franki being stockier is a story trait. Also fixed a
real process failure: a file edit of mine recompiled Unity and wiped
the user's live tuned values (Unity reimports on timestamp, not
content), so the tuner now has a PRINT-to-Console button whose output
survives Stop. Verified: 3 new states baked, no missing clips, values
confirmed in-scene, bike root scale 1.15, drop test PASS (90deg real
pitch, 95/100 airborne, 1.7deg roll), both validations PASS. Windows
EXE built. See the MINI-066 follow-up 4 entry in PROJECT-HANDOFF.md.

### MINI-066 — rider on the bike: purchased mocap, runtime IK, seats, test rig

Goal: get a character riding the TMAX with real animations, plus a
pillion later. Research first found a hard blocker - Mixamo has NO
motorcycle riding animations - so the user bought the Animo Mocap pack
($4.99, 30 clips). All 6 FBX shipped Generic and had to be reimported
as Humanoid (verified: 6/6 valid avatars, 30/30 retargetable) before
they could touch this project's Humanoid characters. Clip
identification came from the USER watching the vendor's preview video,
not from filenames, which are misleading - `cheer01` is actually the
WHEELIE. Every point they gave was then confirmed numerically by
measuring torso angles out of the FBX skeletons in Blender (riding
-59.8deg, stopped -38.8deg, wheelie +1.7deg, leans +/-6.6deg
symmetric). Built: `VehicleSeat` (with EntryStyle, because the user
flagged bike vs car entry differ), `VehicleRider` (runtime TwoBoneIK
from the character's own Humanoid avatar), `BikeInteractable`,
`BikeRiderAnimation`, driver + pillion seats, 8 baked riding states,
and a test rig that auto-mounts a real character in
`TMAX_Physics_Test` (previously the bike only existed after buying it,
making the rider impossible to iterate on). Controls per the user: F
on/off, A/D lean, R wheelie, Q/E look. Camera gained sticky mouse
orbit that deliberately never auto-returns, for screenshots. Four real
Play Mode bugs then found from the user's screenshots and fixed: rider
floating 1.6m up (measured cause - the seated clip already carries its
hips 0.846m above its root, and the anchor was ALSO at seat height),
riding crouch too hard (seat now pitches the rider back 18deg), front
wheel disc visible, and two overlay labels drawing on top of each
other. Physics unaffected: drop test PASS (90deg real pitch, 95/100
airborne steps, 2.1deg roll), both validations PASS. Not yet done:
transition-clip sequencing (leans/wheelie currently snap), and the
pillion is built but unridden. See the MINI-066 entry in
`PROJECT-HANDOFF.md`.

### MINI-064/065 bugfix round 3 — real root cause found: source scan rotated, mis-scaled and tilted

Goal: fix why the bike still wasn't drivable after two rounds. Root cause
was never the physics - it was the source asset. The scan is yaw-rotated
~39 degrees (so every dimension I'd "measured" came from a diagonal
object's bounding box and was wrong, including the scale), pitched
nose-down ~6.7 degrees (only the front tyre ever touched the ground), and
its pivot offset had been baked into the export. A pitch-levelling step
existed but silently did nothing because the airborne rear tyre fell
outside its search band. Fixed by rewriting the Blender pass with
minimum-area-rectangle alignment, binned per-tyre levelling, correct
scaling from the true length, and hard self-checks that raise instead of
shipping silently; and by rewriting the Unity prefab builder to MEASURE
the mesh (wheel positions, radius, facing direction) instead of
hardcoding spec constants. Process lesson: I had been running Unity with
`-nographics`, which is why renders were blank - Blender renders headless
fine here and diagnosed in minutes what three blind rounds had not. The
Blender pipeline now lives in `Tools/TmaxAssetPipeline/` with a README.
Verified by rendered image for the first time (bike upright, level, both
tyres down, axle rings concentric on the real wheels) plus
`Mini065TmaxValidation` (PASS) and `Mini001SceneValidation` (PASS).
Driving feel remains untested - Play Mode still doesn't run in batch mode.
See the MINI-064/065 bugfix round 3 entry in `PROJECT-HANDOFF.md`.

### MINI-064/065 bugfix round 2 — magenta "missing shader" wheels + still bumpy/sliding

Goal: fix what the user's real screenshots showed after round 1 - solid
magenta blobs on the wheels (Unity's missing-shader colour) and the
bike still "bumping"/"not staying on the road." Root cause was two
compounding material bugs (no shader fallback, then a throwaway
in-memory Material that didn't survive the prefab's own reload/re-save
step) plus under-damped suspension/low sideways friction from the
previous round's fix. Fixed by making the wheel-disc material a real
persisted asset (matching the project's own proven `GetOrCreateMaterial`
pattern) and re-tuning suspension/friction. Added a genuine automated
missing-shader check to the validation harness - it actually caught the
second material bug before the user would have seen it again. Verified
via `Mini065TmaxValidation` (PASS) and `Mini001SceneValidation` (PASS).
Directly answered the user's "don't you have a delayed-screenshot
system" question: no working one exists in this environment (Play Mode
doesn't run in batch mode; static rendering has been broken all
session) - the new shader check is a precise substitute for that one
failure class, not a general screenshot capability. See the MINI-064/065
bugfix round 2 entry in `PROJECT-HANDOFF.md`.

### MINI-064/065 bugfix — bike free-falling/spinning on first Play Mode test

Goal: fix a real bug the user caught on their first actual Play Mode
test - "spinning in the air like crazy... not starting off steady on
the road." Root cause: the test scene spawned the bike at a hardcoded,
unmeasured height that sat above the real road surface, combined with
too little WheelCollider suspension travel to reach it - the bike
free-fell with zero wheel-ground contact, and the wheelie's pitch
torque (which had no "grounded" gate, unlike the existing roll
stability code) kept firing on the airborne body every FixedUpdate.
Fixed: suspension travel increased, the wheelie torque now skips
entirely while fully airborne (matching the existing roll-stability
pattern) plus a hard magnitude clamp as insurance, and the test scene
now places the bike via a real `Physics.Raycast` onto the measured
road instead of a guessed constant (which itself needed a
`Physics.SyncTransforms()` fix, since Edit Mode doesn't auto-sync
physics state for colliders created moments earlier). New: the
validation harness gained an actual geometric ground-clearance check
(would have caught this before ever calling it done) since headless
rendering remains broken in this environment. Verified via the
extended `Mini065TmaxValidation` harness (PASS) and the MINI-001
regression check (PASS). See the MINI-064/065 bugfix entry in
`PROJECT-HANDOFF.md`.

### MINI-064 + MINI-065 — TMAX 560 asset prep + arcade bike physics prototype

Goal: the roadmap's TMAX bike tasks, done together per the user's
request for "the most efficient hybrid arcade-bike method." The real
`Assets/Tmax 560.glb` turned out to be a ~2M-triangle, 2353-fragment
photogrammetry-style scan with no separable wheel geometry - decimated
to 15k tris and real-world scaled to the real TMAX's 2.195m length via
a headless Blender pass (original GLB untouched; cleaned mesh exported
as a new file). Assembled into one `TMAX_560.prefab` with the exact
hierarchy specified (WheelColliders never parented under the cosmetic
VisualLeanRoot - lean is purely visual, the physics root stays close
to upright). `TmaxBikeController`/`TmaxWheelVisuals` adapted from the
user's own reference architecture (decoupled `SetInput(throttle,
steer, brake)`); the downloaded MIT `UPIZUP_Motorcycle_Controller` was
reviewed and explicitly REFERENCE-ONLY'd, not adopted (single front
wheel, Input coupled to physics, rotates the Rigidbody itself for
lean - exactly what this architecture avoids). Two items originally
scoped OUT of this pass were folded back in per the user's explicit,
repeated mid-task override: a wheelie mechanic (progressive pitch,
capped angle, reduced steering, smooth recovery - rider-pose reaction
still deferred, no rider exists) and a minimal buy-it-and-it-spawns-
next-to-you purchase hook on the Car Dealer (not MINI-068's full
vehicle-ownership architecture, a deliberate stopgap). Built and
verified by Claude on 2026-08-18 via a new structural validation
harness (`Mini065TmaxValidation`, PASS) and the MINI-001 regression
check (PASS) - actual FixedUpdate physics behaviour (acceleration,
lean, wheelie, stability, recovery) is NOT exercised, since Play Mode
doesn't tick in this project's batch-mode environment; an isolated
`TMAX_Physics_Test.unity` scene exists specifically for the user's own
first real test. Headless visual rendering failed again this session
(blank frame / segfault, same environment-wide issue on record from
the MINI-060 follow-up-2 entry) - no snapshot image exists for this
task. See the MINI-064+065 entry in `PROJECT-HANDOFF.md`.

### MINI-062 — Lalay house (ownership, rest, save, vitals recovery, selectable respawn)

Goal: the roadmap's Lalay house task, done next per the user's explicit
reordering ("do mini-062 next put mini-061 and mini-063 right down in
the pecking order"). Ownership/rest/save/vitals-recovery already
existed (`SafehouseInteractable`, MINI-013/MINI-042); the genuinely new
piece was **selectable respawn** - a `[4] Set Respawn` bed-menu action
that makes a house (once owned) where death/out-of-bounds respawns
actually send the player, replacing the previous single hardcoded
farm-only spawn point. Round-trips through save/load. Also fixed two
small pre-existing bugs found along the way: a terrain-resample Y-drift
that made "select the farm as respawn" not quite a no-op, and the
`prop_safehouse` deed being mislabelled "Montine Safehouse Deed" when
it has always granted the Lalay House (renamed to "Lalay House Deed").
Built and verified by Claude on 2026-08-18 via a new validation harness
(`Mini062SafehouseValidation`, PASS) plus the MINI-001 regression check
(PASS). No EXE build this round, per the user's explicit hold-off. See
the MINI-062 entry in `PROJECT-HANDOFF.md`.

### MINI-060 follow-up-3 — Cheat code refinement (stamina, toggle-off)

Goal: the "000000" cheat now also pins stamina at 100 (reuses the same
`GlobalUnlimitedStamina` flag MINI-049's cheat already sets), and is a
toggle rather than one-shot - pressing "000000" again turns
invincibility/full-stamina/the heat lock back off via a new
`EconomyManager.UnlockHeat()`. Money already granted and the 600s
already skipped don't reverse (no sensible undo). Compile-checked only
by Claude on 2026-08-18 - no scene rebuild or EXE build, per the
user's explicit instruction to hold off on building. See the MINI-060
follow-up-3 entry in `PROJECT-HANDOFF.md`.

### MINI-060 follow-up-2 — Follow revert, boat man return-bug fix, recruiter reposition/pricing, cheat code

Goal (from user feedback on the second build): companion/gang-member
following reverted from MINI-052's NavMesh-based steering back to the
old direct-line steering (glitching near a hill); the boat man's 500s
disappear timer never brought him back (real coroutine-death bug, now
fixed by no longer `SetActive(false)`-ing the coroutine's own
GameObject); the gang recruiter moved from Dog Life's block to Boss
C/Chevy's cluster and repriced $150→$2000/member; the Gardey Zafeh
reveal repriced $1000→$3000; and a new "000000" (six "0" presses)
cheat code for invincibility, $100,000, heat locked at 0, and a 600s
skip applied to the boat man's away-timer and any in-progress
Guadeloupe trip. Built and verified by Claude on 2026-08-18 via three
passing validation harnesses (`Mini058FactionsValidation`,
`Mini060GardeyZafehValidation`, `Mini001SceneValidation`) plus a
numeric position check confirming the recruiter/Chevy/Boss-C
clustering (visual snapshot rendering crashed environment-wide this
run - confirmed not a regression by re-crashing a known-good prior
snapshot the same way). See the MINI-060 follow-up-2 entry in
`PROJECT-HANDOFF.md`. Windows build succeeded clean. Not yet hands-on
playtested.

### MINI-060 follow-up — Boat Man consolidation + explicit E/R choice

Goal (from user feedback on the first build): one NPC on the jetty, not
two - fold Gardey Zafeh's dialogue into the original boat man, whose job
is "just to provide transportation to Guadeloupe," disappearing for 500s
after either service. Refined further to require an explicit player
choice (not an automatic pick) between the two services. Removed
`NPC_GardeyZafeh`; `E` on the boat man is the unchanged produce trade,
`R` is Gardey Zafeh (`TownNPCInteractable.InteractGardeyZafeh`/
`GardeyZafehLabel`, mirroring `FarmPlot`'s existing `[E] Harvest`/`[R]
Clone` dual-prompt pattern exactly). Either choice starts a
`DisappearForTrip` coroutine - he `SetActive(false)`s for ~500s (after a
short delay so the feedback text isn't hidden before it can be read),
deregistering from interaction like any other pooled NPC. Built and
verified by Claude on 2026-08-17 via a rewritten validation harness
(confirms the old NPC is truly gone, the R-path logic, and that a second
reading is refused) - see the MINI-060 follow-up entry in
`PROJECT-HANDOFF.md`. The coroutine's actual timed disappearance isn't
testable outside Play Mode (same known limitation as
PoliceReinforcementSpawner). Second Windows build produced this session.

### MINI-060 — Gardey Zafeh reveal

Goal: use a Guadeloupe character (undefined in the brief - confirmed with
the user directly) to reveal Dog Life as the real plantation thief, plus
give her a repeatable, random "reading" system. User's full spec: a
character reached via the boat man who reveals Dog Life for $1000 (3x
normal EC price), then offers free random 10-minute buffs on return
visits - energy boost (stamina locked at 100), health boost (health
locked at 100), police immunity, or double money - plus a recurring
"you'll be rich, watch jalousie people" line. Built `NPC_GardeyZafeh` at
the jetty (no separate Guadeloupe map exists, per D-007), a new
`GardeyZafehBuffState` static holder (one active buff at a time, same
idiom as the MINI-049 cheat code's global flags), and wired all four
buffs into the real systems they claim to affect (CharacterVitals,
PoliceOfficer, EconomyManager) rather than cosmetic flags. The reveal
also updates MINI-059's theft notification and MINI-058's Dog Life
dialogue to actually name them once triggered. Built and verified by
Claude on 2026-08-17 via a validation harness (passed first run) - see
the MINI-060 entry in `PROJECT-HANDOFF.md`. User requested and received a
Windows build this pass (succeeded, ran clean headlessly) - the roadmap
from MINI-051 through MINI-060 is now fully built. Known gap: she's
modeled on a male body (the character pack has no female model) despite
being written as "she/her" - flagged for a follow-up asset.

### MINI-059 — Plantation guarding/theft mystery

Goal: at low reputation, unattended Zeb can be stolen (volehed) - risk
begins after the player has been away, isn't guaranteed every time,
scales with reputation, and an assigned guard reduces/prevents it. Do not
reveal Dog Life yet - just create suspicion. Built `PlantationTheftController`
(away-timer with a real reset-when-home behaviour, a periodic probability
roll scaled by GrandBayGangs reputation via `ComputeChance()`, and a
Not Ah Word member on GuardPlantation duty from MINI-058 fully preventing
it) plus `FarmPlot.HasStealableZeb`/`Voleh()` for the actual steal-and-
reset. No faction is named anywhere in the theft notification text -
purely "somebody volehed your crop," matching the brief's own "create
suspicion first" instruction. Built and verified by Claude on 2026-08-17
via a validation harness that caught two real test-construction bugs
(a reputation-delta miscalculation, and the established Awake/OnEnable-
outside-Play-Mode limitation) before passing cleanly - see the MINI-059
entry in `PROJECT-HANDOFF.md`. Not yet hands-on playtested. No EXE build
made.

### MINI-058 — Factions

Goal: player gang Not Ah Word (recruit up to 4, assignments: follow/guard
plantation/stay at home/unavailable-injured/later errands) and rival gang
Dog Life (controls Lalay initially, pooled/distance-activated rather than
fully active across the map, "roughly ten" members' worth of
architecture). Built a pooled 4-member Not Ah Word roster
(`GangMemberController`/`GangMemberInteractable`, forked from
FollowController with real MINI-038 combat so "Unavailable/injured" can
really happen) and a `RivalGangSpawner` for Dog Life's distance-pooled
presence near Lalay (red-tinted, territorial dialogue only - no faction
named, per MINI-059's own "don't reveal them yet" instruction). "Later
errands" deferred exactly as the brief itself defers it; only 4 of "roughly
ten" Dog Life members authored (the pooling architecture scales, more
characters is just more pool entries). Built and verified by Claude on
2026-08-17 via a validation harness against the real saved-and-reloaded
scene - caught and fixed a genuinely serious bug this way (the Dog Life
pool had no `[SerializeField]` and would have been empty on any real game
boot) - see the MINI-058 entry in `PROJECT-HANDOFF.md`. Not yet hands-on
playtested. No EXE build made. Follow-up same day, from the user's own
playtest feedback: Rasta mentor moved off the farm plots onto the access
road; Dog Life's four members were moving in visible lockstep (identical
waypoint/speed/pause for all four) - now randomised per member; Chevy
moved from the paid pool to a standalone respect-gated recruit (20
GrandBayGangs reputation, no money) positioned by Boss C instead of mixed
in with Dog Life.

### MINI-057 — Normy

Goal: a crooked police NPC ("not representative of all police") supporting
side missions, information, relationship/reputation, and self-interested
assistance. Built `NpcRole.Normy` with a working bribe mechanic (pay to
reduce heat, 45s cooldown, cost falls as a new `Faction.Normy` reputation
grows from repeat business) and dialogueSet-driven "information" flavour
(vague foreshadowing only - no faction named, since Dog Life doesn't
exist in-game yet). Deliberately not added to `MissionSystem`'s linear
mission queue - a real branching side-quest is flagged as a follow-up,
not built here. Found and fixed a real pre-existing bug along the way:
every police officer's cap (from MINI-012) was floating on the chest, not
the head - confirmed by rendering a real `NPC_Police`, not just Normy,
and fixed with the same bone-anchoring technique MINI-055's necklace
needed. Built and verified by Claude on 2026-08-17 via a validation
harness (bribe cost/heat/reputation math, cooldown, relationship pricing)
and visual re-renders of both Normy and a real officer - see the MINI-057
entry in `PROJECT-HANDOFF.md`. Not yet hands-on playtested. No EXE build
made.

### MINI-056 — Rasta mentor and strain progression

Goal: an older Rasta mentor who "teaches advanced strain work after
missions." Confirmed with the user first that "Strong" in the brief's
progression list is descriptive (same ambiguity as MINI-055), not a new
strain - scope is the mentor NPC only. Built `NPC_RastaMentor` near the
farm, reusing the MINI-053 dialogueSet path (no new mechanic) with real
Jamaican Patois dialogue (I an' I, Iyah, bredrin, seen, nuh true?, Zion -
deliberately distinct from the Dominican register every other NPC uses),
gated on the same CropUnlocked thresholds that already gate Boss C's
strain offers - four tiers (fallback, Black Sugar, Purple, Blue Cheese),
always showing the deepest tier the player has actually earned. Dialogue
only - no seeds/discounts granted, so it can't undercut Boss C's economy.
Built and verified by Claude on 2026-08-17 via a validation harness
against the real built scene's NPC and progression state (caught and
fixed a real reputation-math bug in the harness itself along the way) -
see the MINI-056 entry in `PROJECT-HANDOFF.md`. Not yet hands-on
playtested. No EXE build made.

### MINI-055 — Boss consolidation

Goal: move toward two major bosses (Boss J - lower-level, Bushers, street
connections, substantial chain; Boss C - bigger status, higher-value
progression, larger/multiple chains, nice black SUVs nearby), migrating
progression responsibilities from the old placeholder bosses without
breaking working systems. Consolidated BossM/BossP/BossQ (one NPC per
strain) into one Boss C NPC offering Black Sugar/Purple/Blue Cheese via a
new `TownNPCInteractable` multi-crop path; renamed Boss K to Boss J in
every player-visible string while deliberately keeping the internal
`Faction.BossK`/`npcName`/mission-targetId identifiers unchanged (several
missions already key off them - a full rename was judged higher regression
risk than benefit). Added chain/SUV visual flourishes, fixing two real
placement bugs caught by rendering and inspecting before shipping (a
first-pass necklace floated off the shoulder/sank near the hip; a
first-pass SUV read as a plain crate). Built and verified by Claude on
2026-08-17 via a 6-scenario validation harness against live progression
state — see the MINI-055 entry in `PROJECT-HANDOFF.md`. Not yet hands-on
playtested. No EXE build made. Follow-up same day: fixed a real placement
bug the user caught from the renders (one SUV sat on the road centreline
due to an offset that cancelled itself out; the necklace sat on the
stomach, not the chest) - both confirmed fixed by re-rendering.

### MINI-054 — Dialogue/dialect bible and opening conversation

Goal: expand the dialogue reference with the roadmap's specific term list
(Zeb, volehing, Awtic, fresh, Zion, Gwa Bay, Gwada, etc.), used naturally
and sparingly, plus write the opening conversation between the two boys
(kicked out of school, hungry, need money, one suggests Zion/Zeb, the
other hesitant, they settle on normal crops). Built `Docs/DIALECT-
LEXICON.md` with an honest confidence rating per term — 5 of 17 remain
genuinely undefined and were deliberately left unused rather than
guessed. Built an 8-line scripted opening conversation
(`OpeningConversationController`) that plays before Mission 1's own
briefing via a new `MissionSystem.firstBriefingDelay` hook. Built and
verified by Claude on 2026-08-17 via a validation harness proving the
timing math and the deferred-banner sequencing — see the MINI-054 entry
in `PROJECT-HANDOFF.md`. Not yet watched/listened to in Play Mode. No EXE
build made.

### MINI-053 — Persistent mission/objective UI + dialogue foundation

Goal: mission details must not disappear before the player can read them
(banner/objective card should show mission name, progress, distance, and
hold long enough to actually read), plus a data-driven dialogue foundation
supporting normal/inner-thought/conditional/mission/shop/faction/story-
reveal lines. Added mission name to the persistent objective card and
made banner + NPC-dialogue hold times scale with text length instead of a
fixed 3s. Built a new `DialogueCondition`/`DialogueLine`/`DialogueSet`
foundation (reads existing `ProgressionManager`/`EconomyManager` state,
picks the most-specific eligible line, cycles ties) and demonstrated it
on the ambient villager NPC — real content for the rest of the cast is
`MINI-054` scope, not duplicated here. Built and verified by Claude on
2026-08-17 via a real validation harness (caught and fixed a genuine
`AddComponent` vs. `Awake`-timing bug in the harness itself, not the game
code) — see the MINI-053 entry in `PROJECT-HANDOFF.md`. No EXE build made.

### MINI-052 — NPC navigation and stuck recovery

Goal: replace simplistic direct-line steering with proper navigation for
the companion, villagers, and police — path around houses, stop
walking into walls, recover if stuck, re-path when the target moves.
Built a reusable `NavPathSteerer` helper (real NavMesh pathing via
`com.unity.ai.navigation`, already installed, plus the brief's exact
4-rung stuck-recovery ladder: recompute path → nearby valid point →
rotate/recover → relocate) layered onto the existing
`CharacterController`-driven `FollowController`/`PatrolNPC`/
`PoliceOfficer` scripts rather than swapping in `NavMeshAgent` project-
wide (would have touched 13 other unverified systems). Built and
verified by Claude on 2026-08-17 via a batch-mode NavMesh coverage/
obstacle-exclusion check (proves the bake is real and correctly
shaped) — see the MINI-052 entry in `PROJECT-HANDOFF.md`. **Not yet
exercised in Play Mode** — the actual frame-to-frame steering/recovery
behaviour the brief asked to be tested there is the explicit next step,
not assumed working from the static check alone.

### MINI-051 — Real Zeb strain flower/bud visuals

Goal: fix weed strains' designated bud colours not being visible — root
cause was worse than reported: `fruitCount: 0` on the weed crop visual
meant zero strains' colours ever rendered, since MINI-011 Phase C. Added
three cola clusters (apical + two lateral, tapering 3-sphere stacks) with
pistil accents (cream while flowering, rust-orange when ripe) and a
frost/gloss lighten pass at full ripeness, all as opt-in layers on
`CropStageVisual` so tomato/banana/carrot are unaffected. Foliage stays
plain green — buds/colas are the only recoloured part. Built and verified
by Claude on 2026-08-17 via two rendered snapshots (close-up + 4-strain
side-by-side) — see the MINI-051 entry in `PROJECT-HANDOFF.md`. No EXE
build made, per the task brief. Not yet hands-on viewed in Play Mode/real
light at gameplay camera distance.

### MINI-050 — Banana plant uses a real banana-tree model

Goal: banana crop uses a real banana-tree model (Tropical Nature Pack)
instead of the tomato plant mesh recoloured yellow, via a new per-crop
visual registry on `FarmPlot`. Built and verified by OpenClaw on
2026-08-17 — see the MINI-050 entry in `PROJECT-HANDOFF.md`. Not yet
hands-on playtested.

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

### MINI-084 — AI production workflow and visual approval harness

Goal: Establish one low-cost, repo-owned workflow that Codex and Claude can both follow for parallel read-only investigation, single-owner Unity integration, Hitem3D/Blender asset intake, visual approval gates, mobile-first validation, and durable handoffs.

Status: Complete (documentation/tooling only). No gameplay or Unity content was changed.

Acceptance criteria:

- A repo-scoped Codex skill routes game-development work through the project rules and approval gates.
- Claude and Codex both point to one canonical workflow document.
- A reusable work-packet template defines scope, ownership, protected files, acceptance evidence, budget, and rollback.
- A visual approval register protects user-approved placements and looks from silent automated changes.
- The workflow distinguishes what can run in parallel from scene/prefab/package work that requires a single integrator.
- The workflow contains a staged, budget-conscious plan for map truth, Hitem3D characters/clothing, bike IK, NPC movement, combat, and mobile QA.
- No gameplay, scene, prefab, package, or project-setting files are changed.

### MINI-085 — Current-build regression baseline and safety checkpoint

Goal: Preserve the complete current MINI-052–084 working state on a safety branch, compile and run the existing validation/build pipeline, inspect the built game live, and record accepted/rejected visual and motion evidence before starting map or Hitem3D production.

Status: Evidence ready; automated pipeline passed and live opening inspected. Awaiting user play feedback for visual/motion acceptance.

Acceptance criteria:

- A dedicated safety branch preserves the complete current working tree without deleting or rewriting prior work.
- Unity compiles and the established high-value validation harnesses run, with failures recorded rather than hidden.
- A current Windows player is built and smoke-tested.
- Static screenshots and a live gameplay inspection cover player locomotion, NPC movement, interaction UI, farming, combat, TMAX riding/wheelie, and Range Rover driving where reachable.
- Findings are classified as accepted, needs revision, blocked, or not exercised; visual locks are added only for explicitly user-approved results.
- No Hitem3D credits are spent and no map/character/gameplay redesign is performed in this task.
- The verified state is committed as an explicit baseline checkpoint; ownership is released.

### MINI-086 — Progression truth, dialogue, follower, combat, HUD, and equipment corrections

Goal: Apply the user's hands-on regression feedback to the existing game systems without expanding into new map production, paid assets, shooting, or a full animation redesign. See `Docs/WorkPackets/MINI-086.md`.

Status: Implemented and built. Sacat's approved chain transform is captured and locked; purchase equips only the buyer, Boss C uses the real chain, and Boss J has none. Waiting only for the user's movement visibility/swing playtest.

### MINI-087 — Sacat chain inward-fit revision gate

Goal: Reopen Sacat's VA-001 chain placement from its saved profile, let the user move it slightly inward, and stop before any Boss C resizing. After Sacat is approved, capture/rebuild/test it; Boss C becomes a separate next visual decision.

Status: Evidence ready. User approved Sacat's revised fit and requested that the duplicated nape/back piece be preserved. Root plus both child transforms are locked in `VA-002`; scene rebuild, exact runtime equipment validation, and Windows build pass. Waiting only for the user's walking/running clipping check. Boss C remains a separate next visual task.

### MINI-088 — Boss C two-piece chain fit

Goal: Preserve Boss C and Sacat's existing measured 1.85 m height match; match Boss C's shoulder/body width to Sacat while preserving depth; give Boss C the same front-plus-nape chain presentation; and capture separate Boss C body/chain settings without changing Sacat's VA-002 lock.

Status: Complete. User manually placed and approved Boss C. Body scale and every chain hierarchy transform are captured in separate profiles; canonical rebuild and exact validation pass; Boss C and Sacat shoulder widths both measure 0.4403 m; Sacat VA-002 is unchanged; Windows build succeeded.

### MINI-089 — Locked-progression dialogue wording

Goal: Replace the generic locked-character response with the user's exact Dominican line: `Keep doing your ting. I'll maybe organize you when you build up ur self`.

Status: Complete. Exact requested text returned by a locked Boss C validation; canonical rebuild and Windows build succeeded. No other dialogue, progression gates, or visual profiles changed.

### MINI-090 — Input and interaction foundation

Goal: Introduce one keyboard-compatible, touch-ready input facade and route the core on-foot controls through it without changing the current control scheme or installing a new input package.

Status: Complete. Move/run/jump/look, interact/clone/farmhand, character switching, tutorial, and melee now use the touch-ready `GameInput` facade with unchanged PC keys. Focused validation, canonical rebuild, Windows build, and headless smoke passed. Vehicles and menu numeric selections remain later migrations over the same facade.

### MINI-091 — NPC and companion intelligence

Goal: Improve companion/recruit formation spacing, dynamic character avoidance, obstacle waiting, patrol crowd behavior, and police chase/search/recovery states while preserving the existing terrain-friendly movement and pooled gang behavior.

Status: Evidence ready. Formation spacing/hysteresis, character separation, blocked waiting, staggered patrols, and police Patrol/Chase/Search/Recover/Down states pass focused validation and canonical rebuild. Motion quality remains for the combined hands-on test after MINI-092 combat contact.

### MINI-092 — Combat contact truth

Goal: Replace immediate omnidirectional proximity damage with reusable animation-timed forward contact, line-of-sight/angle checks, one hit per swing, and a small stamina cost while preserving current keys, damage roles, and mission/heat integration.

Status: Evidence ready. Shared windup/active/recovery timeline, forward contact geometry, line-of-sight, one hit per swing, stamina spending, and police-only max-heat escalation pass focused validation plus companion/gang regressions. Canonical rebuild, Windows build, and headless smoke pass. Awaiting the user's combined systems 1–3 test.

### MINI-093 — Police melee retaliation

Goal: Make chasing police punch back through the MINI-092 contact timeline so the active character's health decreases and existing death/safehouse behavior can trigger.

Status: Evidence ready. Police now retaliate through timed forward contact for 12 damage, one hit per swing, with cooldown and existing death-event integration. Saved-scene defaults, focused validation, and MINI-092 regression pass. No scene rebuild or Windows build was made per the user's batching policy.

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
