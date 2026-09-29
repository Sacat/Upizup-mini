# MINI-182: Enhance Grand Bay houses and key areas from the user's reference photos

Date: 2026-09-29. Status: PLAN (no scene edits yet). Owner: Claude.

## Backup (done before planning)
- Tag `backup/pre-mini182-house-enhance-2026-09-29`; ref `refs/backup/mini182-worktree` (whole uncommitted tree).
- Folder `Backups/MINI-182-pre-house-enhance-2026-09-29/` (live GrandBayProof.unity + meta, ExpansionImport scene, expansion-v1 assets). Live scene hash verified identical to the copy.
- All work happens on an isolated copy scene (`GrandBayProof_HouseEnhance.unity`), never on live until approved.

## Reference set (18 photos, `G:\Other computers\My Laptop\Monroe University\Courses\Game images`)
Filenames say what each shows. Groups: Lalay top/closer/dense views (7), Lalay bay-to-hill and bay-to-Lalay (3), wall by the bay (2), roundabout (2), Geneva field (1), Pierre Charles Secondary School and housing beside it (3).

## What the photos show (facts to build to)
- Lalay: one straight road (parked cars one side) with dense, irregular 1-2 storey houses right on the frontage; many colours (red, coral, teal, mint, blue, yellow, white); roofs mostly corrugated grey/rust/red, some green/teal; lots of mixed sizes, back yards with banana/palm trees, small shops and concrete-frame unfinished buildings. Upslope rear houses scattered, forest behind.
- Wall by the bay: low concrete sea wall along the road painted in a repeating red/yellow/green/teal band, rock/gravel beach and seaweed on the sea side, grass and rock cut bank on the land side.
- Roundabout: small oval grass island with an orange cone, worn asphalt, a sand/graded lot behind, small red/blue roadside shack, painted wall on the sea side, boulders below.
- Geneva field: big open cricket oval, boundary rope, pale pitch strip, small two-storey pavilion, low stands, steel footbridge to the right.
- Pierre Charles Secondary School: L/U-shaped blocks with grey roofs and orange/yellow upper walkway walls, basketball/court yard, old pale block, greenhouse-like sheds, road along the front; white long block and an orange-roofed apartment complex (peach walls, red roofs, solar panels, balconies) beside it.

## Plan (each stage ends in a fixed-camera review you approve before the next)
1. **Audit (read-only).** Count and classify every house now in the map (86 ApprovedHouse_MINI142, ~52 expansion massing boxes, Lalay/Highland massing) with position, footprint, storeys, frontage side; render the current top view of Lalay, school and Highland in the same angles as your photos for side-by-side comparison.
2. **House kit.** Extend the approved Blender house generator (proportioned plaster/pitched-roof style) with variants taken from the photos: flat-roof concrete two-storey, hip red-roof two-storey, corrugated gable, green/teal roof, shop-front ground floor, unfinished concrete-frame, wooden board house. Shared Palette material + per-house tint from the photo colour set. Budget: mobile-friendly, target under 1,500 triangles per house, one material.
3. **Lalay frontage pass.** Re-place Lalay houses along the straight road at photo density (tight frontage, irregular setbacks and heights, parked-car bays one side), keeping every existing house position you tuned as authoritative unless it clashes; add banana/palm clusters and back-yard fences. Verification: no house on the road, no overlaps, all on ground.
4. **Expansion housing.** Replace the 52 grey massing boxes with kit houses; build the orange-roof apartment complex beside PCSS from its photos (three connected 2-storey blocks, red roofs, balcony rails).
5. **PCSS refit.** Compare the current school to the aerial photos and correct block shapes, roof colour, orange walkway wall, court yard and front road.
6. **Landmarks.** Painted red/yellow/green/teal bay wall along the coastal road with rock beach, roundabout details (oval island, cone, shack), Geneva pavilion and footbridge details.
7. **Mobile/perf and integration.** Static batching, LOD or distance cull, triangle count check versus the current 217k extension budget, NavMesh/minimap/area-name updates only if positions moved.
8. **Verification and handoff.** Fresh-process renders vs each reference photo, measured checks, Windows build for hands-on test, update Docs/Systems/MapGeneration.md and PROJECT-HANDOFF, commit; promote to the live scene only after you approve.

## Acceptance criteria
- Each stage's renders match the corresponding photo's colours, density and building types to your satisfaction.
- No new road/house overlaps; live scene hash unchanged until promotion; backup restorable.
- Triangle and material counts stay within the mobile budget.

## Decisions I need from you
1. Quality target: keep the current approved house style (recommended) or go rougher/closer to the photos.
2. Should I keep every existing Lalay/Highland house position and only restyle, or may I re-place houses to match the photo layout?
3. Order: start with Lalay (recommended, biggest visual win) or PCSS/apartments first.


## Progress log
### Stage 1 - Audit (2026-09-29, done, images approved)
`Mini182LalayAudit.cs` (read-only, live scene hash unchanged). Active: 85 approved-art, 52 expansion massing, 5 Highland massing, 31 procedural (masonry) homes; 31 old Lalay massing inactive. Lalay strip 82 houses; none on a road; 4 within 3m of a road. Evidence: `Logs/Tasks/MINI-182/Audit.csv`, `Audit-Summary.txt`, `Renders/a1..a4`.

### Stage 2 - House Model System + first variant (2026-09-29)
- `Tools/ArtPreview/house_definitions.json` (table), `house_system.py` (part builders; regression rows plaster_gable_1s/2s), `render_house_turnaround.py`, `house_export.py` (additive export).
- Variant `flat_concrete_2s` (6.2 x 5.2 m, flat roof + parapet + front balcony + side windows; colours white / cream yellow / coral / sky blue): 3,388 tris LOD0, 1,694 LOD1, 7 materials. USER APPROVED the Blender turnaround, then the Unity result.
- Export writes to `Logs/Tasks/MINI-182/Export` only; palette = MINI-142 palette + appended cells (existing cells 0-19 verified identical). Shared palette texture `Assets/UpIzUpMini/Art/Environment/Mini142/palette.png` now the superset (cells 20+ appended).
- Unity: `Mini182HouseKit.cs` imports meshes to `Assets/UpIzUpMini/Art/Environment/Mini182`, builds 4 prefabs (`Prefabs/HouseFlatConcrete2s_<Colour>.prefab`: LOD0/LOD1 + body/base box colliders, shared Palette.mat), places a test row in isolated copy `GrandBayProof_HouseEnhance.unity` (not in build). Live scene hash verified unchanged.
- Evidence: `Renders/k1_row_street.png`, `k5_context.png`, `k3_first_close.png`, `Kit/sheet_*.png`, `KitImport-Report.txt`.
- Lessons: (1) my first coping was one full-footprint slab hiding the roof deck - build coping as a ring; (2) place-search must ignore huge bounds (hills/ground) or it finds no free lot; (3) Unity GetPixel is bottom-up, PNG rows top-down: palette cell row r is at unity y = r*16.
- Next: remaining variants one at a time (hip red-roof two-storey, corrugated gable, green/teal roof, shopfront, unfinished concrete frame, wooden board), then stage 3 Lalay placement.

### Variant 2 - hip_red_2s (2026-09-29, user approved Blender turnaround and Unity result)
- 6.0 x 5.0 m two-storey, four-plane hip roof (rise 1.3, overhang .3, 45 deg hips, ribs run straight up each face and stop on the ridge/hip line, ridge cap, eave fascia), balcony, side windows; walls cream yellow / peach / white / seafoam. 4,494 tris LOD0, 2,250 LOD1, 8 materials.
- Hip roof lesson: ribs must run perpendicular to the eave and end on the hip line (first version fanned ribs toward the ridge ends and looked wrong); fascia sides shortened so corners do not overlap.
- Palette bug caught before shipping: `house_export.py` rebuilt the palette from MINI-142 every run, so exporting a second family would have dropped the first family's appended cells and mis-coloured its houses. Fixed: the exporter now starts from the latest MINI-182 palette (append-only across families, asserts cells 0..19 unchanged). ALWAYS re-export ALL approved families in order (flat first, then hip) after changing the palette logic. Palette is now 24 cells (20 shipped + white, cream yellow, coral, sky blue).
- Unity kit now sizes colliders from footprint/height in the JSON and places a test row per family (`MINI182_FAMILY` env var, root `MINI182_KitTest_<family>`).
- Evidence: `Renders/k1/k3/k5` (hip row), `Kit/sheet_hip_red_2s.png`, `Kit/sheet_pair_hip_red_2s_vs_flat.png`.

### Variant 3 - corrugated_gable_1s (2026-09-29, user approved Blender turnaround and Unity result)
- 5.4 x 6.4 m single-storey, gable ridge running front-to-back (gable end faces the street), 0.4 m front overhang, corrugation ribs every .13 m, ridge cap, bargeboards, door canopy on two posts, side windows. Colour pairs (wall+roof): White+Rust, Yellow+Grey, Blue+Rust, Pink+Grey. 2,846 tris LOD0 / 1,426 LOD1, 7 materials.
- New: `roof_gable_depth` builder, `canopy` facade option, `variants` (wall+roof pairs) in the table and exporter; new colours roof_rust, wall_pink. Palette now 26 cells (20 shipped + 6 appended); all three families re-exported in order (flat, hip, corrugated).
- Unity test-row search widened (x 0-200, z -158..-240, road distance 9-30 m) because earlier rows fill the nearby ground; the row landed beside the church at x~164-191, z~-158, each house facing its nearest road so orientations differ. This is a model check only; real placement is stage 3.
- Evidence: `Renders/k3_first_close.png`, `k6_row_high.png`, `k7_pair_close.png`, `Kit/sheet_corrugated_gable_1s.png`, `Kit/sheet_pair_corrugated_gable_1s_vs_plaster.png`.
