## MINI-166 revision — visibly individual wardrobe (2026-09-12)

Wardrobe now states that changes apply only to its named wearer. Added distinct character starting outfits without replacing saved selections. Expanded actual Play Mode regression verifies the other character's choices, renderer meshes and indexed colours stay unchanged through menu selection/Restore/Cancel/Apply, plus the existing two-character save/load round trip. Built-player proof now shows each character's own default outfit instead of dressing both in the same test outfit. See `Docs/WorkPackets/MINI-166.md`.

## MINI-166 — complete wardrobe and matching portraits (Codex repair, 2026-09-12)

Safehouse E -> 5 now exposes all eight clothing designs on both characters, with seven colours and per-character persistence. Fixed front orientation, width-aware framing, indexed material-property-block copying, and immediate paused-pose mesh baking. Skin/soles/buttons are never tinted. Watch/shades have Wear/Remove buttons; headphones retain their toggle; the obsolete duplicate cap trial moved out of Accessories (use Hats). Cancel and Restore opening restore clothes plus accessories and pending colour state; Apply retains choices, F5 saves clothes/headphones. Free unowned accessory try-ons remain session-only.

The new Play Mode test exercises the same ChoosePiece and RestoreOpeningOutfit methods as the buttons and verifies the portrait's material colour against the live renderer. Built-player proof captures actual GPU-skinned players and portrait textures separately, avoiding the old blank ScreenCapture result. Evidence and remaining human appearance review: `Docs/WorkPackets/MINI-166.md`, latest section.

## MINI-166 — VisualWardrobePanel wired to OutfitWardrobe (2026-09-11)

`VisualWardrobePanel`'s Shirts/Pants/Hats/Shoes tabs used to be a hardcoded
"IN PRODUCTION" placeholder regardless of what was actually fitted. Now
reads `wearer.GetComponent<OutfitWardrobe>()` and lists real per-slot pieces
(WORN/WEAR buttons + colour swatches where tintable), falling back to the
placeholder only for slot/character combos genuinely not built yet. Cancel
now also restores the `OutfitWardrobe` selection (captured on open) - it
previously only restored the legacy trial/accessory system, silently
leaving new outfit picks unrestored. Verified through the exact call path
`SafehouseInteractable`'s E -> 5 menu uses
(`Mini166WardrobeUIValidation.cs`, real Play Mode test, not just the
underlying API): opens, lists Franki's Shirt/Pants pieces, select/Cancel/
reopen/select/Apply all behave correctly. See `Characters.md`'s MINI-166
entry for what's actually fitted, and `Docs/WorkPackets/MINI-166.md` for
full detail.

## MINI-165 — Headphones as a removable head accessory (2026-09-10)

Sacat's existing headphones are now a separate skinned accessory. Open the home wardrobe, Accessories, then Headphones / REMOVE or WEAR; Apply keeps the choice, Cancel and Restore opening outfit restore it. Existing game saves capture the selection through sacatUnequippedItems; legacy saves default to wearing the headphones. Franki has no headphone assignment; this task does not add a second fitted asset.

Six disconnected pieces (6,760 triangles) were extracted without changing positions, UVs, normals, weights or materials. The wave scalp, face and repaired clothing remain in the original body mesh. Compile, Play Mode toggle/Cancel/Apply/GameSave JSON tests, saved-scene renders, Windows build (411,132,437 bytes) and player proof process passed. Logs and on/off images: Logs/Tasks/MINI-165; compiler/build logs: Logs/mini165-*.log. Automated player UI captures were blank and are not visual acceptance evidence; hands-on button layout review remains owed. See Docs/WorkPackets/MINI-165.md.
# UI
## MINI-146 — home wardrobe ledger (2026-09-07)

Reuse SafehouseInteractable prompt: E opens owned bed, 5 opens wardrobe, 1 wears/2 removes watch, E returns. Only controlled nearby character may change; locked homes refuse. Named GameInput actions appended without renumbering old actions. Save/rest retains choice. Text and handler checks passed, but editor Camera.Render screenshots cannot verify IMGUI menu: user must test live home flow and normal save/reload. No full garment UI or mobile certification claimed.


Added 2026-08-29 - a real gap in the original 9-system list, created per `README.md`'s own instruction not to leave a real system undocumented just because it wasn't on the initial list.

## Current state

Multiple `Canvas` objects coexist in the scene at different `sortingOrder` values, layered deliberately (HUD at the bottom, gameplay panels like shop/dialogue above it, the pause menu meant to sit above everything). Pause menu (Esc, Resume/Quit) exists and is functionally wired, but its canvas sorting order was wrong until this session's fix.

## Architecture

- `HUDCanvas` - base gameplay HUD (health, minimap, etc.), `sortingOrder=0`.
- `GameplayUICanvas` - shop/dialogue/interaction panels, `sortingOrder=5` (renders above the HUD, correctly).
- `PauseMenuCanvas` - the Esc pause menu, `sortingOrder=100` as of this fix (previously `0`, the same as the HUD - meaning it could render BEHIND `GameplayUICanvas`).
- `Assets/UpIzUpMini/Scripts/UI/PauseMenuController.cs` - Esc toggles `_paused`, sets `Time.timeScale`, cursor lock state, and the panel's active state. Also handles Q-to-quit while paused.
- Built by `Mini011PhaseBSetup.cs` (search for `PauseMenuCanvas`) - a `ScreenSpaceOverlay` canvas, a full-stretch-anchored `PausePanel` (semi-transparent black `Image`), Resume/Quit buttons, `EventSystem`/`StandaloneInputModule` for button navigation.

## What worked / what didn't

- **(2026-08-29) "ESC doesn't bring up the menu" was NOT a wiring bug.** A live-scene inspection (`Mini120DiagnosePauseMenu.cs`) confirmed the `PauseMenuController`, its panel, and both buttons were all correctly referenced and the panel genuinely does activate on Escape. The real cause: `PauseMenuCanvas` defaulted to `sortingOrder=0`, the same as `HUDCanvas`, while `GameplayUICanvas` sits at `5` and renders on top of both - so the pause panel activates but can render BEHIND whatever gameplay UI happens to be on screen, which reads as "nothing happened" when Esc is pressed. Fixed by setting the pause canvas to `sortingOrder=100`, comfortably above every other known canvas.
- **A `RectTransform` with `sizeDelta=(0,0)` is not automatically a bug** - full-stretch anchors (`anchorMin=(0,0)`, `anchorMax=(1,1)`, zero offsets) legitimately report `sizeDelta=(0,0)` while still correctly filling the parent. Don't flag this as broken without checking the anchor mode first - this cost a wasted diagnosis step this session before the real (sorting order) cause was found.

## Open items

- MINI-160 (2026-09-08): `VisualVehicleDealerPanel` reuses `VisualWardrobePanel`'s isolated-stage/IMGUI pattern for a rotate-and-buy vehicle preview at the Car Dealer NPC. If this pattern gets reused a third time, consider factoring the shared "hidden-layer render stage + IMGUI panel" scaffolding out of both, rather than a third copy-paste.

- MINI-151 (2026-09-07): VisualWardrobePanel is runtime-only, entered from owned-home E -> 5. Bakes skinned preview; shares GPU-only static meshes without reading vertices; rotate and session-only Apply/Cancel. Hides/restores enabled canvases while paused. Use opaque whiteTexture tinted dark, not blackTexture, for backdrop; keep negative GUI depth rather than resetting it before the event ends. Hidden player screenshot was black: visible built-player proof is required. New garments/colours are explicitly unavailable. Final visual acceptance and actual home-button interaction remain user playtest gates. See WorkPackets/MINI-151.md.

- No other UI-specific gaps logged yet as of this file's creation. Future UI/menu work should extend this file rather than being folded into whichever gameplay system happens to be the reason for the change.

## Key files

- `Assets/UpIzUpMini/Scripts/UI/PauseMenuController.cs`
- `Assets/UpIzUpMini/Scripts/UI/ShopPanelController.cs`, `InventoryPanelController.cs`
- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (search `PauseMenuCanvas`, `HUDCanvas`, `GameplayUICanvas` for where each is built)
