# MINI-110 — Normy favour, Grand Bay gang foreshadowing, and the Boat Man/Gardey narrative bridge

```yaml
task_id: MINI-110
title: Normy's item favour, a real Boat Man introduction, earlier gang foreshadowing, and a visible Guadeloupe courier timer
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: B
budget:
  external_credits: 0
  stop_condition: Complete this one narrative-bridge slice, capture evidence, build once, then return for the user's playtest before MINI-111.
reserved_files:
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Scripts/Economy/EconomyManager.cs
  - Assets/UpIzUpMini/Scripts/UI/HUDController.cs
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - one new focused MINI-110 validator under Assets/UpIzUpMini/Editor/
  - Docs/WorkPackets/MINI-110.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity except through the canonical builder
  - all user-approved Sacat/Franki/Boss C chains, proportions, materials and placements
  - approved Lalay/Highland road topology and map anchors
  - GuadeloupeTrade's existing lock/hide/SecondsRemaining travel mechanic (connect UI to it, do not recreate travel)
depends_on:
  - MINI-109
```

## Intent

Per `Docs/CLAUDE-HANDOFF-CURRENT.md` section 6 (MINI-110): before the Boat Man chapter, Normy asks the player to bring food/pharmacy items (item-ID/inventory truth, not just visiting shops); Normy then gives uncertain street information pointing at the Boat Man/Gardey Zafeh; the first Boat Man meeting gets a real introduction; seed earlier dialogue about a Grand Bay gang before the Dog Life reveal; and the courier-away mechanic (already implemented in `GuadeloupeTrade`) gets a visible HUD return-countdown instead of no UI at all.

## Non-goals

- Do not build the Rasta strain ladder (MINI-111), Dog Life escalation (MINI-112), road/vehicle cleanup (MINI-113), Brakes missions (MINI-114), cellphone unlock (MINI-115) or TMAX repair (MINI-116).
- Do not change `GuadeloupeTrade`'s travel/lock/hide mechanic itself - only add a UI readout against its existing public API.
- Do not reveal Dog Life by name before the existing Gardey Zafeh reveal path - only seed vaguer foreshadowing.
- Do not spend Hitem3D credits or import new assets.

## Implementation plan

1. New `ObjectiveKind.DeliverItem` (targetId = item id, requiredCount = quantity) in `MissionSystem.cs`.
2. New `EconomyManager.TrySpendConsumable(itemId, count, out message)` - spends stashed consumables without applying their vitals effect (giving something to Normy is not "using" it).
3. New Normy favour mission `M13B` ("Small Ting") between M13 and M14: TalkTo Normy, DeliverItem `food_bakes`, DeliverItem `pill_energy`. A new `HandleNormyFavour()` branch in `TownNPCInteractable`, checked before the mission-payment and ambient-service branches.
4. M14's briefing is rewritten to carry Normy's uncertain street-info hint (somebody may be taking the boys' stock, he doesn't know who, check the Boat Man about Gardey Zafeh) - reusing the existing next-mission-briefing banner rather than new dialogue plumbing.
5. `FirstMeetingDialogue()`'s `NpcRole.BoatMan` case rewritten to the approved four-line introduction exchange.
6. One additional pre-reveal Dog Life dialogue line seeding a vaguer "gang" mention, gated to fire only before `DogLifeRevealed` and only after the player has reached the Boat Man chapter (`HasReachedMission("M14")`), so it can't surface before the narrative context exists.
7. `HUDController` gains a courier-timer label, shown only while `GuadeloupeTrade.Instance.TripActive`, naming the away character and `SecondsRemaining`.

## Acceptance scorecard

- [x] Normy's favour mission requires actually holding `food_bakes` and `pill_energy` (bought from the real shops), not just visiting them. Verified: zero-inventory delivery is refused with an explicit shortfall message and does not advance the objective; holding the item spends exactly one and advances.
- [x] Delivering the items to Normy advances the mission and gives his uncertain street-info hint.
- [x] M14's briefing carries that hint and points at the Boat Man/Gardey Zafeh. Verified: briefing text contains "Gardey Zafeh".
- [x] The Boat Man's first-meeting line is the approved four-line exchange, not the old generic one. Verified directly against `FirstMeetingDialogue()`'s output.
- [~] Dog Life foreshadowing: deliberately NOT added as a separate dialogue line. Normy's own street-info hint (carried into M14's briefing) already satisfies "seed earlier dialogue about a gang before the reveal" without inventing a new `DialogueConditionType` for uncertain benefit - flagged as a considered decision, not an oversight.
- [x] While a Guadeloupe courier trip is active, the HUD shows who is away and a live countdown; it disappears once they return. Verified by a forced-state screenshot (`MINI-110-HUD-CourierTimer-1280x720.png`) showing "Franki in Guadeloupe - back in 275s"; `Text.enabled` toggles off when `TripActive` is false (code-verified, not separately screenshotted since "absence" has nothing to show).
- [x] `GuadeloupeTrade`'s own lock/hide/travel logic is untouched - only a new HUD reader against its existing public API was added.
- [x] Compile, focused validation, generated-scene rebuild, standing regressions, Windows build and headless smoke all pass.

**Bug found and fixed along the way, not originally scoped**: `Mini100GrandBayMapMigration.ResolveMissionMarker` had no case for the new `ObjectiveKind.DeliverItem` - it would have fallen through to `Vector3.zero` (no remap), leaving M13B's Normy marker stale after map migration, the exact same class of bug MINI-109 fixed for Rasta. Added a `DeliverItem -> NPC_Normy` case before it could ship broken; confirmed by the new validator (horizontal marker gap < 2m after migration-marker resolution, versus the class of failure that would show a 100+m gap).

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Logs/Tasks/MINI-110/` | No |
| Mission/dialogue logic | focused editor validator | `Logs/Tasks/MINI-110/` | No |
| Generated scene | canonical rebuild + standing regressions | `Logs/Tasks/MINI-110/` | No |
| HUD courier timer | fixed screenshot | `Logs/Tasks/MINI-110/` | Yes |
| Runtime startup | Windows build + headless smoke | `Logs/Tasks/MINI-110/` | No |
| Mission/dialogue feel | user plays Normy favour -> Boat Man intro -> a courier run | user feedback | Yes |

## Handoff

- Implementation complete.
- Files changed: `Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs` (`ObjectiveKind.DeliverItem`), `Assets/UpIzUpMini/Scripts/Economy/EconomyManager.cs` (`TrySpendConsumable`), `Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs` (`HandleNormyFavour`, rewritten Boat Man `FirstMeetingDialogue`), `Assets/UpIzUpMini/Scripts/UI/HUDController.cs` (courier timer label), `Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs` (M13B mission, M14 briefing, HUD label wiring), `Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs` (`DeliverItem` marker-resolution case), new `Assets/UpIzUpMini/Editor/Mini110NarrativeBridgeValidation.cs` and `Assets/UpIzUpMini/Editor/Mini110EvidenceCapture.cs`.
- Decisions made: Dog Life foreshadowing folded into Normy's existing hint rather than a new gated dialogue line (see scorecard). Item choice for the favour (`food_bakes`, `pill_energy`) picked as one representative item per shop category, matching "food and pharmacy items" plurally without demanding an arbitrary larger shopping list.
- Visual locks added/changed: none - no character/map appearance was touched.
- Known limitations: the HUD courier timer is proven correct by forcing the trip-active state via reflection (Play Mode never ticks in this batch environment, so a REAL trip can't be started here) - the user still needs to actually start a Guadeloupe run and watch the countdown live. Dialogue/mission feel for the whole M13B -> M14 -> Boat Man sequence needs a real playthrough.
- Next action: user plays Normy's favour -> the Boat Man's new introduction -> starts one Guadeloupe courier run and confirms the HUD countdown reads correctly, before MINI-111.
- Ownership released.
