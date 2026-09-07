# MINI-143 — kick movement lock, contact reliability, Dog Life first activation

Owner Codex; implementing2026-09-07. User requests no walking while kicking, proper hit/damage contact, and investigation of missing Dog Life near houses. Zero external credits. Single integrator; independent health initialization/test worker owns only NpcCombatHealth.cs and Mini143GangValidation.cs. All other runtime/editor files reserved by main.
Status: evidence-ready, implemented/built; ownership released. User playtest remains.

Scope: preserve existing animations, damage values, player identity, map/house placements and all saves. Reproduce source/lifecycle bugs, add a kick-only planar/jump/turn lock, validate contact during active window with at most one successful damage event, fix confirmed gang initialization failure. No new combat styles, assets or world rebuild.

Evidence gate: compile, deterministic attack timing/contact/damage and first pool activation/respawn tests; inspect live-scene gang footprint without relocation; screenshots for static placement; normal-speed combat remains live/user acceptance if automation cannot prove it. Build once after checks, short capped smoke to avoid uncapped warning spam, handoff and scoped commit.

## Findings and implementation

- Kicks had no locomotion lock and resolved damage at0.22s, far before maximum extension. Survey sampled real rig/clip feet: Sacat clip2.1667s at1.5x peaks0.7944s; Franki clip1.6s at1x peaks0.6800s. Window/recovery and animation fade now align; no Animator asset or clip edits. Gravity remains active during kick. Punch movement unchanged.
- Player swings checked only once on window entry. New player-only window advancement retries until first successful contact then latches; old NPC timeline API unchanged. Excludes inactive candidates and disabled/downed health. Existing forward arc/reach/LOS and damage values remain; this is NOT a new bone-swept hitbox system.
- First-inactive gang reset used uninitialized homeScale before Awake, yielding invisible zero-sized members. Idempotent initialization now captures original home transform before reset and never recaptures transient fade scale. Existing pool cooldown/policies preserved.
- Patrols no longer directly move a ragdolled/disabled-controller character. No map/house/character proportions/shops/vehicle placements changed.

## Verification

- MINI143CombatValidation.Survey: Logs/Tasks/MINI-143-Survey.log and MINI-143/kick-survey.txt.
- MINI143CombatValidation.ValidateAndBuild: Logs/Tasks/MINI-143-Build.log. Focused both-character jab/kick held virtual movement/sprint/jump, exact damage once, late contact, lock release, wall/behind/out-of-range checks PASS. MINI143GangValidation tests first activation before Awake, authored nonunit scale, later Awake, cooldown, faded-scale restore and repeated pooling PASS. MINI132 shared-impact policy/strength regression PASS.
- First compile caught wrong CharacterController API (it is a Collider, not Behaviour); corrected to enabled + gameObject.activeInHierarchy. Initial scene screenshot lookup assumed wrong object prefix; fixed to use actual serialized _pool. Do not weaken assertions or rename characters to fit diagnostic assumptions.
- Live-scene activation: four NPC_DogLife_0..3 positive scale(1,1,1), unchanged positions(-38.59,8.72,-152.24),(-34.59,8.67,-152.79),(-30.59,8.58,-153.34),(-26.59,8.54,-153.89). Chest sphere/box overlap probe found none. Evidence MINI-143/gang-placement.txt and DogLife-Visible.png. Static Unity camera capture only; does not prove motion or whole walking route clearance. Scene NOT saved during tests.
- Windows build397918803bytes PASS at Builds/GrandBayProof/UpIzUpMini.exe.12-second headless startup stayed alive:0 exceptions and0 inactive-controller warnings; MINI-143/player-smoke.log. Only spawned test process stopped.
- Remaining: user normal-speed combat test (hold movement during fifth combo kick, test approaching enemies during swing), pass Dog Life block and test later return after defeat. Group-walk roof sampling, wider NPC navigation and subjective ragdoll/combo feel remain separate. No new assets/credits, no paid tools.
