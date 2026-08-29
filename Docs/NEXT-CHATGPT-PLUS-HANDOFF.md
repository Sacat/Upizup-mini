# Copy-paste startup prompt for Claude Work / Claude Code / Codex

Copy everything inside the block into the next agent. The project documentation contains the detail so it does not have to rediscover the game.

```text
Continue development of Up Iz Up Mini in the existing Unity project:

E:\Unity\Up Iz Up Mini

Do not create a new project. E:\Unity\Up iz up and E:\Assets are read-only reference sources. Preserve all unrelated dirty files and all user-approved manual character/accessory/placement work. Never reset, clean or bulk-copy either project.

Before changing files, read these completely in order:

1. E:\Unity\Up Iz Up Mini\.agents\skills\upizup-mini-production\SKILL.md
2. E:\Unity\Up Iz Up Mini\AGENTS.md
3. E:\Unity\Up Iz Up Mini\Docs\CURRENT.md (top entry has the actual current state)
4. E:\Unity\Up Iz Up Mini\Docs\CLAUDE-HANDOFF-CURRENT.md, section 0
5. E:\Unity\Up Iz Up Mini\Docs\Systems\README.md - THIS IS THE ROUTER. Read it, classify which system(s) your actual task touches (combat, vehicles, map generation, missions, dialogue, economy, characters, NPCs/AI, or the build/verification tooling itself), then read ONLY those system file(s) under Docs\Systems\ - not the whole of PROJECT-HANDOFF.md. Each system file has its own "what worked / what didn't" ledger - check it before writing any code, so a dead end isn't re-tried blind.
6. The Current claim block in E:\Unity\Up Iz Up Mini\PROJECT-HANDOFF.md (now an INDEX of one-line pointers into the system files, not the detail itself)
7. Relevant sections of TASKS.md, Docs\STORY.md, Docs\GAME-DESIGN.md and Docs\DIALECT-LEXICON.md if the task touches Missions or Dialogue

Read Docs\WORLD-EXPANSION-WORKFLOW.md before any map/world edit (Map Generation system) and Docs\CHARACTER-PRODUCTION-WORKFLOW.md before any character/asset/rig/wardrobe edit (Characters system) - these stay mandatory in full even with the system-ledger router above, which points at them but does not replace them.

Follow this project's per-prompt workflow (also written out in Docs\Systems\README.md): classify which system(s) the task touches -> read only those system files -> if the system's own ledger already has a proven approach for this kind of request, default to continuing it -> if genuinely undetermined, do a bounded measure/reproduce step first (see Docs\Systems\BuildAndVerification.md for how to check things without a human driving the Editor) and either proceed with the clearly better option or ask the user -> implement scoped to the identified system(s) only, do not drift into unrelated systems in the same pass -> before finishing, update the matched system file(s)' own ledger AND add a one-line pointer entry in PROJECT-HANDOFF.md.

Use live Unity/Blender control and screenshots when necessary, but do not purchase, upload, publish, install broad packages or spend Hitem3D/asset-store credits without explicit user approval. A compile proves code only; movement, hit-detection feel and appearance require screenshots/video and user acceptance - see Docs\Systems\BuildAndVerification.md for exactly why batch-mode checks alone are not sufficient sign-off on this project.
```
