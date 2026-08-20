# Up Iz Up Mini — Work Packet Template

Copy this section into `TASKS.md` or a task-specific document before implementation.

```yaml
task_id: MINI-###
title: Short outcome
request_owner: User
integrator: Codex | Claude
status: proposed | approved | implementing | evidence_ready | accepted | blocked
approval_class: A | B | C
budget:
  codex_time: bounded description
  claude_time: bounded description
  external_credits: 0
  stop_condition: concrete limit
reserved_files:
  - exact/path
protected_files:
  - exact/path
depends_on:
  - task or decision
```

## Intent

What the player should see, feel, or be able to do.

## References

- User reference paths/URLs:
- Existing in-game reference:
- Geographic or technical source and license:

## Non-goals

What must remain unchanged.

## Acceptance scorecard

- [ ] Functional behavior
- [ ] Static visual evidence
- [ ] Motion evidence, if applicable
- [ ] Mobile-distance/resolution check
- [ ] Performance/content budget
- [ ] Save/load or regression check, if applicable
- [ ] User approval required/received

## Implementation plan

Smallest reversible steps and rollback point.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | | No |
| Behavior | Validation/live test | | |
| Appearance | Fixed screenshot | | |
| Motion/feel | Short live capture | | Yes until accepted |

## Handoff

- Files changed:
- Decisions made:
- Visual locks added/changed:
- Known limitations:
- Next action:
- Ownership released:
