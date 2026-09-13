---
name: reference-driven-game-asset-production
description: Research, model, inspect, optimize and integrate recognizable 3D game assets in Blender and Unity. Covers architecture, props, vehicles, vegetation, clothing, accessories and characters through asset-specific construction methods.
---

# General Game-Asset Production Skill

## Purpose

Create recognizable, well-proportioned, usable game assets from the
user's references and requirements.

Do not confuse:
- A valid mesh with a convincing model.
- Smooth surfaces with correct proportions.
- Added details with improved recognition.
- A successful render with visual acceptance.
- A studio preview with a verified game asset.

Use this workflow for unfamiliar objects instead of improvising a
different unverified process for every task.

The user's latest instructions and project rules remain authoritative.

## 1. Classify the asset before choosing tools

Identify the construction and runtime requirements.

| Asset class | Starting approach to consider |
|---|---|
| Buildings | Modular structural meshes, profiles, openings, repeated components |
| Furniture and rigid props | Controlled hard-surface modeling, bevels, explicit assemblies |
| Vehicles and machinery | Reference-based body surfaces plus separate mechanical components |
| Fabric accessories | Shaped surfaces with thickness, seams and purposeful layering |
| Clothing | Body/rig-aware surfaces with preserved fit and deformation topology |
| Characters and creatures | Anatomical blockout, sculpting where useful, retopology and rigging |
| Plants and trees | Botanical branching, attached leaves/fruit, controlled variation and LODs |
| Rocks and terrain details | Large-form shaping, sculpting/displacement where useful, silhouette-aware reduction |
| Repeated systems | Reusable modules or Geometry Nodes when repetition justifies them |

These are starting options, not mandatory recipes.

Do not use one universal construction method for everything.
Not everything should be sculpted, subdivided, Booleaned, shrinkwrapped,
decimated or built from rounded primitives.

## 2. Define what "correct" means

Before detailed modeling, establish:

- Exact object or design being represented.
- Intended use and interaction.
- Reference images and their reliability.
- Dimensions or meaningful proportion ratios.
- Intended camera distance.
- Front/up directions and pivot.
- Static, moving, articulated or skinned behavior.
- Required collision.
- Materials and major construction boundaries.
- Performance constraints.
- Protected existing assets and placements.

Identify ambiguities that would materially change the result.
Ask only for those decisions that cannot reasonably be inferred.

If the reference shows only one side, label hidden details as inferred.
Do not present invented construction as reference-verified fact.

## 3. Build a recognition map

Break the object into three levels:

### Primary forms
The masses, proportions and silhouette that identify the object.

Examples:
- Vehicle wheelbase, roofline and body volume.
- Building roof type, height and footprint.
- Character head/body proportions.
- Tree trunk structure and canopy distribution.

### Secondary forms
The construction that distinguishes the particular design.

Examples:
- Vehicle lights, grille, windows and wheel arches.
- Building porch, balcony and opening layout.
- Shoe tongue, collar, panels and lacing.
- Furniture joints, supports and cushion divisions.

### Tertiary detail
Surface features that enrich an already recognizable object.

Examples:
- Stitching.
- Small fasteners.
- Grain and roughness variation.
- Minor wear.
- Tiny grooves.

Complete primary and defining secondary forms before tertiary detail.

A coloured approximation with missing defining features is incomplete,
even when the mesh is technically sound.

## 4. Research unfamiliar construction

For unfamiliar assets, inspect:
- Several reference views.
- Real construction or assembly examples.
- Artist-authored demonstrations.
- Relevant official Blender documentation.

Extract techniques, not just tutorial titles.

Record:
- Source.
- Technique actually inspected.
- Problem it solves.
- Applicability to this asset.
- Limitations or version differences.

Do not claim to have watched a tutorial if only its description was
accessible.

Do not call a technique "industry standard" instead of explaining why
it suits the current geometry.

If a technique is uncertain, test it on one small representative part
before rebuilding the whole model.

## 5. Plan physical parts

Organize the model according to how the object is constructed.

Separate parts when they represent:
- Different physical components.
- Articulating elements.
- Layered materials.
- Replaceable modules.
- Different deformation requirements.

Use continuous topology where the surface is physically continuous
and continuity matters.

Do not weld everything into one mesh by default.
Do not create accidental cracks by assembling a continuous surface
from disconnected patches.

For each part, identify:
- Shape.
- Attachment.
- Thickness.
- Material.
- Visibility.
- Movement.
- Relationship to neighboring parts.

"Object exists in the scene" does not prove its feature is visible
or recognizable in the finished model.

## 6. Model in staged passes

### Pass A — Blockout

Create only the large forms.

Check:
- Overall dimensions.
- Major proportions.
- Silhouette from multiple directions.
- Spacing and alignment.
- Contact with the ground or supporting object.

Render early.

If the blockout is wrong, fix it before adding detail.

### Pass B — Construction

Build the defining secondary structures.

Check:
- Supports meet supported surfaces.
- Panels fit their underlying volume.
- Openings have intentional depth.
- Moving parts have clearance.
- Attached features are actually attached.
- Layer thickness is plausible.

### Pass C — Surface refinement

Refine curvature, edge treatment and transitions.

Use the appropriate method:
- Support loops or creases.
- Bevels.
- Surface shaping.
- Sculpting.
- Retopology.
- Curves.
- Controlled thickness.

Inspect the evaluated result after modifiers.

### Pass D — Materials and detail

Add material separation and only the detail that contributes at the
intended viewing distance.

Do not use texture noise or dramatic lighting to conceal wrong forms.

### Pass E — Game preparation

Prepare export, runtime fit, collision, deformation, LODs and budgets.

Do not optimize away identifying features before the model is recognizable.

## 7. Use procedural modeling appropriately

Blender Python is valuable for:
- Repeatable construction.
- Explicit dimensions.
- Variants.
- Repeated components.
- Export and validation.
- Reproducible cameras and renders.

It is not proof of artistic accuracy.

Use named parameters and components.
Tie important dimensions to references.
Separate:
- Geometry construction.
- Materials.
- Cameras and lighting.
- Export.
- Validation.

Avoid a monolithic script full of unexplained coordinates.

If the procedural representation prevents the required shape, change
that representation or use direct mesh editing/sculpting where available.

Do not endlessly add corrective offsets to a fundamentally wrong base.

## 8. Inspection is part of modeling

Inspect actual rendered images, not only code or scene statistics.

Maintain:
- Front.
- Side.
- Rear.
- Top where useful.
- Three-quarter.
- Intended gameplay-distance view.

Use diagnostic views when necessary:
- Neutral clay.
- Wireframe.
- Flat colour.
- Component isolation.
- Cross-section.
- Normal orientation.

These distinguish:
- Shape errors.
- Topology errors.
- Occlusion.
- Material contrast.
- Lighting problems.
- Camera mistakes.

Check the camera before judging a confusing render.
Then check the geometry; do not blame the camera without evidence.

## 9. Compare against references honestly

Use comparable orientation, framing and perspective.

Do not stretch images independently to make proportions match.
Do not treat a perspective photo as an orthographic plan.

Compare identifiable landmarks and ratios.

Examples:
- Height to width.
- Wheel diameter to wheelbase.
- Opening width to facade width.
- Head size to body height.
- Sole thickness to shoe length.
- Branch length to trunk height.

Use overlays or measurements when helpful.
Record their assumptions.

Do not invent an objective similarity percentage.
Numerical checks support visual judgment; they do not replace it.

## 10. Correct failures systematically

When the result is poor:

1. Identify the largest three visible discrepancies.
2. Classify each cause:
   reference, proportion, construction, topology, camera, material,
   lighting, export or runtime fit.
3. Fix the highest-impact cause.
4. Render the same views again.
5. Confirm improvement before moving on.

Do not respond to every failure with:
- More subdivision.
- More polygons.
- More texture.
- A complete rewrite.
- A different camera that hides the problem.

After repeated failures, revisit the construction method and reference
interpretation rather than applying another blind patch.

A user rejection reopens the relevant modeling stage.
Your earlier claim that something looked good is not an approval.

## 11. Geometry validation

Check according to the asset's requirements:

- Finite coordinates.
- Sensible component dimensions.
- Degenerate or collapsed geometry.
- Duplicate/coplanar surfaces.
- Winding and normals.
- Unexpected holes.
- Self-intersections.
- Modifier behavior.
- Thickness and clearance.
- Pivot and orientation.
- Exported scale.

Do not require every decorative surface to be watertight.
Do require appropriate closed geometry where collision, baking,
fabrication or the intended shading needs it.

For articulated/skinned assets, also check:
- Joint positions.
- Transform hierarchy.
- Bone weights.
- Bind pose.
- Representative movement.
- Contact and clearance during motion.

Static appearance does not prove deformation.

## 12. Materials

Use material behavior appropriate to the object.

Distinguish major surfaces through:
- Base colour.
- Roughness.
- Metalness where applicable.
- Normal detail.
- Thickness/transmission when genuinely needed.

Texture scale should be consistent with object scale.

Do not:
- Make every material equally shiny.
- Add random noise and call it realism.
- Add wear before the clean construction works.
- Change reference colours simply to hide geometry problems.

Inspect both neutral lighting and the game's actual lighting.

## 13. Optimization

Choose budgets from:
- Platform.
- Number of simultaneous instances.
- Camera distance.
- Screen coverage.
- Animation needs.
- Material and transparency cost.

Measure:
- Evaluated triangles.
- Renderers and material slots.
- Texture memory.
- LOD behavior.
- Collision complexity.
- Runtime performance when available.

Reduce:
- Hidden/redundant surfaces.
- Subpixel detail.
- Excessive curve resolution.
- Unnecessary material changes.
- Overdraw.
- Detail that can be baked without damaging silhouette.

Do not use one triangle target for every asset class.

Keep editable source separate from optimized export.
Inspect every LOD rather than trusting a decimation ratio.

## 14. Integration into Unity

Before replacing a live asset, verify the candidate's:
- Units.
- Axes.
- Pivot.
- Scale.
- Winding/normals.
- Materials/UVs.
- Texture import.
- Collision.
- Animation or skinning.
- LODs.
- Fit in the actual environment.

Use the project's established export path.
Do not force every asset through the same pipeline.

A scene copy can still reference shared mutable assets.
Isolate candidates or explicitly reserve and back up dependencies.

Preserve stable IDs, manual placements and unrelated work.
Use bounded additive patches instead of broad scene regeneration.

Test in the actual game when claiming runtime correctness.

## 15. Tool use

Use the capabilities actually available:
- Blender UI for direct inspection/editing where supported.
- bpy and mathutils for reproducible construction.
- bmesh for explicit topology operations.
- Geometry Nodes for suitable procedural repetition.
- Sculpting/retopology when warranted.
- UV/baking tools for game preparation.
- Shell tools for processes, logs and reports.
- Image tools for actual render inspection.
- Unity Editor tools for import and runtime verification.
- Git for controlled checkpoints.

Do not claim tool actions that were not performed.
Do not assume a tool is installed or callable without checking.

Save:
- Editable .blend.
- Generator or reproducible construction notes.
- References/provenance.
- Before/after renders.
- Exported candidate.
- Validation report.

A returned process ID means started, not completed.

## 16. Persistent modeling memory

Maintain project memory that survives conversations.

Use one concise index and asset-specific records rather than an
ever-growing generic instruction dump.

Record:
- Asset/task ID.
- Reference/version.
- Approved and rejected evidence.
- Important dimensions.
- Axis/pivot conventions.
- Construction method.
- Source/export paths.
- Failed approach and observed cause.
- Verified correction.
- Tests performed.
- Known limitations.
- Checkpoint and next action.

Label entries:
OBSERVED / USER-APPROVED / HYPOTHESIS / NOT TESTED.

Do not store assumptions as facts.
Do not turn one failure into a universal prohibition.

Read relevant memory before repeating a task.
Update it after meaningful verified progress.

## 17. Acceptance gates

An asset is ready for the requested delivery only when applicable
gates pass:

1. Recognition:
   intended object and defining design features read clearly.

2. Construction:
   major parts, thickness, contacts and openings are coherent.

3. Appearance:
   materials and shading support the reference.

4. Technical:
   geometry/export are valid for their purpose.

5. Runtime:
   fit, collision, articulation/deformation and interactions work
   where those behaviors are part of the request.

6. Performance:
   measured budgets are met or remaining profiling is disclosed.

7. User acceptance:
   do not claim approval that has not occurred.

If a defining feature is missing, it is unfinished work—not optional
polish to defer while asking whether to stop.

## 18. Reporting

Report:
- What changed.
- Why it improves the target.
- Actual images inspected.
- Source and export locations.
- Tests performed and results.
- Remaining limitations.
- Build/integration status.
- Scoped checkpoint.

Do not call something "perfect," "production-ready," "AAA" or
"mobile-ready" without the evidence those claims require.

Finish the user's requested asset, not merely a successful script.
