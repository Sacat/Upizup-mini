# Copy-paste startup prompt for Claude Work / Claude Code

Copy everything inside the block into Claude. The project documentation contains the detail so Claude does not have to rediscover the game.

```text
Continue development of Up Iz Up Mini in the existing Unity project:

E:\Unity\Up Iz Up Mini

Do not create a new project. E:\Unity\Up iz up and E:\Assets are read-only reference sources. Preserve all unrelated dirty files and all user-approved manual character/accessory/placement work. Never reset, clean or bulk-copy either project.

Before changing files, read these completely in order:

1. E:\Unity\Up Iz Up Mini\.agents\skills\upizup-mini-production\SKILL.md
2. E:\Unity\Up Iz Up Mini\AGENTS.md
3. E:\Unity\Up Iz Up Mini\Docs\CURRENT.md
4. E:\Unity\Up Iz Up Mini\Docs\CLAUDE-HANDOFF-CURRENT.md
5. E:\Unity\Up Iz Up Mini\Docs\AI-PRODUCTION-WORKFLOW.md
6. E:\Unity\Up Iz Up Mini\Docs\WorkPackets\MINI-109.md
7. The Current claim plus MINI-108/MINI-109 sections in E:\Unity\Up Iz Up Mini\PROJECT-HANDOFF.md
8. Relevant sections of TASKS.md, Docs\STORY.md, Docs\GAME-DESIGN.md and Docs\DIALECT-LEXICON.md

Read Docs\WORLD-EXPANSION-WORKFLOW.md before any map/world edit and Docs\CHARACTER-PRODUCTION-WORKFLOW.md before any character/asset/rig/wardrobe edit.

Take over only MINI-109. Confirm Current owner is None, run:

& .\Tools\AIWorkflow\Invoke-Preflight.ps1 -Agent Claude -TaskId MINI-109

Then claim MINI-109 and reserve exact files before editing.

MINI-109 is a bounded mission-correctness pass. First reproduce, then fix:

- Clean Face can remain stuck at Normy. One valid mission payment must complete the Normy objective immediately and must not double-charge. Preserve Normy's reusable ambient $100/20%-heat service separately.
- Delivering harvested Black Sugar to Boss J sometimes does not advance. Validate the real black_sugar inventory, BossK compatibility target ID, transaction and mission notification together; remove/credit the required crop exactly once.
- The Rasta Ital mission uses his old location. Resolve the marker from Rasta's current generated placement without moving him.
- Establish a consistent retrospective-completion/progression-lock rule so the player is not forced to repeat a purchase or action the game can prove was already completed, while story-gated contacts/items remain locked.

Create a focused MINI-109 validator. Rebuild GrandBayProof only after the compound changes through UpIzUpMini.EditorTools.Mini011PhaseBSetup.BuildScene. Run compile, focused validation, standing mission/map regressions, a Windows build and built-player smoke. Capture fixed screenshots showing the active Normy, Black Sugar/Boss J and Rasta objectives on the minimap. Put evidence under Logs\Tasks\MINI-109 and show the user the important screenshots.

The canonical scene is generated. Never hand-edit GrandBayProof.unity as the source of truth. Preserve internal save IDs BossK, black_sugar, MontineFarm and land_montine; player-facing text says Boss J and Highland. Do not touch the temporary banana, paused MINI-107 character LODs, Sacat/Boss C chains, approved Lalay/Highland topology, render pipeline or input package.

Do not start the later roadmap in the same pass. Docs\CLAUDE-HANDOFF-CURRENT.md records the approved next sequence in detail: Normy favour and Boat Man/courier timer; Rasta strain ladder and hybrids; Dog Life escalation/respawn/roaming; road/hedge/vehicle-marker repair; Brakes blessing missions; cellphone unlock; TMAX visual/mobile optimization. Stop after MINI-109, update the current documentation, release ownership and ask the user to playtest its four acceptance cases.

Use live Unity/Blender control and screenshots when necessary, but do not purchase, upload, publish, install broad packages or spend Hitem3D credits without explicit user approval. A compile proves code only; movement and appearance require screenshots/video and user acceptance.
```
