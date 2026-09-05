# Up Iz Up Mini — MINI-125 TMAX test spawn

```yaml
task_id: MINI-125
title: Spawn the upgraded TMAX beside the active character for playtesting
request_owner: User
integrator: Codex
  status: evidence_ready
approval_class: B
budget:
  codex_time: one bounded runtime-spawn change and focused verification
  claude_time: 0
  external_credits: 0
  stop_condition: one TMAX spawns near Sacat or Franki without spawning the Range Rover or SuperMoto
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs
  - Docs/WorkPackets/MINI-125.md
  - Docs/Systems/Vehicles.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
depends_on:
  - MINI-124
```

## Intent

Spawn the upgraded TMAX once beside the active character at game start so the user can immediately inspect and ride the new front/rear wheels and steering assembly.

## References

- Existing TMAX purchase/spawn flow in `VehicleSpawnController`.
- MINI-124 upgraded `TMAX_560.prefab`.

## Non-goals

- Do not change vehicle ownership, pricing, missions, physics, rider pose, prefab visuals, or the live scene.
- Do not spawn or auto-mount the Range Rover, SuperMoto, or stock demo bike.

## Acceptance scorecard

- [x] Exactly one TMAX is wired to spawn near the active character
- [x] The TMAX uses the existing prefab and normal walk-up mount flow
- [x] Range Rover and SuperMoto do not dev-spawn
- [x] Unity compile passes
- [x] Standing TMAX prefab/scene validation passes
- [ ] User ride/visual approval remains required

## Implementation plan

Add a narrowly scoped TMAX-only test flag and method to the existing runtime spawner. Keep the normal purchase path untouched. Rollback is one flag change.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/Tasks/MINI-125/compile.log` PASS | No |
| Spawn isolation | Source assertions plus MINI-064/065 scene/prefab validator | `Logs/Tasks/MINI-125/tmax-validation.log` PASS | No |
| Appearance and ride | User Play Mode test | Pending | Yes |

## Handoff

- Files changed: `VehicleSpawnController.cs` and task/current vehicle documentation
- Decisions made: TMAX-only temporary runtime spawn; no auto-mount
- Visual locks added/changed: None
- Known limitations: User must confirm the actual spawned position and riding visuals
- Next action: Run from Unity Hub and test the TMAX
- Ownership released: Yes
