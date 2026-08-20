# MINI-085 — Current-build regression baseline and safety checkpoint

```yaml
task_id: MINI-085
title: Current-build regression baseline and safety checkpoint
request_owner: User
integrator: Codex
status: implementing
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

- [ ] Functional behavior
- [ ] Static visual evidence
- [ ] Motion evidence
- [ ] Mobile-distance/resolution observation
- [ ] Build-size baseline
- [ ] Save/load or regression check
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
| Compiles | Unity batch compile | Pending | No |
| Behavior | Validators + built player | Pending | Yes for feel |
| Appearance | Fixed screenshots | Pending | Yes until user accepts |
| Motion/feel | Live Computer Use inspection | Pending | Yes until user accepts |

## Handoff

- Files changed: Pending
- Decisions made: None yet
- Visual locks added/changed: None yet
- Known limitations: Pending
- Next action: Pending
- Ownership released: No
