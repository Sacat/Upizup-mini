# MINI-189 — Claude handoff for the Lalay Tool

```yaml
task_id: MINI-189
title: Write an exact, source-grounded Claude handoff for the Lalay Tool model and grip
request_owner: User
integrator: Codex
status: complete
approval_class: A
budget:
  external_credits: 0
  stop_condition: one handoff document checked against current files and logs
reserved_files:
  - Docs/CLAUDE-HANDOFF-MINI-186-188.md
  - Docs/WorkPackets/MINI-189.md
  - PROJECT-HANDOFF.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
depends_on:
  - MINI-186
  - MINI-187
  - MINI-188
```

## Intent

Give Claude a self-contained transfer of the current model, source processing, scene wiring, colorways, reference-based hand fit, validation, known limitations and safe next steps. This task changes documentation only.

## Acceptance

- [x] Paths, measurements and statuses cross-checked against source reports and scripts.
- [x] Current limitations distinguished from verified behavior.
- [x] Ownership released after document review.

The handoff lives at `Docs/CLAUDE-HANDOFF-MINI-186-188.md`. It includes a paste-ready prompt, the latest scene state, superseded offsets, asset geometry and process numbers, source paths, runtime anchors, color materials, validation logs, external visual references and remaining work. Key listed files exist; the document has no trailing whitespace. This documentation-only task did not alter game assets or run Unity.
