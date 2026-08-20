# Dialect Lexicon — MINI-054

Source-of-truth term list supplied directly by the user for this task.
Companion to `Docs/DIALOGUE-REFERENCE.md` (which is sourced from a real
WhatsApp conversation and already verified against actual usage) — this
file covers the newer, narrower list of terms the roadmap brief named
specifically, several of which are **not yet independently confirmed** by
a usage example the way the WhatsApp-sourced terms are.

**Honesty note, not a formality:** Dominican/Eastern Caribbean Creole
vocabulary is a real living language, not a stereotype to approximate. For
terms below where I do not have a confirmed usage example, I say so
plainly and mark confidence as **unconfirmed** rather than guess a
definition and risk misrepresenting it. Only terms marked **confirmed**
or **inferred (high confidence)** were used in the MINI-054 opening
conversation script; unconfirmed terms are catalogued for the user to
correct/confirm before they're put in a character's mouth.

## Terms

| Term | Meaning | Confidence |
|---|---|---|
| `Zeb` | Weed / cannabis. Already load-bearing in-game (`MINI-051`'s "Zeb strain" flower/bud visuals, this task's opening conversation). | Confirmed (already in active use as the crop-category name) |
| `Zion` | Used alongside Zeb ("one suggests Zion/Zeb") — a Rastafari-derived term for a promised/ideal place, used here as an informal name for the weed trade/scene rather than a literal location. Fits `MINI-056`'s later Rasta mentor content. | Inferred (high confidence) |
| `Domnicah` | "Dominica" in local pronunciation/spelling. | Inferred (high confidence) |
| `Gwa Bay` | "Grand Bay" — already the established in-world village name (see `Docs/MAP-STRATEGY.md`). | Confirmed (matches existing world-building) |
| `Gwada` | "Guadeloupe" — informal short form, consistent with the existing Guadeloupe sea-trade abstraction (`DECISIONS.md` D-007). | Inferred (high confidence) |
| `diah` | "there" / "that way" (di + ah). | Inferred (high confidence) |
| `awa` | "our". | Inferred (moderate confidence) |
| `awa wii` | An expression of disbelief - "no way" / "unbelievable" / "impossible" - not a possessive. **Confirmed directly by the user (2026-08-17):** "Awa wii is just a slang when you cannot believe it. Like impossible." Not related to `awa` or `wii` individually the way the earlier guess assumed - correcting the earlier "our own" inference below, which was wrong. | Confirmed (user) |
| `wah is di word` / `what is di word diah` | A greeting — "what's going on" / "what's the news over there." | Inferred (moderate confidence) |
| `Not Ah Word` | Likely the source phrase behind the player gang name from `MINI-058` ("say nothing" / keep quiet — a loyalty-and-secrecy ethos fitting a gang name). Flagging the connection since it isn't spelled out in the brief. | Inferred (moderate confidence) — **worth the user confirming the gang-name connection specifically** |
| `go up` | Likely a directional/idiomatic phrase ("go up the road", or "move up" in status/opportunity) — genuinely unsure which sense, possibly both depending on context. | **Unconfirmed** |
| `scrub` | Could mean unwanted/low-status person (common English slang) or literal scrubland/bush terrain (matches the game's "dirty path"/farm-clearing scrub). | **Unconfirmed** |
| `fadah` | "father", patois pronunciation. | Inferred (high confidence) |
| `gasah` | Not confident of the meaning — possibly gas/gasoline slang, possibly something else entirely. | **Unconfirmed** |
| `fresh` | Likely a general positive descriptor ("good", "sharp-looking", possibly crop-quality-adjacent) — not confident enough of the exact register to commit it to a specific line yet. | **Unconfirmed** |
| `volehing` / `voleh` | "Stealing" — matches the brief's own usage directly ("`MINI-059`: at low reputation, unattended Zeb can be volehed"), which resembles French *voler* (to steal), consistent with Kwéyòl's French-derived vocabulary. | Inferred (high confidence, directly evidenced by the brief's own sentence) |
| `Awtic` | No usage example or context clue available in anything supplied so far. | **Unconfirmed — flagging for the user rather than guessing** |

## Rasta speech style

Per the brief: "Rasta uses a Jamaican speech style." No Rasta character
exists in the game yet (the mentor is `MINI-056` scope). Noted here so the
convention is on record before that task starts:

- Jamaican Patois markers distinct from the Dominican/Gwa Bay speech used
  elsewhere in this game: `I and I` (self-reference), `Iyah`/`Idren`
  (brethren/friend), `seen` (understood/agreed), `nuh true?` (isn't that
  right?), dropped `th` sounds (`di`/`dis`/`dat`, already shared with the
  Dominican register so this isn't a hard switch, more a different accent
  weighting), Rastafari vocabulary (`Zion`, `Babylon`, `livity`).
- Must read as a **distinct voice** from the Dominican/Gwa Bay characters,
  not the same slang with a different accent painted on - that's the
  actual design ask, not just "add Jamaican words."
- **Written and in-game as of `MINI-056`**: `NPC_RastaMentor`
  (`Assets/UpIzUpMini/Data/Dialogue/RastaMentorLines.asset`, built by
  `Mini011PhaseBSetup.BuildRastaMentor`) uses `I an' I`, `Iyah`, `bredrin`,
  `seen`, `nuh true?`, `Zion`, and dropped-`th` `di`/`dis`/`dat` -
  deliberately none of the Dominican register's own markers (`mn`/`nuh`/
  `wii`/`allu`) so the two read as different people, not the same slang
  repainted.

## Usage rule (same as DIALOGUE-REFERENCE.md)

Sparingly - a line or two per NPC, not every sentence. Don't use an
**Unconfirmed** term in shipped dialogue until the user confirms its
meaning; using a real Creole word incorrectly is worse than not using it
at all.

## Currently used in-game (this task)

`Assets/UpIzUpMini/Scripts/Dialogue/OpeningConversationController.cs`
(`MINI-054`'s opening conversation, see `PROJECT-HANDOFF.md`): uses `Zeb`,
`Zion`, and the already-confirmed `mn`/`nuh`/`wii` register from
`Docs/DIALOGUE-REFERENCE.md`. Deliberately does not use any term marked
**Unconfirmed** above.

## Purchase and service reactions

These short reactions were approved by the user as the intended in-game
register. They are category-specific; they must not be reused by unrelated
shops or NPC roles.

| Situation | Player reaction |
|---|---|
| Clothes, shoes, cap, chain or watch | `Yah, I looking more fresh now.` |
| Vehicle or vehicle upgrade | `Yah, I can move better now.` |
| Food | `Yah, I can put something in my stomach now.` |
| Land or property | `Yah, I can do something for myself now.` |

`Paro` is the player-facing term for the rough-sleeping low-price weed buyer.
His dialogue should sound like one particular struggling person, not a generic
shopkeeper and not a caricature. The internal mission target remains `Vagrant`
for old-save compatibility, but that label must never be displayed to players.
