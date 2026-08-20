# MINI-093 — Police melee retaliation

```yaml
task_id: MINI-093
title: Police melee retaliation
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: B
budget:
  codex_time: wire police into existing timed contact, focused validation, compile, and checkpoint only
  claude_time: 0
  external_credits: 0
  stop_condition: no scene rebuild, Windows build, animation replacement, arrest system, weapons, or damage rebalance outside police punches
reserved_files:
  - Assets/UpIzUpMini/Scripts/Interaction/PoliceOfficer.cs
  - Assets/UpIzUpMini/Scripts/Combat/MeleeContactResolver.cs
  - Assets/UpIzUpMini/Editor/Mini093PoliceCombatValidation.cs
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Art
  - ProjectSettings
  - Packages
depends_on:
  - MINI-092
```

## Intent

When a wanted character is close enough, a police officer should face them, play the current melee action, and apply damage once during the active contact moment. Repeated hits can reduce health to zero, which already fails the mission and respawns both boys at the chosen safehouse.

## Non-goals

- No arrest/cuff animation, gun, ragdoll, combo, or new attack clip.
- No changes to player damage, police stamina, heat thresholds, missions, or respawn rules.
- No scene/build regeneration for this small script-only adjustment.

## Acceptance scorecard

- [x] Police commit a timed swing only while actively chasing and within strike range.
- [x] Damage occurs at the active contact moment, not immediately.
- [x] Target behind/outside contact or already dead is not damaged.
- [x] One police swing damages once and respects cooldown/recovery.
- [x] Repeated police damage can reach zero through existing `CharacterVitals.Damage`.
- [x] Focused validation and Unity compile pass.
- [x] No scene or build artifact changes.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/MINI-093-Validate.log` — pass | No |
| Retaliation/damage | Focused deterministic validation | `Logs/MINI-093-Validate.log` — 100→88, one hit, behind miss, death event pass | No |
| Motion/feel | User runs project from Unity Hub | Pending | Yes |

## Handoff

- Files changed: `PoliceOfficer.cs`, shared contact helper, focused validator, and workflow records.
- Decisions made: Police use the existing melee action and MINI-092 timing/contact layer; 12 damage with a 1.1-second cooldown is the first test tuning.
- Visual locks added/changed: None.
- Known limitations: Current attack clip remains placeholder quality; runtime feel awaits the user's Hub test.
- Next action: User opens the project from Unity Hub, lets police reach striking distance, and watches the health meter/death flow.
- Ownership released: Yes.

Rollback: `75b1404` restores the last gameplay checkpoint (`2f93c07` is its handoff-only follow-up).
