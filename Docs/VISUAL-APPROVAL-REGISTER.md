# Up Iz Up Mini — Visual Approval Register

## VA-009 - Round gold watch cuff-fit preview

id: VA-009
status: PROPOSED
date: 2026-09-07
task: MINI-145
subject: GoldWatchMobile / SacatWatchFit / FrankiWatchFit
scene_or_prefab: Assets/UpIzUpMini/Scenes/WatchWardrobeProof.unity
evidence: Logs/Tasks/MINI-145/Sacat-Watch-Close.png; Franki-Watch-Close.png; Sacat-Watch-Full.png; Franki-Watch-Full.png
approved_aspects: MINI-144 linked gold style; MINI-145 round design accepted by user, with90-degree clockwise orientation revision requested and now previewed.
still_editable: exact wrist/cuff scale, rotation, position and final in-game appearance until accepted.
notes: Watch currently sits over original long-sleeve cuffs. No chain edits. Mobile content budget passed, not device performance certification. No shop or gameplay integration yet.
latest_revision: User clarified whole-watch90-degree wrist orbit and confirmed proceeding; profiles updated without changing size. New evidence Logs/Tasks/MINI-145/Sacat-Watch-Side.png and Franki-Watch-Side.png. Placement confirmation pending.

This file protects looks and placements the user has explicitly approved. Absence from this file does not mean an agent may ignore the design brief; it means the item has not yet received a formal visual lock.

## Status values

- `PROPOSED`: evidence exists; user has not decided.
- `APPROVED`: approved aspects are locked.
- `REVISE`: user rejected or requested changes.
- `SUPERSEDED`: replaced by a later approved record.

## Record template

```yaml
id: VA-###
status: PROPOSED | APPROVED | REVISE | SUPERSEDED
date: YYYY-MM-DD
task: MINI-###
subject: Stable object/prefab/scene ID
scene_or_prefab: path
evidence:
  - absolute screenshot/video path
approved_aspects:
  - placement
  - scale
  - silhouette
  - color/material
  - camera/framing
  - motion/tuning
still_editable:
  - clearly bounded aspects
user_words: Short exact confirmation or rejection
supersedes: null
notes: Important constraints
```

```yaml
id: VA-008
status: APPROVED
date: 2026-08-29
task: MINI-123
subject: Highland no-sidewalk spline treatment and two-sided bridge connection
scene_or_prefab: Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-123\MBRoad-Highland-NoSidewalk-PlayerHeight-1280x720.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-123\MBRoad-Highland-BridgeConnection-1280x720.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-123\MBRoad-Highland-Lalay-DriveLine-1280x720.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-123\MBRoad-Highland-Bridge-HighlandApproach-1280x720.png
approved_aspects:
  - line-free Highland road with no sidewalks
  - terrain-aware road banking capped at eight degrees
  - collidable graded transitions overlapping both bridge ends
  - reversible isolated proof that leaves the live game untouched
still_editable:
  - the other phase-one road conversions and their junction finish
  - final vehicle-driving tuning
  - live-scene migration after complete-network approval
user_words: "looks good for now so go ahead"
supersedes: null
notes: This authorizes continued work across the remaining proof network. It does not authorize live-scene replacement by itself.
```

## Active records

### VA-009 — Approved Blender house/grass/crop direction

- Date: 2026-09-06; packet MINI-141, integration MINI-142.
- User words: "all" / "all are good for now" / "continue".
- Evidence: Logs/Tasks/MINI-141/01-Houses-Grass-Preview.png and 02-Crop-Preview.png, matching Blender sources.
- Approved: one/two-storey house style, pastel plaster, pitched metal roofs, framed openings/verandas; sparse grass; carrot/banana/cannabis candidate silhouettes.
- Still editable: production mesh consolidation, LODs, mapping onto existing lots and growth stages; final in-game lighting and fitting require screenshots.
- Existing road routes, shops, manual accessories, characters, vehicles and gameplay identities remain protected. Road gap near Dog Life is authorized for an evidence-based local repair.
- MINI-142 refined bud silhouette approved by "yes looks better" (2026-09-06), evidence Logs/Tasks/MINI-142/Buds/Cannabis-BudClose.png. Actual Unity house screenshot then shown; user instructed "continue and try to build exe before tokens expire", authorizing application/build. Runtime visual/handling acceptance remains separate from design approval.


```yaml
id: VA-001
status: SUPERSEDED
date: 2026-08-20
task: MINI-086
subject: Sacat purchased GoldChain18k placement
scene_or_prefab: Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\MINI-086-CaptureApprovedChain.log
  - E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Data\Equipment\SacatChainPlacement.asset
approved_aspects:
  - local position (-0.03414066, 0.40609157, 0.06550227)
  - local rotation (337.00732, 0, 0)
  - local scale (1.066973, 1.066973, 1.066973)
  - GoldChain18k model on Sacat when Sacat owns chain_gold
still_editable:
  - runtime swing stiffness and damping after movement playtest
  - a separately approved Franki-specific fit profile
user_words: "ok the chain was done"
supersedes: null
notes: User placement is authoritative. Do not normalize or recalculate it. Boss C uses the same cleaned chain prefab with rig-specific placement; Boss J wears no chain.
```

```yaml
id: VA-002
status: APPROVED
date: 2026-08-20
task: MINI-087
subject: Sacat purchased GoldChain18k two-piece fitted placement
scene_or_prefab: Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\MINI-087-CaptureChain.log
  - E:\Unity\Up Iz Up Mini\Logs\MINI-087-Validate-2.log
  - E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Data\Equipment\SacatChainPlacement.asset
approved_aspects:
  - root local position (-0.047002427, 0.48330128, 0.07208218)
  - root local rotation (341.27582, 0, 0)
  - root local scale (0.9118362, 1.1559348, 0.9118362)
  - original front mesh fitted transform
  - duplicated mesh fitted around the nape/back of the neck
  - runtime recreation of both fitted pieces when Sacat owns chain_gold
still_editable:
  - runtime swing stiffness and damping after movement playtest
  - Boss C model scale and his separate chain placement
user_words: "Sacat chain done" / "i made a duplicate of the chain as well so save that too"
supersedes: VA-001
notes: This is a user-authored visual lock. Do not recalculate the root or either fitted child transform. The duplicated nape piece is profile data and is recreated at runtime; it is not a source-prefab edit.
```

```yaml
id: VA-003
status: APPROVED
date: 2026-08-20
task: MINI-088
subject: Boss C body width and two-piece GoldChain18k fit
scene_or_prefab:
  - Assets/UpIzUpMini/Data/Character/BossCVisualProfile.asset
  - Assets/UpIzUpMini/Data/Equipment/BossCChainPlacement.asset
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\Snapshots\MINI-088-BossC-Body-Manual.png
  - E:\Unity\Up Iz Up Mini\Logs\Snapshots\MINI-088-BossC-Chain-Manual.png
  - E:\Unity\Up Iz Up Mini\Logs\MINI-088-Validate.log
approved_aspects:
  - Boss C visual-root scale (1.4496428, 1, 1)
  - 1.85 m height preserved
  - 0.4403 m shoulder width matching Sacat
  - front-to-back depth preserved
  - chain root local position (-0.035, 0.093, -0.423)
  - chain root local rotation (280.99878, 179.99992, 180.00008)
  - chain root local scale (0.7353191, 0.7353193, 0.73531926)
  - original front mesh and duplicated nape/back mesh fitted transforms
still_editable:
  - runtime animation deformation after hands-on movement observation
  - a future canonical shared rig/body project only with a new explicit approval
user_words: "i placed it manually so you can save it"
supersedes: null
notes: User manual placement is authoritative. Do not replace this with calculated bounds or Sacat's bone-local root numbers. Sacat VA-002 remains independently locked.
```

```yaml
id: VA-004
status: APPROVED
date: 2026-08-20
task: MINI-095
subject: Lalay road network and street-scale target
scene_or_prefab: Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-094\LalayToBeach-Overhead.png
  - C:\Users\PCSS-PC\AppData\Local\Temp\codex-clipboard-d726a081-cdcd-4f24-a8aa-a8291d100147.png
approved_aspects:
  - MINI-094 OSM preview road network and Lalay-to-coast relationship
  - narrow two-lane Lalay road with grey paved sidewalks
  - close house spacing on both sides without road overlap
  - view/orientation toward the bay
  - smooth bump-free Lalay surface with no more than a mild continuous rise
still_editable:
  - final house meshes, colors, yards, vegetation, drains and street props
  - exact landmark architecture and final Highland farm dressing
  - performance-driven LOD and material consolidation
user_words: "this image looks just like the lalay road" / "this image you gave me is very good for the road network" / "make sure the laylay road is flat and no bumps"
supersedes: null
notes: Preserve the road topology, width, sidewalks, bay orientation and dense settlement scale. Do not trace or ship satellite pixels. Houses may be upgraded but cannot be placed across any mapped road. The later intermediate screenshot questioned by the user (`codex-clipboard-a1692bce-5179-4110-85ed-4d2b5a94d026.png`) is explicitly rejected and is not part of this lock.
```

```yaml
id: VA-005
status: APPROVED
date: 2026-08-20
task: MINI-099
subject: Corrected Lalay–Highland phase-one graybox
scene_or_prefab: Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-099\MapLab-Overview-1600x1000.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-099\MapLab-Lalay-1280x720.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-099\MapLab-Highland-1280x720.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-099\MapLab-Bay-1280x720.png
approved_aspects:
  - curated connected nine-road phase-one topology
  - uniform 6.2 metre paved-road width and centered two-vehicle bridges
  - smooth Lalay and gentler Highland connections
  - dense houses on both sides of Lalay and moderate Highland settlement
  - church on land with beach across the road and jetty extending into sea
  - no houses on the beach/sand exclusion
still_editable:
  - final building meshes, vegetation, drains, props and material consolidation
  - inactive future farm parcel dressing when progression unlocks it
  - runtime-only adjustments found during walking/driving acceptance
user_words: "this is a lot better ... this exactly what i want" / "i am ready to transfer the map to my upizup mini game"
supersedes: null
notes: Map-lab remains the rollback-safe visual backup. Migration must preserve stable gameplay/save IDs.
```

```yaml
id: VA-006
status: APPROVED
date: 2026-08-20
task: MINI-105
subject: Sacat canonical modular base appearance
scene_or_prefab: PREVIEW_ONLY_NOT_INTEGRATED
evidence:
  - E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\Approved\Sacat-ModularBase-Approved.png
approved_aspects:
  - recognizable Sacat face and skin tone
  - short black haircut with a sharp shape-up and no cap or headphones
  - approved body silhouette and proportions
  - fitted white vest and fitted black boxer pants as the modest wardrobe base
  - bare feet so footwear can remain modular
  - neutral T-pose modeling target
still_editable:
  - topology, UV layout, skin weights and LODs needed to reproduce this appearance
  - hidden body-region masks beneath future garments
  - hair geometry detail provided the approved silhouette and hairline remain unchanged
user_words: "ok great use this one"
supersedes: null
notes: This locks the modeling target, not an AI-generated production mesh. Preserve the existing Sacat identity and skeleton; obtain new approval from fixed screenshots before replacing the playable character.
```

```yaml
id: VA-007
status: APPROVED
date: 2026-08-29
task: MINI-121
subject: MB Road System Lalay spline foundation and rollback approach
scene_or_prefab: Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-121\Legacy-Lalay-PlayerHeight-1280x720.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-121\MBRoad-Lalay-PlayerHeight-1280x720.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-121\Legacy-Lalay-Overhead-1600x1000.png
  - E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-121\MBRoad-Lalay-Overhead-1600x1000.png
approved_aspects:
  - exact existing Lalay X/Z alignment rebuilt as MB Road System splines
  - approved 6.2 metre two-vehicle width
  - existing road elevations sampled rather than guessed
  - legacy Lalay roads retained as an inactive rollback backup
  - isolated Map Lab proof before any live-scene migration
still_editable:
  - asphalt texture and material treatment
  - road-system junction finish
  - bridge templates and later road classes
  - runtime driving adjustments found during hands-on testing
user_words: "yes i love it so far continue"
supersedes: null
notes: This approval authorizes continued work in the isolated proof, not replacement of the playable road. Live migration still requires upgraded visual evidence and a vehicle pass.
```
