# Characters & Animation
## MINI-146 — watch integration ledger (2026-09-07)

Proven: CharacterEquipment uses watchPrefab + per-character watchPlacement exactly like approved chain placement, without touching chains. Approved MINI-145 fits locked by user. Hidden-item set separates ownership from wearing; CaptureWardrobe/RestoreWardrobe enable future slots without a new economy. Current UI supports watch only. Canonical scene patched references only; all transforms/profile hashes guarded. Editor removal must use DestroyImmediate on instances outside Play Mode; runtime uses Destroy. Purchase screenshots Logs/Tasks/MINI-146 show actual approved prefab. Do not re-fit or regenerate characters. Full wardrobe/body LOD repair and live animation clipping are not signed off.


## Current state

MINI-145 round gold watch is an isolated Unity candidate: 1644/788-triangle LODs, one atlas material, independent Sacat/Franki cuff profiles and fixed screenshots. Original characters and chains untouched; no purchase integration. See Docs/WorkPackets/MINI-145.md and VA-009 (PROPOSED). Clothing/body work remains unfinished.

2026-09-07: User accepted MINI-143 gameplay and explicitly resumed wardrobe production. MINI-144 isolated base-body audit and Blender gold-watch concept are evidence-ready; no playable swap. See Docs/WorkPackets/MINI-144.md. Target one polo/jeans/trainers/cap/gold-watch capsule and existing chain, separate saved outfits, safehouse changing. User asks to match original clothed-character quality/approach. The previous mobile motion gate remains failed until re-proven.

Sacat and Franki are switchable protagonists with separate vitals/positions and shared economy/progression. MINI-107 (modular Sacat production - Hitem3D generation, AccuRIG rigging, mobile LODs) is **paused mid-task**: the original 100k rig animates correctly and remains the playable character; the derived 25k/12k/4.5k mobile LODs pass static checks but fail motion deformation and are NOT approved for playable integration. Do not resume this without the user explicitly requesting character production again, and repair the LOD motion deformation before any playable swap.

User-approved manual placements are visual locks - e.g. Sacat's and Boss C's two-piece chain placement (`VA-001`/`VA-002`-style register entries in `Docs/VISUAL-APPROVAL-REGISTER.md`). Never recalculate or replace an approved transform; treat it the same way `MapGeneration.md` treats manual world placement - authoritative, bug-hunt elsewhere first.

## Architecture

- `Assets/UpIzUpMini/Scripts/Character/HumanoidAnimationManager.cs` - the reusable action-layer foundation EVERY system with character animation depends on (Combat, Vehicles). See `Combat.md`'s "what didn't work" entry - this component's own `actions` list is NOT what actually plays at runtime; the shared `StarterAssetsThirdPerson.controller` asset is.
- Character production pipeline (MINI-105/107): Hitem3D generates candidates -> AccuRIG (free, local) auto-rigs -> Blender owns topology/UVs/skeleton/weights/bind pose/export. `Docs/CharacterPipeline/System/` and `Tools/CharacterPipeline/` hold the manifest system. `Docs/CHARACTER-PRODUCTION-WORKFLOW.md` is mandatory reading before touching this - read it in full, this file is a ledger, not a substitute.
- One canonical full-finger Humanoid body per wardrobe family; clothes are separate skinned geometry sharing the same skeleton/bind pose, hiding covered body regions; accessories use named per-character attachment profiles.
- Animation retargeting: any real Mixamo/motion-capture Humanoid clip retargets automatically onto ANY valid Humanoid avatar via Mecanim - confirmed repeatedly working this session (see `Combat.md`) - no per-character remapping needed as long as both source and target are genuinely Humanoid.

## What worked / what didn't

- **MINI-145 whole-wrist clarification:** User meant whole accessory orbit, not dial roll. Apply rotation around bone's forearm axis to BOTH attachment position and orientation, retaining axial distance/scale. Updated Sacat/Franki profiles by+90 localY and captured new Side.png views. No mesh changes. BuildCapture preserves these profiles; RotateAroundWristOnce is a one-shot user revision, not the normal rebuild entry point.

- **MINI-145 rotation revision:** User approved design and requested90 degrees clockwise. Rotate case/dial/crown geometry only, not entire bracelet attachment; keeps band's fit and separate profiles identical. Captures verify crown at right; budgets and attachment checks unchanged. Generator preserves this orientation for future rebuilds.

- **MINI-145:** Rebuilding the approved watch silhouette as deliberately low-poly pieces (rather than decimating each tiny high-poly part) reduced 29,800 to1,644 triangles while retaining circular case, hands, markers and bracelet; 788-triangle distant mesh, 32x8 colour/metallic atlas, one shared Standard material. Fit profiles on LeftLowerArm preserve original character scale. Existing long sleeves require over-cuff preview fitting; moving toward the hand hides the dial. Never auto-recompute approved profiles: proof tool now preserves stored values. 36 sampled idle/walk/run frames prove only attachment stability, not live clipping-free motion. Gameplay integration waits for screenshot approval.

- **MINI-144:** Original and mobile FBXs have no missing/non-normalized weights; mobile has <=4 influences. Isolated Blender bone perturbations are similar across all four meshes, so do not assume a new retopology fixes the Unity-specific tearing. Next inspect Unity LOD bone bindings/bindposes with matched motion proof. Compare evaluated rest/posed data, not raw vertices against evaluated world-space vertices.
- **MINI-144:** Existing CharacterEquipment provides individual ownership, colour-only garments and primitive cap/watch; preserve chain profile exactly. New original Blender watch concept is preview-only (29.8k evaluated triangles/144 parts); bake/merge/LOD after visual approval, never import this heavy preview directly. Evidence: Logs/Tasks/MINI-144/Gold-Watch-Preview.png. No credits spent.

- **(MINI-107, unresolved) The 25k/12k/4.5k mobile LODs fail motion deformation despite passing static checks.** A static pose/silhouette check is NOT sufficient proof a decimated rig is safe to ship - it must be watched moving before being approved. This is the same lesson as `Combat.md`/`Vehicles.md`'s repeated "batch/static checks can pass while real behaviour fails" theme, here applied to mesh decimation instead of code.
- **(2026-08-15) Local scan-derived meshes were often absurdly oversized for mobile** (~2,000,000-triangle photogrammetry scans, 183MB each) with no growth-stage variants - decimated to ~0.2% of original triangle count via vertex-clustering while still visibly reading correctly, then the multi-hundred-MB sources were deleted, keeping only the small decimated result.
- **(2026-08-28, this session) Real motion-capture clips retarget cleanly onto this project's Humanoid characters with zero extra rigging work** - confirmed via direct rendered comparison (`Combat.md`'s Mixamo jab-punch fix). This is a strong argument for sourcing new character ANIMATION from real mocap (Mixamo, ActorCore, similar) rather than hand-keyframed free packs, which have repeatedly looked wrong (see `Combat.md`'s cartoonish-pack lesson).

## Open items

- MINI-107 LOD motion-deformation repair - paused, not resolved. Do not integrate the failed LOD candidates.
- TMAX see-through/oversized-payload issue is arguably a Characters/asset-pipeline problem more than a Vehicles one - see `Vehicles.md`'s open items, listed there since it's vehicle-specific, but the underlying cause (normals/backface holes, texture import settings) is the same class of problem as character asset production.

## Key files

- `Assets/UpIzUpMini/Scripts/Character/HumanoidAnimationManager.cs`
- `Docs/CHARACTER-PRODUCTION-WORKFLOW.md` (mandatory before any character/wardrobe/rig work)
- `Docs/CharacterPipeline/System/`, `Tools/CharacterPipeline/`
- `Docs/VISUAL-APPROVAL-REGISTER.md` (locked placements - never recalculate)
- `Docs/ASSET-REGISTER.md` MINI-AST-121 (Sacat Modular Base Rigged, the MINI-107 candidate)
