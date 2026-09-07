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

- MINI-151 (2026-09-07): VisualWardrobePanel is runtime-only, entered from owned-home E -> 5. Bakes skinned preview; shares GPU-only static meshes without reading vertices; rotate and session-only Apply/Cancel. Hides/restores enabled canvases while paused. Use opaque whiteTexture tinted dark, not blackTexture, for backdrop; keep negative GUI depth rather than resetting it before the event ends. Hidden player screenshot was black: visible built-player proof is required. New garments/colours are explicitly unavailable. Final visual acceptance and actual home-button interaction remain user playtest gates. See WorkPackets/MINI-151.md.

- No other UI-specific gaps logged yet as of this file's creation. Future UI/menu work should extend this file rather than being folded into whichever gameplay system happens to be the reason for the change.

## Key files

- `Assets/UpIzUpMini/Scripts/UI/PauseMenuController.cs`
- `Assets/UpIzUpMini/Scripts/UI/ShopPanelController.cs`, `InventoryPanelController.cs`
- `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (search `PauseMenuCanvas`, `HUDCanvas`, `GameplayUICanvas` for where each is built)
