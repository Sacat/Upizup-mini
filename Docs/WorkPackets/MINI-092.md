# MINI-092 — Combat contact truth

```yaml
task_id: MINI-092
title: Combat contact truth
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  codex_time: one reusable contact/timeline layer, migrate player/companion/gang strikes, validation, rebuild, Windows build, smoke, and test handoff
  claude_time: 0
  external_credits: 0
  stop_condition: no shooting, ragdolls, new models, new paid clips, or combo tree in this slice
reserved_files:
  - Assets/UpIzUpMini/Scripts/Combat/MeleeContactResolver.cs
  - Assets/UpIzUpMini/Scripts/Combat/SimpleMeleeCombat.cs
  - Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs
  - Assets/UpIzUpMini/Scripts/Combat/NpcCombatHealth.cs
  - Assets/UpIzUpMini/Scripts/Character/CompanionCombatAssist.cs
  - Assets/UpIzUpMini/Scripts/Character/CharacterVitals.cs
  - Assets/UpIzUpMini/Editor/Mini046CompanionCombatValidation.cs
  - Assets/UpIzUpMini/Editor/Mini092CombatContactValidation.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
protected_files:
  - Assets/UpIzUpMini/Art/Animations
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - ProjectSettings
  - Packages
  - Docs/VISUAL-APPROVAL-REGISTER.md
depends_on:
  - MINI-090
  - MINI-091
```

## Intent

A punch should play first and only connect during its active moment, in a short forward hand path, when the target remains in front and unobstructed. A swing can damage a target only once. Player, companion, and gang combat should share these rules. The player spends a small amount of stamina per committed swing.

## Non-goals

- No replacement punch clip in this task; the existing clip remains a known visual limitation.
- No combos, block, dodge, shooting, weapon inventory, or ragdoll.
- No faction/story/reputation changes.
- No damage rebalance beyond player swing stamina cost and correcting police-only heat escalation.

## Acceptance scorecard

- [x] Reusable attack profile and forward swept contact resolver exist.
- [x] Contact rejects targets behind/outside the forward arc/path and includes a runtime line-of-sight blocker check.
- [x] Player, companion, and faction AI resolve once during windup/active/recovery timing rather than on button/proximity start.
- [x] A swing cannot hit twice, and cooldown/recovery prevents spam.
- [x] Player swing uses stamina; insufficient stamina prevents a new attack.
- [x] Max heat is applied for striking police, not an unrelated rival with the same health component.
- [x] Focused and prior combat validations pass.
- [x] Canonical rebuild, Windows build, and headless smoke pass.
- [ ] User receives one combined hands-on test for systems 1–3.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/MINI-092-Validate.log` — pass | No |
| Contact truth | Deterministic timing/geometry plus runtime LOS wiring validation | `Logs/MINI-092-Validate.log` — pass | Wall blocking and feel remain part of live test |
| Prior behavior | Updated MINI-046 + MINI-069 regressions | `Logs/MINI-092-CompanionRegression.log`, `Logs/MINI-092-GangRegression.log` — pass | Motion still owed |
| Motion/feel | User combined test | Pending | Yes |

## Handoff

- Files changed: reusable melee contact/timeline layer; player, companion, faction combat; NPC health registry; character stamina spending; validators; generated scene; workflow records.
- Decisions made: Use one approximate forward fist path and timed active event for all current unarmed combat. Keep the existing clip until a separate animation-approved task.
- Visual locks added/changed: None.
- Known limitations: Current sword-like/warped punch clip remains; no combo/block/dodge/ragdoll/shooting; actual wall blocking and animation/contact feel need the live player test.
- Next action: User plays the combined systems 1–3 build and reports concrete movement/police/combat observations.
- Ownership released: Yes.

Rollback: `7996bf4` restores the MINI-091 gameplay checkpoint.
