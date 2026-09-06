# Economy

## Current state

Already the project's own best example of "design as a reusable system" (see `../../` memory `design-as-reusable-systems.md`) - shop/inventory items are generic data (id, display name, category, price, optional seed-crop/qty), not hardcoded per-item logic, specifically so new content (vehicles, accessories, businesses) can be added without rework. The Koss bike (MINI-120... actually MINI-119 follow-up) purchase is a direct proof of this working: adding a new purchasable vehicle was a one-line addition to the Car Dealer's item table plus a purchase-flow branch, not a new economy subsystem.

## Architecture

- Shop item tables: static arrays of tuples in `Mini011PhaseBSetup.cs`, e.g. `DealerSpecs` (vehicles), similar tables per shop category (farm/apparel/land/food/pharmacy). This is the pattern `Combat.md`'s `MeleeMoveLibrary` deliberately copied.
- `ShopPanelController` / `ShopCategory` - generic UI reading from whichever item table it's handed.
- `VehicleSpawnController.SpawnPurchasedVehicle(itemId)` - the purchase-to-spawn bridge specifically for vehicles (see `Vehicles.md`); other categories (crops, land, apparel) have their own simpler grant-on-purchase paths since they don't need a physical world object spawned.
- Farming: crop stages, watering, harvesting, cloning, inventory, plot ownership, and an automated farmhand loop.

## What worked / what didn't

- **2026-09-05 MINI-141 crop art preview only.** Carrot, banana, Bushers and Purple candidates rendered in Blender (Logs/Tasks/MINI-141/02-Crop-Preview.png). First banana leaf/bunch silhouette failed internal review and was refined before presentation. Counts: carrot 348 triangles, banana 3696, cannabis 1796 each. No crop IDs, growth, harvest, inventory or existing prefab changed. User approval, mesh consolidation/LODs, stage-specific soil/root handling and Unity visual verification remain required.

- **(established pattern, reconfirmed 2026-08-28) Internal item IDs must never change once shipped**, even when the display name changes (renaming would strip the item off any existing save) - e.g. `tmax_560` kept its id when renamed to "TNAX 560", `chain_gold` kept its id when renamed to "Gucci Law". Display-name-only renames are safe; id renames are not.
- **(2026-08-28) Adding a new vehicle to the economy was cheap specifically because the item table was already generic** - the Koss required one new tuple in `DealerSpecs` plus one new branch in `SpawnPurchasedVehicle` (which itself reused a shared vehicle-wiring helper, see `Vehicles.md`), not a new purchase architecture.

## Open items

- None specifically logged as of this system file's creation (2026-08-29).

## Key files

- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (`DealerSpecs` and other per-category item tables, `BuildShopPanel`)
- `Assets/UpIzUpMini/Scripts/UI/ShopPanelController.cs`
- `Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs` (vehicle purchase bridge)
- `Docs/ASSET-REGISTER.md` (for anything purchased/acquired as a real-world asset, not in-game currency)
