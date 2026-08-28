# Copy-paste startup prompt for Claude Work / Claude Code / Codex

Copy everything inside the block into the next agent. The project documentation contains the detail so it does not have to rediscover the game.

```text
Continue development of Up Iz Up Mini in the existing Unity project:

E:\Unity\Up Iz Up Mini

Do not create a new project. E:\Unity\Up iz up and E:\Assets are read-only reference sources. Preserve all unrelated dirty files and all user-approved manual character/accessory/placement work. Never reset, clean or bulk-copy either project.

Before changing files, read these completely in order:

1. E:\Unity\Up Iz Up Mini\.agents\skills\upizup-mini-production\SKILL.md
2. E:\Unity\Up Iz Up Mini\AGENTS.md
3. E:\Unity\Up Iz Up Mini\Docs\CURRENT.md (top entry has the actual current state as of 2026-08-28)
4. E:\Unity\Up Iz Up Mini\Docs\CLAUDE-HANDOFF-CURRENT.md, specifically section 0 (the rest of that file's sections 5/6/10 describe a DONE, superseded MINI-109-116 sequence - do not act on those, section 0 explains why)
5. E:\Unity\Up Iz Up Mini\Docs\AI-PRODUCTION-WORKFLOW.md
6. E:\Unity\Up Iz Up Mini\Docs\VEHICLE-INTEGRATION-WORKFLOW.md if touching any vehicle
7. The Current claim block plus the most recent MINI-119 follow-up chain entry in E:\Unity\Up Iz Up Mini\PROJECT-HANDOFF.md
8. Relevant sections of TASKS.md, Docs\STORY.md, Docs\GAME-DESIGN.md and Docs\DIALECT-LEXICON.md

Read Docs\WORLD-EXPANSION-WORKFLOW.md before any map/world edit and Docs\CHARACTER-PRODUCTION-WORKFLOW.md before any character/asset/rig/wardrobe edit.

Everything through MINI-109 up to and including a long MINI-119 follow-up chain is DONE: SuperMoto riding/wheelie/pillion (user-confirmed via real exe play), NPC ragdoll-on-hit (Police/Villager/Gang), a Koss bike purchasable at the Car Dealer for $2,500, expanded Lalay/Highland villager population, a corrected Lalay/Highland area-name zone boundary, a farm-hedge containment fix, a wheelie back-clip damping fix, and auto-mount turned back off for handoff. Full detail: git log --grep=MINI-119, or the matching entry in PROJECT-HANDOFF.md.

The user has NOT yet hands-on playtested this session's full follow-up chain (only gave live direction mid-session, e.g. correcting the zone boundary three times, choosing the wheelie-damping approach). Confirm with the user whether that playtest has happened before starting new work. If it hasn't, ask them to test the current build (Builds\GrandBayProof\UpIzUpMini.exe) first rather than starting the next task blind.

The next task, once that playtest is confirmed clean, is a COMBAT BUG-FIX PASS - not new content, not shooting yet. The user's own words: "Can we start working on the combat mechanics maybe fighting first and then shooting after?" then, asked to choose between fixing existing bugs vs adding combos, chose fixing existing bugs first: the warped punch pose and over-generous hit detection (both previously reported by the user, referenced in Docs\CLAUDE-HANDOFF-CURRENT.md section 4 as "Combat animation quality is still placeholder"). This is a bounded mission-correctness-style pass. First reproduce both bugs (do not guess at a fix from the code alone - every real fix in the MINI-119 chain above started from a batch-mode or user-reported reproduction), then fix, then re-verify the same way.

The user is separately deciding whether to buy a paid or free fighting/boxing animation pack to replace the current placeholder clip. A paid, real-mocap unarmed/boxing pack was recommended over free (free packs are the most common source of exactly this warped-pose symptom) - do not spend any money/credits on this without the user's own explicit pick and approval; if they haven't decided yet, ask before assuming a specific asset.

Use live Unity/Blender control and screenshots when necessary, but do not purchase, upload, publish, install broad packages or spend Hitem3D/asset-store credits without explicit user approval. A compile proves code only; movement, hit-detection feel and appearance require screenshots/video and user acceptance. Follow this project's established ownership protocol (claim a task in PROJECT-HANDOFF.md's Current claim block, reserve exact files, release on completion) and its "one compound change, then playtest" discipline - do not batch the combat fix pass together with shooting mechanics or any other unrelated task.
```
