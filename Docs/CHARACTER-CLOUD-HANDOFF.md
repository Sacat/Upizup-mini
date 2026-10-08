# Character design handoff (for a cloud or fresh Claude session)

Written 2026-10-08. Read `AGENTS.md`, `CLAUDE.md` and `Docs/Systems/Characters.md` first, then this file. Goal of the cloud work: neaten and improve the look of Sacat (and later Franki) while the local machine keeps ownership of Unity, scenes and builds.

## What a cloud session can and cannot do
- It can read the repo, edit C# and Blender Python, and write docs. It CANNOT run the Unity Editor (no licence/GUI), so it cannot regenerate wardrobe assets, render Play Mode proofs or build the exe.
- If Blender is available in the sandbox (`pip install bpy` on Linux), it can model and render in Blender. Treat that as the design lane: produce FBX/blend files plus renders, commit them, and let the local machine integrate and validate in Unity.
- Never edit `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` or other scenes from the cloud. Scenes, prefabs and generated meshes under `Art/Characters/Garments/Outfits166` are produced by local Unity batch tools only.

## What exists now (Sacat)
Sacat's original model (`Visual/Ch06`, 31.8k verts) has a hoodie and shorts baked in and NO bare torso, neck or arms. The wardrobe (MINI-166) replaced it with separate pieces. Current state after MINI-196..200:
| Piece | How it is made | Tool |
|---|---|---|
| Head, hands, hair | original Ch06 faces (head-bone weighted plus x>0.672) | `Mini166Repair.BuildCharacter` |
| Shirt (polo/tee) | ORIGINAL Ch06 upper-torso faces, hood collapsed to a polo neckline, clean upper-back yoke over the torso skin | `Mini166RepairClothes.cs` `Shirt()` |
| Bare arms | ring sweep along his arm bones, anatomical sections, skin gradient shoulder to wrist | `Mini198SacatArms.cs` |
| Torso + neck | ring sweep with trapezius slope and shoulder yoke, skinned to spine/neck/head/shoulder | `Mini199SacatTorso.cs` |
| Trousers, shoes | still Franki's garments retargeted to Sacat's bones | unchanged |
Run all Sacat pieces locally with: `Unity.exe -batchmode -quit -projectPath <project> -executeMethod UpIzUpMini.EditorTools.Mini166Repair.BuildSacatTorso` (opens `GrandBayProof_HouseEnhance.unity` unless env `MINI196_SCENE` is set; rebuilds shirt, arms and torso). Inspection: `Mini196SacatPlayRender` (Play Mode renders, env `MINI196_ZOOM`, `MINI196_CY`, `MINI196_DIST`), `Mini196Measure` (silhouette numbers), `Mini199SkinTone` (texture colour samples).

## Facts that matter for any redesign
- Skeleton rest (bind space, metres): shoulder joint x 0.22, elbow 0.43, wrist 0.65 (T-pose), neck y 1.456, Spine2 y 1.291. Bones are found by name suffix (`:LeftArm`, `:Spine2`), so a replacement rig must keep those names or the lookup must be generalised.
- Original silhouette numbers are in `Docs/WorkPackets/MINI-197-refs/Sacat_original_measurements.txt`; original arm radius is about 0.05 m (near-circular).
- Skin tones in the original texture: face (0.34, 0.23, 0.21), forearm (0.48, 0.31, 0.24), hand (0.57, 0.36, 0.30). The hands are lighter than the face; this is original and currently left as is.
- Mobile budget: body LOD0 at most about 20k triangles.

## User preferences (from many rounds)
- Wants natural, realistic proportions: no square or straight shoulders, no inward dip at the triceps, no awkward bends. "More natural" means closer to the original model's silhouette.
- Show renders (front, side, back, three-quarter) after each piece and build piece by piece: arms, then torso and neck, then legs.
- Long-term plan: rebuild the bare body with Hitem3D then rig with Reallusion AccuRig (`Docs/WorkPackets/MINI-197.md`). Needs the user for logins.
- Do not rerun historical whole-scene builders (MINI-011/095). Never type passwords. Never touch the live scene without approval.

## Current rough spots (next work)
1. Hands are lighter than arms/face (decide: tint hands or accept).
2. Small jagged edges where the sleeves meet the back yoke; polo front collar leaf is large and flat.
3. Legs/trousers not yet rebuilt; trousers still Franki-based.
4. Franki has not had any of this treatment; his arms and shoulders likely have similar problems.

## Evidence images
`Docs/WorkPackets/MINI-197-refs/current/` (latest renders) and `Docs/WorkPackets/MINI-197-refs/` (original model front/side/back).

## Hand-off to the local machine
Cloud work should land as a branch with: Blender source files, exported FBX, renders, and a short note of what changed. The local session then imports, runs the Unity validators, and builds.
