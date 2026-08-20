# Grand Bay — Lalay to Highland — Map District Packet

```yaml
map_id: dm-dom-grand-bay-lalay-highland-v1
task_id: MINI-094 / MINI-095 / MINI-096
workflow_stage: graybox
integrator: Codex
approval_class: C
country: Dominica
island: Dominica
region: Saint Patrick
rollback_commit: 87fc3ea
```

## Player purpose

The opening Grand Bay chapter: Sacat and Franki move along Lalay, connect to Highland for the first farm, reach shops/bosses/gangs/institutions, and later access the jetty/Guadeloupe route.

## Boundaries and connections

- Included corridor/district: Lalay main street, nearby side roads, Highland inroad/first farm and coastal story jetty relationship.
- Entry gateways: future Grand Bay/Berekua continuation.
- Exit gateways: future Roseau expansion and Guadeloupe boat abstraction.
- Target travel time: to be measured in the migrated playable scene.
- Explicitly excluded: Roseau map, whole Dominica, Guadeloupe land map and final architecture.

## Sources and local truth

Full licences belong in `SOURCES.md`.

| Stable ID | Meaning | Trust level | User evidence |
|---|---|---|---|
| lalay_to_highland_inroad | Required playable connection | user_verified | Annotated map and direct confirmation |
| highland_first_farm | First active parcel | user_verified/artistic placement | Annotated map and progression direction |
| story_jetty | Guadeloupe gateway | user_verified/artistic placement | Annotated coastal relationship |

## Gameplay grading

| Surface | Width | Maximum grade | Shoulder/sidewalk | Notes |
|---|---:|---:|---|---|
| Lalay main road | 6.5 m | 1.5% | Grey sidewalks | Smooth/bump-free user requirement |
| Secondary road | 2.8–5.8 m | 10% | Contextual | Terrain-conforming and collidable |
| Dirt/farm track | 3.2 m | pending live test | Dirt shoulder | Connects Highland parcel |

## Existing-system migration

| Gameplay role/ID | Old location | New anchor | Must preserve | Validation |
|---|---|---|---|---|
| Player spawn | | | save/respawn | |
| Mission giver | | | progression ID | |
| Farm/property | | | ownership state | |
| Vehicle/boat | | | home/purchase state | |

## Fixed evidence cameras

| Camera ID | Purpose | Resolution | Approval |
|---|---|---|---|
| overview | Network relationship | 1600×1000 | pending |
| main_street | Player-height road scale | 1280×720 | pending |
| destination | Player-height expansion area | 1280×720 | pending |

## Acceptance gates

- [x] Licensed map truth and attribution recorded.
- [x] Trust-marked anchors reviewed locally for the first corridor.
- [x] Overhead preview approved.
- [x] Separate graybox builds repeatably.
- [x] Road continuity/collision/clearance/grade checks pass statically.
- [x] Approved and rejected screenshots recorded correctly.
- [ ] Rollback-safe migration complete.
- [ ] Walking and driving accepted both ways.
- [ ] NPC/navigation/mission regressions pass.
- [ ] Mobile budget recorded.
- [ ] Handoff updated and ownership released.

## Handoff

- Current stage:
- Changed files:
- Approved locks:
- Rejected evidence:
- Known placeholders:
- Human tests owed:
- Next single action:
- Ownership released:


