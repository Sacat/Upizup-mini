# Up Iz Up Mini — Game Design

## Vision

A compact, modern 2.5D crime-and-farming progression game set in Grand Bay, Dominica. Two boys leave school to pursue farming, discover that weed pays more, work for increasingly exploitative people, build their own market position, and face police and territorial pressure.

## Presentation

- Stylized low-poly 3D, warm Caribbean color, lush vegetation, corrugated roofs, steep terrain, and narrow village roads.
- Perspective three-quarter camera with readable characters and buildings.
- Text-first Dominican dialogue; voice can follow later.
- PC controls first with systems designed for future touch controls.
- Every marked mission uses the same navigation language: a blinking yellow objective on the minimap, clamped to its edge when the destination is outside the camera footprint, plus a nearby world marker. Yellow always means the current mission, regardless of district.
- Long dialogue and mission briefings sit on a dynamically sized transparent black panel.

## Early route and heat teaching

- K takes the boys directly to Boss J; L briefly continues legal planting and selling until poor returns push them to the same meeting.
- The first heat recovery lesson is safehouse rest. The next is social: carry no weed or seeds, speak to two different regular officers, then learn that Normy takes $100 to remove 20% heat.
- Paro becomes a buyer only after his first dialogue interaction. Locked contacts, products and systems remain hidden until their relevant mission.
- Developer playtest shortcut `111111` completes only the current mission and advances normally through the mission list.

## Progression truth rules

- A mission transaction is idempotent: completing a payment, delivery or unlock consumes/rewards exactly once and advances immediately.
- If the game can prove a unique purchase or inventory requirement was already satisfied, the mission gives retrospective credit instead of making the player pay twice.
- Story-critical contacts, strains, crew recruitment, vehicles, phone functions and Guadeloupe options remain hidden or unavailable until their mission unlock.
- Objective markers resolve from stable generated anchors or live named targets, never stale pre-map coordinates.
- Normy's one-time mission favors are separate from his repeat heat-cooling service.
- Guadeloupe character dispatch uses the inactive protagonist, locks switching, hides that character and exposes a visible return countdown.
- Rasta owns the advanced strain/hybrid teaching ladder; he does not send the player back to basic tomato work.
- Guadeloupe unlocks before Roseau expansion.

## Reusable production systems

- Missions are data/content built through the canonical scene generator and generic objective handlers; crop- or NPC-specific rules should not be scattered through unrelated controllers.
- Districts use stable map manifests, licensed map truth, an isolated graybox and runtime acceptance before decoration or expansion.
- Characters use an approved multiview reference, Hitem3D candidate, Blender-owned canonical full-finger body/rig, separate compatible garments, mobile LOD/motion gates and per-character attachment/vehicle-fit profiles.
- Imported assets require provenance, isolated inspection, mobile budgets, LOD/collider/material checks and visual approval before gameplay integration.
- Static visuals require screenshots; movement, combat, NPC pathing, riding and driving require short live capture plus user acceptance.

## Core loop

1. Accept work or buy seeds.
2. Travel to suitable land.
3. Plant, water, and harvest.
4. Sell in Lalay for lower prices.
5. Improve equipment, land access, transport, and crop quality.
6. Take higher-profit risks that increase heat and rivalry.

## First chapter target

- Lalay village road and market.
- Highland dirt inroad and farm clearing.
- Two safehouses.
- Tomatoes, bananas, carrots, and three weed tiers.
- One usable vehicle.
- Six to eight NPCs.
- Four to six missions forming a complete 20–40 minute chapter.

## Weed progression

- Bushers: early, cheap, lower yield/value.
- Black Sugar: mid-game unlock, better quality and price.
- Purple: late Grand Bay unlock, highest local value and heat.

Higher tiers must unlock through mission progression and contacts, not merely from the numeric passage of time.

## Scope exclusions for the first slice

- No complete Dominica island at literal scale.
- No multiplayer.
- No full combat system in `MINI-001`.
- No broad collection of unreviewed asset packs.
- No claim of AAA production quality.
