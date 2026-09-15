---
name: upizup-map-district-expansion
description: Expand Up Iz Up Mini's map into new territory (a new district/corridor) as an isolated, protected scene copy, never on the live game - district scaffolding, curated/simplified real source-road selection, iterative terrain+road+house generation with measured verification, and a fixed-camera review gate before any gameplay migration. Use for "expand the map", "add a new area/district", or continuing MINI-168-style work. See upizup-map-house-repair for bounded fixes to the ALREADY-LIVE map instead.
---

# Up Iz Up Mini — Map District Expansion Skill

## Purpose

Grow the game world into new real-world territory (a new corridor,
neighbourhood or district) without ever touching the live playable scene
or an already-approved map-lab proof, and without silently overclaiming
"the rest of Grand Bay" when only a bounded corridor was actually built.

Distilled from MINI-168 (Codex's Berekua-to-Pierre-Charles-Secondary-
School expansion, 2026-09-14) - full process record and exact commands in
`Docs/Maps/<map-id>/CLAUDE-CONTINUE.md` and `Docs/WorkPackets/MINI-168.md`.
This is the general, reusable pattern extracted from that real session,
not a description of MINI-168 itself - read those files for the concrete
worked example.

This is the EXPANSION-INTO-NEW-GROUND skill. For fixing a placement/fit/
seam defect on the map that's already live, use `upizup-map-house-repair`
instead - different risk profile, different tools.

## Scope discipline - read the user's correction, not the original ask

A request like "expand to the rest of Grand Bay" can get corrected mid-
task to a specific bounded corridor ("Berekua to the high-school area").
When that happens, the correction SUPERSEDES the original open-ended
scope - do not keep building toward the original wording. Record the
correction explicitly in the work packet's Intent section, and interpret
an ambiguous landmark (e.g. "the high school") as the nearest existing
named anchor in the project's own anchor data unless the user corrects
that too.

Road simplification is normal and often explicitly authorized - but
"simplified" means: preserve principal connections, landmark order, and
coast/hill relationships, while dropping/merging minor spurs; it does NOT
mean silently omitting a route without recording it. Every retained,
simplified, or omitted source route must be named in the record (way IDs,
not just "some roads").

## Step 1 — Isolate before generating anything

Never generate expansion geometry directly into `GrandBayProof.unity` or
an already-approved map-lab proof scene. Instead:

1. `Tools/World/New-MapDistrict.ps1` - scaffold a new map ID
   (`dm-<country>-<place>-<variant>-v1` convention).
2. `Tools/World/Test-MapDistrict.ps1 -MapId <id> -Stage scaffold` - must
   pass with zero errors before any Unity work starts.
3. A dedicated Unity editor copy tool (e.g. `Mini168GrandBayExpansionCopy.cs`)
   duplicates the approved source proof scene into a NEW scene file. This
   tool must:
   - Hash the live gameplay scene and the approved source scene BEFORE and
     AFTER the copy, and refuse/report if either changed. Version that
     baseline in the district's `Evidence/protected-scene-hashes.txt` -
     never reset this baseline to force a later failure to pass; if a
     genuine mismatch appears, diff it against Git history first.
   - Exclude the new copy scene from `EditorBuildSettings` so it can never
     accidentally ship.
   - Become a no-op (or a distinct "reset" operation) once real expansion
     work exists in the copy - a copy tool that unconditionally deletes-
     and-recreates its target will silently erase generation work on a
     second run. Guard this explicitly.

Only after this checkpoint validates (`COPY PASS` in both a build log and
a validation log, Unity exit 0) does any expansion geometry get built.

## Step 2 — Curate real source data, don't dump it wholesale

The project's source road/waterway/anchor catalogue (e.g.
`GrandBayPhase1MapData.json`) is real OSM-derived data, but "N roads in
the file" is not the same claim as "N roads are part of this expansion."
For the corridor actually in scope:

- Select specific named source ways (record their stable IDs, e.g.
  `way/361079570`), not a geometric blob.
- Any coordinate-box or Z/X clipping applied to a route is a SCOPE CUT,
  not a surveyed boundary - say so explicitly, don't imply it's the real
  extent of that road.
- An entrance lane, campus loop, or similar connector with no real source
  way backing it is an ARTISTIC addition - label it as such, not as
  traced geography.
- Convert real-world anchor coordinates through the project's own
  established compression factor (this project's precedent: divide
  metres by 3) rather than inventing a new scale for one district.

## Step 3 — Generate iteratively, with a genuinely idempotent generator

The extension generator (e.g. `Mini168Expansion.cs`) should:

- Replace only its OWN previously-generated root object on a re-run
  (e.g. `MINI168_Expansion`), not the whole scene - so re-running after a
  fix doesn't require re-copying from scratch.
- Store every new/altered mesh and material under the district's own
  Generated/Staging directory. A scene copy is NOT asset isolation -
  duplicating a scene still shares its external mesh/material references
  with the source unless you deliberately break that.
- Keep the SOURCE terrain collider live and queryable for the ENTIRE
  generation pass, only disabling it at the very end. Disabling it early
  and then sampling lot/road heights against it is a real, repeatable bug
  (produces partially-buried houses) - this was caught in a real MINI-168
  pass and is worth checking first if new geometry looks sunk into the
  ground.
- Grade new roads with ONE continuous, explicitly-authored slope function
  toward the existing network, not "blend to the nearest old-road sample"
  - nearest-sample blending is unstable near junctions and can spike to
  extreme grades (21%+ observed) exactly where two grade functions
  disagree. A single measured/authored grade line, blended smoothly
  across a defined distance band, is what actually stayed bounded (5.5%
  observed) in the corrected pass.
- Reuse the existing approved house family/meshes for new lots (read-only
  reference, matching `upizup-map-house-repair`'s house pipeline) rather
  than modeling new houses for an expansion pass - district growth is a
  placement/layout problem, not a new-asset-modeling problem, unless the
  user asks for new house designs specifically.
- Expect existing schematic systems (waterways, prior road ribbons) laid
  out for the OLD terrain to visually break against new grading (e.g.
  float above a regraded terrace) - disable/flag them rather than leave
  them silently wrong, and record the follow-up as deferred work, not a
  fixed defect.

**Keep every rejected pass's evidence.** When a pass fails a real check
(too few viable house sites, buried geometry, an extreme grade), save its
renders/measurements under `Evidence/Rejected-PassN/` and move on to the
next pass - do not overwrite or delete the evidence of what didn't work.
A later passing render must never quietly replace the record of an
earlier failure.

## Step 4 — Verify the SAVED result, not the generator's own claims

A separate `FinalReview`-style tool should, against the ACTUALLY SAVED
scene (reopen it, don't trust in-memory state from the generation pass):

- Measure real centreline slopes along the saved road meshes and enforce
  a maximum grade ceiling (this project's precedent: 8% ceiling, with a
  passing corrected result around 5.5%).
- Confirm every road mesh has a matching MeshCollider (count them; a
  visual-only road segment without a collider is a real, easy-to-miss
  gap).
- Capture a small fixed set of camera evidence: an overview, and at least
  one player-eye-height street view (this project's precedent: 1.75m
  above the actual road mesh, not an arbitrary height) plus one view of
  the new destination/landmark. Reuse identical camera definitions across
  reruns so before/after is comparable.
- State plainly what this does NOT prove: a passing grade/collider check
  is not vehicle-handling proof, not junction-traversal proof, not NPC/
  navigation proof, and not a mobile performance budget. Say so in the
  same breath as reporting the pass, not as a buried caveat.

## Ownership and documentation pattern

- Reserve the work packet, the district's `Docs/Maps/<id>/` folder, the
  district's own scene/generated-asset paths, and the copy/generator
  tool scripts - never the live scene or the approved source proof (list
  those under `protected_files`, not `reserved_files`).
- Keep a persistent `CLAUDE-CONTINUE.md` (or equivalent) inside the
  district folder with: current checkpoint status (do not describe an
  unperformed step as completed), tools actually used, evidence paths and
  their limitations, a running "known construction hazards" list (real
  bugs found, e.g. the early-collider-disable burial bug above), and the
  single next action - written so a future session can resume without
  re-deriving the same failures. Append a dated entry per pass rather
  than overwriting the log.
- Release ownership at each genuine review checkpoint (a copy validated,
  or a pass that's ready for the user's visual review) rather than
  holding the claim through open-ended follow-on work - the next session
  reclaims it explicitly.

## Common mistakes (do not repeat)

- Treating "the source JSON has N roads" as proof of coverage or
  connectivity for the claimed area - verify actual geographic bounds and
  routes crossing the boundary first.
- Running a whole-district generator a second time without checking
  whether it will delete existing expansion work.
- Editing a shared source mesh/material because "the scene is just a
  copy" - the copy still points at the same asset files unless isolated.
- Disabling the terrain collider before finishing height sampling for lot/
  road placement.
- Nearest-sample road-height blending near a junction where two grade
  regimes meet.
- Silently dropping a source route from the network without recording it
  as omitted.
- Calling a district "full [place name]" when only a bounded corridor was
  actually generated.
- Resetting a protected-hash baseline to make a failing check pass,
  instead of diffing against the real historical baseline.
- Treating a passing grade/collider/hash validator as gameplay,
  passability, or visual acceptance - those are separate, explicitly
  later gates.

## See also

- `upizup-map-house-repair` - bounded fixes to the ALREADY-LIVE map
  (house fit, road seams, junction geometry); read this first to tell
  which skill actually applies.
- `upizup-building-modeling` / `upizup-blender-modeling` - if the task
  genuinely needs a NEW house/building design rather than reusing the
  existing approved family.
- `reference-driven-game-asset-production` - the umbrella asset-
  production methodology this and the other project skills sit under.
