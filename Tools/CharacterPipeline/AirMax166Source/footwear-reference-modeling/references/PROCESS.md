# Air Max modeling process and Claude continuation

This is a standalone MINI-166 preview handoff, authored 2026-09-13. Read this file, MEMORY.md, both measurement JSON files and the executable generator before making changes. `build_airmax.py` is the full modeling recipe, not pseudocode. `fix_pass2.py` through `fix_pass5.py`, the first-pass source and rejected renders preserve what changed during inspection.

## Scope and ownership

The user requested improved Air Max 90/97 models, thinner soles, visible Air bubbles and closer shape, plus complete documentation. The Mini project currently has Claude's cap-editing reservation in PROJECT-HANDOFF.md. Codex therefore made these previews entirely in this separate artifact directory. No project claim was stolen; no scene, prefab, game asset, skill, package, build or save was changed. These are editable proposals, not shipped wardrobe replacements. Do not run old broad wardrobe generators to integrate them.

The game project is `E:\Unity\Up Iz Up Mini`. `E:\Unity\Up iz up` is a different, read-only reference project. The shell's default cwd was the reference project; every project read used an explicit workdir. The sandbox helper failed for normal shell/image calls, so approved elevated calls were used for reads and artifact generation. This is an environment failure, not a modeling requirement.

## Tools actually used

- `C:\Program Files\Blender Foundation\Blender 5.0\blender.exe`, reports Blender 5.0.1. Background Python via `--python`, Blender `bpy` and `mathutils`. No Blender plugin/MCP server was used.
- PowerShell: file inspection, explicit process invocation, logs, isolated file writes and reading rendered images.
- `C:\Python314\python.exe` plus Pillow: downscaled JPEG inspection copies and contact sheets. Those copies show actual Blender renders; no AI-generated replacement image or painted correction is used.
- Web search and official Nike pages: inspect structural product photos. Local reference files are only research evidence, not included as model textures.
- `skill-creator/SKILL.md` supplied the portable skill structure: concise entrypoint, detailed supporting references, validation, evidence-backed memory. It supplies organization, not automatic modeling ability.
- No paid assets, generation credits, external model generation, hidden artist work, subagents, Unity instance or EXE build were used.

## References and what they establish

1. Nike AM90 IB7680-101: https://www.nike.com/gb/t/air-max-90-mens-shoe-exykZV6r/IB7680-101
2. Nike AM97 Silver Bullet DM0028-002: https://www.nike.com/sg/launch/t/air-max-97-silver-bullet
3. Nike's AM97 product description establishes full-length Air: https://www.nike.com/ca/t/air-max-97-mens-shoes-NxjJ4UxM

Official product photos were actually inspected at large size. `reference90.png` shows white/turquoise AM90 structural panels; `reference97.jpg` shows Silver Bullet. The 90 preview uses infrared accents to make components easier to distinguish, not an exact recreation of the photographed colourway. The inspected photos establish visual relationships, not manufacturing dimensions. No shoe was physically measured or scanned. No complete tutorial video was watched during this task.

Observed 90 landmarks: low forefoot; rising heel foam; heel-only side Air window; layered toe/vamp and quarter panels; moulded lace supports; visible tongue; dipped padded collar; separate rubber tread. Observed 97 landmarks: long Air visibility with a rear support bridge, flowing upper bands, smaller midfoot mark, mesh strips between synthetic overlays, low rounded forefoot, tongue and rear pull loops.

## Existing models: source-based diagnosis

The live C# `Mini166RepairAccessories.cs` shoe function had a uniform 61mm sole-top elevation before the upper began. The 90 row heights were 0/13/22/48/61mm and the 97 0/12/23/46/61mm. Its side windows were quads against a continuous sole. The upper used a cosine taper toward a point, not a designed open collar.

Claude's untracked `Tools/ArtPreview/air_max_style_shoe_preview.py` was read but not edited: 11mm outsole plus 40mm heel midsole; lens centred on a rear-facing heel surface. These observations explain thickness and visibility risks. No new live-game screenshot baseline was captured in this task; do not claim a same-camera old/new game comparison.

## Coordinates and measurement provenance

Authoring coordinates are millimetres. Helpers divide by 1000 exactly once when constructing Blender vertices, curve points and primitive transforms. Blender scene units are metric with millimetre display; actual coordinates remain metres.

- X: shoe length; heel -145, toe +145. Nominal outer length 290mm.
- Y: width. Negative Y is the presented lateral view. Both sides are built, currently symmetric; a clinically anatomical left/right last is not claimed.
- Z: elevation. Nominal outsole contact near 0; discrete tread extends slightly below that. Ground is a presentation object.
- Width is a longitudinal table, not a single ellipse: broad forefoot, narrower waist, rounded heel and toe. AM97 multiplies the AM90 half-width table by .95.
- Profile samples use a bounded Hermite interpolation. First pass used smoothstep per segment and exaggerated small undulations; the final profile uses neighbouring slopes and clamps each interval to its endpoint range.
- Sole top: heel about 30mm; forefoot about 20mm; toe-spring adds up to 10mm near the tip. This is an authoring choice responding to the user's thick-sole complaint, not a claim of exact Nike stack height.

All width/top stations, Air cavity centres/lengths/heights, resulting budgets and camera transforms are in `AM90/measurements.json` and `AM97/measurements.json`. Exact formula parameters are in the source; regenerate JSON after changing them. Never write a static measurement table that silently diverges from the generator.

## Construction, in executable order

1. Clear the isolated Blender scene, create named material nodes and establish units. Output paths are derived from the script location, not the live project.
2. Define width, sole elevation, toe spring and upper-height profiles independently. `top()` must remain above `base()` at every sample; `surf()` clamps local half-width positive. Otherwise the forefoot reverses/inverts where the upper becomes lower than the rising toe sole.
3. Build upper side patches from longitudinal X and cross-sectional angle. The opening-angle table preserves the rear collar and throat; forefoot patches close into a vamp. Keep correct outward winding: for this parameterization positive-Y faces need reversing.
4. Add Solidify inward to the textile upper; its thickness is .9mm. Add inner lining and a continuous padded edge curve. Footbed sits below the opening and does not close it at collar height.
5. Add actual layered mudguards and quarter/toe overlays using the same surface evaluator. Their slight offsets are surface-relative. A normal error in this shell originally made Solidify grow into overlays; moving panels farther away would have hidden the cause.
6. AM90 uses separate suede quarter and toe overlays with dark mudguard. AM97 uses four silver ripple bands and reflective edge curves. These are independent surface patches, not colours assigned to one arbitrary generic upper.
7. Build the tongue as an arched longitudinal grid from -52 to +73mm, with padding and top-edge piping. The shared `tongue_surface(x,y)` function describes both the tongue and attached lace heights.
8. Add AM90 TPU supports or AM97 textile loops. Six lace rows run at X=59,44,26,8,-10,-28mm. Ribbon laces cross between alternating rows with 2mm base clearance and an additional 1.6mm sinusoidal lift for the over-strand. The ribbon is a closed rectangular section, not camera-facing quads. Each path uses 25 samples. The bow and tails are separate paths.
9. Create thin rubber outsole (4.2mm), then closed foam solid following the plan. Both use the same toe-spring function. Curved sidewall polygons are smooth; top/bottom caps remain flat.
10. Apply rounded closed boolean cutters through the full width of the foam. AM90 has one heel tunnel. AM97 has a small rear tunnel and a long front/midfoot tunnel separated by a structural bridge. Rounded cutter dimensions are in JSON. Remove only temporary cutter objects afterward.
11. Build translucent elliptical-section bladders inside each opening. Their paths follow local width instead of a fixed tangent plane. Add interior pillars; centres remain inward of the bladder's visible face. Air material uses transmission and clearcoat in Cycles; this shading must be assessed separately on export.
12. AM90 adds a coloured ring surrounding its Air opening and small moulded ribs. This ring has a real hole; it is not a solid plate over the bladder. Separate outsole tread elements add detail without thickening the entire sole.
13. Side markings are tessellated in a two-dimensional drawing plane, subdivided with barycentric samples, then projected onto the upper using `side_xyz()`. A single non-planar n-gon originally produced an angular floating patch. Blender 5's tessellation output in this run was indices; resolve indices to vectors before interpolation.
14. Add pull loops with a stable ribbon frame. Near-vertical paths need a fallback lateral axis when world-Z cross tangent approaches zero. Without it, ribbons collapse on vertical segments.
15. Add small toe knit geometry, retain editable components, export selected asset objects only, collect evaluated mesh counts, save Blender source and render.

## Materials and lighting

Named materials distinguish foam, rubber, textile lining, mesh, suede, silver synthetic, reflective piping, red TPU/embroidery, lace fibre, seam thread, translucent bladder and internal supports. Procedural Noise-to-Bump is used at submillimetre distance; it is preview microtexture, not a completed production UV bake. Small knit dashes are actual curves in the source and contribute to evaluated triangle count.

Final studio: Cycles 32 samples with denoising; AgX, Medium High Contrast, exposure 0; world RGB .27/.30/.35 at strength .35. Area lights: Key (100,-330,490)mm / 3.2W / .45m diameter; Fill (-170,290,310) / 1.5W / .35m; Rim (-350,-30,300) / 2.5W / .28m. Initial 18/11/14W lighting washed out material differences at this small scale and was reduced. Do not infer material colour from that rejected image.

## Elevations and fixed inspection cameras

Images are 1440x960, orthographic. Camera locations, target and scale are authored in mm below; saved JSON contains actual Blender transforms in metres/radians.

| View | Position | Target | Ortho scale |
|---|---|---|---|
| Three-quarter | 340,-540,280 | 0,0,50 | 350 |
| Lateral | 0,-700,54 | 0,0,50 | 335 |
| Plan | 0,0,700 | 0,0,0 | 335 |
| Toe elevation | 700,0,50 | 0,0,50 | 165 |
| Heel elevation | -700,0,58 | 0,0,50 | 165 |
| Clay | same as three-quarter | same | same |

The lateral and heel views have a tiny target-height difference, recorded rather than silently called mathematically exact elevations. Contact sheets label orientation. A fixed comparison camera is used to evaluate revisions; geometry is not changed to face a render camera.

## Exact repeat commands

Run in the artifact directory, NOT a Unity Assets folder. Re-running overwrites the selected model's source/export/current named renders; archive approved files first.

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --python build_airmax.py -- --model 90 --views hero,side,top,front,back,clay --samples 32 *> build90-final.log
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --python build_airmax.py -- --model 97 --views hero,side,top,front,back,clay --samples 32 *> build97-final.log
python make_review.py
```

For a geometry-only regeneration use `--views ''` only if the calling shell preserves the empty argument; otherwise use the saved Blender source for export. Do not interpret Blender's zero exit status as success: a Python exception can still return zero. Require `AIRMAX_PREVIEW_COMPLETE`, output timestamps and render inspection; inspect tracebacks explicitly.

## Verification and evidence limits

The standalone validation report records evaluated object/triangle counts, finite coordinates, bounds, material slots and GLB reopening. Render inspection establishes static appearance only. A phone-sized image is a readability check, not GPU profiling. Procedural bump may be lost in GLB, and transmission may render differently in Unity. The large editable object count is unsuitable as-is for a repeated mobile wardrobe asset.

Not performed here: production retopology/LOD baking, exact-size last fitting, left/right anatomical asymmetry, Franki/Sacat bone binding, toe deformation, animation/motorbike contact, tint/save/menu regression, Unity compile/play tests, runtime transparency evaluation or EXE build. Those claims require the next integration stage. No user visual approval has been assumed.

## Safe game integration after visual review

1. Read the Mini project's current AGENTS.md, CURRENT.md, MINI-166 history and live ownership block. Arrange a non-conflicting reservation; do not override Claude's cap work.
2. Preserve the approved authoring files and renders. Create a new staged asset folder and controlled exporter. Reduce object/material count, bake necessary texture detail, and make a separate measured LOD. Preserve the silhouette, openings and dominant band/lace shapes.
3. Inspect actual Left/RightFoot and ToeBase bind transforms on both characters. Convert Blender coordinates exactly once; verify handedness, winding, metre scale and forward direction in an isolated Unity prefab.
4. Decide rigid Foot binding versus toe-weighted deformation using the existing animation needs. Test both feet and both characters. A zero-pose preview is not proof of running fit.
5. Replace only the appropriate shoe-slot meshes in a controlled test scene. Preserve item IDs, colours, ownership, save fields, other garment selections and all cap/hair accessories. Do not rerun the broad old integration script over newer work.
6. Compile, run the existing wardrobe selection/persistence tests, capture idle/walk/run and bike-foot placement, then capture at actual game/portrait distance. Test transparent Air shading or a deliberately authored opaque fallback on target hardware.
7. Record exact commands/logs, budgets and remaining visual decisions; update the project ledger under MINI-166, commit only reserved files, release ownership. Build only when requested or required by the agreed integration scope.

## Reusing the workflow for other models

Transfer the process of observed landmarks, separate plan/elevation dimensions, component-specific geometry, controlled attachment frames, local experiments, fixed-camera inspection and verified memory. Do not transfer a shoe cross-section algorithm to houses, faces or vehicles merely because it ran successfully. Select a new construction model from the object's physical structure and reference requirements.

## Final inspection additions

The plan view exposed a pointed forefoot endpoint despite plausible side shape. `width()` now uses an elliptic end cap for X>=115mm (radius 30mm along X, 39mm half-width scaled .95 for AM97) instead of interpolating the final table values with a finite endpoint slope. The station table remains input history; `profile-samples-mm.csv` is the effective final profile sampled every millimetre.

Rear elevations exposed missing end closure and the AM90 rear badge partly inside the shell. Final source adds end cross-section caps, raises the rear-most upper stations to a coherent heel height, extends the mudguard/ripple bands to the end and solves the rear badge's X attachment independently for each Y/Z vertex. This is why hero-only approval was insufficient. Exact correction is preserved in `fix_pass7.py`.

Additional exact verification commands:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --python validate_models.py *> validation.log
python sample_profiles.py
python make_review.py
python -m pip install --target .tooling PyYAML --disable-pip-version-check *> dependency-setup.log
$env:PYTHONPATH=(Join-Path (Get-Location) '.tooling')
python C:/Users/PCSS-PC/.codex/skills/.system/skill-creator/scripts/quick_validate.py footwear-reference-modeling *> skill-validation.log
```

PyYAML was installed into `.tooling` in this artifact folder only because both available Python runtimes lacked it. The skill validator then passed. Its pass establishes valid skill structure, not expert modeling capability. The portable handoff excludes that disposable dependency folder; install PyYAML locally only if rerunning the validator is necessary.

The artifact folder is not a Git repository. No commit was made in either game project, and no unrelated dirty work was staged. Final file hashes in `SHA256SUMS.txt` identify this delivery instead.
