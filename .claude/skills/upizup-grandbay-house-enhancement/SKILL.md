---
name: upizup-grandbay-house-enhancement
description: Enhance Up Iz Up Mini's Grand Bay houses (Lalay first, then expansion housing, school area and landmarks) from the user's reference photo set - audit, house-kit variants in the approved house style, frontage re-placement, and image-confirmed stages on an isolated copy scene. Use for "enhance the houses", "make Lalay look like the photos", house kit variants, or any photo-driven Grand Bay housing pass. For one new landmark building use upizup-grandbay-landmark-buildings; for Blender house modeling detail use upizup-building-modeling; for live-map seam/fit repairs use upizup-map-house-repair.
---

# Up Iz Up Mini - Grand Bay House Enhancement (photo-driven, staged)

Distilled from MINI-182 (plan written 2026-09-29). Full plan: `Docs/WorkPackets/MINI-182.md`. Stages 1-3 (audit, house kit, Lalay) were requested first, each confirmed with images. Update this skill after each stage with what worked and what did not.

## Locked decisions (user, 2026-09-29)
- KEEP the current approved house style (proportioned plaster / pitched-roof Caribbean houses; see memory "preferred house modeling style"). Photos drive variety, colour, density and building types, not a rougher art style.
- Start with Lalay.
- Every stage ends with rendered images sent to the user (SendUserFile) and waits for approval before the next stage.

## Source photos
Folder: `G:\Other computers\My Laptop\Monroe University\Courses\Game images` (18 JPGs; the FILENAMES say what each shows). Groups: Lalay top/closer/dense views, Lalay bay-to-hill and bay-to-Lalay, colourful primary school beside Lalay, wall by the bay, roundabout to the field, Geneva playing field, Pierre Charles Secondary School, housing complex before the school.
- Read the actual images (Read tool) before each stage; do not work from memory of this file. These are aerial video stills (watermarked); they support building types, colours, density and layout, not surveyed footprints. Say so.
- Copy the ones you use into `Docs/Maps/dm-dom-grand-bay-expansion-v1/Reference/` only if the user agrees (they are the user's files; do not publish them).

## What the Lalay photos show (verified by viewing them)
- One long straight main road, cars parked along one side, houses directly on the frontage with almost no setback.
- Dense, irregular mix of 1-2 storey houses; heights and depths vary house to house; gable and low-slope corrugated roofs (grey, rust, red, a few green/teal).
- Wall colours: red/coral, teal, mint, sky blue, yellow, cream, white, pink, plus bare concrete and unfinished concrete-frame buildings; some shop-fronts on the ground floor and balconies with railings.
- Banana and palm clusters in back yards; forest slope behind; scattered houses climbing the hill.
- Wall by the bay: low concrete wall painted in repeating red / yellow / green / teal bands; rock beach and seaweed seaward, grass bank landward.
- Apartment complex near the school: peach walls, red hip roofs with solar panels, balconies.

## House Model System (data-driven, integrated with the modeling skills)
The user wants ONE reusable system for house models, not one-off houses (memory: design as reusable systems; house modeling style). Today `house(x,two)` in `Tools/ArtPreview/mini141_preview.py` is hard-coded (5.6 x 4.7 core, 2.75 m per storey, .32 foundation, gable roof with ribs, balcony or veranda, palette by index). Stage 2 turns that into a table-driven generator. Process comes from `reference-driven-game-asset-production` (classify -> define correct -> recognition map -> plan parts -> staged passes -> inspect -> acceptance gates) and the mandatory 10-step sequence; house specifics from `upizup-building-modeling`; Blender setup from `upizup-blender-modeling`. Follow them; this section only adds the house-specific system.

**Classification (gate 1):** houses are architecture, mid-distance gameplay viewing (street level to overhead), static, mobile budget, one shared palette. Recognition map for a Lalay house, in priority order: (1) footprint/massing and storeys, (2) roof form and colour, (3) wall colour, (4) ground-floor use (door, shop opening, veranda), (5) balcony/rail, (6) window rhythm, (7) small trim. Get 1-3 right before any of 4-7.

**HouseDefinition table** (one row per variant; a plain JSON/ScriptableObject-shaped file such as `Tools/ArtPreview/house_definitions.json`, read by the Blender generator and later by the Unity placer):
- `id` (stable, e.g. `lalay_flat_concrete_2s`), `family` (plaster_gable, flat_concrete, hip_red, corrugated_gable, shopfront, concrete_frame, board_house), `storeys` (1-2), `footprint` (w, d in metres, allowed range), `storeyHeight` 2.75, `foundation` .32.
- `roof`: type (gable / hip / flat / shed), pitch, overhang, material slot, ribs on/off.
- `facade`: front features (door, shopOpening, veranda, balcony, rail), window count/rhythm per storey, side windows on/off, trim (course band, fascia).
- `palette`: named slots (wall, roof, trim, door, rail, accent) each pointing at a palette cell; allowed wall/roof colour sets taken from the photos so tinting stays photo-plausible.
- `budget`: max triangles LOD0/LOD1, `lod`.
- `notes/source`: which reference photo(s) motivated it. Row status: draft / proved / approved.
Rule: no house dimension or colour lives only in code. Adding a variant = adding a row (plus, at most, one new part builder), never copying the function.

**Generator structure:** small part builders (`foundation`, `shell`, `storey_course`, `windows`, `door`, `shopfront`, `balcony`, `veranda`, `roof_gable/hip/flat/shed`, `fascia`, `downpipe`) composed by `build_house(row)`. Same builders serve the existing one/two-storey rows, so the approved look is a row (regression: the two existing houses must re-render unchanged). Export through `mini142_export.py` unchanged (Unity X,Z,Y, reversed winding, shared 8x8-cell 128x128 palette; append new colours, never reorder existing cells, or every existing house re-tints).

**Unity side:** `Mini142ArtIntegration` already fits a model to a footprint. Placement chooses a definition id per lot (deterministic by lot index or a stored id, no per-run randomness so results are reproducible), fits non-uniformly in X/Z only, never scales height, and records the id on the instance. Keep a per-map manifest (`Logs/Tasks/MINI-182/HouseAssignments.csv`: house root, definition id, colour set, position) so later passes are additive and auditable.

**Proof discipline for the kit:** prove ONE new variant fully (blockout compared with its photo, then construction, then fixed-camera turnaround: front, side, top, 3/4, plus Unity in-game street view) before building the next. Fixed cameras/lighting across iterations. Name the three biggest discrepancies vs the photo before polishing. Remove a detail after 3 failed attempts rather than ship it broken. Save evidence and label unverified explanations as hypotheses.

**Acceptance gates for a variant:** silhouette recognisable from the photo type at street distance; proportions within the row's ranges; triangle/material budget met; renders in Unity with the palette shader; no z-fighting; approved style preserved (same plaster/trim language as the approved houses); user approves the turnaround image.

## Stage 1 - Audit (read-only, no scene saves)
1. Open the isolated copy scene (see "Scenes"). List every house-like root: `ApprovedHouse_MINI142`, `Lalay_House_*`, `Lalay_Home_*`, `Highland_*` massing, `ExpansionHouse_*`, special/scripted buildings (safehouses, Credit Union, shops). Record name, world position, rotation, footprint (renderer bounds), storeys/height, active state, parent, frontage side and distance to nearest road.
2. Classify: approved house-art / procedural home / grey massing / special (do not touch).
3. Write `Logs/Tasks/MINI-182/Audit.txt` + CSV. Render the current Lalay street, top-down and one 3/4 view at angles matching the photos (fresh Unity process, no `-nographics`). Send them with the photo names they pair with.
4. Report counts, obvious problems (overlaps, houses on roads, floating), and what is protected.

## Stage 2 - House kit
- Implement it as the House Model System above (definition table + part builders), extending the approved Blender generator (`Tools/ArtPreview/mini141_preview.py` -> `mini142_export.py` -> `Mini142ArtIntegration.*`) rather than inventing a new pipeline; it exports JSON buffers with a shared palette texture. Read `upizup-building-modeling` and `upizup-blender-modeling` first; follow the mandatory 10-step modeling sequence.
- Variants to add from the photos, each proved on ONE house first, then rendered as a turnaround sheet: flat-roof concrete two-storey; red hip-roof two-storey; corrugated gable (grey/rust); green/teal-roof house; shop-front ground floor; unfinished concrete frame; wooden board house.
- Budget: mobile first. Target < 1,500 triangles per house, one shared palette material, LOD0/LOD1 as the existing pipeline does. Check the palette texture can carry the photo colours before adding materials.
- Judge the Unity result (palette shader, gameplay lighting), not the Blender studio render.

## Stage 3 - Lalay pass
- Existing positions the user tuned are authoritative. Default: restyle in place. Re-place only houses that overlap, sit on the road or float, and log each move. Ask the user before wholesale re-layout.
- Preserve each replaced root's position/rotation; DISABLE originals (rollback), never delete.
- Reuse the additive fit pattern (MINI-142 `PatchHouses`, MINI-173/174 spacing: oriented-rectangle separating-axis test .18 clearance, never scale height, sample footprint grid against road colliders, ground raycast only on `*Terrain*` colliders).
- Assign variants so neighbours differ (colour + roof + storeys); density and irregular setbacks like the photos; add banana/palm clusters and back-yard fences as cheap merged props.
- Validate on a fresh scene reload: no house on a road, no overlaps, all grounded; log PASS markers and exit 1 on failure. Numeric checks do not prove it looks right: render and look.

## Scenes and safety
- Work in an isolated copy (`GrandBayProof_HouseEnhance.unity`) created from live by save-as; hash live before/after and abort on change. Never edit `GrandBayProof.unity` until the user approves. Keep it out of build settings.
- After MINI-180 the expansion is IN `GrandBayProof.unity` (older skills say to work in `GrandBayProof_ExpansionImport.unity`; that is now historical). Check `PROJECT-HANDOFF.md` current claim first.
- Backup: tag `backup/pre-mini182-house-enhance-2026-09-29`, ref `refs/backup/mini182-worktree`, folder `Backups/MINI-182-pre-house-enhance-2026-09-29/`.
- Scenes are binary: change them only through Editor APIs. Do not run historical whole-world builders.

## Running Unity
`C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe -batchmode -quit -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.<Class>.<Method> -logFile Logs\<name>.log`. Renders: no `-nographics`. Play Mode validators: no `-quit`. Wait for the log's PASS marker; a returned process is not success.

## Honesty rules
- Photo-inspired, not photo-matched or surveyed: say "stylised, based on aerial stills".
- Never claim a stage done without having looked at its images.
- Record every relocated house and every disabled original.

## Worked example: flat_concrete_2s (MINI-182, approved)
Commands: `blender -b -P Tools/ArtPreview/render_house_turnaround.py -- <row_id> <out_dir> [compare_row]` (front/side/top/34 sheet; run once alone, once with a comparison row), then `blender -b -P Tools/ArtPreview/house_export.py -- <row_id>` (one JSON per wall colour + superset palette), then Unity `Mini182HouseKit.Run` (import + prefabs + test row in `GrandBayProof_HouseEnhance.unity`) and `Mini182HouseKit.Render` (no -nographics). New variant = new JSON row + at most one new part builder; reuse the scripts. Pitfalls: coping must be a ring not a slab; ignore huge bounds when searching a free lot; palette cell row r is at Unity GetPixel y=r*16 (PNG rows are top-down); never reorder palette cells; export additively.
- Multi-family palette rule (learned on hip_red_2s): export is append-only from the latest MINI-182 palette; after any exporter change re-export every approved family in order. Hip roof: 45 deg hips, ridge half-length = hx - hy, ribs perpendicular to the eave ending on the hip line. Run the Unity kit per family: `set MINI182_FAMILY=<family>` before `Mini182HouseKit.Run/Render`.
