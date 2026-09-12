---
name: upizup-blender-modeling
description: Model and refine stylized game assets for Up Iz Up Mini using Blender Python, inspectable previews, measured budgets, and verified Unity integration. Use for houses, props, vegetation and scoped mesh improvements.
---

# Up Iz Up Mini — Blender Modeling Skill

## Purpose

Produce recognizable, well-proportioned, editable 3D assets that look correct in the actual game.

Use the user's references and existing approved work. Do not substitute arbitrary primitive arrangements, unverified exports or attractive studio renders for finished game assets.

This skill guides modeling decisions. It does not authorize unrelated changes, purchases or replacement of approved assets.

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
