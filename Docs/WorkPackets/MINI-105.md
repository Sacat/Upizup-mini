# MINI-105 — Sacat modular-character visual proof

Owner: Released (Codex)

## Goal

Use the existing Mixamo Sacat (`Strong.fbx`) as the identity and quality reference, produce clean multi-view evidence, and prepare one user-approved canonical boxer-body concept before any Hitem3D credits or gameplay integration.

## Non-goals

- No replacement of the playable Sacat model in this packet.
- No change to VA-002 chain placement or other user-locked accessories.
- No paid Hitem3D generation before screenshot approval.
- No mass wardrobe generation.
- No scene, player prefab, Animator controller, chain, or gameplay integration; Unity work is limited to an isolated import-validation staging asset.

## Acceptance

- Actual Sacat FBX is rendered from useful clean angles.
- Concept preserves Sacat identity, realistic proportions, skin, hair, and accessory quality.
- Boxer-base concept is shown to the user for explicit yes/no approval.
- Source prompts, evidence, and limitations are recorded.
- The production rigging tool decision and full-finger runtime contract are recorded before further skin-binding work.
- A Unity-targeted FBX, external 2K base-color texture, material, and isolated Humanoid import-validation prefab are staged without replacing playable Sacat.

## Current decision

The first three Blender-only skin-binding experiments failed their deformation gate and remain rejected. Use AccuRIG 2 Free for the next five-finger bind, Blender for cleanup/retopology/weight correction, and the Mini project's existing Unity Animation Rigging package for wrist contact targets. See `Docs/CharacterPipeline/MINI-105/RIGGING-TOOL-RESEARCH.md`.

## Reserved files

- `Docs/CharacterPipeline/MINI-105/`
- `Docs/WorkPackets/MINI-105.md`
- `Assets/UpIzUpMini/Art/Characters/Modular/Sacat/`
- `Assets/UpIzUpMini/Editor/Mini105SacatModularImport.cs`
- `Docs/ASSET-REGISTER.md`
- `Docs/CURRENT.md`
- `TASKS.md`
- `CHANGELOG.md`

## Result and evidence

Status: Evidence ready; playable Sacat intentionally unchanged.

- Source candidate: `C:\Users\PCSS-PC\Downloads\Hi3D_Untitled_allparts_20260820_223739.glb`.
- Production rig: AccuRIG Unity export with 101 deform bones and full five-finger chains.
- Unity staging: `Assets/UpIzUpMini/Art/Characters/Modular/Sacat/`.
- Unity audit: `Logs/Tasks/MINI-105/Sacat-UnityImport-Audit.json`.
- Front/back proof: `Logs/Tasks/MINI-105/Sacat-UnityImport-front-three-quarter.png` and `Sacat-UnityImport-back-three-quarter.png`.
- Unity result: valid Humanoid; fingers 30/30; 62,515 vertices; 100,000 triangles; four maximum influences per vertex; one 2K material texture.
- Remaining gate: make mobile LODs and visually test deformation before replacing the gameplay character. No playable prefab, scene, Animator or approved accessory placement changed.
