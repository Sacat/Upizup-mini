---
name: footwear-reference-modeling
description: Build and inspect reference-driven footwear in Blender, including shaped soles, visible Air cavities, separate uppers, tongues and laces. Use for shoe modeling and refinement; consult the recorded example before adapting it to another construction.
---

Use `references/PROCESS.md` for this Air Max 90/97 example, measured authoring stations, command lines, evidence and integration limits. Read `references/MEMORY.md` before revising these assets. The self-contained executable generator is `scripts/build_airmax.py`; it writes outputs beside itself. The complete delivered artifact folder also retains the original root script and finished evidence.

## Construction decisions

- Establish side elevation and plan independently. A plausible top outline does not fix a wrong side profile. Model in physical units; label reference-derived estimates separately from measured dimensions.
- Choose components from observed construction. A continuous surface can supply attachment coordinates, while panels, tongue, lining, sole, Air bladder and laces remain separate editable objects.
- Define an open collar and throat. Do not collapse every upper ring toward one point or cover the entire opening with a cap.
- A visible Air window requires an opening in the surrounding sole. An inset lens hidden behind a solid wall cannot become visible through camera adjustment. Test the cavity without its bladder, then add the bladder and supports.
- Compute panel and accessory positions from the same surface function. Check face winding before Solidify: reversed normals can make thickness grow into overlays.
- Validate one difficult repeated component before replicating it. Ribbon laces need a defined cross-section, smooth path, correct endpoints and controlled separation at crossings. They remain attached in object space.
- Subdivision, curves, booleans and decals are conditional tools, not universal quality fixes. No single-mesh requirement applies to all shoes. Camera-facing billboards are unsuitable for attached laces on an inspectable 3D shoe.

## Inspection and reporting

Compare fixed side, plan, front, rear and three-quarter views. Use a neutral clay pass to separate geometry from materials. Inspect the actual rendered file, not just the process exit status. Preserve rejected renders with the specific failure and correction. Fix recognition and attachment errors before micro-detail.

Exported geometry must be reopened and checked; a Blender material with procedural bump is not automatically equivalent to glTF or Unity shading. Separate authoring budget from runtime budget. Do not call this example fitted, animated, optimized, integrated or visually approved without corresponding evidence.

For a different asset category, reuse reference analysis, explicit dimensions, component decomposition, staged experiments and evidence logging. Replace the shoe-specific surface, construction and validation logic.
