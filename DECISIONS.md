# Up Iz Up Mini — Decisions

## MINI-166 revision — distinct clothing and fuller shoulders (2026-09-12)

User requests individually dressed characters and a muscular shoulder/trapezius transition closer to the original fit. Keep both selectable catalogues and independent saved choices; give Franki navy tee/denim jeans/black Mike 90 defaults and Sacat green polo/black trousers/white Mike 97 defaults. Do not overwrite existing deliberate saved selections or forbid the user choosing matching clothes. Add a localized, smooth upper-shoulder volume adjustment to both shirt designs without changing skeletons, faces, sleeve ends or signature accessory transforms. Rejected the first steep trap lift after close-up inspection; refine before final build. User visual acceptance remains owed.

## MINI-166 — independent fitted garment surfaces (2026-09-12)

User authorized completing and correcting the rejected wardrobe. Use separate skinned clothing bindings with explicit material tint regions and preserved original character rigs/faces. Fit the repaired continuous garment topology through corresponding rest-bone matrices; a fused material index is not a reason to leave Sacat's wardrobe absent. Use clean skin geometry below shorts, distinct shoe panel meshes, and a curved cap. Preserve existing save IDs and free access. Treat generated visual evidence as verification, not as user acceptance of final styling.

## D-013 — Bike wheelie ceiling and collision-only rider ejection

- Date: 2026-08-31
- Decision owner: User
- Decision: Cap commanded bike wheelies at 89 degrees. Wheelie angle or bike tilt alone must never crash or eject driver/pillion; only a sufficiently hard physical collision may eject them. This supersedes the earlier 90-degree balance / 95-degree fall-back rule.
- Consequence: Both bike controllers use a shared 89-degree runtime clamp. Crash detection retains MINI-135's ground-filtered, contact-normal hard-impact path and has no over-angle path. Future tuning must not reintroduce angle-triggered crashes unless the user explicitly revises this decision.

## D-012 — TMAX uses the SuperMoto as its moving-wheel and steering reference

- Date: 2026-08-30
- Decision owner: User
- Decision: Fit black SuperMoto-reference wheel meshes to the TMAX and synchronize the front wheel, fork/handlebar and rider grip targets, while leaving the proven TMAX WheelColliders and handling untouched.
- Consequence: MINI-124 is a reversible visual integration. The fused scan geometry remains until the user decides whether the overlay is sufficient or authorizes a later Blender mesh-separation cleanup.

## D-011 — Every world expansion uses a gated district manifest

- Date: 2026-08-20
- Decision owner: User
- Decision: Future Dominica districts and other maps use `Docs/WORLD-EXPANSION-WORKFLOW.md`, a stable map ID, district packet and machine-readable manifest. Work advances only through scaffold, map truth, approved preview, graybox, approved graybox, migration and runtime acceptance.
- Consequence: A sourced map cannot be migrated directly into gameplay; rejected evidence blocks promotion; road rendering alone does not prove passability; one integrator owns Unity migration; every district preserves licences, approvals, stable anchors, gameplay-role mappings, mobile budgets and rollback information.

## D-009 — Highland is the first remote planting district

- Date: 2026-08-20
- Decision owner: User
- Decision: The first Grand Bay farming/planting connection will lead to Highland instead of Montine. Montine remains available for later missions or district expansion.
- Consequence: MINI-094 will label Highland as the intended planting route, but will not invent an exact road junction until licensed data or a user-confirmed local pin establishes it.

## D-010 — Approved phase-one Lalay map truth

- Date: 2026-08-20
- Decision owner: User
- Decision: Use the MINI-094 OSM overview as the road-network truth and the approved original Lalay street screenshot as the scale/look target. Lalay is narrow and bump-free with grey sidewalks and close houses; only a mild continuous rise away from the bay is retained for playability.
- Consequence: Future map and art passes may replace graybox buildings and add detail, but must preserve `VA-004`, keep houses off all roads, retain the Lalay-to-Highland inroad, and avoid tracing commercial satellite imagery.

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

2026-09-10 MINI-164: User requested short black waves beneath removable caps. Use continuous head-weighted scalp surfaces and procedural albedo/normal waves; preserve live clothing and face assets. Visual user acceptance remains pending; no visual lock recorded.

2026-09-10 MINI-165: User explicitly defines headphones as a removable head accessory like a cap. Preserve Sacat's original fitted geometry as a separate skinned accessory; use the existing wardrobe unequipped-item save data, with worn as the legacy default.
