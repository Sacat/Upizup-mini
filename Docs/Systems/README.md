# System Ledgers — router and workflow

Introduced 2026-08-29 at the user's explicit request: "a modular system that determines which systems should be involved... when I type a prompt it would decipher if its combat or driving mechanics or riding mechanics, or gameplay, dialogue, map building, missions or whatever system(s) it needs to tap into." This replaces "read the entire project history" with "read only what this prompt actually touches."

**Read this file first for any non-trivial task.** It tells you which system file(s) to read next — read only those, not the whole of `PROJECT-HANDOFF.md`. `Docs/CURRENT.md` and `Docs/CLAUDE-HANDOFF-CURRENT.md` remain required reading regardless (they carry the immediate "what's the actual current state, what's next" truth); this router is for everything below that.

## The workflow every prompt goes through

1. **Classify.** Match the prompt's intent against the router table below. A prompt can touch one system or several (e.g., "the police chase animation looks wrong during a car chase" touches Vehicles + NPCs & AI + Combat). If genuinely unclear which system(s) apply, say so and ask rather than guessing.
2. **Read only the matched system file(s)**, plus `Docs/CURRENT.md`/`Docs/CLAUDE-HANDOFF-CURRENT.md`. Each system file's own "What worked / what didn't" section may already answer the question — check it before writing any code.
3. **Decide the approach.** If the system's ledger already has a proven pattern for this kind of request, default to continuing it rather than re-deriving a new approach. If there's a genuine choice to make (a new technique, a new asset, a design direction) and it's not obvious, do a bounded investigation first (the "reproduce/measure before fixing" discipline already standard on this project - see any `Mini1*.cs` diagnostic tool for the pattern), then either proceed with the clearly better option or ask the user via a real question if it's still genuinely open.
4. **Implement scoped to the identified system(s) only.** Don't drift into unrelated systems in the same pass - if the work reveals a second system needs a change too, say so explicitly rather than silently expanding scope.
5. **Update logs before finishing:**
   - Add/update the relevant entry in the matched system file(s) - architecture changes, what worked, what didn't, new open items.
   - Add a one-line pointer in `PROJECT-HANDOFF.md` (date, system(s), one sentence, link to the system file). `PROJECT-HANDOFF.md` is now an INDEX, not the place detail lives - keep entries there short.
   - If the change affects what a fresh agent needs to know to pick this project up cold, update `Docs/NEXT-CHATGPT-PLUS-HANDOFF.md` too (see "Handover" below).

## Router table

| If the prompt mentions... | Read |
|---|---|
| punching, hitting, combos, damage, hit detection, ragdoll-on-hit, NPC health/knockdown | `Combat.md` |
| bikes, cars, mounting, riding, wheelie, driving, pillion, vehicle purchase/spawn | `Vehicles.md` |
| roads, terrain, zones, districts, building/NPC placement, the world builder, Lalay/Highland | `MapGeneration.md` |
| quests, objectives, story beats, mission markers, progression gates | `Missions.md` |
| conversations, dialect/patois lines, dialogue panels, NPC lines | `Dialogue.md` |
| money, shops, crops, farming, inventory, purchases, strains | `Economy.md` |
| rigging, LODs, wardrobe, character models, animation import/retargeting | `Characters.md` |
| patrol behaviour, police/gang AI, heat, faction reputation, NPC pathing | `NPCsAndAI.md` |
| Editor tools, batch verification, builds, the diagnostic-tool pattern itself | `BuildAndVerification.md` |
| "what's the current state", "what's next", session handoff to a fresh agent | this file, then `../NEXT-CHATGPT-PLUS-HANDOFF.md` |

If a system doesn't exist yet as a file below, create it using the template - don't let a real system stay undocumented because it wasn't on the original list.

## Per-system file template

Copy this structure for any new system file:

```markdown
# <System Name>

## Current state
What's shipped, what's data-driven vs. still hardcoded, as of the last update.

## Architecture
The core classes/data tables/scripts. Enough that an agent can jump straight
to the code without searching the whole codebase.

## What worked / what didn't
A running ledger of dead ends and proven approaches, so a failed technique
is never re-tried blind. Newest entries at the top.

## Open items
Bugs, TODOs, and known gaps specific to this system.

## Key files
Exact paths.
```

## Handover (Claude <-> Codex/ChatGPT)

`Docs/NEXT-CHATGPT-PLUS-HANDOFF.md` is the copy-paste prompt for a fresh agent. It should point here first, then name the specific system file(s) the next task needs - not re-explain the whole project inline. Keep it short; the system files carry the detail.

## Systems currently tracked

- [Combat](Combat.md)
- [Vehicles](Vehicles.md)
- [Map Generation](MapGeneration.md)
- [Missions](Missions.md)
- [Dialogue](Dialogue.md)
- [Economy](Economy.md)
- [Characters](Characters.md)
- [NPCs & AI](NPCsAndAI.md)
- [Build & Verification](BuildAndVerification.md)
