# MINI-197: Rebuild Sacat's body (Hitem3D, then Reallusion rig) - brief and checklist

```yaml
task_id: MINI-197
title: New bare Sacat body from Hitem3D, rigged with Reallusion AccuRig, matching his original proportions
request_owner: User ("we can maybe use hitem3d to rebuild the character and reallusion to rig it")
integrator: Claude (prep, validation, integration); User (account steps: Hitem3D generation, AccuRig/ActorCore login and export)
status: brief ready; waiting on the Hitem3D input images and a first generation
approval_class: B
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
```

## Why
- The current wardrobe Sacat wears Franki's garment shape with extra shoulder/arm/chest forms (MINI-166). MINI-196 refitted the shirt torso to his original silhouette, but the bare body underneath does not exist: only the original arms, hands and head are real skin, and the torso and legs are hollow under the clothes.
- User complaint: the arms have an awkward shape, the triceps go inward (the back of the upper arm is concave). Original arms are convex front and back. This must be an explicit acceptance check.
- A real bare body that matches the original lets every shirt, trouser and shoe sit on a proper form.

## Pipeline
1. **Reference set (Claude prepares).** Clean front / side / back of the original Sacat are in `MINI-197-refs/` (idle pose, plain background). Hitem3D works best from a T- or A-pose, neutral light, plain background, bare arms and legs, hands open, no headphones. The original has a hoodie and shorts baked in, so the user should create a bare-body concept (or edit the original renders) in A-pose before generation. Numbers to match are in `MINI-197-refs/Sacat_original_measurements.txt`.
2. **Hitem3D (user).** Multi-view input (front, back, left, right) gives the best shape. Hi3D V3 exports GLB/OBJ/FBX/STL and a target face count (100k to 2M). Generate the body WITHOUT clothes, hair on a separate pass if possible, head with the face kept. Export FBX/OBJ plus the texture.
3. **Retopology (Claude, Blender).** AI meshes are dense and messy. Target for mobile: body at most 20k triangles (LOD0), about 8k (LOD1). Clean edge loops at shoulder, elbow, wrist, hip, knee. Separate fingers (AccuRig needs distinct fingers). Re-project the textures. Check the arms are convex front and back.
4. **Reallusion AccuRig (user).** Free download for Windows; import FBX/OBJ, auto-rig (19-joint body rig plus fingers), export with the Unity preset (needs a free ActorCore account). Only the user can log in or create accounts.
5. **Unity import (Claude).** Humanoid avatar, mapping check, existing animations (the project uses Humanoid clips, so they should transfer), then run the pistol pose (FirearmPose) and ride/wheelie poses as a regression.
6. **Clothes (Claude).** The wardrobe tools find bones by the Mixamo-style `:Hips` suffix, but AccuRig bones are named `CC_Base_*`. Either rename bones on import or generalise the wardrobe lookup to Humanoid bones. Then refit shirt, trousers and shoes onto the new body (same per-height silhouette fit as MINI-196, now against a real skin body).

## Acceptance checks (all measured, with renders)
- Height, shoulder width, chest/waist/hip width and depth within 1.5 cm of the original table.
- Arms: upper-arm cross-section convex at the triceps and biceps (side-view profile has no inward dip); no pinching at the elbow in a 90-degree bend; wrist twist does not collapse.
- Skinning: walk, run, punch, wheelie ride and pistol aim/reload renders with no candy-wrapper twists.
- Fingers: all five separate; pistol grip pose works.
- Budget: LOD0 at most 20k triangles, LOD1 about 8k; one material per skin/eyes/hair.
- Existing outfits still swap with no clipping through the skin.
- Original Sacat remains available (the model and wardrobe assets are in git) until the user approves.

## Risks
- Generated hands and faces are often fused or soft; the face and head are best kept from the existing original head if Hitem3D loses likeness.
- Licence terms for Hitem3D and Reallusion output should be checked by the user for commercial use.
- Every body needs its animation set validated; allow time for rig fixes.

## Next step
User supplies the A-pose bare-body concept images (or approves using edited renders from `MINI-197-refs/`) and runs a first Hitem3D generation. Claude then runs the import, retopology and measurement checks.

## Sources
- Hitem3D Hi3D V3 multi-view: https://wavespeed.ai/docs/docs-api/hitem3d/hitem3d-hi3d-v3.0-multi-view-to-3d
- AccuRig: https://gamefromscratch.com/accurig-auto-rigging-tool-from-reallusion/ and https://manual.reallusion.com/AccuRig-2/2.0/09-add-motions/export.htm
