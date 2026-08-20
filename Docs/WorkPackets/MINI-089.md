# MINI-089 — Locked-progression dialogue wording

```yaml
task_id: MINI-089
title: Locked-progression dialogue wording
request_owner: User
integrator: Codex
status: complete
approval_class: B
budget:
  codex_time: one exact text replacement, focused validator, rebuild, and build
  claude_time: 0
  external_credits: 0
  stop_condition: do not rewrite unrelated dialogue
reserved_files:
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Editor/Mini089DialogueValidation.cs
  - Docs/WorkPackets/MINI-089.md
  - PROJECT-HANDOFF.md
  - TASKS.md
protected_files:
  - Boss C VA-003 body and chain profiles
  - Sacat VA-002 chain profile
depends_on:
  - MINI-088
```

## Intent

Locked progression characters should respond with the user's exact line: `Keep doing your ting. I'll maybe organize you when you build up ur self`.

## Non-goals

- Do not change role gates, missions, reputation thresholds, or other dialogue.
- Do not alter any character or chain visuals.

## Acceptance scorecard

- [x] Exact requested line replaces the former generic progress response.
- [x] Focused validation exercises a locked Boss C interaction and confirms the returned text.
- [x] Scene rebuild and Windows build succeed.

## Evidence

- Exact text validator: `Logs/MINI-089-Validate.log` — pass.
- Canonical scene rebuild: `Logs/MINI-089-Rebuild.log` — success.
- Windows build: `Logs/MINI-089-Build.log` — succeeded, 407,521,243 bytes.

Rollback: `43814d9` restores MINI-088 with the previous dialogue.
