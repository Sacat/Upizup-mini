---
name: upizup-building-modeling
description: Model buildings and structures (houses, mansions, shops, institutions) for Up Iz Up Mini to a professional, consistent standard - proven Blender construction techniques, real architectural proportions/specs, and the staged part-to-whole inspection process. Use for any new or modified building.
---

# Up Iz Up Mini — Building Modeling Skill

Architecture-specific companion to `upizup-blender-modeling` (the general
cross-cutting process: reference research, precise specs, staged
inspection, options-with-images). Read that skill for the process; this
one is what a building specifically needs to get right.

## Foundation: reuse Codex's proven house() construction, don't reinvent it

The approved house family (`Tools/ArtPreview/mini141_preview.py`,
integrated into 86 live map houses via `Mini142ArtIntegration`) is the
base pattern for every building in this project. Its helper functions
(`mat`, `cube`, `mesh`, `rod`, `ico`, `window`, `setup`, `save_render`,
`count_group`) are proven and should be copied/reused verbatim, not
reimplemented. Its roof technique - an explicit 6-vertex, 2-face mesh for
the two pitched planes, small `rod()` ribs, a triangular `mesh()` gable-end
cap, `cube()` fascia boards, a `rod()` downpipe - is simple, has shipped,
and should be the default for any new gabled building. Do not invent new
geometry techniques (custom bmesh, procedural hip-roof math, etc.) without
first validating them against a small test case - see the "untested logic"
case study in `upizup-blender-modeling`.

Reference dimensions from the standard house (a coherence baseline, not a
hard rule for every building): 5.6m wide x 4.7m deep core, 2.75m per
storey, .32m foundation, .95m ridge rise over a ~2.57m half-depth run.

## Researched specs for larger/grander buildings (Highland mansion case study)

These are real numbers found via `WebSearch`/`WebFetch` and verified by
computing the actual built measurement against them - not just applied
by feel. Reuse these for any similarly-styled building; re-research for a
genuinely different style (this is Caribbean colonial plantation
Georgian, not every architectural style shares these numbers).

- **Storey height**: keep at 2.75m to match the existing house family,
  even at mansion scale - consistency across the town matters more than
  matching one specific historic reference's storey height.
- **Roof pitch**: 25-35 degrees for Caribbean/tropical climates (steep
  enough to shed heavy rain, shallow enough to resist hurricane wind
  uplift). Target ~30 degrees. Compute the actual result -
  `atan(rise/run)` - and check it lands in range; an early mansion attempt
  measured out at ~19 degrees despite looking plausible by eye.
- **Attic/third level**: real multi-storey plantation houses (e.g.
  Barrett's Plantation House, 1735) use TWO main full-height living floors
  and a garret/attic above, not three equal full floors. Model the attic
  as roof dormers (see below), not a third storey.
- **Dormers**: a dormer is its own small box with its OWN small gable roof
  that projects THROUGH the main roof slope partway between eave and
  ridge - it is not a smaller inset floor sitting below the roofline. Confirmed
  by downloading and directly inspecting real dormer photos (Wikipedia's
  Dormer article), not just reading a text description. Dormer total width
  should equal the window+trim width it serves on the floor below (Fine
  Homebuilding, "Designing Gable Dormers") - compute and check this ratio,
  target close to 1.0-1.2.
- **Veranda/gallery**: full-width, on columns, often on multiple storeys,
  with a balustrade upstairs (reuse the approved house's exact
  Balcony-slab/support/rail/Baluster pattern, just spanning the whole
  front). Leave a gap in the column row at the entrance - an evenly-spaced
  column grid will otherwise plant one directly in the doorway.
- **Entrance door**: must be on the floor the entry steps and veranda
  actually reach (normally ground floor) - a grand double door on an upper
  floor with no way to reach it from outside is a real circulation defect,
  not a style choice, and was caught only by a user visually checking, not
  by any geometric validation. When adding a "grand" feature, ask whether
  a real person could actually walk to it.
- **Cupola/belvedere** (only if the design calls for one; the Highland
  mansion ended up without one per final approval - don't add one by
  default): must read as a distinct roof FEATURE, not a second shrunken
  house. Use a slender drum (octagonal cylinder primitive), thin columns
  echoing the veranda's, louvred vents instead of full house-style
  windows, and a shallow conical or shallow-pitched roof - not a boxy
  walled room with a repeated gable matching the main roof's shape. Keep
  its footprint well under the main roof's span (researched guidance:
  comfortably under ~15%).

## Staged inspection for a building specifically

Following `upizup-blender-modeling`'s general staged-inspection rule,
for a building the natural stages are:

1. Roof-level features alone (dormers, cupola if present) - floating, no
   walls - to check their own shape/proportion in isolation.
2. Those features + the roof they attach to.
3. The roof + the floor immediately below it (checks the roof-to-wall
   junction with no attic features in the way).
4. Roof-level features + roof + that floor together.
5. + the floor(s) below that.
6. Everything together, final hero angle.

Implement this by tagging every created object into named groups as it's
built (`GROUPS['roof'].append(...)` etc., or snapshot `set(bpy.data.objects)`
before/after each construction block and diff), then toggle
`obj.hide_render` per stage and render each stage as an actual separate
image - not a crop of the final render, which would still show occluded-
but-present geometry. A leftover dead-weight offset variable (this
project's actual bug: a removed floor's height was still being added into
the roof's base-height calculation, floating the whole roof 1.9m above the
wall) is exactly the kind of error this staged process, plus checking
dependent measurements after any structural edit, is meant to catch.

## Circulation and structural sanity checklist

Before calling a building done, verify by ACTUALLY LOOKING (not assuming):

- Every door has a real path to it from outside (steps, a veranda, level
  ground) at the level it's placed on.
- No column, post, or other vertical element stands in front of a door,
  the main flow of traffic, or a window's centre.
- Every added detail (dormer, cupola, balcony) sits flush on the surface
  it's attached to - check the specific junction close up.
- Roof pitch, storey height, and any other measured quantity match the
  research finding numerically, not just "looks about right."
