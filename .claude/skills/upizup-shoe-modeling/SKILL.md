---
name: upizup-shoe-modeling
description: Model footwear (sneakers, boots, sandals) for Up Iz Up Mini using Blender Python ring-sweep lofting, a single welded multi-material body, and real-reference checklist inspection. Use for any new shoe/footwear concept or wardrobe shoe slot work.
---

# Up Iz Up Mini — Shoe Modeling Skill

## Purpose

Produce a recognizable, correctly-proportioned shoe whose silhouette and
colour-blocking genuinely match real footwear anatomy, not an abstract
primitive arrangement mistaken for a shoe.

This skill is the footwear-specific companion to `upizup-blender-modeling`
(the general precision workflow) and to `upizup-wardrobe-repair` (the
in-game C# wardrobe shoe slots). Read the general skill for reference
research, precise-spec, and staged-inspection rules that apply here too;
this file is what a SHOE specifically needs to get right.

## Project and tools

Working project:
E:\Unity\Up Iz Up Mini

Blender:
C:\Program Files\Blender Foundation\Blender 5.0\blender.exe

Available workflow:
- Blender Python: bpy, mathutils, math, json.
- `WebSearch`/`WebFetch` for real footwear proportions and construction facts.
- `curl` via Bash + `Read` to actually view a downloaded reference photo -
  `WebFetch` only reads page text, it cannot look at an image.
- An image-reading tool for inspecting rendered PNGs.
- Git for scoped checkpoints.

## Read the relevant project references

Before building:
- `upizup-blender-modeling`'s general precision workflow and "Organic/
  consumer-product detail modeling" section (the hard lessons below come
  from there in more narrative form).
- `Assets/UpIzUpMini/Editor/Mini166RepairAccessories.cs`'s `Shoes()` -
  the in-game Mike90/Mike97 shoe generator. This is the PROVEN, ALREADY-
  SHIPPED ring-sweep technique this skill is built on. Read it before
  writing new shoe geometry logic.
- If touching an in-game wardrobe shoe slot rather than a standalone
  concept: `upizup-wardrobe-repair`'s full workflow and ownership rules.

## Choose the correct modeling approach: ring-sweep, not box-stacking

A shoe is an organic curved form. Box modeling (stacking axis-aligned
cubes) CANNOT produce a coherent shoe last - a real early attempt at this
produced a pile of disconnected blocks, rated "0.1/100." Use a continuous
swept-ring body instead, exactly like the proven in-game `Shoes()`:

- `Ring(a, y, scale)`: a cross-section function where `a` sweeps 0..2*pi
  around the foot (front/back via `cos(a)`, left/right via `sin(a)`), `y`
  is height, `scale` is the cross-section's size at that ring. The heel
  side is narrower than the toe side (`front_bias` around .78) for a real
  foot-like asymmetry.
- `Upper(a, t)`: continues the same Ring() math upward for the shoe body,
  with `t` in 0..1 driving both height (rising) and taper (shrinking
  cross-section) via `sin`/`cos(t*pi/2)`.
- **For a HIGH-TOP specifically**: the proven `Upper()` lets its taper
  reach exactly 0 at `t=1`, collapsing the whole rim to a single point -
  correct for a LOW-cut shoe (the point is hidden at the ankle), WRONG for
  a high-top, which needs a real OPEN ring where the foot enters. Clamp
  the taper's driving parameter (e.g. `min(t, .55)`) so it stops shrinking
  before reaching zero, while height still rises with the true `t`.

## Colour panels: follow real seam curves, not fixed angle wedges

A first attempt assigned panel colours using FIXED angle-degree ranges per
row-band (e.g. "front -55 to 55 degrees is red at all heights"). This
does not match how a real shoe is cut - it produced a diagonal "sash" with
no real-shoe equivalent, rated "6/100" even after other fixes landed. Real
shoe panels follow curves:

- The toe cap/quarter wraps the ENTIRE front-to-back hemisphere at LOW
  rows (near the sole) - there is no vertical toe-to-collar seam.
- The lace THROAT is a narrow V opening that exists only from roughly the
  ball of the foot upward, widening as height increases toward the collar.
  Model this as `throat_half_angle(row)`, returning 0 below the ball of
  the foot and increasing with row above it - not a static constant.
- The collar band wraps FULLY around at the top rows (all angles), not
  just the front.

## Subsurf: one welded multi-material mesh, not separate objects

A Subdivision Surface modifier only smooths continuously across a TRULY
shared, welded mesh. Separate mesh objects glued edge-to-edge each shrink
toward their own centre at the open boundary and visibly pull apart -
this happened here and was mistaken at first for a modeling error rather
than a Subsurf/topology mismatch. Build ONE shared vertex grid (row-major,
angle-major), assign `face.material_index` per region using the seam
curves above, and put exactly one Subsurf modifier (`levels=2,
render_levels=2`) on that single object.

## Small hardware (eyelets, laces, buckles): verify spacing analytically

Discrete small detail objects placed by procedural angle/row math are a
real, repeat failure mode - three separate attempts at eyelets/laces here
all produced a tangled, overlapping cluster rather than a clean line.
Root cause each time: a placement formula (like the throat boundary above)
returning the same or near-zero value for several consecutive inputs,
clustering objects on top of each other. Before rendering:

- Compute (don't assume) the actual angular/spatial gap between
  consecutive placement points using the SAME formula that positions them.
- Start placement past any region where the boundary function is flat or
  zero (e.g. past the toe cap, where the throat hasn't opened yet).
- If a detail still tangles after a genuine fix attempt, REMOVE it rather
  than ship a fourth broken version. A clean shoe without laces is a more
  honest result than one with a visible tangled mess.

## Camera: verify which axis is the shoe's length before framing

The ring-sweep's `cos(a)` term drives front-back (length), `sin(a)` drives
left-right (width). A camera aimed down the LENGTH axis produces a
nonsensical cone/tunnel view that looks catastrophically broken even when
the geometry is structurally sound - this happened here and was initially
mistaken for a modeling failure. For a recognizable side-profile shot
(matching how real product photography frames a shoe), the camera's
dominant offset must be along the WIDTH axis, with the target centred on
the body.

## Checklist inspection - required before presenting any result

A glance ("looks roughly shoe-shaped") let three genuinely broken results
through in a row here, including after the user explicitly said the
inspection itself was "still very very poor." Before showing a result:

1. Download and actually view a real reference photo (`curl` + `Read`),
   not just search-result text.
2. List its actual visible features one by one: toe cap boundary shape and
   colour, vamp colour and where it's actually visible, panel seam
   locations, eyelet count/spacing/line, lace crossing pattern, tongue,
   collar height and wrap, heel counter, midsole band, outsole tread.
3. Check each feature off against the candidate render individually -
   present or absent, right shape or wrong shape, right colour or wrong.
4. Only present the result once this checklist has actually been walked,
   and state what was checked, not just "looks good."

## Known real ceiling for this asset class

Professional shoe modeling relies on interactive sculpting, boolean panel
cutting with live visual feedback, and tracing over a reference image
plane by eye - none of which a headless batch Blender script can do.
Silhouette, proportion, and colour-block anatomy ARE achievable this way
with the technique above. Fine surface detail (stitching, precise seam
tracing, discrete hardware, material texture/wear) is a genuine capability
gap in this environment, not a coefficient to keep tuning indefinitely.
State this plainly when it's the actual limiting factor, rather than imply
more iteration alone will close it.

## Verification and completion

Report at completion:
- What changed and why, referencing the specific checklist feature it fixes.
- Editable `.blend` and rendered PNG paths.
- Triangle count and material count.
- Which reference photo was checked against, and the checklist result.
- Known unresolved defects, stated explicitly (a persistent render
  artifact, a removed detail, an unfitted proportion) - never cropped
  around silently in a comparison image.

Stop when the requested defect is resolved or the real capability ceiling
is reached and stated. Do not keep redesigning an already-correct
silhouette merely because more changes are possible.
