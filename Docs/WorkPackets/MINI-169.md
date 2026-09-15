# MINI-169 — Walkable safehouses + Highland Mansion garage/integration

User: "can you fix up the first safehouse into a walkable house, it doesnt have to be big and then make other houses purchasable, walkable and usable as a safehouse" followed by "the mansion you build earlier should be implemented with a two car garage". Scoped via AskUserQuestion into three sequenced, separately-verified checkpoints (not one pass): (1) Highland/Farm Safehouse walkable interior - DONE this pass; (2) Lalay House + Lalay Estate walkable interiors (both already purchasable via existing `prop_safehouse`/`prop_lalay_estate` economy items - no new purchase system needed); (3) Highland Mansion two-car garage + first-ever Unity integration (currently Blender-concept-only).

## Part 1 — Highland/Farm Safehouse walkable interior (Claude), 2026-09-14

Surveyed the live scene first (`Mini169SurveySafehouses.cs`) rather than assuming: `FarmSafehouse_Building` was ALREADY a real 3-walled room (Floor/Roof/BackWall/SideWallL/SideWallR, all primitive Cubes with real `BoxCollider`s, materials `SafehouseWall`/`SafehouseFrame`/`SafehouseRoof`) with a real bed (BedFrame/Mattress/Pillow) - it was structurally walkable already, just open on the front (no wall, no door, no blocking collider), reading as a lean-to shed rather than a house. `SafehouseInteractable`'s menu is purely distance-based (`Vector3.Distance <= 4f`), not raycast/line-of-sight, so no interaction-script change was needed - the room was already fully usable from inside.

Fix (`Mini169FarmSafehouseInterior.cs`): added a real front wall with a 1.2m walkable doorway (two Cube wall segments + a lintel above the opening), reusing the EXACT existing `SafehouseWall` material and the same primitive-Cube-with-BoxCollider convention as the three walls already there - not a new construction technique. Repeatable/idempotent (destroys and rebuilds its own three objects by name on rerun).

Verified: Unity batch compile clean, scene saved (`MINI169_FARM_INTERIOR_PASS`), real renders (`Logs/Tasks/MINI-169/FarmSafehouse-Exterior-Front.png`, `-ThreeQuarter.png`, `-Interior-FromDoor.png`) inspected and confirmed: exterior now reads as an actual small house with a real doorway (not an open shed), 3/4 view confirms a solid enclosed structure, interior-from-door view confirms the bed and both characters are visible/reachable through the opening. Windows build succeeded (437,304,133 bytes).

Pre-existing, unrelated finding (not caused by this change, not fixed this pass): `Mini062SafehouseValidation.Validate` fails with `expected RespawnLabel to read 'Farm Safehouse', got 'Highland Safehouse'` - a stale test expectation from before MINI-156 renamed the safehouse; the failure is purely about a text label, nothing about geometry/colliders, and this safehouse's `safehouseName` field was never touched this pass.

Not yet done: no actual Play Mode walk-through was performed (only static renders); no NavMesh/AI pathing check for whether NPCs would try to walk through the new wall; camera-inside-a-small-room occlusion/feel is unverified in real gameplay.

## Part 2 — Lalay House + Lalay Estate interiors: not started

`LalayHouse_Rest`/`LalayEstate_Rest` sit on `LalaySafehouse_TwoStorey`/`EstateHouse`, both closed-shell generic-house-style exteriors (single merged `Walls` mesh, decorative `Door`) - unlike the Farm Safehouse, these need a genuine interior ADDITION, not just a missing wall. `EstateHouse` also has a `Garage` child object already built with the same 3-walls-no-front-no-collider-blocking pattern as the original Farm Safehouse shed (Wall_Left/Wall_Back/Roof/Wall_Right, no Wall_Front) - worth reusing as a reference technique for Part 3's mansion garage.

## Part 3 — Highland Mansion garage + integration: not started

Mansion is currently Blender-concept-only (`Tools/ArtPreview/highland_mansion_preview.py`), never integrated into Unity. Needs: (a) a two-car garage added to the Blender concept, (b) actual first-time Unity integration as a real placeable/purchasable building - new scope beyond the garage itself.
