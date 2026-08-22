# MINI-119 — Post-playtest batch: dialogue, Rasta selling, crop key order, breeding marker, hints, War Story win condition, bike climbing/anti-spin, cheat rep

```yaml
task_id: MINI-119
title: Fix dialogue truncation, a stray villager route, Rasta selling Blue Cheese, crop key ordering, breeding-station marker, per-crop hints, War Story's real win condition, bike hill-climbing/anti-spin, and 000000's missing Normy rep
request_owner: User
integrator: Claude
status: done
approval_class: B
budget:
  external_credits: 0
  stop_condition: Fix each item with real evidence, rebuild once, build the EXE, document the manual road/hedge steps separately (user is doing those by hand).
reserved_files:
  - Assets/UpIzUpMini/Scripts/Progression/ProgressionManager.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Scripts/UI/MissionHUD.cs
  - Assets/UpIzUpMini/Scripts/UI/GameplayHintController.cs
  - Assets/UpIzUpMini/Scripts/Missions/MissionSystem.cs
  - Assets/UpIzUpMini/Scripts/Interaction/RivalGangSpawner.cs
  - Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/TmaxBikeController.cs
  - Assets/UpIzUpMini/Editor/Mini064TmaxAssetPrep.cs
  - Assets/UpIzUpMini/Editor/Mini065TmaxPhysicsTest.cs
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini100GrandBayMapMigration.cs
  - one new focused MINI-119 validator under Assets/UpIzUpMini/Editor/
  - Docs/WorkPackets/MINI-119.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - approved Lalay/Highland road topology and map anchors (road intersection/hedge fine-tuning is explicitly deferred to the user's own manual pass this round)
depends_on:
  - MINI-118
```

## Intent

Batch of post-playtest reports:
1. Boss J's/Boss C's dialogue is getting cut off (first meeting and a long end-of-line each).
2. Boat Man's dialogue should be dynamic, not the same fixed lines.
3. A villager is walking toward the mountain using a stale pre-migration route.
4. Rasta should be able to sell Blue Cheese seeds directly, with a specific line.
5. Swap the Blue Cheese/Purple Sugar number-key order to match their actual unlock order; add a marker for the breeding station; extend per-crop hints to every strain, not just the three hybrids.
6. War Story (M16) should require defeating all Dog Life members to win, not just walking into the block.
7. The bike still bumps too much, can't climb a simple hill, and spins wildly off a small hedge - needs real climbing power and an anti-spin assist that reduces but doesn't eliminate spin.
8. The `000000` cheat should also set Normy reputation to 100 (it sets every other faction already).
9. (Deferred to the user's own manual pass) road-intersection and farm-hedge adjustment instructions.

## Non-goals

- Do not attempt the manual road-intersection/hedge geometry fixes this round - explicitly deferred to the user, who asked for instructions instead.
- Do not touch anything from MINI-108 through MINI-117's mission content beyond what's listed above.

## Handoff

Root causes and fixes, one per report:

1. **Dialogue truncation.** Not a hold-duration problem (user explicitly
   corrected an earlier wrong diagnosis of that): `MissionHUD.ResizeBanner()`
   capped the dark background panel's height at 330px. Long dialogue's own
   text was never clipped (`verticalOverflow = Overflow`), but any part of
   it rendering past that 330px panel had no dark backing behind it and
   became illegible against the bright game world - readable only in the
   middle, exactly the reported symptom. Raised the cap to 620px
   (`MissionHUD.cs`). `bannerHoldMax` was also raised 9->20s as a harmless
   extra margin, though it was not the actual cause.
2. **Boat Man dialogue "dynamic."** His most common repeat interaction (no
   cargo loaded) always answered with one exact fixed sentence.
   `TownNPCInteractable.HandleBoatMan()` now rotates that case through 3
   lines via the same `NextLine` mechanism every other villager already
   uses.
3. **Villager's stale mountain route.** `Mini100GrandBayMapMigration.
   PlaceRoadsideNpc` repositioned an NPC's transform but never reset its
   `PatrolNPC` waypoints (set at original pre-migration build time, pointed
   toward the old map's terrain). Now resets waypoints to a short stretch
   either side of its real, migrated position - fixes `NPC_Villager` and
   is a safe no-op for the helper's other (non-patrolling) callers.
4. **Rasta sells Blue Cheese seed.** New `TryOfferRastaBlueCheeseSeed()` in
   `TownNPCInteractable.cs`: gated on `ProgressionManager.IsCropUnlocked
   ("blue_cheese")` (never advertised before his own teaching mission,
   M13C4, actually unlocks it) and on not already being stocked; sells 3
   seeds for $650 via the existing `TryBuySeed` helper, with the line "Try
   out this new strain I have, Blue Cheese," per the user's own wording.
5. **Blue Cheese/Purple Sugar key swap + breeding marker + hints.**
   `Mini011PhaseBSetup.CropSpecs` reordered so key `[7]` = Blue Cheese and
   `[8]` = Purple Sugar (matches Rasta's real teaching order - was
   backwards). Added a `BreedingStation` minimap marker. `GameplayHint
   Controller` hints extended to every Rasta-taught base strain (M13C2-4,
   keys 5/6/7) in addition to the existing 3 hybrid hints, and M13C5's own
   hint text corrected from key 7 to key 8 to match the swap.
6. **War Story (M16) real win condition.** New `RivalGangSpawner.
   AllDefeated` (true once every pooled member is currently knocked out)
   and a new `ObjectiveKind.DefeatAllRivals` in `MissionSystem`, polled the
   same way `ReachArea`/`EscapeHeat` already are. M16 now has a
   `DefeatAllRivals` objective (`targetId = "DogLifeSpawner"`) between
   walking into the block and laying low, so the mission requires an
   actual fight, not just a walk-in.
7. **Bike hill-climb power + anti-spin.** `motorTorque` raised 700->950 (a
   real increase past MINI-118's mass-parity value, which only preserved
   the OLD power-to-weight ratio rather than making hills easier). New
   `ApplyHillClimbAssist()` adds extra forward push (mass-independent,
   `ForceMode.Acceleration`) proportional to the real slope under the rear
   wheel (0 on flat ground) - covers both open hillsides and ledges/kerbs.
   New `ApplyYawSpinAssist()` damps yaw angular velocity, but only the
   portion ABOVE a 220deg/s threshold, and only removes a tunable fraction
   of that excess per second (`yawSpinDamping`, default 0.65) - ordinary
   steering-induced yaw is far below the threshold and untouched, and a
   real hedge/ledge hit still spins, just far less wildly, matching "lose
   control slightly, not all that spin."
8. **Cheat `000000` missing Normy rep.** `ProgressionManager.
   UnlockEverything()` now also sets `NormyReputation = 100`.
9. **Manual road/hedge instructions.** Deferred by the user's own request -
   answered directly in chat, not as a code change (see conversation).

Verification: full vehicle + scene rebuild pipeline (`Mini064TmaxAssetPrep.
BuildPrefab` -> `Mini065TmaxPhysicsTest.WireController` -> `BuildTestScene`
-> `Mini071RoverVehiclePrep.BuildVehicle` -> `Mini011PhaseBSetup.BuildScene`,
which also runs `Mini100GrandBayMapMigration.ApplyToOpenScene`) completed
with no compile errors. All standing validators re-run clean (MINI-058,
065 prefab, 109, 110, 111, 112, 113) plus the MINI-065 real-physics drop
test (still PASS, wheelie/lean/recovery unaffected by the new motorTorque/
hill-climb/yaw-spin additions). New `Mini119BatchFixValidation.cs` covers
every item above with a real gameplay-state assertion except dialogue
truncation (a pure layout fix with no state to assert on) - PASS.

Not covered by any automated check: how the dialogue panel actually reads,
how the bike feels climbing a real hill or hitting a real hedge, and
general dialogue/mission feel - all need the user's own Play Mode/EXE
test, same limitation noted on every other visual/feel fix this session.
