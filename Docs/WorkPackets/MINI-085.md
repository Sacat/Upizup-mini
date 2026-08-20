# MINI-085 — Current-build regression baseline and safety checkpoint

```yaml
task_id: MINI-085
title: Current-build regression baseline and safety checkpoint
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: B
budget:
  codex_time: one bounded verification and evidence pass
  claude_time: 0
  external_credits: 0
  stop_condition: stop before any map, character, Hitem3D, combat, or vehicle redesign
reserved_files:
  - "**"
protected_files:
  - all existing user/Claude gameplay and asset work
depends_on:
  - MINI-084
```

## Intent

Create a recoverable, evidence-backed baseline of the game exactly as it exists now so future work starts from known behavior instead of an uncommitted pile of assumptions.

## References

- Existing build: `Builds/GrandBayProof/UpIzUpMini.exe`
- Current state: `Docs/CURRENT.md`
- Full task history: `PROJECT-HANDOFF.md`

## Non-goals

- No gameplay, map, art, animation, balance, rendering, input, or asset redesign.
- No Hitem3D generation or external credit use.
- No automatic changes to previously tuned placements or vehicle settings.

## Acceptance scorecard

- [x] Functional behavior
- [x] Static visual evidence
- [x] Motion evidence (partial live coverage; unreachable systems classified below)
- [x] Mobile-distance/resolution observation
- [x] Build-size baseline
- [ ] Save/load regression check (not exercised in this bounded pass)
- [ ] User approval received for any visual lock

## Implementation plan

1. Claim the whole project and run preflight.
2. Create a safety branch while preserving the dirty tree.
3. Compile, run established validators, rebuild, and smoke-test the player.
4. Inspect the live build with Computer Use and capture evidence.
5. Record findings without fixing them in this task.
6. Commit the verified baseline, update handoff/current state, and release ownership.

Rollback point: the pre-task branch/commit remains untouched; the new safety branch contains the checkpoint.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity 6000.3.10f1 batch compile | PASS — `Logs/MINI-085-Compile.log` | No |
| Generated scene | Rebuild via `Mini011PhaseBSetup.BuildScene` and asset prep methods | PASS — `Logs/MINI-085-Rebuild-*.log` | No |
| Behavior | TMAX/Rover/factions/gangs/chain validators + built-player smoke | PASS for wired/automated claims — `Logs/MINI-085-Validate-*.log`, `Logs/MINI-085-PlayerSmoke.log` | Yes for feel |
| Windows build | `Mini001Build.BuildWindowsPlayer` | PASS — about 388.9 MB, `Logs/MINI-085-WindowsBuild.log` | No |
| Appearance | Fixed scene/vehicle screenshots | Captured — `Logs/Snapshots/*.png`, `Logs/rover-render/*.png` | Yes until user accepts |
| Live opening | Computer Use at 1280x720 | Character switch, objective advance, inventory, H help, and world-space E prompts work; both heroes render in non-T-pose idle | Yes for animation feel |

## Evidence classification

### Working in the live build

- `H` opens and closes the tutorial/control panel.
- `Tab` switches Sacat/Franki and advances the opening objective.
- Sacat and Franki both render as full Humanoid characters in normal idle poses.
- The persistent health, energy, heat, money, crop, seed inventory, objective card, and distance readout render at 1280x720.
- Proximity UI renders `[ E ] Send to farm (locked)` on the companion and `[ E ] Use bed` at the safehouse.
- Basic keyboard movement changes player position and the camera can orbit with mouse drag.

### Needs revision

- The active player's name is not visible anywhere in the live HUD, so the user cannot reliably tell whether Sacat or Franki is active.
- The opening camera can start pressed against/over the safehouse roof, obscuring the heroes and dialogue until the camera is rotated.
- The current world remains a procedural prototype: oversaturated repeated grass, a very plain safehouse, broad empty terrain, regular house rows, and no defensible Lalay-to-beach geographic fidelity.
- The TMAX and Range Rover look substantially more detailed than the environment and remain unsuitable for a mobile target at their current build payload sizes.

### Not exercised live in this bounded pass

- Sustained run/stamina drain (the UI automation can only send discrete key presses, not a trustworthy held sprint).
- Jump timing/animation, NPC patrol motion, combat contact/reactions, crop interaction/growth, TMAX mount/lean/wheelie, Rover driving, police pursuit, save/load, and later missions.
- These systems retain their automated/static PASS results where listed above, but are not visually accepted.

## Handoff

- Files changed: Verification documentation/logs plus the deliberate baseline checkpoint; no gameplay, scene, prefab, package, setting, or asset redesign was made by MINI-085.
- Decisions made: Preserve the exact pre-test state as commit `b367fc5` on branch `codex/mini-085-baseline-20260820`; use live evidence to prioritize small presentation fixes before map/Hitem3D work.
- Visual locks added/changed: None. Only the user can approve a visual lock.
- Known limitations: Live automation could verify the opening, UI, interaction prompts, switching, camera orbit, and basic movement, but not trustworthy held-input motion or distant systems.
- Next action: Fix only the missing active-player name and obstructed opening camera as a small separate packet, then request a short user playtest. After that, begin the measured Map Truth/map-lab packet.
- Ownership released: Yes
