# MINI-160 — visual vehicle dealer panel (see it, rotate it, buy it)

```yaml
task_id: MINI-160
title: Vehicle buying mirrors the wardrobe panel - see/rotate the real vehicle, then buy
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: B
budget:
  claude_time: one session, reused an existing proven UI pattern
  external_credits: 0
  stop_condition: compiles clean, real 3D preview verified, existing purchase pipeline untouched
reserved_files:
  - Assets/UpIzUpMini/Scripts/UI/VisualVehicleDealerPanel.cs
  - Assets/UpIzUpMini/Scripts/UI/ShopPanelController.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Editor/Mini160VerifyDealerPanel.cs
  - Docs/WorkPackets/MINI-160.md
  - Docs/Systems/Vehicles.md
  - Docs/Systems/UI.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - Logs/Tasks/MINI-160/
depends_on: []
```

## Intent

User: "i want the vehicle buying to be just like the wardrobe changing
like you can actually see the vehicle you want to choose it and buy it."

## Implementation

New `VisualVehicleDealerPanel.cs`, deliberately structured as a near
line-for-line mirror of `VisualWardrobePanel.cs`'s proven pattern (same
project, same author intent, same "pause game, hide canvases, isolated
off-scene render stage on hidden layer 31, orthographic camera into a
RenderTexture, IMGUI overlay" technique) rather than inventing a new one:

- Talking to the Car Dealer NPC (`E`) now opens this panel instead of the
  plain number-key `ShopPanelController` text list every other shop still
  uses.
- The panel reuses the dealer's own already-wired `ShopPanelController.stock`
  (added a one-line public `Stock` getter) - the exact same
  `ShopItemDefinition` instances, so item id/name/price/ownership are never
  duplicated in a second place.
- For the selected vehicle, it instantiates the SAME prefab
  `VehicleSpawnController.SpawnPurchasedVehicle` would place in the world
  (added `GetPreviewPrefab(itemId)` - a small lookup over the controller's
  existing `tmaxPrefab`/`roverPrefab`/`stockDemoBikePrefab` fields, no new
  asset references) onto the hidden preview stage, strips physics/input/AI
  (kinematic rigidbodies, disabled colliders, disabled MonoBehaviours) so it
  just sits there to look at, and frames/rotates it with Rotate
  left/right buttons.
- "Buy this vehicle" calls the EXISTING, unmodified purchase pipeline:
  `EconomyManager.TryPurchase` (money/ownership) then
  `VehicleSpawnController.SpawnPurchasedVehicle` (the real in-world spawn) -
  nothing about how a purchase actually works changed, only how you choose
  what to buy.

## Non-goals

- No change to how a vehicle actually spawns/parks/is driven once bought.
- No change to prices, unlock gating, or which vehicles are sold.
- No touch to the wardrobe panel itself, or any other shop (farm, apparel,
  land office, food, pharmacy) - they keep the existing text-list UI.

## Verification

A straight `Camera.Render()` capture of the panel's own preview texture
doesn't work from a cold Editor batch-mode scene load for an unrelated
reason worth recording: `VehicleSpawnController.Instance` (a singleton set
in its own private `Awake()`) is still null outside Play Mode, so
`GetPreviewPrefab` silently returned null and the preview never built. Not
assumed - caught by logging `model=False` and diagnosing before writing
more code around it. Fixed the *verification tool* (not the real game,
which always runs through normal Awake() in Play Mode) by force-invoking
`Awake()` via reflection, the same documented pattern
`Docs/Systems/BuildAndVerification.md` already names for this exact class
of problem. After that fix, the preview genuinely renders both stocked
vehicles correctly.

## Acceptance scorecard

- [x] Compiles clean (0 `error CS`)
- [x] Real 3D vehicle preview renders correctly for every dealer stock item (not assumed - captured and looked at)
- [x] Reuses the existing purchase pipeline unchanged (code review - `TryPurchase`/`SpawnPurchasedVehicle` calls untouched)
- [ ] User hands-on confirmation - opening the dealer, rotating, buying, seeing it spawn in the world
- [ ] Windows build - not built yet this pass, offered

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/MINI-160-compile.log`, 0 `error CS` | No |
| Real vehicle preview renders | Reflection-extracted RenderTexture from the panel's own preview camera, for every stocked vehicle | `Logs/Tasks/MINI-160/Preview-TNAX560.png`, `Preview-RangeRova.png` | Yes - final look |
| Dealer's real stock reused, not duplicated | Code review (`ShopPanelController.Stock` getter) | `ShopPanelController.cs` | No |
| Purchase pipeline unchanged | Code review | `VisualVehicleDealerPanel.cs` `Buy()` | No |

## Handoff

- Files changed: new `VisualVehicleDealerPanel.cs`; one-line additions to `ShopPanelController.cs` (public `Stock` getter) and `VehicleSpawnController.cs` (`GetPreviewPrefab`); `TownNPCInteractable.cs`'s `CarDealer` case now opens the new panel instead of the generic shop.
- Decisions made: mirror the wardrobe panel's exact pattern rather than build a new UI system; reuse the dealer's existing stock data rather than duplicate it.
- Visual locks added/changed: none.
- Known limitations: not hands-on played yet; the dealer's current live stock only has 2 vehicles wired (TMAX, Range Rova) - Koss ("koss") wasn't found in this NPC's stock array, which is a pre-existing scene-data fact unrelated to this change (the old text-list UI would show the same 2 items).
- Next action: user plays it - talk to the Car Dealer, rotate/inspect, buy, confirm the vehicle actually spawns as expected.
- Ownership released: yes, at the end of this session's pass.
