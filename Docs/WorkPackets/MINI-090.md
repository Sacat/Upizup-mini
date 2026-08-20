# MINI-090 — Input and interaction foundation

```yaml
task_id: MINI-090
title: Input and interaction foundation
request_owner: User
integrator: Codex
status: complete
approval_class: A
budget:
  codex_time: one reusable facade, migrate the core on-foot controls, focused validation, compile, rebuild, and Windows smoke build
  claude_time: 0
  external_credits: 0
  stop_condition: do not migrate vehicles, menus, save/load, cheats, or project input packages in this slice
reserved_files:
  - Assets/UpIzUpMini/Scripts/Input/GameInput.cs
  - Assets/UpIzUpMini/Scripts/Character/PlayerController.cs
  - Assets/UpIzUpMini/Scripts/Interaction/InteractionDetector.cs
  - Assets/UpIzUpMini/Scripts/Character/CharacterSwitchManager.cs
  - Assets/UpIzUpMini/Scripts/Combat/SimpleMeleeCombat.cs
  - Assets/UpIzUpMini/Scripts/UI/ControlsPanelController.cs
  - Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs
  - Assets/UpIzUpMini/Editor/Mini090InputFoundationValidation.cs
protected_files:
  - ProjectSettings/InputManager.asset
  - Packages/manifest.json
  - Assets/UpIzUpMini/Art
  - Docs/VISUAL-APPROVAL-REGISTER.md
depends_on:
  - MINI-089
```

## Intent

The existing PC controls should feel unchanged, while gameplay code reads named actions and axes from one reusable facade that a future touch UI or controller source can drive without rewriting player, interaction, switching, tutorial, combat, or camera scripts.

## References

- Existing keyboard controls in `ControlsPanelController`.
- Mobile-first and single-integrator requirements in `Docs/AI-PRODUCTION-WORKFLOW.md`.
- Legacy Input Manager remains in place; package/input-system migration is explicitly out of scope.

## Non-goals

- No touch UI artwork or Android package/settings changes.
- No vehicle, shop-menu, crop-number, pause, save/load, radio, cheat, or debug-control migration in this slice.
- No remapping screen and no visible control change.
- No combat timing/animation changes.

## Acceptance scorecard

- [x] Named actions cover the core on-foot controls and preserve their current keys.
- [x] Move/look axes support both legacy input and injectable virtual input.
- [x] Virtual button press/hold/release behavior is deterministic and testable.
- [x] Core consumers contain no direct `Input.*` calls.
- [x] Focused validation passes.
- [x] Unity compile, canonical scene rebuild, Windows build, and headless smoke pass.
- [x] Existing visual locks and project input/package settings remain unchanged.

## Implementation plan

1. Add a small static facade over the current Legacy Input Manager with named actions plus virtual move/look/button injection.
2. Route only the core on-foot consumers through it.
3. Validate binding parity, injection behavior, and direct-input removal.
4. Rebuild and run the normal regression/build gates.

Rollback: checkpoint `8e44087` restores the completed MINI-089 state.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/MINI-090-Validate.log` — pass | No |
| Behavior | Focused validation | `Logs/MINI-090-Validate.log` — binding parity, virtual injection, and six consumers passed | No |
| Regression | Canonical rebuild + Windows/headless smoke | `Logs/MINI-090-Rebuild.log`, `Logs/MINI-090-Build.log`, `Logs/MINI-090-PlayerSmoke.log` — pass | Only normal hands-on control feel |
| Appearance | No intended visual change | Not applicable | No |

## Handoff

- Files changed: `GameInput.cs`; six core input consumers; focused validator; canonical generated scene; workflow records.
- Decisions made: Preserve Legacy Input Manager now; create an injectable facade before touch UI.
- Visual locks added/changed: None.
- Known limitations: Vehicles and secondary/menu controls remain direct legacy input until later bounded migrations.
- Next action: MINI-091 NPC/companion intelligence.
- Ownership released: Yes.
