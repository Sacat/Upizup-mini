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
## D-009 - Illegal-sale heat and buyer hierarchy

Boss K is the premium Grand Bay weed buyer, the vagrant is a lower-paying alternative, and the produce buyer remains a fallback. A weed sale for the active mission adds 50 heat; another illegal sale adds 30. Police proximity cannot create heat when the shared inventory contains neither weed nor weed seed.

## D-010 — Chain ownership and boss distribution

Gold-chain purchases belong to the active protagonist only and must refresh visibly at purchase time. Sacat's approved manual transform is authoritative (`VA-002`); Franki may receive a separate fit profile later. Boss C wears the same cleaned `GoldChain18k` prefab as a status item with rig-specific placement. Boss J wears no chain.

## D-011 — Boss C uses an independent manual body/chain profile

Boss C keeps the existing 1.85 m cast height and front-to-back depth, but his visual root is widened on X to the user's approved scale `(1.4496428, 1, 1)`, producing the same measured 0.4403 m shoulder width as Sacat. His two-piece front/nape chain fit is stored separately as `VA-003` because Boss C's metarig bone axes and scale are not compatible with Sacat's bone-local placement. Future rebuilds must load these profiles rather than recalculate them.
