# MINI-086 — Progression truth, dialogue, follower, combat, HUD, and equipment corrections

```yaml
task_id: MINI-086
title: Progression truth, dialogue, follower, combat, HUD, and equipment corrections
request_owner: User
integrator: Codex
  status: evidence_ready_waiting_for_user_chain_placement_and_playtest
approval_class: B
budget:
  codex_time: bounded implementation and verification pass over existing systems
  claude_time: 0
  external_credits: 0
  stop_condition: stop before new map construction, new paid/generated assets, shooting, or a combat-animation redesign
reserved_files:
  - Assets/UpIzUpMini/Scripts/**
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini086*.cs
  - Assets/UpIzUpMini/Data/Dialogue/**
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Docs/WorkPackets/MINI-086.md
  - Docs/DIALECT-LEXICON.md
  - Docs/CURRENT.md
  - TASKS.md
  - CHANGELOG.md
  - PROJECT-HANDOFF.md
protected_files:
  - user-approved character identities and existing manual accessory placements
  - vehicle models and rider tuning
  - map layout outside the requested NPC placement correction
depends_on:
  - MINI-085
```

## Intent

Make the current chapter communicate and enforce its rules consistently: show the active hero and reputation, provide contextual hints and first-meeting dialogue, hide locked content, gate recruitment/jobs/purchases, fix Boss J and cooldown mission truth, improve companion/AI collision behavior, make gang damage/defeat/retreat readable, keep purchases character-specific, and prepare the chain for a user-controlled scene placement.

## Non-goals

- No new Grand Bay map, paid assets, Hitem3D generation, shooting, new combat animation set, or broad mobile/input migration.
- Do not guess the final chain coordinates; the user will place it visually and that placement will then be saved and locked.
- Do not claim movement/combat feel is final without live evidence.

## Acceptance scorecard

- [x] Active hero name and reputation percent are wired into the HUD.
- [x] Camera now sphere-casts against roofs/walls and pulls in before an obstruction.
- [x] NPC dialogue is role-specific, supports first meeting, uses `Paro`, and purchase responses match item category.
- [x] Initial hints teach cloning and carrying-weed/seeds heat without exposing locked content.
- [x] Jobs, recruitment, gang establishment, vehicles, land, and later plots respect mission/reputation unlocks.
- [x] Boss J prioritizes held Bushers hand-in and cool-down accepts zero heat at the lay-low marker.
- [x] Followers have local obstacle/ledge checks; hero collision is ignored; bike exit restores only the active rider.
- [x] Dog Life defeat/despawn, last-rival retreat, and encounter respawn are wired.
- [x] Apparel ownership/equipping is per character and migrates old saves.
- [x] Land & Surveys NPC stands in front and objective text names the building/placement clearly.
- [ ] User visually places Sacat's selected chain and approves its transform; Codex then captures it to the placement profile.
- [x] Unity compile, scene rebuild, static wiring validation, gang validation, and Windows build pass.

## Evidence

- Compile: `Logs/MINI-086-Compile-2.log` — clean exit.
- Scene rebuild: `Logs/MINI-086-Rebuild.log` — GrandBayProof rebuilt and saved.
- Static scene: `Logs/MINI-086-StaticValidation.log` — PASS.
- Gang/control regression: `Logs/MINI-086-GangValidation.log` — PASS; runtime fight feel still requires the user's playtest.
- Windows build: `Logs/MINI-086-WindowsBuild.log` — SUCCEEDED, 407,519,547 bytes.
- Manual chain workspace: Unity is open on `GrandBayProof`, `MANUAL_CHAIN_PREVIEW_SACAT` selected and framed with the Move tool.

## Implementation order

1. Audit existing state/progression/data paths and write focused regression assertions.
2. Fix HUD, camera, hints, dialogue roles, mission completion truth, and progression gates.
3. Fix per-character purchases/equipment and chain visibility plumbing without choosing final placement.
4. Improve companion/gang collision, obstacle recovery, damage, defeat, retreat, and respawn using existing systems.
5. Rebuild and validate; inspect the live opening and reachable systems.
6. Open Unity with the chain selected for the user's manual placement; record and lock the approved transform afterward.

Rollback point: commits `b367fc5` and `44949fa` on `codex/mini-085-baseline-20260820`.
