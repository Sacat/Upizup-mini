# MINI-096 — Reusable world-expansion workflow

```yaml
task_id: MINI-096
title: Reusable Dominica and future-map expansion workflow
request_owner: User
integrator: Codex
status: accepted
approval_class: A
budget:
  codex_time: One bounded workflow, template, scaffolder and validator pass
  claude_time: 0
  external_credits: 0
  stop_condition: Stop after the reusable documents and scripts validate against a disposable sample district; do not alter a Unity scene or production map.
reserved_files:
  - .agents/skills/upizup-mini-production/SKILL.md
  - Docs/WORLD-EXPANSION-WORKFLOW.md
  - Docs/Templates/MAP-DISTRICT-PACKET.md
  - Docs/Templates/MAP-DISTRICT-MANIFEST.template.json
  - Tools/World/New-MapDistrict.ps1
  - Tools/World/Test-MapDistrict.ps1
  - Docs/Maps/dm-dom-grand-bay-lalay-highland-v1/**
  - Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/**
  - workflow and handoff records
protected_files:
  - Assets/UpIzUpMini/Scenes/**
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini095LalayMapLabSetup.cs
  - Assets/UpIzUpMini/Art/Materials/ObjectiveMarker.mat
depends_on:
  - MINI-094
  - MINI-095
  - VA-004
```

## Intent

Make every future Dominica district, island expansion, or unrelated map follow the successful parts of MINI-094/095: licensed map truth, explicit local knowledge, cheap previews, user visual decisions, playable grading, passability validation, single-owner migration, and a concise handoff another agent can resume.

## Non-goals

- Do not change any Unity scene, terrain, gameplay system, package, or project setting.
- Do not start Roseau or another district.
- Do not download geographic data or spend asset credits.

## Acceptance scorecard

- [x] Canonical stage-by-stage world workflow exists.
- [x] Reusable district packet and manifest templates exist.
- [x] Scaffolder creates a consistent district workspace without overwriting files.
- [x] Validator catches missing identity, sources, approval state, cameras, budgets, and passability gates.
- [x] Up Iz Up production skill routes all future map work through the workflow.
- [x] Disposable scaffold validated under Unity's ignored `Temp` area without touching production content.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Documentation complete | Required-section check | PASS — `Docs/WORLD-EXPANSION-WORKFLOW.md` | No |
| Scaffold works | Disposable `New-MapDistrict.ps1` run | PASS — scaffold and validation returned `valid: true` | No |
| Bad/partial state rejected | `Test-MapDistrict.ps1` | PASS — missing sources/bounds/origin/anchors rejected; Lalay approval skip rejected | No |
| Production content protected | Git diff/status check | PASS — no Unity scene/builder changed | No |

## Handoff

- Files changed: Reusable workflow/templates/scripts; first live Lalay/Highland district manifest; workflow/skill/handoff records.
- Decisions made: Every map uses the gated district state machine and stable map ID; visual rejection blocks migration.
- Visual locks added/changed: None.
- Known limitations: Tools validate workflow metadata, not geographic correctness or driving feel.
- Next action: Correct and approve the player-height Lalay graybox view, then create a separate migration packet with the required role mapping.
- Ownership released: Yes.
