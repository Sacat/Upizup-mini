# Up Iz Up Mini — Asset Register

Every non-generated art asset imported into the Mini project must have a row
here before (or in the same change as) import, per `AGENTS.md`. Nothing
below is imported into the Mini *project* yet — acquisition (adding to the
Unity account) is done for the Asset Store rows; the Unity Editor import
step into `Assets/` is still pending. See `Docs/MINI-011-VISUAL-PLAN.md` for
the Phase A gate this is waiting on.

## Acquired via Unity Asset Store (2026-08-15) — primary plan

Added to the user's Unity account directly from `assetstore.unity.com`,
signed in as the user, with the user's explicit in-chat permission for both
the browser session and accepting the Standard Unity Asset Store EULA per
item. Confirmed present on `assetstore.unity.com/account/assets` with a
purchase date of Aug 15, 2026. These are the five packages the user
specifically asked for.

| asset_id | asset_name | publisher | licence | purpose |
|---|---|---|---|---|
| MINI-AST-101 | Low Poly Character Pack | Floreswa | Standard Unity Asset Store EULA (free) | Primary rigged humanoid source — candidate base for Smart/Strong and ambient NPCs. |
| MINI-AST-102 | Low Poly Environment - Nature Free | Polytope Studio | Standard Unity Asset Store EULA (free) | Vegetation/environment dressing; well-rated (111 ratings, 10,495 favourites), actively maintained (v1.1.2, URP 12/14/16/17 shader variants). |
| MINI-AST-103 | Low Poly Tropical Beach | Aquaset | Standard Unity Asset Store EULA (free) | Palm/beach/dock props for coastal dressing. |
| MINI-AST-104 | POLYGON - Starter Pack - Art by Synty | Synty Studios | Standard Unity Asset Store EULA (free) | Neutral modular props; actively maintained (v1.1.0, shader graph conversion, 2022.3.56 update) — test pieces for style match before use, per the corrective brief's own caution. |
| MINI-AST-105 | Cartoon Farm Crops | False Wisp Studios | Standard Unity Asset Store EULA (free) | Candidate crop meshes for the tomato/farming loop — user's brief already flagged this as old; test/convert materials for URP before use. |

## Already owned — newly discovered, strong candidates

Found while checking the account's existing library (`account/assets`).
Several were added Aug 14, 2026 — before this session — likely staged in
preparation for this exact task. Licence: Standard Unity Asset Store EULA
(same verification level as above — genuinely owned by this account, not
the "unverified local file" caveat that applies to the `E:\Assets` rows
below).

| asset_id | asset_name | publisher | purpose |
|---|---|---|---|
| MINI-AST-110 | Demo City By Versatile Studio (Mobile Friendly) | Versatile Studio | 282.7MB, explicitly mobile-optimized — likely candidate for Grand Bay village buildings, stronger fit than anything found in `E:\Assets`. Needs a visual-style check against the shanty/corrugated-roof direction before committing. |
| MINI-AST-111 | Human Basic Motions FREE | Kevin Iglesias | Modern, actively-maintained (v2.4.2) locomotion/animation pack — idle/walk/run/strafe/turn, separate masculine/feminine rigs, animation layers. This is the animation source the user's original brief specifically named, and it removes the biggest technical risk flagged in the prior plan (Shanty Town 2's legacy pre-Mecanim animation format). |
| MINI-AST-112 | Human Melee Animations FREE | Kevin Iglesias | Bonus combat/interaction animation source if needed later (heat/police escalation). |
| MINI-AST-113 | Starter Assets - ThirdPerson \| URP | Unity Technologies | Official reference third-person CharacterController + Cinemachine + Input System setup — useful reference/base for `PlayerController`/camera rework, URP-native. |
| MINI-AST-114 | Robot Kyle \| URP | Unity Technologies | Rigged URP-compatible humanoid mascot; fallback test rig for the Humanoid-avatar pipeline before committing Low Poly Character Pack. |
| MINI-AST-115 | Vehicle Physics Pro - Community Edition | Edy | Matches `MINI-007` (pickup/bike drivable slice) later in `TASKS.md`; not needed for MINI-011 but worth noting now. |
| MINI-AST-116 | Rocks FREE pack | DexSoft | Terrain dressing for the Montine trail/farm clearing. |
| MINI-AST-117 | Grass Flowers Pack Free | ALP | Ground-cover dressing. |

## Not yet pulled into the Unity project

Acquisition (adding to the account) is done for every row above. None of
these files exist in `Assets/` yet — that requires either the Editor's
Package Manager "My Assets" download+import (a native Unity Editor GUI
action I have no tool to drive or observe) or the user completing it
manually. This is the next concrete blocker before any Phase B geometry
work can start.

## Local library (`E:\Assets`) — fallback only, not currently planned

Kept from the earlier audit pass in case the Asset Store packages above
don't pan out visually or technically.

| asset_id | asset_name | publisher | source path (local) | licence | purpose |
|---|---|---|---|---|---|
| MINI-AST-001 | Arteria3d - Shanty Town | Arteria3d | `E:\Assets\Pack\Arteria Pack\Arteria3d - Shanty Town\shanty town\` | Unverified — user to confirm Asset Store entitlement | Corrugated-roof shanty structures, barrels, clotheslines, crates. Still the closest single-pack match for "shanty/favela" architecture if Demo City doesn't fit. |
| MINI-AST-002 | Arteria3d - Shanty Town 2 | Arteria3d | `E:\Assets\Pack\Arteria Pack\Arteria3d - Shanty Town 2\Arteria3d - ShantyTown 2\` | Unverified | Additional buildings + 9 characters + soldier. **Technical risk unchanged:** pre-Mecanim-era export, frame-range animations, Humanoid-avatar compatibility unconfirmed. Deprioritized now that MINI-AST-101/111 (Store-acquired, confirmed-modern) cover the same need with less risk. |
| MINI-AST-003 | Arteria3d - Tropical Island Foliage Pack | Arteria3d | `...\Arteria3d - Tropical Island Foliage Pack.unitypackage` | Unverified | Vegetation fallback if MINI-AST-102/103 don't give enough variety. |
| MINI-AST-004 | Tropical Nature Pack | Unlisted | `...\Tropical Nature Pack\Tropical Nature Pack.unitypackage` | Unverified | Same as above. |

## Explicitly rejected

- Adventurer Character, Fighter Character, Warriors And Commoner (local `E:\Assets`) — fantasy/medieval sword-fighter aesthetic, wrong genre.
- `Grandbay entire.jpg` and `heightmapper-*.png/.raw` — explicitly marked "do not ship" / "not ready for direct Unity import" in `GrandBayReference`'s own assessment docs. Reference only, never imported as a texture/heightmap asset.

## Not yet sourced (blocking gaps)

- No local or acquired pack matches "landmark buildings" (school, church, credit union) closely enough — those will need custom-built modular pieces regardless of which building pack wins.
- Cartoon Farm Crops (MINI-AST-105) is the only crop-specific asset acquired; still needs a URP material test per the user's own brief before trusting it for the tomato growth stages.
