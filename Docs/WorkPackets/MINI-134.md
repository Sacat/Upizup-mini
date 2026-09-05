# MINI-134 — Crash, Damage, and Recovery Pass

Status: Implemented and validated; awaiting live feel approval
Risk: Class C (runtime physics and player-control behavior)

## Scope

- Eject TMAX and SuperMoto riders after hard solid impacts or a wheelie past the recoverable angle.
- Reduce excessive NPC launch force while preserving readable ragdoll hits.
- Add lightweight vehicle durability with automatic upright recovery.
- Make nonfatal NPC ragdolls wait for physics to settle, then recover from the landing position.

## Acceptance

- [x] TMAX and SuperMoto expose the same crash-ejection bridge.
- [x] Normal NPC contact uses a higher ejection threshold; live feel remains to confirm.
- [x] Hard-impact and 92-degree tilt paths release rider and pillion through the same bridge; live collision proof remains.
- [x] Vehicle impact damage remains meaningful while momentum/lift are bounded.
- [x] Bikes and cars accumulate collision damage and recover upright with partial health.
- [x] Sellers/named NPCs recover and return to post; ambient NPC defeat policy remains unchanged.
- [x] Focused validation and Unity compile pass.
- [x] Windows player rebuild succeeds.

## Runtime playtest still required

- Crash the TMAX and SuperMoto into a solid wall at speed.
- Pull a wheelie past the balance point.
- Hit a seller and a walking NPC with a vehicle.
- Confirm impact strength and recovery timing feel believable.

## Evidence

- `Logs/Codex-MINI-134-Validation.log`: focused thresholds/bridges PASS; Unity exit code 0.
- `Logs/Codex-MINI-134-MINI132-Regression.log`: standing shared combat/impact regression PASS.
- `Logs/Codex-MINI-134-Windows-Build.log`: Windows build successful; `Assembly-CSharp.dll` and `level0` refreshed 2026-08-30 21:53.
- Rebuilt player remained alive for a 12-second startup smoke and produced no matching exception.
