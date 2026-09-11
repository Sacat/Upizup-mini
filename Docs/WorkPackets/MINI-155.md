# MINI-155 — arrow keys pan/tilt the camera like the mouse

```yaml
task_id: MINI-155
title: Arrow keys drive camera look (mirroring mouse), WASD keeps movement
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: B
budget:
  claude_time: one small change, single file
  external_credits: 0
  stop_condition: compile-clean, no ProjectSettings edit
reserved_files:
  - Assets/UpIzUpMini/Scripts/Input/GameInput.cs
  - Docs/WorkPackets/MINI-155.md
  - Docs/Systems/Camera.md
  - Docs/Systems/README.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - ProjectSettings/InputManager.asset (not touched - see rationale below)
  - Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs (not touched - already device-agnostic)
depends_on: []
```

## Intent

User: "ok i want the arrow keys to move like the mouse up down left right."
Clarified via question: arrow keys should pan/tilt the camera the way
mouse movement does; WASD keeps character movement; arrow keys stop also
walking the character (to avoid one press doing both at once).

## References

- Existing in-game reference: `ThirdPersonFollowCamera.cs`'s mouse-look
  (`GameInput.Look` -> yaw/pitch), already the exact behavior to mirror.

## Non-goals

- No `ProjectSettings/InputManager.asset` edit.
- No change to vehicle throttle/steer (still reads raw
  `Input.GetAxis("Horizontal"/"Vertical")` directly, unaffected - riding
  still uses arrow keys to steer, and the camera ignores look input
  entirely while `OrbitLocked` during a ride, so there is no conflict).
- No new sensitivity tuning UI - arrow-key look reuses the camera's
  existing mouse sensitivity fields.
- No mobile/touch look control (out of scope, separate future work).

## Implementation

`Assets/UpIzUpMini/Scripts/Input/GameInput.cs`:

- `Move` now reads WASD directly (`Input.GetKey(KeyCode.W/A/S/D)`) instead
  of Unity's default `Horizontal`/`Vertical` axes, which also bind the
  arrow keys. This is the one change needed to stop arrow keys from also
  walking the character - no `ProjectSettings` edit required, and vehicle
  scripts are unaffected since they read the raw axis directly, not this
  facade.
- `Look` now adds a new `ArrowKeyLook()` vector (Right/Left -> x,
  Up/Down -> y, matching the sign convention `ThirdPersonFollowCamera`
  already expects from mouse X/Y) on top of the existing mouse delta, so
  either input (or both together) drives the same camera look value.

Why this approach: `ThirdPersonFollowCamera.cs` was already fully
input-device-agnostic (it just reads `GameInput.Look`), so no camera code
needed to change at all - the fix is entirely in the input facade.

## Acceptance scorecard

- [x] Functional behavior (compile-clean batch build, logic reviewed)
- [ ] Static visual evidence (not applicable - input-only change)
- [ ] Motion/feel evidence - held-arrow-key camera pan/tilt feel, and
      confirming WASD-only movement feels normal, need the user's hands-on
      check (same headless-render limitation as MINI-154 applies to any
      attempt at a screenshot proof of "the camera moved")
- [x] Mobile-distance/resolution check - n/a, PC keyboard input only,
      touch layer untouched (`SetVirtualLook`/`SetVirtualMove` hooks unused)
- [x] Performance/content budget - n/a, no new allocations/hot-path cost
      beyond four extra `GetKey` calls per frame
- [ ] Save/load or regression check - n/a, no save-affecting state
- [x] User approval - direction confirmed via clarifying question before implementing

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/MINI-155-compile.log`, 0 `error CS` matches | No |
| Arrow keys no longer move character | Code review: `Move` now reads WASD only | `GameInput.cs` | No |
| Arrow keys pan/tilt camera like mouse | Code review: `Look` adds `ArrowKeyLook()` with matching sign convention to `ThirdPersonFollowCamera`'s mouse-delta usage | `GameInput.cs` | Yes - actual feel/rate needs a hands-on check |
| No conflict with vehicle steering | Code review: vehicles read raw `Input.GetAxis` directly, camera ignores look while `OrbitLocked` | `BikeInteractable.cs`, `SuperMotoInteractable.cs`, `ThirdPersonFollowCamera.cs` | No |

## Handoff

- Files changed: `Assets/UpIzUpMini/Scripts/Input/GameInput.cs`.
- Decisions made: WASD-only movement (freeing arrow keys) rather than
  editing shared `ProjectSettings/InputManager.asset`; arrow-key look
  reuses the camera's existing mouse sensitivity rather than adding a
  separate tunable.
- Visual locks added/changed: none.
- Known limitations: held-arrow-key pan/tilt rate not hands-on tuned yet;
  no touch equivalent.
- Next action: user plays and confirms the feel (both on foot and that
  vehicle steering/riding is unaffected); if arrow-key look feels
  too fast/slow relative to mouse, add a dedicated sensitivity field.
- Ownership released: yes, at the end of this session's pass.
