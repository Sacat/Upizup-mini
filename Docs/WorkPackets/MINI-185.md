# MINI-185 — Get Your Tool mission

```yaml
task_id: MINI-185
title: Introduce the gun as a "tool" through a Lalay story mission
request_owner: User
integrator: Codex
status: evidence_ready; awaiting hands-on UI/story playtest
approval_class: B
external_credits: 0
stop_condition: M12 transitions into a playable tool-acquisition mission, with old save indexes migrated, checks and Windows build
reserved_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scripts/SaveLoadSystem.cs
  - Assets/UpIzUpMini/Scripts/Combat/FirearmController.cs
  - Assets/UpIzUpMini/Data/Shop/Mini183/LalaySidearm.asset
  - Assets/UpIzUpMini/Editor/Mini183FirearmSetup.cs
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Docs/STORY.md
  - Assets/UpIzUpMini/Editor/Mini185*.cs
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
  - Assets/UpIzUpMini/Art/Environment/Mini182/
```

## Intent

After M12, Sacat and Franki meet the Lalay trader, buy their "tool" (the local in-game word for a gun), and learn to fire one test shot. Existing saves must stay on the same story mission after the insertion.

## Acceptance

- [x] Mission order M12 -> M12T -> M13, with trader waypoint and correct objectives.
- [x] Buying the tool completes its objective; prior ownership gets retrospective credit.
- [x] A real fired round sends the FireTool event.
- [x] Old save indexes migrate, and players past M12 without a tool take this mission once before resuming their previous mission.
- [x] Unity compile, focused validation, static scene check and Windows build pass.
- [ ] Player-controlled mission card, trader interaction, and test-shot flow visual playtest.

## Implementation and evidence (2026-09-29)

M12T "Get Your Tool" follows M12 in the canonical GrandBayProof scene and the source mission builder. The black-market NPC at its real scene position is the first waypoint. The objectives are TalkTo `BlackMarket`, BuyItem `lalay_sidearm`, then FireTool (one successful shot, with the shop closed and aim active). The shop and HUD display "Tool"; IDs remain stable. The $750 shop price remains, and the briefing tells players to earn money if needed. Mission completion pays $100.

New saves store a stable mission ID and schema version. Old index-only saves map to the correct original story mission. If a player has passed M12 and does not own the tool, loading queues M12T and stores the previous mission/objective progress; completing M12T resumes it. The deferred return state also survives a new save made midway through M12T. Already-owned tools get retrospective BuyItem credit. Old saves that own the tool keep their mission position. The completed-story sentinel returns to all-complete after the tool mission.

Unity 6000.3.10f1 batch evidence, all under `Logs/Tasks/MINI-185/`:

- `compile.log` and `resume-compile.log`: exit 0, no C# errors.
- `apply.log`: targeted scene patch passed; the historical whole-world builder was not run.
- `verify.log`: M12/M12T/M13 order, trader marker, objectives, migration, label pass.
- `exercise.log` and `resume-exercise.log`: talk, purchase, test-shot event, M13 transition, early ownership, legacy index and deferred mission restoration pass.
- `scene-validation.log`: GrandBayProof static regression pass.
- `final-build2.log`: Windows build succeeded; `Builds/GrandBayProof/UpIzUpMini_Data/level0` updated 2026-09-29 20:35 local.

Backup before patch: `Backups/MINI-185-pre-tool-mission-2026-09-29/GrandBayProof.unity` (SHA-256 `6FF8AF7822B062BB7EE4B09023C5DA64E150C9233E02E58DC45D140AA4995278`). Protected HouseEnhance scene SHA-256 remains `CF0E4E8A06140F3DA0B65989F9C791D77E80590E9A4F9CF748E5DC242ED07CF0`. Existing dirty scene/project files predate this task; no mixed-history Git checkpoint was made.
