# Camera & Look Input

## Current state

The third-person follow camera orbits the active character under mouse
control (both axes, only while the cursor is locked / not paused). MINI-155
added arrow-key camera look as a second input source feeding the same
`GameInput.Look` value the mouse already drives, and split character
movement off Unity's default `Horizontal`/`Vertical` axes onto explicit
WASD so arrow keys no longer double as movement.

## Architecture

- `Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs` - reads
  `GameInput.Look` every `LateUpdate` while the cursor is locked and not
  `OrbitLocked` (vehicle riding), applies it to yaw/pitch with
  `mouseSensitivity`/`verticalSensitivity`, then smooths position/rotation.
  Unaware of *which* physical device produced the look vector - anything
  that writes into `GameInput.Look` drives the camera the same way.
- `Assets/UpIzUpMini/Scripts/Input/GameInput.cs` - the input facade.
  `Move` and `Look` are the two analog-style outputs gameplay reads;
  `WasdMove()`/`ArrowKeyLook()` are the concrete key readers behind them.
  A virtual (touch) override exists for both (`SetVirtualMove`/`SetVirtualLook`)
  for a future mobile layer, independent of the keyboard/mouse path.
- Vehicle throttle/steer (`BikeInteractable.cs`, `SuperMotoInteractable.cs`,
  `TmaxTestInput.cs`) read Unity's raw `Input.GetAxis("Horizontal"/"Vertical")`
  directly, NOT through `GameInput.Move` - they still see the arrow keys
  (and WASD) for driving. This is intentional and untouched by MINI-155:
  the camera ignores `OrbitLocked` mouse/arrow look entirely while riding
  (`ThirdPersonFollowCamera`'s locked-chase-cam branch), so there is no
  conflict between "arrows steer the bike" and "arrows pan the camera" -
  only one of those input paths is ever live at a time.

## What worked / what didn't

- **(MINI-155) Don't edit `ProjectSettings/InputManager.asset` for this.**
  The default `Horizontal`/`Vertical` axes bind arrow keys AND WASD together
  (`negativeButton: left / positiveButton: right / altNegativeButton: a /
  altPositiveButton: d`), so on-foot movement and vehicle steering both read
  through the same shared axis names. Editing that project-settings file to
  strip arrow keys would be a protected/shared surface change with a wider
  blast radius (menu/UI navigation, vehicles) for no benefit. Reading WASD
  directly via `Input.GetKey` inside `GameInput.Move` gets the same result
  (arrow keys freed up) with a change scoped to one file.
- **(MINI-155) Held-key look mirrors mouse-delta look by construction.**
  `GameInput.Look` already just returns a `Vector2` that
  `ThirdPersonFollowCamera` multiplies by `mouseSensitivity * Time.deltaTime`
  every frame; a held arrow key returning a constant `1f`/`-1f` on that same
  axis produces a smooth continuous pan/tilt for as long as it's held,
  identical in effect to sustained mouse movement - no new smoothing/rate
  logic was needed on the camera side.

## Open items

- Arrow-key look rate has not been hands-on tuned separately from mouse
  sensitivity (`mouseSensitivity`/`verticalSensitivity` on
  `ThirdPersonFollowCamera` apply to both); if held-arrow panning feels too
  fast/slow relative to mouse feel, it needs its own sensitivity field
  rather than reusing the mouse one as-is.
- No touch/mobile look control implemented yet - `SetVirtualLook` exists as
  the hook but nothing calls it.

## Key files

- `Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs`
- `Assets/UpIzUpMini/Scripts/Input/GameInput.cs`
