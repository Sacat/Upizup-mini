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

## D-006 — Dominica-first world framing

Grand Bay is the first of several intended Dominica regions, not the whole game. After the Grand Bay chapter, the plan is to expand to Roseau, then other parts of Dominica. Map-anchor data (`MINI-002`) and any travel/region-select UI should be framed accordingly rather than assuming Grand Bay is the entire map.

## D-007 — Guadeloupe sea-trade abstraction (no Guadeloupe map yet)

One of the two boys can be sent by sea to Guadeloupe to sell produce or weed for **3x** the normal Dominica sell price. While away, that boy is not controllable — a distinct "away on a trip" character state, not just off-screen/following. Guadeloupe itself is explicitly deferred to the next major version: for Mini this is an abstracted transaction (send character + inventory, wait, return with 3x money), not a playable scene. Character-switching and inventory/economy systems should leave room for this state now even though the trip mechanic itself isn't built in `MINI-011`.

## D-008 — Mobile/multi-platform is a first-class constraint, not a later pass

The game must target all platforms with mobile phones/tablets prioritized, and WebGL browser play is a stated goal to keep in mind. This goes beyond AGENTS.md's existing mobile-performance guidance (URP, LODs, pooling): touch/tablet ergonomics and flexible aspect ratios should factor into camera, HUD, and input design decisions as they're made, not retrofitted after a keyboard-and-mouse-only pass.

