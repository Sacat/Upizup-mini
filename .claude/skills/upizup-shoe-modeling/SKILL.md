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

**A second, more specific ceiling, confirmed on an Air Max 90 concept
(rated 1/100 after multiple real fix passes - toe spring, superellipse
taper, front-camera-centring bug, collar-height flattening all genuinely
landed and were verified, and it STILL read as a boat/canoe hull):** pure
ring-sweep with material-coloured bands on ONE continuous swept tube
works for a shoe whose upper is itself basically one smooth wrapped
surface (a high-top like the Jordan concept, where the collar/vamp/toe
are all naturally one continuous form). It does NOT work for a low-top
running-shoe archetype whose defining secondary forms - a toe overlay cap
that bulges independently of the mesh underneath it, a heel counter patch
that's its own raised shape, a midsole "wrap" with its own scalloped
edge - are genuinely SEPARATE, independently-shaped pieces on a real shoe,
not just different colours painted on the same underlying curve. Colouring
regions of one shared swept tube can only ever produce parallel offset
copies of that tube's own silhouette, which reads as generic hull/boat
striping no matter how well the base curve, taper, or camera framing are
tuned - the fix is BUILDING those parts as separate welded/attached
geometry (the same technique already proven for the Jordan's eyestay
flaps and the DA cap's brim - see
[[camera-axis-singularity-and-flat-decal-frame]] for the brim's real-weld
technique), not tuning the shared tube's colour bands or profile curve
further. Diagnose which asset class you're in (one-continuous-surface vs.
independently-shaped-overlays) BEFORE choosing pure ring-sweep-with-bands,
per `reference-driven-game-asset-production`'s classification step - don't
assume the Jordan's technique generalizes to every shoe silhouette.

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

## CONFIRMED FIX for low-top running shoes: station-table profile, not an ellipse

After the Air Max 90 attempt above was dropped (rated 1/100, "a boat
hull"), a second attempt using the corrected technique below succeeded -
Mike90, Mike97 and a new Mike270 concept all read as real, correctly-
proportioned running shoes on first full render. Root cause and fix,
confirmed by direct comparison (own code both times, only this one
change):

- **The actual bug**: `Ring(a,...)` used `sin(a)`/`cos(a)` directly to
  build BOTH the horizontal cross-section AND the longitudinal length at
  every angle - a plain ellipse. An ellipse parametrized this way pinches
  to a mathematically sharp POINT at its two axis extremes (a=0, a=pi) -
  exactly a canoe bow/stern, not a rounded shoe toe/heel - and keeps an
  IDENTICAL plan-view proportion at every height since only one `scale`
  factor varies per row. Tuning the collar-height curve, the toe-spring,
  or the camera did nothing for this, because none of those touch the
  base cross-section shape.
- **The fix**: build the shoe from an explicit STATION TABLE - a list of
  `(x_mm, value_mm)` control points sampled along the shoe's length for
  both width and top-height, interpolated with a smooth spline (Catmull-
  Rom/Hermite, not linear) via an `interp(x, POINTS)` helper. Past the
  last real station, switch to an elliptic END-CAP function
  (`width_end*sqrt(max(0,1-((x-last)/radius)**2))`) so the toe/heel
  rounds off smoothly instead of pinching to zero. This is real reference
  tracing (a per-station "ruler" measurement down the shoe), not a
  closed-form shape formula - treat it the same as a Bezier profile curve
  read off a reference photo.
- **Also required**: explicit closed END CAPS (a small fan of triangles
  at the very heel and toe tips) - a bare swept tube's ends are open
  rings, not solid last tips, and this stayed wrong through several
  earlier tuning passes without being the thing actually fixed.
- **Air cavity**: cut a real cavity into the midsole with a Boolean
  DIFFERENCE modifier (`solver='EXACT'`) against a temporary cutter cube,
  then model the visible Air bladder as its own transparent mesh sitting
  in that real cavity - not a coloured inset disc glued to the surface.
  This is what makes the Air window read as a genuine recess with depth
  from every angle, including Top/Front, not just the Side hero shot.
- **Panels stay separate, on the shared surface**: mudguard, toe overlay,
  ripple bands etc. are each their own `patch()` call sampling the SAME
  `surf(x, angle, side, offset)` function the main upper uses (with a
  small outward `offset`) - keeps them flush and prevents the "parallel
  offset stripes on one hull" look from the failed attempt.

See `Tools/ArtPreview/mike_airmax_style_shoes.py` for the working
implementation (own code; informed by, but not copied from, a parallel
reference delivery that used the same class of technique - see
[[air-max-90-concept-dropped]] memory for that history). Reuse this
station-table technique for any future low-top running-shoe silhouette
instead of falling back to the ellipse.
