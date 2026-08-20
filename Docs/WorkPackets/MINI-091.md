# MINI-091 — NPC and companion intelligence

```yaml
task_id: MINI-091
title: NPC and companion intelligence
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: B
budget:
  codex_time: improve existing local steering/follow/patrol/police states, focused validation, rebuild, and checkpoint
  claude_time: 0
  external_credits: 0
  stop_condition: preserve current animations, NavMesh, scene layout, gang pools, and mission content
reserved_files:
  - Assets/UpIzUpMini/Scripts/Character/FollowController.cs
  - Assets/UpIzUpMini/Scripts/Character/GangMemberController.cs
  - Assets/UpIzUpMini/Scripts/Character/LocalSteeringSafety.cs
  - Assets/UpIzUpMini/Scripts/Interaction/PatrolNPC.cs
  - Assets/UpIzUpMini/Scripts/Interaction/PoliceOfficer.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini091NpcIntelligenceValidation.cs
protected_files:
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - ProjectSettings
  - Packages
  - Docs/VISUAL-APPROVAL-REGISTER.md
depends_on:
  - MINI-090
```

## Intent

Followers should hold a readable trailing formation instead of targeting the same point, wait and re-evaluate rather than continuously pressing into blocked geometry, and steer apart from nearby characters. Walking NPCs should apply the same cheap dynamic avoidance. Police should visibly transition between patrol, chase, last-known-position search, and tired recovery instead of behaving as one endless pursuit mode.

## Non-goals

- No new character models or animation clips.
- No NavMesh rebake, scene-layout change, or return to NavMesh pathing for the companion.
- No combat hitbox changes; those belong to MINI-092.
- No change to gang unlocks, damage, reputation, missions, or pool sizes.

## Acceptance scorecard

- [x] Companion and recruited followers use stable trailing formation targets with stopping hysteresis.
- [x] Local steering applies bounded separation from nearby characters and waits if obstacle/ledge checks fail.
- [x] Patrol NPCs apply the shared local safety layer to dynamic obstacles.
- [x] Police expose Patrol/Chase/Search/Recover/Down states, drain stamina only while chasing, and search a last known position before returning to patrol.
- [x] Existing last-rival retreat, defeat despawn, and distance-pool respawn behavior remains intact.
- [x] Focused validation and Unity compile/rebuild pass.
- [ ] Motion remains pending the user's combined hands-on test after MINI-092.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/MINI-091-Validate.log` — pass | No |
| Behavior | Focused structural/logic validation | `Logs/MINI-091-Validate.log` — pass | No |
| Motion/feel | Combined live game test after MINI-092 | Pending | Yes |

## Handoff

- Files changed: follower/recruit/local-steering/patrol/police scripts, focused validator, generated scene, and workflow records.
- Decisions made: Keep direct terrain-friendly companion steering; layer formation, separation, waiting, and hysteresis onto it. Keep existing pooled gang lifecycle.
- Visual locks added/changed: None.
- Known limitations: Existing movement clips are unchanged; natural motion still requires the combined hands-on test.
- Next action: MINI-092 combat contact truth, then one Windows build and player test.
- Ownership released: Yes.

Rollback: `f77cb16` restores the MINI-090 gameplay checkpoint (`71c48fe` only updates its handoff hash).
