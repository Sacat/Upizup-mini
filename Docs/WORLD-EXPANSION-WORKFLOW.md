# Up Iz Up Mini — World Expansion Workflow

This is the source of truth for expanding Grand Bay, adding another Dominica district, adding another island, or building a separate future map. It turns MINI-094/095 into a repeatable system that Codex and Claude can resume without guessing.

## Non-negotiable rules

1. Map truth and game art are different layers. Roads, coastlines, elevation sources and verified anchors retain provenance; buildings, vegetation and compression may be artistic.
2. Never trace, import or ship Google/other commercial satellite pixels or 3D geometry. Satellite imagery is visual reference only unless its licence explicitly permits production use.
3. One Unity integrator owns generated world data, scenes, prefabs, NavMesh and migration at a time.
4. A district is never migrated directly from research into gameplay. It must pass `map_truth -> approved_preview -> graybox -> approved_graybox -> migration -> runtime_acceptance`.
5. User-approved images and placements are visual locks. Rejected screenshots must be named as rejected and excluded from evidence.
6. Real elevation is an input, not a command. Roads, sidewalks, yards and mission spaces may be graded for playability while the surrounding land retains recognizable relief.
7. A road is not complete because it renders. It needs continuous geometry, collision, clearance, acceptable grade, junction coverage and a live walking/driving check.
8. Future parcels, missions and districts remain absent or inactive until progression unlocks them. Do not expose colored placeholder slabs in the player build.

## Standard identity and folders

Use a stable lowercase ID such as `dm-dom-grand-bay-lalay-v1`:

```text
Docs/Maps/<map-id>/
  DISTRICT-PACKET.md       scope, references, gates and handoff
  MANIFEST.json            machine-readable state and budgets
  SOURCES.md               datasets, dates, bounds, licences, attribution
  APPROVALS.md             approved/rejected screenshots and user wording
  Evidence/                retained evidence when licensing allows

Assets/UpIzUpMini/Maps/Regions/<map-id>/
  Source/                  compact derived data; never satellite pixels
  Generated/               roads, terrain, anchors and parcel data
  Staging/                 isolated graybox/import output
```

Create and validate a district with:

```powershell
& .\Tools\World\New-MapDistrict.ps1 -MapId dm-dom-grand-bay-example-v1 -DisplayName "Grand Bay Example" -Country Dominica -Island Dominica -Region "Saint Patrick"
& .\Tools\World\Test-MapDistrict.ps1 -MapId dm-dom-grand-bay-example-v1 -Stage scaffold
```

## District state machine

Only move forward one state at a time. A rejected visual decision returns to the preceding cheap stage.

| Stage | Required outcome | User gate | Gameplay scene |
|---|---|---|---|
| `scaffold` | Identity, folders, placeholders and budgets | No | Untouched |
| `map_truth` | Licensed roads/coast/water/DEM and trust-marked anchors | Local corrections | Untouched |
| `approved_preview` | Overhead network preview with uncertainty marked | Required | Untouched |
| `graybox` | Separate terrain/road/lot scene and passability checks | Required | Untouched |
| `approved_graybox` | Accepted topology, scale, grades and relationships | Required | Untouched |
| `migration` | Rollback-safe integration preserving gameplay IDs | No new art decision | One integrator changes it |
| `runtime_acceptance` | Walking, driving, NPC pathing, camera and missions observed | Required | Accepted checkpoint |
| `decorated` | Approved art, vegetation, props, LOD/culling | Required for visible art | Accepted checkpoint |

## Stage 1 — Scaffold and scope

- Create one district/corridor, not the whole island.
- Define player purpose, boundaries, travel-time target, connections and non-goals.
- Record protected scenes and a rollback commit.
- Set terrain, road, building/NPC, material, texture and light budgets.
- Choose fixed evidence cameras before building.

Exit check: `Test-MapDistrict.ps1 -Stage scaffold`.

## Stage 2 — Map truth

- Prefer OpenStreetMap/Geofabrik for roads and waterways, Copernicus/public DEM for elevation, official institution sources and user-owned local photos.
- Record dataset, retrieval date, bounds, licence, attribution and redistribution limits.
- Use a local metric origin; one Unity unit equals one metre before deliberate travel compression.
- Every anchor has a stable ID and trust level: `source_verified`, `user_verified`, `candidate`, or `artistic`.
- User knowledge may upgrade a candidate, but never silently changes the source record.
- Keep compact derived data separate from raw downloads.

Exit check: sources/licences, numeric bounds/origin, trust-marked anchors and an overhead preview that distinguishes verified data from artistic additions.

## Stage 3 — Preview and local approval

Produce a cheap overhead image with a north arrow, scale, roads, coast, waterways, district boundary, connection arrows, trust-marked landmarks, attribution and a clear “not gameplay” note.

Record the user's exact approval/correction words in `APPROVALS.md` and `Docs/VISUAL-APPROVAL-REGISTER.md`. Preserve approved and rejected IDs; never let a later rejected screenshot replace an earlier approved target.

Exit check: `mapTruthStatus` is `approved` and its evidence path is retained.

## Stage 4 — Gameplay terrain and roads

- Build a separate map-lab through a repeatable editor method.
- Preserve surrounding relief; grade roads, sidewalks, yards, farm plots, vehicle lots and interaction areas independently.
- Resample long road segments against terrain so they cannot bridge valleys as floating slabs.
- Define width, shoulder/sidewalk, maximum grade, surface and collider per road class.
- Keep junctions continuous and road surfaces consistently above terrain.
- Houses use footprint-aware clearance. Move or resize them; do not destroy approved density.
- Use non-blocking outlines or small massing for landmarks, not giant colored slabs.
- Only starting parcels are active. Future parcels are stable inactive anchors.

Required static checks:

- road root count equals road collider count;
- meshes have valid bounds;
- required connection IDs exist;
- buildings/props clear drivable roads;
- grades stay within district limits;
- sea/edge boundaries and fall recovery exist in the migration plan;
- fixed overview, main-street and destination screenshots exist.

## Stage 5 — Graybox approval

Show an overhead network view, player-height main street, player-height destination, and close views of disputed slopes/junctions. Ask about topology, scale, slope, density and relationships—not final models. Lock approved aspects and name rejected evidence explicitly.

Exit check: `grayboxStatus` is `approved`; topology/grade/scale locks are named; placeholder art remains listed as editable.

## Reusable Caribbean road-and-settlement recipe

MINI-099 established the default method for later Grand Bay districts, the rest of Dominica, Guadeloupe and other Caribbean maps:

1. Retain the complete licensed source catalogue in compact data, but render only a curated phase network. A small connected spine is preferable to every OSM spur appearing at once.
2. Name the required district connections first. Snap road endpoints to exact shared junction coordinates; never cover a visible gap with a decorative mesh.
3. Disable automatic long gap connectors. Only repair sub-metre digitising seams automatically; every larger link must be a reviewed stable road ID.
4. Give paved phase roads one consistent two-vehicle width unless local evidence requires a narrower class. Bridges inherit that width, use centered equal-length rails and exist only where a retained road intersects a retained waterway.
5. Grade the terrain corridor and road from the same canonical height function. The road surface must never use an independent height calculation that can float above or sink below terrain.
6. Use district-specific slopes: very mild main streets, gentler farm/inland connectors, and wider terrain blending around steep approaches. Preserve recognizable relief outside those corridors.
7. Encode land use separately from roads: dense close-built town rows, moderately populated hillside/farming districts, and explicit coast/no-house polygons. Houses use footprint clearance and cannot sit on roads, bridges, sand or protected parcels.
8. Keep landmark and future-property IDs even when their visible placeholder is removed. Colored planning slabs do not enter gameplay; future farms remain inactive stable anchors until progression unlocks them.
9. End beach geometry at the shoreline. Jetties begin on accessible land, cross sand/stone, and visibly extend into navigable water.
10. Validate counts, colliders, bridge symmetry, grades, no-build zones and fixed cameras before migration. Then require live walking and two-way driving after migration.
11. When replacing graybox ribbons with a spline-road tool, copy the approved map to an isolated proof, sample each legacy collider for Y while retaining its exact X/Z route, and move every replaced ribbon into an inactive rollback root. Generate district-specific materials and sidewalks explicitly: Lalay's main street may retain sidewalks; Highland, farm, Backstreet and secondary routes do not receive them automatically.
12. Treat bridge approaches as their own graded road sections. Sample only authoritative road and terrain colliders—not unrestricted scene hits—overlap both ends of the driveable deck, match the bridge width, cap crossfall, attach matching mesh collision and capture both driving directions before approval.

This recipe is parameterized by each district manifest: road widths, grade caps, density classes, coast polygons, travel compression, materials, budgets and progression visibility change per map; the sequence and gates do not.

## Stage 6 — Migration into gameplay

Migration is a separate Class C task with its own rollback commit.

1. Inventory every gameplay role/ID: players, spawns, missions, NPCs, buyers, bosses, police, gangs, plots, safehouses, vehicles, boat, boundaries and cameras.
2. Create an `old role -> new anchor -> validation` table.
3. Extend or replace the authoritative scene builder; never depend on undocumented manual placement.
4. Migrate bounded groups: terrain/roads/boundaries, players/spawns, missions, NPCs, vehicles, farms/properties and navigation.
5. Preserve save IDs and progression; add migration only if IDs must change.
6. Rebuild once after the compound change and run all regressions.

## Stage 7 — Runtime acceptance

Static checks cannot prove feel. Test and record:

- Sacat and Franki walking/running/jumping the main corridor;
- TMAX and car travel through required junctions in both directions;
- no snagging, road gaps, excessive grade, sea escape or fall trap;
- companion, patrol, police and gang navigation;
- interaction prompts and mission routing at relocated anchors;
- active/future parcel progression;
- camera obstruction;
- phone-like resolution and performance.

Mark each `accepted`, `rejected`, or `not tested`. Screenshots never prove motion.

## Stage 8 — Decoration and expansion

- Replace placeholders in small approved families: houses, institutions, roadside props, vegetation and signs.
- Keep stable anchors and road topology; decoration stays swappable.
- Use district-based additive content, pooling and distance culling.
- Connect districts through named gateways that remain blocked or abstract until their destination reaches runtime acceptance.
- Roseau, other Dominica areas and Guadeloupe each receive their own manifest and approval history.

## Agent handoff contract

Every handoff states map ID, stage, sources/licences, approved and rejected evidence, stable anchors/connections, grading/compression rules, owner/reservations, validator logs, placeholders and the next single action.

Codex and Claude may research non-overlapping data in parallel, but one integrator imports data, runs Unity, changes scenes/builders and publishes evidence.

## Definition of done

A district is done only at `runtime_acceptance`: the user accepted walking/driving, required gameplay roles are mapped, passability/regressions pass, mobile budgets and credits attribution are recorded, the handoff is current and ownership is released.
