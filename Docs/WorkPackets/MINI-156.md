# MINI-156 — spawn Sacat/Franki by the Highland Safehouse; build EXE

```yaml
task_id: MINI-156
title: Move player spawn to the Highland Safehouse; build a Windows EXE with this + MINI-155
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: B
budget:
  claude_time: one small additive scene patch + one build
  external_credits: 0
  stop_condition: scene patched, build succeeds, headless smoke clean
reserved_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini156FindSafehouses.cs
  - Assets/UpIzUpMini/Editor/Mini156MoveSpawnToSafehouse.cs
  - Docs/WorkPackets/MINI-156.md
  - Docs/Systems/MapGeneration.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - Builds/GrandBayProof/
protected_files:
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs (not touched - not a scene regeneration)
depends_on:
  - MINI-155 (arrow-key camera look, already landed, included in this build)
```

## Intent

User: "put me and the other character to spawn by the safe house." Three
safehouses exist in the live scene (Highland Safehouse - free/starting,
Lalay House and Lalay Estate - both purchasable); asked which, user
confirmed Highland.

## Investigation

`Mini156FindSafehouses.cs` listed every live `SafehouseInteractable` and
both characters' actual current positions (`Logs/MINI-156-find-safehouses.log`):

| Object | Position | spawnPoint |
|---|---|---|
| Lalay Estate | (22.20, 4.90, -171.18) | (22.20, 4.23, -171.18) |
| Lalay House | (46.76, 7.24, -144.57) | (46.76, 6.84, -144.57) |
| Highland Safehouse | (124.97, 5.59, -112.55) | (124.97, 5.14, -112.55) |
| Sacat (before) | (-7.76, 8.46, -152.10) | - |
| Franki (before) | (50.20, 7.30, -160.21) | - |

Neither character was actually sitting at any safehouse's spawn point
before this change - both were somewhere out in the Lalay area.

## Implementation

`Mini156MoveSpawnToSafehouse.cs`: opened the live scene, read
`FarmSafehouse_Rest`'s own `spawnPoint` SerializedProperty (already the
correct terrain-sampled point - the same one `Mini011PhaseBSetup.cs`'s
generator would compute), wrote `Sacat.transform.position = spawnPoint`
and `Franki.transform.position = spawnPoint + (1.4, 0, -1.2)` (matching
the original generator's relative offset between the two characters so
they don't overlap), saved. This is a small additive live-scene patch
(two `Transform.position` writes) - not a `BuildScene()` rerun, per this
project's own established pattern for touching the already-built scene
(see `Docs/Systems/MapGeneration.md`'s architecture notes on
`Mini119AddMoreVillagers.cs` etc.).

Result: Sacat (124.97, 5.14, -112.55), Franki (126.37, 5.14, -113.75) -
both now exactly at/beside the Highland Safehouse.

Then built the Windows player (`Mini001Build.BuildWindowsPlayer`), which
picks up both this change and the already-landed MINI-155 arrow-key
camera look in the same EXE.

## Acceptance scorecard

- [x] Functional behavior (positions written and logged)
- [x] Static visual evidence - N/A for a transform-only change (positions
      logged numerically; visual confirmation is the user playing it)
- [ ] Motion/feel evidence - user needs to actually load in and see both
      characters standing at the Highland Safehouse
- [x] Save/load or regression check - scene diff confirmed to be the same
      byte size (binary), i.e. no structural scene change beyond the two
      transforms
- [x] Build - Windows player built clean, 398,098,421 bytes
- [x] Headless smoke - 15s run, zero exception/error/fatal log matches

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Safehouse positions/spawn points found | Unity batch-mode scan | `Logs/MINI-156-find-safehouses.log` | No |
| Spawn moved to Highland Safehouse | Unity batch-mode scene patch | `Logs/MINI-156-move-spawn.log` (`MINI156MOVE_PASS` line with before/after positions) | No (numeric) / Yes (visual confirmation) |
| Scene diff is minimal | `git diff --stat` | same byte size, single binary file | No |
| Build succeeds | Unity batch build | `Logs/MINI-156-build.log` (`MINI-001 BUILD SUCCEEDED`), `Builds/GrandBayProof/UpIzUpMini.exe` (398,098,421 bytes) | No |
| Startup doesn't crash | 15s headless run of the built EXE | `Logs/MINI-156-smoke.log`, zero exception/error/fatal matches | No |
| Actual spawn location, arrow-key feel | - | - | Yes - user playtest |

## Handoff

- Files changed: `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (two character transforms), new `Mini156FindSafehouses.cs`/`Mini156MoveSpawnToSafehouse.cs` editor tools, `Builds/GrandBayProof/UpIzUpMini.exe` rebuilt.
- Decisions made: Highland Safehouse (free/starting) chosen by direct user answer over the two purchasable alternatives.
- Visual locks added/changed: none - spawn position is not a recorded visual-approval item.
- Known limitations: not hands-on played; exact facing direction/ground clearance at spawn not manually verified beyond the terrain-sampled Y already baked into the safehouse's own `spawnPoint`.
- Next action: user loads `Builds/GrandBayProof/UpIzUpMini.exe` and confirms both the new spawn location and the MINI-155 arrow-key camera look.
- Ownership released: yes, at the end of this session's pass.
