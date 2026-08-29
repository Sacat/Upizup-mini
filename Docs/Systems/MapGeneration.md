# Map Generation

## Current state

Two parallel pipelines exist. **`Mini011PhaseBSetup.cs`** generates the canonical live gameplay scene (`Assets/UpIzUpMini/Scenes/GrandBayProof.unity`) - one very long, hand-written procedural method (terrain, roads, buildings, NPCs, missions, shops, all in one file), NOT yet data-driven in the sense `Combat.md`'s `MeleeMoveLibrary` is, though parts of it already use static data arrays (e.g. `DealerSpecs` for shop items - the pattern to extend, not invent). **The Map Lab / OSM migration pipeline** (`Mini095LalayMapLabSetup.cs` builds a separate, more sourced/accurate `MapLab_LalayHighland.unity`; `Mini100GrandBayMapMigration.cs` migrates approved pieces of it into the canonical scene) is the more rigorous, source-verified path documented in `Docs/WORLD-EXPANSION-WORKFLOW.md` - read that file in full before any terrain/road/anchor work, this file is a ledger, not a substitute.

The live `GrandBayProof.unity` scene has NO Unity `Terrain` component - ground is collider-based (confirmed by direct search this session). Any tool needing ground height must raycast, not call `Terrain.SampleHeight`.

## Architecture

- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` - `BuildScene()` is the entry point; regenerates the whole canonical scene from scratch. **Never hand-edit `GrandBayProof.unity` as a source of truth for anything this method controls** - a rebuild overwrites it. Manually-placed/tuned things that must survive a rebuild (e.g. the farm hedge/plots position) need an explicit "replay this manual placement" step baked into the script itself, not just left in the saved scene.
- `Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs` / `Mini100GrandBayMapMigration.cs` - the sourced/compressed real-world-anchored pipeline (`Docs/MAP-ANCHORS.json`), with its own validation/screenshot/migration/rollback discipline in `Docs/WORLD-EXPANSION-WORKFLOW.md`.
- `AreaNameDisplay` (`Assets/UpIzUpMini/Scripts/UI/AreaNameDisplay.cs`) - the "you entered Lalay/Highland" zone-name popup. Only supports CIRCULAR zones (center + radius, nearest-center-wins when zones overlap) - not polygons, not a hard boundary along a road. Keep this limitation in mind before promising a precise zone edge.
- Additive live-scene patch tools (the established pattern for touching the ALREADY-BUILT scene without the risk of a full rebuild wiping manual placement): `Mini119AddMoreVillagers.cs`, `Mini119RezoneLalayHighland.cs`, `Mini119FixHighlandFarmVillager.cs`, `Mini120PatchAnimatorController.cs` (combat, but same pattern) - open the live scene, find/patch the specific objects needed, save. Prefer this over re-running `BuildScene()` whenever the live scene has hand-tuned state that isn't safely reproducible by the generator yet.

## What worked / what didn't

- **(2026-08-28) Re-running the full `BuildScene()`/world builder against a live, hand-tuned scene is a real risk, not a theoretical one** - this project's own convention (documented in multiple places, e.g. `manual-placement-is-authoritative` memory) is that manual placement always wins and a rebuild can destroy hours of tuning (bike positions, mount setups, etc.). **What worked instead**: small, additive, idempotent Editor tools that open the live scene, find exactly what needs to change, patch it, save - never touching anything else.
- **(2026-08-28) Assuming the scene has a Unity `Terrain` and calling `Terrain.SampleHeight` silently gives wrong results or throws** - this scene's ground is collider-based. `GroundSnap`-style `Physics.Raycast` from height, downward, is the correct technique (already used elsewhere, e.g. `VehicleSpawnController.GroundSnap`).
- **(2026-08-28) Zone-boundary requests need real landmark coordinates, not assumptions.** The Lalay/Highland `AreaNameDisplay` boundary went through three user-directed corrections in one session before landing correctly, each time resolved by looking up the ACTUAL world position of a named object (a bridge, the Car Dealer, the Dog Life block) via a read-only survey tool, then verifying the fix by calling `AreaNameDisplay.ResolveArea()` directly against real coordinates - never by hand math or assumption. A single Lalay circle wide enough to reach a distant landmark can accidentally out-compete a different zone's own tighter circle if that zone's center happens to be geometrically "in the way" - solved by using two same-named circles instead of one overreaching one.
- **(2026-08-15) Satellite imagery and un-cleared heightmap files are explicitly rejected as import sources** - `Docs/ASSET-REGISTER.md`'s "Explicitly rejected" section, `Grandbay entire.jpg` and `heightmapper-*.png/.raw` are reference-only, never shippable textures.

## Open items

- The migration/validation gate (`dm-dom-grand-bay-lalay-highland-v1`) is at "graybox" per `Docs/CLAUDE-HANDOFF-CURRENT.md` - cannot advance until a corrected player-height Lalay view is approved.
- Road-intersection bumps and vehicle-safe spawn positioning were flagged as needing repair (MINI-113-era) - status not reconfirmed since.
- `Mini011PhaseBSetup.cs` itself is not yet "a system" in the `MeleeMoveLibrary` sense - it's one long procedural method. Worth revisiting if map-generation requests become frequent enough to justify extracting a real data table (e.g. NPC placement specs, building specs) the way shop items already are.

## Key files

- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs`
- `Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs`, `Mini100GrandBayMapMigration.cs`, `Mini100GrandBayMapValidation.cs`
- `Assets/UpIzUpMini/Scripts/UI/AreaNameDisplay.cs`
- `Docs/WORLD-EXPANSION-WORKFLOW.md` (mandatory reading for terrain/road/anchor work)
- `Docs/MAP-ANCHORS.json`, `Docs/MAP-STRATEGY.md`
- `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (generated - do not hand-edit as source of truth), `MapLab_LalayHighland.unity`
