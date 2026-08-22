# MINI-105 — Sacat modular base asset card

## Approved target

- Identity: Sacat, preserving the recognizable face of `Assets/UpIzUpMini/Art/Characters/Mainchar.fbx`.
- Appearance: short black haircut with a sharp shape-up; no cap or headphones.
- Base garments: fitted white ribbed vest and fitted black boxer pants.
- Base footwear: barefoot so shoes remain modular.
- Pose: neutral T-pose for inspection and rig transfer.
- Approved reference: `Approved/Sacat-ModularBase-Approved.png`.

## Intended use

Create one canonical production base for modular shirts, trousers/shorts, shoes, hats, chains, watches, and shades. The generated candidate is a donor mesh only. Blender owns final topology, UVs, proportions, skin weights, bind pose, and export. The current playable Sacat is not replaced until deformation and visual gates pass.

## Mobile budget

- Final LOD0: 20,000–25,000 triangles including body, vest, boxer pants, and hair.
- LOD1: approximately 50% of LOD0; LOD2 approximately 15%.
- Textures: one 1024 atlas preferred; maximum two 1024 atlases if the face requires separation.
- Materials: two preferred, three maximum.
- Skinning: four bone weights per vertex maximum.
- Skeleton: reuse the existing valid Humanoid/Mixamo-compatible full-finger skeleton.

## Generation budget

- First candidate: lowest useful Hitem3D tier, target five credits or less.
- Stop after the first candidate for visual and topology inspection.
- Do not exceed fifteen subscription credits total without a new user decision.

## Acceptance gates

1. Exact Sacat identity remains authoritative; generated faces are discarded if they drift.
2. Body silhouette matches the approved image without exaggerated muscles.
3. Vest and boxer pants are modest, fitted, and separable from future clothing.
4. Clean shoulder, armpit, hip, crotch, knee, ankle, hand, and foot deformation.
5. No visible clipping in idle, walk, run, jump, punch, and seated-bike test poses.
6. Unity Humanoid mapping, mobile budgets, and fixed-camera screenshots pass before integration.
