# MINI-172 — Expansion road, PCSS and Geneva field refinement

```yaml
task_id: MINI-172
title: Smooth expansion roads and improve the PCSS/Geneva destination
request_owner: User
integrator: Codex
status: implementing
approval_class: B
external_credits: 0
stop_condition: connected road corners, school road clearance, researched playing-field treatment, compile/validator pass, and fixed-camera evidence
```

## Intent

Make the Berekua-to-PCSS/Geneva expansion read as one driveable road network. Smooth sharp or disconnected road corners, remove the school overlap on the entrance-facing right side, and make Geneva Playing Field resemble a small Dominican community sports ground used for cricket and football, with a central cricket strip and modest spectator stands.

## References

- Public evidence confirms Geneva Playing Field is beside the Pierre Charles Secondary School sports facilities and is used for cricket, football, athletics and community events.
- A 2026 Geneva field photograph shows natural grass, simple white markings, goalposts and spectators/vehicles close to an open perimeter rather than a large enclosed stadium.
- Existing map topology and landmark placement remain the starting point; no Google imagery or 3D geometry enters the game.

## Protected/non-goals

- Do not edit the canonical `GrandBayProof.unity` or MINI-168 source copy.
- Preserve the field/school/roundabout relationship and compressed Grand Bay topology.
- Keep geometry low-poly and mobile-friendly.

## Acceptance

- Road endpoints share continuous paved junction surfaces with smooth corner transitions.
- PCSS building/campus clears the road on the entrance-facing right side.
- Field has a grass outfield, central cricket wicket/strip, restrained white sport markings, simple goalposts and small stands.
- Unity compile, map validator and fixed overhead/player-height renders pass.

## Result — 2026-09-19

- Eight open expansion roads received two bounded centreline smoothing passes; exact endpoints were retained. The asymmetric bay-road seam was explicitly restored from `Import_ExpansionRoad_10.asset` and excluded from symmetric smoothing after the existing regression validator caught that first-pass mistake.
- Three low-poly asphalt junction pads cover endpoint seams and round the paved shoulders. The terrain was refitted 3 cm beneath 1,947 road/junction samples.
- Direct surface sampling identified the actual school conflict: the north edge of `SchoolTeachingWing_1` touched `ExpansionRoad_4` at three samples. Both side wings moved 3 m inward, the courtyard narrowed from 40 m to 34 m, and the entrance-facing right wing, roof and windows received an absolute 3 m road setback. Final road-surface hits: zero.
- Geneva now reads as a modest multi-use community ground: natural striped grass and football markings/goals remain; a 20.12 x 3.05 m central cricket strip, creases, six stumps, bails, oval boundary and two low three-tier stands were added. This is an artistic compressed interpretation informed by public evidence of cricket, football, athletics and community use; it is not a surveyed reconstruction.
- Focused validator passed: 11 road mesh/collider pairs, 3 junction patches, zero school-wing road hits, 6 wickets and 6 stand steps. Existing `Mini171VerifyImport` passed every hard check after the final seam restoration. Compile/apply, validation and final render logs: `Logs/MINI-172-Refine-Final.log`, `Logs/MINI-172-Validate-Final-2.log`, `Logs/MINI-172-Import-Regression-Final.log`, `Logs/MINI-172-Render-Final.log`.
- Evidence: `Logs/Tasks/MINI-172/Renders/01-field-overhead.png`, `02-field-player-height.png`, `03-school-entrance.png`, `04-school-road-overhead.png`, and `05-road-network-overhead.png`.
- Runtime driving and final user visual acceptance remain required before the expansion copy replaces the canonical map.
