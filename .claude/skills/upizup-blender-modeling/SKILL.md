---
name: upizup-blender-modeling
description: General precision workflow for modeling ANY item for Up Iz Up Mini - houses, props, vehicles, vegetation, character bodies/clothing - using Blender Python, real reference research, precise measured specs, and staged part-to-whole inspection. Use for every modeling task; see upizup-building-modeling for building-specific detail and upizup-wardrobe-repair for character-specific detail.
---

# Up Iz Up Mini — General Modeling Workflow

This is the general, cross-cutting skill for modeling any item at a
consistent, professional level of precision. `upizup-building-modeling`
holds building/architecture-specific technique and specs;
`upizup-wardrobe-repair` holds character-body-specific technique. Both
point back here for the process itself.

## Purpose

Produce recognizable, well-proportioned, editable 3D assets that look correct in the actual game.

Use the user's references and existing approved work. Do not substitute arbitrary primitive arrangements, unverified exports or attractive studio renders for finished game assets.

This skill guides modeling decisions. It does not authorize unrelated changes, purchases or replacement of approved assets.

## The precision workflow, in order

This is the standing default for every modeling task, not a special
procedure for when something has already gone wrong. Established across
a real session's worth of course-correction on a Highland mansion concept
- each numbered rule below has a genuine caught mistake behind it.

1. **If the request is genuinely ambiguous, produce a small set of options
   WITH IMAGES and let the user choose** before investing in one full
   build. Don't silently commit to one interpretation of an open-ended ask
   and hope it's what was wanted.
2. **Research real reference before building anything** - see "Research
   real reference before modeling anything" below for the exact tool
   combination (`WebSearch`/`WebFetch` for facts, `curl`+`Read` for
   actually seeing an image).
3. **Get PRECISE numeric specs from real tutorials/standards for anything
   measurable** - a roof pitch, a proportion ratio, a real-world dimension
   - don't guess a coefficient and eyeball it. Compute the actual result
   your model produces (an angle, a ratio, a length) and state it next to
   the spec you're checking it against. A mansion roof that "looked about
   right" measured out at 19 degrees against a real 25-35 degree standard.
4. **Reuse proven, already-tested code/techniques first.** Don't write new
   experimental geometry logic (custom bmesh, novel procedural math) when
   an existing, shipped technique already solves the same kind of problem.
   If new logic is genuinely required, validate it against a small, cheap
   test case before building the full asset with it - an untested custom
   hip-roof function once produced a wildly broken, oversized, off-centre
   roof that could have been caught in five minutes with a simpler test.
5. **Inspect in stages, part-to-whole, like an engineer or architect would**
   - not one hero-angle screenshot at the end. See "Staged, incremental
   inspection" below.
6. **After any structural edit, recheck every dependent measurement/
   variable that referenced the changed piece.** A leftover offset from a
   since-removed component is a classic invisible bug - it floated an
   entire roof 1.9m above its wall while the wide-angle render still
   looked plausible.
7. **State exactly what you checked** - which angles, whether a flat/
   wireframe pass was used, which specific junction was inspected, what
   measurement was verified against what spec - not just "looks good."

## Project and tools

Working project:
E:\Unity\Up Iz Up Mini

Read-only reference project:
E:\Unity\Up iz up

Blender:
C:\Program Files\Blender Foundation\Blender 5.0\blender.exe

Unity:
C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe

Available workflow:
- PowerShell for inspection and process control.
- Blender Python: bpy, mathutils, math, random and json.
- Unity C# Editor tools for import, fitting and verification.
- An image-reading tool for inspecting rendered PNGs.
- Git for scoped checkpoints.

Use equivalent tools actually available in your environment.
Do not assume Codex-specific tool names exist in Claude.

## Read the relevant project references

Before editing:
- AGENTS.md
- Docs/CURRENT.md
- Current ownership in PROJECT-HANDOFF.md
- Relevant system ledger and work packet

For houses, environment assets and map placement:
Docs/CLAUDE-HOUSES-MAP-COMPLETE-HANDOFF-MINI-142.md

For fitted character clothing:
Docs/CLAUDE-WARDROBE-COMPLETE-HANDOFF-MINI-166.md

These guides contain the actual source files, commands, failures and evidence.
Read only the sections relevant to the current asset.

Do not run Unity integration while another task owns the scene or imports.

## When the request is ambiguous, offer options with images first

Direct standing user instruction: "sometimes give me options with images
to start with if you are unclear and then let me choose." When a request
leaves a real design decision open (which of several plausible styles,
which reference to follow, how elaborate a feature should be) and picking
wrong means redoing real work, don't silently commit to one interpretation
and build the whole thing before finding out it wasn't wanted. Instead:
research 2-3 real reference options quickly (images via `curl`+`Read`, or
quick low-cost concept renders), show them together, and let the user pick
before investing in the full build. This is not a license to ask before
every small decision - routine details should still be resolved from
existing approved examples per "Establish the modeling target" below;
reserve this for choices that are genuinely open AND expensive to reverse.

## Choose the correct modeling approach

### Static houses and props

Prefer Blender modeling with reusable Python helpers and named parts.

Use explicit mesh geometry for forms that affect silhouette:
- Roof slopes and overhangs.
- Porches and balconies.
- Window/door surrounds.
- Steps, pillars and supports.
- Curved or tapered structural forms.

Primitives are useful construction pieces. Their arrangement must produce
a coherent object with deliberate proportions and connected parts.

### Existing skinned clothing

Do not automatically send working garments through a new Blender/FBX round trip.

The repaired wardrobe already has continuous topology and working skinning.
For localized changes, inspect the existing Unity mesh tools first:
Mini166RepairClothes.cs and Mini166RepairMesh.cs.

If Blender is genuinely needed:
- Preserve the original skeleton, bone names, bind pose and weights.
- Work on an isolated copy.
- Verify the imported result on the actual character.
- Test deformation before replacing the live garment.

Never assume a hidden complete body exists underneath clothing.
Deleting Franki's sleeve geometry previously left floating hands.

## Research real reference before modeling anything

Applies to every asset this skill covers - houses, props, vegetation,
character bodies/clothing, vehicles - not just muscle definition. Don't
design or tune purely by iterating against your own renders/exports in
isolation. Look up real reference FIRST, then build, then compare the
result back against it explicitly.

Two tools, used together (there is no single bundled "image search" tool
in this environment - this combination IS the capability):

- `WebSearch` / `WebFetch` for facts, proportions and named references -
  house/architecture style guides, anatomy-for-artists proportion rules
  (Proko, Artists Network, Loomis method), vehicle reference specs, plant
  growth-stage references, etc. `WebFetch` only reads and summarizes page
  TEXT - it cannot look at an image.
- To actually SEE a reference image: download it with `curl` via `Bash`
  to a local file, then `Read` it - `Read` displays images, `WebFetch`
  does not. Use this whenever a visual (not just a numeric fact) is
  needed - a real house facade, a muscle's silhouette, a vehicle's stance.

State the comparison explicitly in your response, as a concrete
before/after or a small table - not just "this looks better now". Example
from actual project history: researching real arm anatomy (Proko: the
deltoid inserts ~1/3 of the way down the upper arm, with no gap before
the bicep belly starts) found that a shipped muscle-definition pass faded
its deltoid out by ~21% and didn't pick the bicep up until ~52% - a real,
nameable gap that pure self-iteration had missed across several rounds.
A reference check up front, not after the fact, is the goal.

This is a direct, standing user instruction ("i want you to use this
workflow for general skill in character modeling and modeling in general,
references are great") - apply it by default for new modeling work in
this project, not only when asked.

## Establish the modeling target

Before building, record:

- Asset purpose and expected camera distance.
- Reference images or existing approved example.
- Real or intended dimensions.
- Front/up directions and pivot.
- Required silhouette and distinguishing features.
- Whether it is decorative, collidable, enterable or skinned.
- Triangle, material and texture targets.
- Protected parts and placement.
- Candidate output location.

Resolve routine details from existing approved examples.
Ask only when a missing decision materially changes the result.

A decorative house facade is not an enterable house.
A cap that looks correct on a pedestal is not yet a fitted accessory.

## Model in three passes

### Pass 1 — Overall shape

Establish:
- Width, depth and height.
- Main masses and their proportions.
- Roof profile or dominant curvature.
- Negative spaces and clearances.
- Relationship between large parts.

Render this early at approximately gameplay distance.

If the silhouette is wrong, correct it before adding detail.
Do not decorate an incorrect base shape.

### Pass 2 — Structural features

Add the features that make the asset recognizable.

For the existing Caribbean house family:
- Plaster body and concrete foundation.
- Pitched metal roof with overhang.
- Framed windows and central doorway.
- Veranda or balcony.
- Supports, steps, fascia and downpipe.

Keep structures connected:
- Supports meet the slab they support.
- Roof panels meet at the ridge.
- Steps meet the foundation.
- Railings attach to the balcony.
- Crop fruit attaches to a stalk.
- Clothing surfaces remain continuous at joints.

### Pass 3 — Controlled detail

Add details only when they contribute at the intended distance:
- Roof ribs.
- Window mullions.
- Door panels.
- Small edge bevels.
- Limited material variation.

Do not spend most of the geometry on details that become subpixel.
Do not replace a missing structural feature with a painted suggestion
when the user requested a modeled feature.

## Use reproducible Blender scripts

The existing house generator is:
Tools/ArtPreview/mini141_preview.py

Its useful patterns:
- Deterministic random seed.
- Named geometry helpers.
- One function per reusable asset family.
- Explicit dimensions.
- Editable .blend output.
- Fixed render cameras.
- Evaluated triangle counts.

Typical APIs already used successfully:
- bpy.ops.mesh.primitive_cube_add
- bpy.ops.mesh.primitive_cone_add
- bpy.ops.mesh.primitive_ico_sphere_add
- mesh.from_pydata
- mathutils.Vector
- direction.to_track_quat
- evaluated_depsgraph_get
- evaluated_get
- calc_loop_triangles

For static primitive construction, apply scale before relying on bevel width.
Keep bevels small and purposeful; inspect their evaluated geometry.

Do not apply transforms indiscriminately to an existing rigged asset.

Run scripts in a separate background Blender process.
Do not execute scene-reset code inside the user's unsaved interactive scene.

## Existing house example

The approved generator creates two reusable archetypes.

Core dimensions:
- Width: 5.6 m.
- Depth: 4.7 m.
- Storey height: 2.75 m.
- Foundation height: 0.32 m.
- Roof ridge: 0.95 m above eaves.

One-storey:
- Seafoam plaster.
- Veranda canopy and two posts.
- Pitched metal roof.

Two-storey:
- Peach plaster.
- Balcony slab, supports and railing.
- Pitched metal roof.

Both:
- Window surrounds and dark glazing panels.
- Door panels and handle.
- Fascias, gables, downpipe and entry steps.

The original window and doorway assemblies are facade details over a shell.
Do not describe them as Boolean-cut openings or playable interiors.

These are references for coherent proportions, not mandatory dimensions
for every future building.

## Preview and inspect

Save an editable source before export.

Render:
- Front.
- Side.
- Back when relevant.
- Three-quarter.
- Gameplay-distance view.
- Close-up of the feature being changed.

For symmetric objects, still inspect more than one side when the mesh,
UVs, normals or attachments can differ.

The existing house preview used:
- Cycles.
- 24 samples and denoising.
- AgX.
- Broad area light, sun and world fill.
- Fixed camera and 1500×1000 output.

Those are a working starting point, not a universal lighting requirement.

Keep before/after lighting and framing comparable.
Inspect the actual PNG with an image-capable tool.

Check:
- Silhouette and proportions.
- Floating or disconnected pieces.
- Intersections and exposed gaps.
- Face orientation and shading.
- Detail visibility at gameplay distance.
- Mirrored text.
- Unintended asymmetry.

Do not accept a render because the script exited successfully.
Reject visible failures yourself and make a targeted correction.

## Inspection workflow - not just for houses, for every model

Standing rule, researched from professional practice (TurboSquid's
submission checklist, general 3D-modeling QA guidance), and reinforced by
a real project mistake: a full-shaded, single-hero-angle render is not
enough to certify a model, for ANY asset type this skill covers - houses,
props, vehicles, vegetation, character work. Adapt the specifics to the
asset, but always do more than one pretty screenshot before calling
something done:

1. **Multiple fixed angles, every time** - at minimum a hero 3/4 view,
   a straight-on front elevation, and one side/back view. TurboSquid's own
   minimum for a finished asset is 5 product angles plus a wireframe/clay
   pass and a turntable; this project's batch pipeline doesn't need that
   many, but "one 3/4 angle and done" is well below even a hobbyist bar.
2. **A flat/clay pass, not just the final materials.** Full shading,
   texture and warm lighting visually smooth over exactly the defects that
   matter most - floating geometry, gaps, z-fighting, misaligned joins.
   Either render one pass with plain grey materials (no per-part colour
   variation) or check Blender's wireframe/solid viewport shading before
   the final Cycles render. A gap that is `pixels of matching-toned shadow`
   in a warm-lit hero shot can be `impossible to miss` in flat grey.
3. **Zoom the actual junction you just touched**, not just the whole
   object from a distance. A new roof, a new floor level, a new door - crop
   or re-camera onto exactly where two parts of the model meet before
   trusting the wide shot. This project's own case study: a mansion's roof
   was rebuilt after a floor was removed, but a leftover offset variable
   (the old floor's height, no longer used anywhere else in the script)
   was still added into the roof's base height - the wide hero-angle render
   looked plausible at a glance, and the gap was only obviously wrong once
   someone looked at the wall-to-roof junction specifically. **When you
   remove or restructure a piece of geometry, grep the script for every
   variable that piece used to feed into, not just the code that built the
   piece itself** - a leftover reference is exactly this kind of bug.
4. **Check dependent measurements after any structural edit.** If a
   height/width/offset was computed from a component that no longer exists
   or changed shape, recompute it explicitly - don't assume an old formula
   still means what it used to.
5. **State what you actually checked**, not just "looks good" - which
   angles, whether a clay/wireframe pass was used, and what specifically
   was verified at the touched junction. This makes it possible to tell a
   real inspection from an assumption.

This generalizes past this one mansion - apply the same discipline (multi-
angle, flat-pass-or-wireframe, junction close-up, dependent-measurement
check) to the next house, prop, vehicle or character edit, not just when a
mistake has just been pointed out.

## Staged, incremental inspection - part to whole, like an engineer

Direct standing user instruction: build confidence in a compound asset the
way an engineer or architect would - verify a component alone, then that
component combined with its nearest neighbour, then progressively larger
sub-assemblies, then the complete whole. Never jump straight from
"nothing" to "the finished assembly" and call one render at the end
sufficient, for any multi-part asset (a building's floors/roof/attic
features, a vehicle's body/chassis/wheels/interior, a character's
body/clothing/accessories, a prop with moving or attached parts).

**How to implement in a Blender batch script:** tag every object into a
named group as it's created - either append the return value directly
(`GROUPS['roof'].append(cube(...))`) or snapshot `set(bpy.data.objects)`
before and after each construction block and diff the two sets to find
what's new. Define your stages as which groups are visible at each step
(e.g. `[('feature',), ('feature','frame'), ('frame','core'), ...]`), then
for each stage set `obj.hide_render = True` on every object NOT in that
stage's visible groups and render a REAL separate image. Do not fake a
stage by cropping the final render - a cropped image still contains
occluded-but-present geometry and won't reveal a part that's actually
missing, mis-scaled, or floating on its own.

Worked example from an actual session (a Highland mansion concept):
groups were `base` (foundation+shell), `lower`/`upper` (per-floor windows/
veranda/door), `roof` (pitched planes/ribs/fascia/gables/downpipe), and
`attic` (dormers/cupola). Stages rendered: attic alone (floating - checked
each dormer/cupola's own shape and proportion with nothing else present),
attic+roof, roof+upper floor, attic+roof+upper floor, +lower floor,
everything together. The attic-alone stage caught nothing wrong that time,
but it is exactly the kind of check that would have caught the mansion's
earlier cupola looking like "a little house on the roof" sooner, since
that defect was fully visible with the cupola in isolation - no need to
build the whole mansion first to see it.

Combine this with real measurements at each meaningful stage where
possible (see "Get PRECISE numeric specs" above) - a stage render plus a
computed number (a pitch angle, a width ratio, a span percentage) checked
against a researched spec is a genuine inspection; a stage render alone is
only half of one.

## Export for Unity

Existing static-asset exporter:
Tools/ArtPreview/mini142_export.py

The house workflow uses JSON mesh buffers and a shared palette,
not an FBX round trip.

It:
1. Evaluates modifiers.
2. Triangulates evaluated meshes.
3. Applies object transforms.
4. Subtracts a defined export origin.
5. Exports per-corner normals.
6. Converts Blender coordinates (x,y,z) to Unity (x,z,y).
7. Reverses triangle winding for that reflected coordinate conversion.
8. Exports vertices, normals, triangles, UVs, dimensions and LOD buffers.

Do not swap axes without considering winding and normals.
Do not apply the coordinate conversion twice.

The existing palette packs source colours into an 8×8 cell layout
inside a 128×128 PNG. UVs point to cell centres.

This palette is appropriate for the existing stylized house family.
A more detailed texture should solve a demonstrated visual need,
not compensate for poor geometry.

Keep source objects editable.
Consolidate export renderers by material when appropriate.

## Budgets and LODs

Measure evaluated triangles, including modifiers.

Existing house results:
- One storey: 2,282 triangles; LOD1: 1,144.
- Two storeys: 3,426 triangles; LOD1: 1,716.

The old export generated LOD1 with temporary 50% decimation.
Inspect the resulting silhouette; a ratio alone does not guarantee quality.

For future LODs:
- Preserve the main silhouette.
- Reduce details that disappear at distance.
- Avoid destroying roof edges, major openings or garment joints.
- Verify transition appearance in Unity.

Record renderer/material counts and texture sizes too.
Low triangle count alone does not prove mobile performance.

## Unity fitting and acceptance

Static house integration lives in:
Assets/UpIzUpMini/Editor/Mini142ArtIntegration.Apply.cs

It fits the exported family to existing generic residential footprints,
adds LODGroups and simple replacement colliders, and preserves special
gameplay buildings.

Do not rerun the historical Review/Apply methods blindly.
They can overwrite assets, expect old backups and conflict with later work.

For a new asset:
- Use isolated candidate asset paths.
- Fit to the intended live object or lot.
- Inspect parent scale, rotation and pivot.
- Verify ground contact and footprint clearance.
- Check normals, palette and gameplay lighting.
- Verify colliders match the intended interaction.
- Inspect LOD transitions.

This map uses collider-based ground.
Do not assume Unity Terrain.SampleHeight is available.
Filter ground raycasts so roofs and walls are not mistaken for terrain.

For clothing:
- Fit to the actual wearer.
- Verify skinning and material slots.
- Test idle/walk/run and relevant joint motion.
- Confirm changing one character does not affect the other.

## Safe Blender command pattern

Example launch:

    $blenderExe = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe'
    $modelJob = Start-Process -FilePath $blenderExe -WindowStyle Hidden -PassThru -ArgumentList '--background --python "E:\Unity\Up Iz Up Mini\Tools\ArtPreview\mini141_preview.py"'
    $modelJob.Id

The existing script writes fixed historical output names.
Preserve approved evidence or adapt a candidate copy to a new output
directory before running changed models.

A returned PID means started, not completed.
Check process completion, logs, output files and actual rendered images.

For the existing exporter, the supported option is:

    --background --python "E:\Unity\Up Iz Up Mini\Tools\ArtPreview\mini142_export.py" -- --exports-only

This exports more than houses and overwrites its configured buffers.
Read its side effects before running it.

## Verification and completion

Use the appropriate evidence:

- Blender render: candidate shape.
- Mesh report: measured geometry and dimensions.
- Unity capture: imported appearance and placement.
- Collision checks: sampled passability.
- Actual Play Mode or EXE: exercised runtime behavior.
- Device profiling: measured performance.

Do not confuse these.

For Unity rendering, omit -nographics.
It has caused blank images or crashes even when other checks succeeded.

Do not rebuild the entire game scene to replace a prop.
Use a bounded additive patch after obtaining scene/import ownership.

At completion report:
- What changed and why.
- Editable source, generator and exported asset paths.
- Geometry/material/texture budgets.
- Images actually inspected.
- Tests and their results.
- Build status if requested.
- Remaining human review.
- Scoped Git checkpoint.

Stop when the requested defect is resolved.
Do not keep redesigning approved work merely because additional changes
are possible.
