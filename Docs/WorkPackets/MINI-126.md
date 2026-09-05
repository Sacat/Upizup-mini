# Up Iz Up Mini — MINI-126 Windows rebuild

```yaml
task_id: MINI-126
title: Place TMAX on the market road and rebuild GrandBayProof Windows player
request_owner: User
integrator: Codex
  status: evidence_ready
approval_class: A
budget:
  codex_time: one build and one bounded smoke run
  claude_time: 0
  external_credits: 0
  stop_condition: fresh EXE/data payload exists and launches without an immediate fatal error
reserved_files:
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleSpawnController.cs
  - Builds/GrandBayProof/
  - Docs/WorkPackets/MINI-126.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
  - ProjectSettings/DynamicsManager.asset
depends_on:
  - MINI-125
```

## Intent

Place the temporary TMAX test spawn on the Lalay road between the Farm Shop and Produce Buyer, aligned along the road, then package the current project state into the existing Windows build location without regenerating the live scene.

## Non-goals

- No scene generation, vehicle-physics changes, prefab edits, or ProjectSettings cleanup.

## Acceptance scorecard

- [x] Windows build succeeds
- [x] TMAX spawn uses the midpoint between the Farm Shop and Produce Buyer stalls
- [x] TMAX faces along the road rather than across it
- [x] `level0` and `Assembly-CSharp.dll` are fresh
- [x] EXE launches and remains running for a bounded smoke run
- [ ] Player log is exception-clean (pre-existing gang-respawn and vendor nitrous exceptions remain)

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Build | Unity build pipeline | `Logs/Tasks/MINI-126/windows-build.log` PASS, 388,967,827 bytes | No |
| Fresh payload | File timestamps | `Logs/Tasks/MINI-126/freshness.txt`; `level0` and `Assembly-CSharp.dll` newer than spawn source | No |
| Startup | 15-second standalone smoke | `Logs/Tasks/MINI-126/Player-smoke.log`; process stayed running and TMAX spawn logged at `(-8.00, 10.46, -152.09)` | No |
| TMAX appearance/ride | User playtest | Pending | Yes |

## Handoff

- Files changed: `VehicleSpawnController.cs`, Windows build payload, and task/current documentation
- Decisions made: Use the real two-stall midpoint and face perpendicular to their cross-road line; build the existing saved scene only
- Known limitations: Build smoke cannot judge wheel appearance or riding feel; repeated unrelated/non-spawn runtime exceptions remain in gang respawn and vendor nitrous systems
- Next action: User launches EXE and tests the spawned TMAX
- Ownership released: Yes
