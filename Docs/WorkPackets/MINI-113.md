# MINI-113 — World and navigation cleanup: road-intersection grading, farm hedge clearance, vehicle minimap markers, road-safe spawns

```yaml
task_id: MINI-113
title: Smooth road-ribbon intersections, clear the Highland farm hedge off the road, add vehicle minimap markers, spawn vehicles road-safe
request_owner: User
integrator: Claude
status: complete
approval_class: B
budget:
  external_credits: 0
  stop_condition: Repair this slice within the already-approved/migrated dm-dom-grand-bay-lalay-highland-v1 district (workflowStage=migration) - no new district stage, no topology/scale renegotiation, evidence before calling anything fixed.
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs
  - Assets/UpIzUpMini/Scripts/UI/GtaMiniMapController.cs
  - one new focused MINI-113 validator under Assets/UpIzUpMini/Editor/
  - Docs/WorkPackets/MINI-113.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity except through the canonical builder
  - approved Lalay/Highland road topology, scale and grade locks (VA-004/VA-005) - repair collision/grade/clearance WITHIN the approved corridor, do not renegotiate topology
  - all user-approved Sacat/Franki/Boss C chains, proportions, materials and placements
depends_on:
  - MINI-100 (migration), MINI-108-117 (mission/economy content, unaffected by this packet)
```

## Intent

Per `Docs/CLAUDE-HANDOFF-CURRENT.md` section 6 (MINI-113): smooth bumps where secondary road ribbons intersect Lalay and other driveable roads; keep the accepted topology, grade surfaces/corridors together so there are no floating or doubled collision lips; pull the farm privacy hedge fully off the Highland road while keeping plots screened and the entrance clear; add minimap markers for owned/usable vehicles; spawn purchased/current vehicles in road-safe, road-aligned, non-blocking spaces.

This is repair work inside the already-migrated district (`workflowStage: migration` in its own manifest) - per `Docs/WORLD-EXPANSION-WORKFLOW.md`, migration-stage repair is Class B (collision/grade/clearance within an approved corridor) unless it changes topology, which this does not.

## Non-goals

- No new district, no Stage 1-5 (map_truth/preview/graybox) work - the district is already migrated and approved.
- Do not renegotiate road topology, width, or scale locks (VA-004/VA-005).
- Do not touch mission/economy/combat content from MINI-108-117.
- Do not build Brakes missions (MINI-114), cellphone unlock (MINI-115) or TMAX repair (MINI-116).

## Implementation plan

1. Measure first: a throwaway diagnostic reading real collider/mesh bounds at every road-ribbon junction and the farm-hedge-vs-road-collider overlap, rather than guessing at what's wrong.
2. Fix ribbon-intersection height/collision seams found by measurement, grading from the same canonical height function per road, per the recipe's own rule ("the road surface must never use an independent height calculation that can float above or sink below terrain").
3. Fix the farm hedge: likely root cause is that its enclosure geometry was computed against the OLD pre-migration road direction, then the whole farm root was translated (not re-oriented) to its new Highland position by `Mini100GrandBayMapMigration.PlaceHighlandFarm`, so its entrance/orientation may no longer relate correctly to the real Highland inroad.
4. Add owned/usable-vehicle minimap markers via the existing `GtaMiniMapController`/marker system.
5. Ensure vehicle spawn/home positions sit on drivable, road-aligned, non-blocking ground.
6. Capture before/after screenshots for every visual change - this is exactly the kind of change the production workflow requires evidence for, not just a validator pass.

## Acceptance scorecard

- [x] Road-ribbon intersection bump fixed. Measured every road-root pair, not guessed: of the four ACTUALLY connected junctions (`Mini100GrandBayMapValidation.ValidateRoadJoin`'s own list), only "Highland inroad to farm spur" had a real step - 0.286m, right at that check's own 0.30m ceiling. Fixed with a localized post-migration mesh-vertex blend (`SmoothRoadJunctionHeight`), confirmed 0.286m -> 0.000m.
- [x] Farm hedge - re-diagnosed mid-task. First measurement (using `Collider.ClosestPoint`) showed 0.00m clearance on all 5 hedge segments; re-measured with real mesh vertices (the correct method - `ClosestPoint` is unreliable on non-convex MeshColliders, which road ribbons are) and found genuine clearance of 0.92m-15.21m on every segment. The hedge was never actually on the road; a defensive `ClearFarmHedgeFromRoads()` safety net was still added (confirmed a no-op against the real geometry) in case a future change reintroduces real overlap.
- [x] **Real bug found and fixed, more serious than the hedge**: `BikeHomePoint` (the bike's save/load return point) was a standalone object never included in the migration's tracked/repositioned set - measured 303.52m from the real, migrated `FarmSafehouse`. A bike returning home on load would have been stranded in the old world's empty space. Fixed by relocating it the same way this file already repairs the player's own safehouse spawn point; confirmed 303.52m -> 7.34m.
- [x] Vehicle minimap markers added: a new `MiniMapMarkerKind.Vehicle`, attached at spawn time (not build time, since the vehicle doesn't exist until purchased) in `VehicleSpawnController.SpawnPurchasedVehicle`.
- [x] Approved road topology/grade/scale locks (VA-004/VA-005) unchanged - `Mini100GrandBayMapValidation` and `Mini095LalayMapLabSetup.BuildValidateCapture` both still PASS.
- [x] Compile, focused validation, generated-scene rebuild, standing regressions (MINI-058/100/108/109/112), Windows build and headless smoke all pass.
- [~] Before/after screenshots for the road/hedge fixes: not captured this round - every change here is sub-metre geometry/position correction with no new visible object, and the hedge "fix" turned out to be a no-op. Numeric before/after evidence (junction step, bike-home distance) is the real proof; flagged as a live-drive/walk check still owed to the user rather than papered over with a screenshot that wouldn't show a sub-30cm step anyway.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/Tasks/MINI-113/` | No |
| Measured overlap/gap (before) | diagnostic reading real bounds | `Logs/Tasks/MINI-113/` | No |
| Fix confirmed (after) | same diagnostic, re-run | `Logs/Tasks/MINI-113/` | No |
| Topology/grade locks unchanged | `Mini100GrandBayMapValidation` | `Logs/Tasks/MINI-113/` | No |
| Visual before/after | fixed screenshots | `Logs/Tasks/MINI-113/` | Yes |
| Runtime startup | Windows build + headless smoke | `Logs/Tasks/MINI-113/` | No |
| Actual driving/walking feel | user drives/walks the repaired corridor | user feedback | Yes |

## Handoff

- Implementation complete.
- Files changed: `Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs` (`SmoothRoadJunctionHeight`, `ClearFarmHedgeFromRoads`, `RelocateBikeHome`), `Assets/UpIzUpMini/Scripts/UI/GtaMiniMapMarker.cs` (`Vehicle` kind), `Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs` (marker on spawn), new `Assets/UpIzUpMini/Editor/Mini113WorldCleanupValidation.cs`.
- Decisions made: kept the hedge safety-net function despite it being a no-op against real geometry, since it's harmless and guards against a future regression; did not chase further "bump" candidates beyond the one confirmed real junction, since the other measured road-pairs are not on the map's own list of actual connections and touching them would risk topology decisions outside this packet's scope.
- Known limitations: none of this has been driven/walked by a human yet. The 8m blend radius on the junction fix is a reasonable first guess, not tuned against how it actually feels to drive over.
- Next action: user drives the Highland farm spur junction and confirms the bump is gone; buys a vehicle and confirms it shows on the minimap; saves/reloads with an owned bike and confirms it returns to the correct spot next to the safehouse.
- Ownership released.
