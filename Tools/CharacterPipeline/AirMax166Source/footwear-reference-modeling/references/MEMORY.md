# Verified memory and failure log

## User intent

The user rejected thick, weakly recognizable footwear. Preserve thinner soles, actual visible Air units, distinct AM90/AM97 shape and complete reproducible documentation. Visual acceptance is still pending. Do not lower the target merely because the script rendered.

## Observed during this task

- Existing live shoe source used 61mm sole top. New preview parameterization uses approximately 30mm heel and 20mm forefoot; these are artist estimates, not factory specifications.
- Shared project claim belongs to Claude's MINI-166 cap work. This entire delivery is outside the project; do not merge over its files.
- Original patch winding produced inward upper normals. Inward Solidify offset therefore grew outward into overlay panels, yielding severe patterned intersections. Fix winding before changing offsets.
- First-pass studio light power washed out materials. Reduced power restores separation; colour diagnosis must use the corrected lighting.
- First-pass tongue used an independent elevation profile and floated above the upper. Final attachment heights derive from the same cross-sectional geometry.
- A broad text replacement accidentally changed a lace expression into tongue_surface multiplied by squared width fraction. The laces plunged through the tongue; this was found in the rendered result and source, then corrected to surface height plus clearance. Do not copy that rejected patch as the final algorithm.
- Tessellation output in this Blender 5 run contained integer vertex indices. Treating them as vectors raised AttributeError. Final code resolves indices to vectors and then interpolates.
- Raw upper height at the tip fell below the sole after toe spring; half-width subtraction also became negative. Both folded the tip. Clamp meaningful geometric bounds, then inspect the toe again.
- One big non-planar logo polygon was visibly faceted and detached. Tessellate/subdivide in the drawing plane before surface projection.
- First AM97 preview had three equal-looking Air slots. Revised to a long main opening plus a smaller heel opening and bridge, closer to the inspected photo.
- World-Z cross tangent can vanish on vertical ribbon segments. Use a stable fallback lateral frame for pull loops.
- Blender can log a Python traceback and still exit with code 0. Completion sentinel, expected files and actual inspection are required.

## Hypotheses / not yet production-verified

- These static authoring models will need material consolidation, texture baking and LOD reduction before mobile use.
- Transparent Air in Cycles may need a different shader or authored fallback in Unity; no runtime result is inferred from the render.
- Both source sides are symmetric. Exact left/right anatomical last accuracy and fit to Franki/Sacat have not been established.
- User may request further silhouette, colourway or panel refinements after viewing. No claim of exact replica or photogrammetric accuracy is justified.

## What not to memorize as a universal rule

Do not conclude that all modeling needs booleans, all shoes need one mesh, all laces must be ribbons, every component must be manifold, all material detail needs geometry, or that subdivisions guarantee quality. This example uses those tools only where their construction and observed result support them.

- Final all-view inspection found a real missing heel/toe end closure and partly buried heel badge. Added end-cap geometry and surface-solved badge attachment. A hero view had concealed the rear defects; fixed elevations are necessary evidence.
- Toe plan changed to an elliptic end-cap function; use effective profile CSV, not just the earlier raw station list, for reproduction.
- Initial skill validation failed because PyYAML was absent from both Python runtimes. An artifact-local `.tooling` install allowed the actual validator to pass; this is tooling setup, not part of the model.
