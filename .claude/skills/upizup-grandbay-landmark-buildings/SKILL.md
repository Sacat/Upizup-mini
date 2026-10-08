---
name: upizup-grandbay-landmark-buildings
description: Build and place new Grand Bay landmark buildings (Credit Union, Pierre Charles Secondary School, shops, civic buildings) and procedural Lalay-style homes directly in Unity Editor C# for Up Iz Up Mini's GrandBayProof_ExpansionImport scene - published-coordinate anchoring, road-frontage placement, parcel clearing, shared-material box construction, and reload-based validation. Distilled from MINI-168 to MINI-175. Use for "add a building at <address>", new Grand Bay landmarks, or replacing generic houses. For Blender-modeled houses use upizup-building-modeling; for new districts use upizup-map-district-expansion.
---

# Up Iz Up Mini - Grand Bay Landmark Buildings (procedural, Unity Editor)

Worked examples (read before writing a new generator):
- `Assets/UpIzUpMini/Editor/Mini175CreditUnion.cs` - landmark: Apply / RepairShop / Validate / Inspect / Render
- `Assets/UpIzUpMini/Editor/Mini168Expansion.cs` (school massing, `OrientSchoolToMainRoad`) and `Mini172RefineExpansion.cs` (`FitSchool`) - campus
- `Assets/UpIzUpMini/Editor/Mini173LalayRevision.cs` - procedural home family + per-material mesh merge
- `Assets/UpIzUpMini/Editor/Mini174CoastAndSpacing.cs` - seawall, oriented-rectangle house spacing

Provenance: these were built with Unity Editor C# only (no Blender, QGIS, or paid generators). Do not describe them as Blender work.

## Before any edit
1. AGENTS.md startup sequence. Read the `### Current claim` block in PROJECT-HANDOFF.md. If `current_owner` is another agent, stop and report; do not edit scenes or files they reserved.
2. Claim one task ID plus exact files, run `Tools/AIWorkflow/Invoke-Preflight.ps1 -Agent Claude -TaskId MINI-###`, one Unity Editor only, check `git status --short`.
3. Work only in `GrandBayProof_ExpansionImport.unity`. Never touch `GrandBayProof.unity` or `MapLab_GrandBayExpansionCopy.unity`. Scenes are binary: edit through Editor APIs, never text replacement.
4. Check `Docs/VISUAL-APPROVAL-REGISTER.md`; approved placements/appearances are locked.

## Recipe for a landmark building (Credit Union pattern)
1. **Anchor.** Get a published coordinate from the organisation's own site (address) and a directory pin. Reject pins that disagree with the address (MINI-175 rejected a town-centre pin). Convert:
   `x = (lon + 61.3181049) * 107500 / 3`, `z = (lat - 15.2450638) * 110650 / 3`. Record source, converted X/Z, sampled Y and any frontage offset separately. A directory pin is not a surveyed footprint.
   Credit Union: Lalay Main Road, `15.2407793,-61.3166228` (official https://gbccu.com/contact/ for the road; directory pin for the coordinate).
2. **Frontage.** Raycast down onto the actual `*Road*` MeshColliders around the pin (+/-25 in Z, .25 steps), pick the closest, then sample the true cross-section (lo/hi Z along the ray at x) to get centre and half-width. Do not assume a two-vertex ribbon. Tangent from centre at x+/-2; normal = cross(up, tangent), flipped to face the pin. Position = centre + normal * (half + 5.0).
3. **Ground.** Raycast only `*Terrain*` MeshColliders (ground is a mesh, not a Unity Terrain; `Terrain.SampleHeight` is invalid). Set Y = hit + .06.
4. **Orientation.** `Quaternion.LookRotation(-normal)`, so local +Z is the front facing the road.
5. **Construct.** Named root (`MINI###_Name`), destroyed and rebuilt on each Apply so it is idempotent. Cube-primitive helper `Box(parent,name,localPos,size,material)`; materials via a `Mat(name,color)` helper that loads/creates `.mat` under `Assets/UpIzUpMini/Art/Environment/Mini###/` with instancing on. Credit Union baseline: foundation 7x.4x5.2, body 6.8x5.3x4.8, parapet, green fascia, porch canopy with two columns, 3x2 window grid with frames/mullions, glass entrance, ATM, TextMesh sign scaled to <=6.3m wide. Mark parts static.
6. **Parcel.** Deactivate (never delete) generic houses whose renderer bounds overlap the plot (~8.4 x 8.4). Move conflicting scripted props (e.g. `Market_CLOTHES` plus its NPC) together by a searched offset (back 0-16, side +/-6-30) that clears houses, other markets, roads and has valid ground.
7. **Validate on a fresh reload.** 3x3 sample points on every BoxCollider bound must miss all road colliders; no envelope overlap with `Lalay_House_`, `Lalay_Home_`, `Market_`. Write `Placement.txt`, `Validation.txt`, `NeighbourAudit.txt` under `Logs/Tasks/MINI-###/`. Log explicit PASS markers; failures call `EditorApplication.Exit(1)`.
8. **Render.** Camera renders to 1400x1000 PNG: a 3/4 street view (~16 forward, 9 right, 7 up), a location overview (~40 up), plus full-map. Look at them. Numeric clearance does not prove doors are reachable or the arrangement looks right.

## Recipe for a campus (Pierre Charles Secondary School pattern)
- Source: MINI-168 expansion. Campus buildings are artistic massing, not surveyed footprints; say so. Main road is `ExpansionRoad_4` (way/548578022).
- Historical centroid X106.5267 / Z49.2967, entrance faces north (+Z). Objects under `MINI168_Expansion`: `SchoolCourtyard`, `SchoolTeachingWing_0/1`, `SchoolMetalRoof_0/1`, `SchoolWindow` (x7 per wing), `SchoolRearWing`, `SchoolRearRoof`.
- Original massing: courtyard 40x.12x26; wings 8x4.4x21 at +/-14 from centre; roofs 9x.3x22 at +4.5 above; rear wing 23x4.4x6.
- MINI-172 fit (idempotent, absolute targets): wings at centreX +/-11, windows at centreX +/-6.95, courtyard width capped at 34, right (entrance-facing) wing set to Z46.2967 for a 3 m road setback with its roof and windows carried along. Centroid and north entrance retained.
- Check road clearance with roof and porch bounds, not root position. Verify left/right while standing at the entrance looking in. Entrance grade must stay under 8% (`OrientSchoolToMainRoad` throws above that).
- Do not rerun Mini168Expansion or the whole refine chain without reading what it deletes; use the additive fit.

## Recipe for procedural Lalay homes (MINI-173)
- Lot-sized footprint 2.65 x 3.6; height 2.7 (one floor) or 5.1 (two); floor spacing 2.55; porch depth .7, columns .13; roof two planes at 17 degrees; five plaster colours cycled by index; stone plinth; window frames with real depth, sills and glass. Older 6-12 m footprints overran ~3.6 m lots.
- Preserve each replaced root's position and rotation; disable originals (rollback), do not delete.
- Merge decorative boxes by material with `Mesh.CombineMeshes`, save meshes as assets, keep colliders only on body, slabs and steps.
- Spacing pass (MINI-174): oriented XZ rectangles from all mesh bounds including roofs and porches, separating-axis test with .18 clearance, lateral/back search, horizontal scales 1/.94/.88/.82 (never scale height), preserve grounding offset, sample a 5x5 footprint grid against roads.

## Running Unity
`C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`, `-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.<Class>.<Method> -logFile ...`.
- No `-nographics` when rendering. No `-quit` when the method exits itself.
- `Start-Process` returning is not success. Wait, then read the log for PASS markers, exceptions and compiler errors.
- `Render` reopens the saved scene and discards unsaved edits. `Apply` mutates and saves. Read the method before running a historical generator; do not rerun MINI-172 smoothing or the old southern extrusion.

## Honesty rules
- Say "stylised facade; footprint and entrance not surveyed" unless a survey exists. Source photos that could not be opened are not photo matching.
- Web pages support building type and address, not exact replicas.
- A passing footprint/road check is not vehicle, NPC-pathing or visual acceptance.
- Record the frontage adjustment distance and anything relocated to make room.

## Known gaps to check on each new building
- Credit Union and similar landmarks have no interior, and the entrance is decorative.
- Sign is a `TextMesh`; confirm it reads from the road side in the render.
- New buildings do not automatically get collision-based interaction; wire interaction separately.
- Update `Docs/WorkPackets/MINI-###.md`, PROJECT-HANDOFF.md, CHANGELOG.md, then release ownership to `None`.
