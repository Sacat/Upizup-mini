# Up Iz Up Mini — MINI-132 Shared combat and vehicle-impact ragdoll

```yaml
task_id: MINI-132
title: Franki strength, varied NPC fighting, vehicle impacts and rider ejection
request_owner: User
integrator: Codex
status: evidence_ready_partial
approval_class: C
budget:
  codex_time: one bounded shared-impact foundation and combat-data pass
  claude_time: 0
  external_credits: 0
  stop_condition: focused/static checks and one Windows build pass, then stop for live combat/crash approval
reserved_files:
  - Assets/UpIzUpMini/Scripts/Combat/MeleeMoveLibrary.cs
  - Assets/UpIzUpMini/Scripts/Combat/SimpleMeleeCombat.cs
  - Assets/UpIzUpMini/Scripts/Combat/NpcCombatMoveLibrary.cs
  - Assets/UpIzUpMini/Scripts/Combat/NpcRagdoll.cs
  - Assets/UpIzUpMini/Scripts/Combat/NpcCombatHealth.cs
  - Assets/UpIzUpMini/Scripts/Combat/PlayerCrashRagdoll.cs
  - Assets/UpIzUpMini/Scripts/Interaction/PoliceOfficer.cs
  - Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/VehicleImpactResponder.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/BikeInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoVehicleInteractable.cs
  - Assets/UpIzUpMini/Scripts/Vehicles/CarInteractable.cs
  - Assets/UpIzUpMini/Scripts/Character/CharacterSwitchManager.cs
  - Assets/UpIzUpMini/Editor/Mini132ImpactCombatValidation.cs
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Vehicles/TMAX_560.prefab
  - user-approved character models, chains, proportions and vehicle placements
depends_on:
  - MINI-119
  - MINI-120
  - MINI-130
```

## Intent

Franki's whole unarmed chain should hit harder than Sacat's. Police and gangs should use several readable but slower/weaker attacks. A moving bike/car should transfer believable direction and speed into the existing SuperMoto-derived ragdoll. Sellers and named contacts get up and return to their post; ordinary villagers, police and gang combatants keep the fade/respawn policy. A motorcycle passing vertical or striking a solid wall hard ejects driver/pillion into the same crash ragdoll.

## References

- User direction in the current task.
- Existing in-game reference: `NpcRagdoll`, `SacatRagdollBuilder`, and the SuperMoto crash-ragdoll pattern.
- Unity 6 official collision API: use contact/relative-velocity data, not trigger-distance guesses.

## Non-goals

- No shooting, weapons, gore, new paid animations, character remodel, or scene regeneration.
- Do not replace the approved TMAX/SuperMoto physics or seating.
- Do not disturb Dog Life's proven external 100-second pooled respawn.

## Acceptance scorecard

- [x] Franki damage multiplier applies to every combo step and gives stronger ragdoll momentum.
- [x] Police/gang moves come from one slower/weaker data table.
- [x] Vehicle impacts use real collision speed/contact direction with cooldown and mobile-bounded work.
- [x] Named/shop NPCs recover and return; ambient/police/gang fatal hits fade and respawn through their correct policy.
- [ ] TMAX/SuperMoto hard crash or beyond-vertical wheelie ejects both riders.
- [x] Focused validation and compile pass; Windows player is rebuilt.
- [ ] User approves motion/feel in live play.

## Implementation plan

1. Extend existing data libraries instead of duplicating per-character/per-NPC attack logic.
2. Make one collision-to-impact adapter usable by bikes and cars.
3. Extend the existing generic NPC ragdoll with recovery alignment and explicit role policy.
4. Reuse the same humanoid ragdoll builder for player ejection and safely return control after recovery.
5. Validate structure/math, rebuild once, and stop for real motion approval.

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Compiles | Unity batch compile | `Temp/MINI-132-compile-2.log` PASS | No |
| Damage/role/impact math | Focused validation | `Temp/MINI-132-validation.log` PASS | No |
| Windows player | Unity build + short smoke | Fresh `Assembly-CSharp.dll`/`level0`; no exception in `Temp/MINI-132-player-smoke.log` | No |
| Standing police combat | MINI-093 deterministic contact/death check (updated for intentional slower windup) | `Temp/MINI-132-standing-police-2.log` PASS | No |
| Standing gang-war check | MINI-069 | Existing unrelated scene-data failure: Zoomy is 22.4m from recruiter | Yes, separate placement repair |
| Motion/feel | User punch, vehicle-hit, wall-crash and over-vertical tests | Pending | Yes |

## Handoff

- Files changed: combat strength/move tables, role-aware NPC health/ragdoll recovery, shared vehicle impact adapter, three vehicle interactable hooks, focused validator and ledgers.
- Decisions made: Reuse the SuperMoto-derived generic ragdoll and real collision relative velocity; do not alter approved vehicle handling.
- Visual locks added/changed: None
- Known limitations: Motion cannot be signed off from batch mode. Rider ejection is intentionally still unchecked in this partial packet. MINI-069 still reports the pre-existing Zoomy/recruiter placement mismatch; MINI-132 did not change either placement.
- Next action: User drives the TMAX/Range Rover into one seller and one ambient/police NPC, then reports knockback/get-up feel; extend the same adapter to rider ejection after that gate.
- Ownership released: Yes
