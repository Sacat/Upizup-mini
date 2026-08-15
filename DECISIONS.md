# Up Iz Up Mini — Decisions

## D-001 — Separate project

Mini is a separate Unity project rather than a duplicate of the larger game. This prevents inherited prototype complexity and keeps builds smaller.

## D-002 — 2.5D means real 3D

The game uses real 3D geometry with a perspective three-quarter camera. It is not a 2D tilemap and not a flat overhead recreation of early GTA.

## D-003 — Geographic compression

Grand Bay landmark order, road character, slopes, and important areas should be recognizable, but travel distances may be compressed approximately 3:1 to 4:1 for playability.

## D-004 — One scene integrator

Only one agent edits Unity scenes/project settings at a time. Repeatable editor scripts are preferred so another agent can reproduce the scene.

## D-005 — Finishable first chapter

The first release targets one complete Grand Bay chapter. Roseau and Guadeloupe may appear through progression/travel interfaces before becoming playable districts.

