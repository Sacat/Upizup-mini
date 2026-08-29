# Dialogue

## Current state

A data-driven foundation (MINI-053) - `DialogueCondition`/`DialogueLine`/`DialogueSet` - supports conditional-line selection and tie-cycling (re-evaluating live rather than freezing on the first match). Dialect vocabulary is catalogued in `Docs/DIALECT-LEXICON.md` with honest per-term confidence levels; unconfirmed terms stay unused rather than guessed. The opening dialogue has a dynamically sized transparent-black background and reading-time-scaled hold durations (MINI-108).

## Architecture

- Data classes: `DialogueCondition`, `DialogueLine`, `DialogueSet` (check `Assets/UpIzUpMini/Scripts/Dialogue/` for exact files) - conditions gate which line plays, ties between equally-valid lines cycle rather than always picking the same one.
- UI: a transparent black panel sized to the actual text, not a fixed box - avoids truncation, which was previously misdiagnosed as a hold-duration problem when it was actually a background-panel height cap (MINI-119 post-playtest fix).
- Dialect: `Docs/DIALECT-LEXICON.md` - every supplied term gets a confidence rating; do not invent slang not already confirmed there.

## What worked / what didn't

- **(2026-08-post-119-playtest) Dialogue truncation was NOT a hold-duration bug** - the user reported dialogue cutting off, and the first instinct (extend how long a line stays on screen) would have been the wrong fix. Root cause was the background panel's own fixed height clipping longer lines. Lesson: reproduce the actual visual symptom before assuming which subsystem (timing vs. layout) owns it.
- **(historical) A repeated line reading identically every time** (Boat Man's "nothing to load" line) was fixed by rotating between variants instead of always showing the same one - a small but real "feels repetitive" complaint, same class of polish issue worth watching for elsewhere.

## Open items

- None specifically logged as of this system file's creation (2026-08-29) - check `PROJECT-HANDOFF.md`'s index for anything more recent than the MINI-119 batch fix entry.

## Key files

- `Assets/UpIzUpMini/Scripts/Dialogue/` (DialogueCondition/DialogueLine/DialogueSet and the panel controller)
- `Docs/DIALECT-LEXICON.md`
- `Docs/STORY.md`, `Docs/GAME-DESIGN.md`
