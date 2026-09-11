# MINI-162 — flatten Franki rear hood remnant; hair investigation

User accepts arm repair but rejects hoodie silhouette behind neck. Authorized flatten only rear upper torso/neck on current repaired shirt, preserve sleeve vertices/weights, all accessories/pants/Sacat. Back up current derived FBX; reusable Blender edit, Unity close-up verification, build after appearance check. Reserve derived Franki_ArmsRestored.fbx plus script, packet, existing Mini161 proof and shared handoff/current/character/change notes. No scene regeneration, no credits. Visual approval remains user gate.

## Handoff: Claude took over from Codex (2026-09-08)

Codex was rate-limited mid-task (usage resets ~1:51 AM), user explicitly
authorized Claude to continue MINI-162. Picked up from Codex's exact
checkpoint: `Tools/CharacterPipeline/mini162_flatten_hood.py` had already
run once (`Logs/Tasks/MINI-162/Franki-Shirt-NoHood.blend`,
`audit.json`: 1047 rear vertices reshaped, 907 arm vertices confirmed
unchanged), producing a big improvement but with a visible jagged/
overlapping edge right at the collar rim - Codex's own last message
flagged this as not-yet-finished before it got cut off.

**Completed the fix**: new `mini162_smooth_collar.py` opens Codex's saved
`Franki-Shirt-NoHood.blend`, runs a tighter, more repeated smooth pass
(60 iterations vs. the broad flatten's 35) targeted specifically at the
collar-rim vertex band (`z > 1.45`, `|x| < .16` - narrower than the
flatten's whole-upper-back region), asserts arm vertices are still
byte-identical before touching anything, triangulates (MINI-161's
own lesson: Unity discards untriangulated faces on export), exports over
`Franki_ArmsRestored.fbx`, and saves `Franki-Shirt-CollarSmoothed.blend`.
Re-ran `Mini161ArmRepair.Integrate()` to re-wire the updated mesh onto the
live Franki (same narrow Ch28_Hoody-only integration Codex's tool already
does - no broader re-integration attempted), then `Mini161ArmRepair.Preview()`
to re-capture and actually look at the result before calling it done.

Result: neckline now reads as a normal crew-style back, no raised hood
bump/silhouette. A very faint irregularity remains right at the collar
edge on close inspection but is no longer "shaped like the hoodie" - the
user's specific complaint is resolved. Arms remain fully intact (same
short-sleeve, connected forearm, no gap - MINI-161's fix undisturbed).

## Hair investigation (new finding, not yet fixed)

User also asked: don't want either character bald, want short waves with
a clean shape-up on both. Investigated before touching anything (per this
project's own discipline) rather than guessing at scope:

- **Sacat has no real hair mesh at all.** What reads as a "cap" in every
  prior screenshot is a smooth metallic-grey dome **baked directly into
  the `Ch06` head geometry/texture itself** - not a separate accessory
  object (searched by name, confirmed nothing named "cap" exists as its
  own GameObject). There is a hard, visible seam where this baked dome
  meets bare skin at ear level. Giving Sacat real hair means adding new
  geometry/texture to a fused mesh, not toggling anything off.
- **Franki has a real, separate `Ch28_Hair` mesh (15,540 verts), enabled
  and textured** - but the texture itself is visibly broken: a glitchy
  white/brown striped pattern, not a hairstyle. This reads as "bald" from
  a distance because the broken texture's average tone blends with skin,
  not because hair is missing or hidden.

Both are real production work (new geometry for Sacat, texture
diagnosis+fix or new mesh for Franki), not a quick toggle - scoping this
honestly rather than rushing a low-quality result. See evidence renders:
`Logs/Tasks/MINI-162/Head-Sacat-WithCap-Side.png`,
`Head-Franki-WithCap-Side.png`.

## Acceptance scorecard

- [x] Neckline flattened, collar-rim jaggedness smoothed (two Blender passes, both verified by rendering)
- [x] Arm geometry confirmed unchanged at every step (assertion + visual)
- [x] Re-integrated into live scene, re-verified by rendering after
- [x] Hair state investigated with real renders (not guessed) - Sacat: no real hair mesh; Franki: broken hair texture
- [ ] Hair fix (waves + shape-up, both characters) - NOT done, scoped as real follow-up work
- [ ] Windows build with the neckline fix
- [ ] User visual approval
