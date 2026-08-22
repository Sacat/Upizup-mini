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
| MINI-AST-101 | Low Poly Character Pack | Floreswa | Standard Unity Asset Store EULA (free) | Primary rigged humanoid source. **Visually confirmed 2026-08-15** (`Logs/Snapshots/character-pack-sample.png`): male01 and male02 variants are visually distinct (blue polo + cap + beard vs. green long-sleeve + short hair), clean readable low-poly style, both convert to valid Humanoid avatars (see rig-test result above). Good base for Smart/Strong. |
| MINI-AST-102 | Low Poly Environment - Nature Free | Polytope Studio | Standard Unity Asset Store EULA (free) | Vegetation/environment dressing; well-rated (111 ratings, 10,495 favourites), actively maintained (v1.1.2, URP 12/14/16/17 shader variants). |
| MINI-AST-103 | Low Poly Tropical Beach | Aquaset | Standard Unity Asset Store EULA (free) | Palm/beach/dock props for coastal dressing. |
| MINI-AST-104 | POLYGON - Starter Pack - Art by Synty | Synty Studios | Standard Unity Asset Store EULA (free) | Neutral modular props; actively maintained (v1.1.0, shader graph conversion, 2022.3.56 update). **Checked 2026-08-15:** its "Building" prefabs are generic industrial/sci-fi kit pieces (pipes, beams, ladders, flat background silhouettes), not village houses — not usable as the building source either. Synty's bold-flat-colour low-poly style is still a fine visual reference for props/vehicles generally. |
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
| MINI-AST-110 | Demo City By Versatile Studio (Mobile Friendly) | Versatile Studio | 282.7MB, mobile-optimized. **Visually tested 2026-08-15 (`Logs/Snapshots/demo-city-houses.png`, rendered directly via `Mini011AssetSnapshot.cs`, not guessed from names): flat gray/white concrete cube buildings, black roller-shutter garage doors, zero colour, zero corrugated-roof character — a modern/industrial city kit, not a village. Rejected as the building source.** |
| MINI-AST-111 | Human Basic Motions FREE | Kevin Iglesias | Modern, actively-maintained (v2.4.2) locomotion/animation pack — idle/walk/run/strafe/turn, separate masculine/feminine rigs, animation layers. This is the animation source the user's original brief specifically named, and it removes the biggest technical risk flagged in the prior plan (Shanty Town 2's legacy pre-Mecanim animation format). |
| MINI-AST-112 | Human Melee Animations FREE | Kevin Iglesias | Bonus combat/interaction animation source if needed later (heat/police escalation). |
| MINI-AST-113 | Starter Assets - ThirdPerson \| URP | Unity Technologies | Official reference third-person CharacterController + Cinemachine + Input System setup — useful reference/base for `PlayerController`/camera rework, URP-native. |
| MINI-AST-114 | Robot Kyle \| URP | Unity Technologies | Rigged URP-compatible humanoid mascot; fallback test rig for the Humanoid-avatar pipeline before committing Low Poly Character Pack. |
| MINI-AST-115 | Vehicle Physics Pro - Community Edition | Edy | Matches `MINI-007` (pickup/bike drivable slice) later in `TASKS.md`; not needed for MINI-011 but worth noting now. |
| MINI-AST-116 | Rocks FREE pack | DexSoft | Terrain dressing for the Montine trail/farm clearing. |
| MINI-AST-117 | Grass Flowers Pack Free | ALP | Ground-cover dressing. |

## Imported into the Unity project (2026-08-15)

All 8 packages above (MINI-AST-101 through 105, plus MINI-AST-110/111/112)
are now physically in `Assets/` — imported via `Unity.exe -importPackage`
against the `.unitypackage` files the user downloaded to the Asset Store
cache (`%APPDATA%\Unity\Asset Store-5.x\...`), since the Package Manager
GUI itself isn't something I can drive. Landed under
`Assets/Floreswa`, `Assets/Kevin Iglesias`, `Assets/Polytope Studio`,
`Assets/Aquaset`, `Assets/Synty`, `Assets/Cartoon_Farm_Crops`,
`Assets/Versatile Studio Assets` (plus `Assets/Standard Assets`, a
dependency pulled in by Cartoon Farm Crops). Full-project batch-mode
compile is clean (0 `error CS`) after all imports, including after adding
`com.unity.ugui` to `Packages/manifest.json` (needed by Human Basic
Motions FREE's demo scene script, and by the project's own planned
Canvas/TextMeshPro HUD anyway).

**Humanoid rig risk RESOLVED:** `Assets/Floreswa/Models/male01_1.fbx` and
`male02_1.fbx` both convert to valid Unity Humanoid avatars when the
importer's animation type is set to Human (confirmed via a one-off test,
`Mini011RigTest.cs`, log at `Logs/rigtest.log`). Low Poly Character Pack
is a usable base for Smart/Strong.

## Local library (`E:\Assets`) — fallback only, not currently planned

Kept from the earlier audit pass in case the Asset Store packages above
don't pan out visually or technically.

| asset_id | asset_name | publisher | source path (local) | licence | purpose |
|---|---|---|---|---|---|
| MINI-AST-001 | Arteria3d - Shanty Town | Arteria3d | `Assets/ArteriaShantyTown/ShantyTown1/` (copied into project 2026-08-15, user-approved) | User-approved local use | **Confirmed usable 2026-08-15** (`Logs/Snapshots/shanty-town-houses.png`): `shanty1`/`shanty5` render with genuine textured corrugated-tin roofing and tan/weathered wall colour — a real match for the brief's "corrugated-roof village architecture." These read as small shacks/stalls/clutter scale, not full multi-room houses — good for market stalls, lean-tos, and yard structures; the main housing stock still needs hand-built modular houses (see risks below). Props (barrels, clothesline, container, door, fence, metal panels, tyres) are also usable set dressing. |
| MINI-AST-002 | Arteria3d - Shanty Town 2 (buildings only) | Arteria3d | `Assets/ArteriaShantyTown/ShantyTown2_Buildings/` (copied into project 2026-08-15, user-approved) | User-approved local use | **Rejected after testing.** Rendered `BuildingA`/`BuildingE`: both showed up flat white/untextured. Investigated why: the sibling "TEXTURES" folder in the source pack doesn't contain image files — it's `.u3d` CAD-format export bundles per building, not usable Unity textures. Fixing this would mean sourcing real texture images that may not exist in this pack at all. Not worth the effort given the hand-build fallback is explicitly sanctioned. Characters/soldier from this pack were never used (Low Poly Character Pack won that role). |
| MINI-AST-003 | Arteria3d - Tropical Island Foliage Pack | Arteria3d | `...\Arteria3d - Tropical Island Foliage Pack.unitypackage` | Unverified | Vegetation fallback if MINI-AST-102/103 don't give enough variety. |
| MINI-AST-004 | Tropical Nature Pack | Unlisted | `...\Tropical Nature Pack\Tropical Nature Pack.unitypackage` | Unverified | Same as above. |

## Derived from the larger project (MINI-012)

| asset_id | asset_name | source | licence | purpose |
|---|---|---|---|---|
| MINI-AST-120 | TomatoPlant_LOD.asset / WeedPlant_LOD.asset | Decimated from `E:\Unity\Up iz up\Assets\Imported Plants\{Tomato,Weed} plant.fbx` | Same ownership as the larger project (user's own project; original scan provenance not recorded there either — worth confirming before release) | Realistic crop visuals. **The sources are ~2,000,000-triangle photogrammetry scans (183MB each), single submesh, no growth stages — unusable directly on a mobile target and never actually referenced in the larger game's scene.** Reduced to 3,259 / 5,561 triangles (0.16% / 0.28%) with `MeshDecimator.cs` (vertex-clustering), preserving stem/leaf/fruit silhouette. Verified by render: still clearly reads as a tomato plant with fruit, and a cannabis plant. The 366MB of source FBXs were deleted after conversion; only the 282KB of decimated meshes are in the repo. |

## User-generated character candidate (MINI-105)

| asset_id | asset_name | source | licence/provenance | purpose |
|---|---|---|---|---|
| MINI-AST-121 | Sacat Modular Base Rigged | User-generated Hitem3D download `C:\Users\PCSS-PC\Downloads\Hi3D_Untitled_allparts_20260820_223739.glb`; rigged locally with AccuRIG Free; cleaned/staged in Blender | Created under the user's paid Hitem3D subscription from the user's approved Sacat reference workflow; local derivative and AccuRIG export records retained in `Docs/CharacterPipeline/MINI-105/` | Isolated canonical body candidate: white vest, black boxer pants, barefoot, 101-bone full-finger Humanoid. The immutable 100k master now has derived 25k/12k/4.5k mobile LOD candidates under `Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/`, with one material, four influences and a validated LODGroup. Playable Sacat remains unchanged until static and motion approval. |

## Explicitly rejected

- Adventurer Character, Fighter Character, Warriors And Commoner (local `E:\Assets`) — fantasy/medieval sword-fighter aesthetic, wrong genre.
- `Grandbay entire.jpg` and `heightmapper-*.png/.raw` — explicitly marked "do not ship" / "not ready for direct Unity import" in `GrandBayReference`'s own assessment docs. Reference only, never imported as a texture/heightmap asset.

## Not yet sourced (blocking gaps)

- No local or acquired pack matches "landmark buildings" (school, church, credit union) or full multi-room village houses closely enough — main housing stock will be hand-built modular pieces (wall/roof/door/window kit), dressed with Shanty Town 1's genuine textured props/small structures where they fit.
- Cartoon Farm Crops (MINI-AST-105) is the only crop-specific asset acquired; still needs a URP material test per the user's own brief before trusting it for the tomato growth stages.

## Building-source conclusion (2026-08-15)

Hybrid, per user direction: **Shanty Town 1's small structures/props** (genuinely good, textured, tropical) for market stalls, lean-tos, and yard dressing; **hand-built modular houses** (wall/roof/door/window kit, explicitly sanctioned by the corrective brief) for the main housing stock along the Lalay road, since nothing tested so far (Demo City, POLYGON Starter Pack, Shanty Town 2) gives full textured Caribbean houses. This is the plan Phase B will build against.
