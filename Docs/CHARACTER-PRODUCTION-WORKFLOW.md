# Up Iz Up Mini — reusable character system

This is the canonical low-cost route for every playable character and important NPC. The per-character JSON manifest is the system of record; never rely on memory or silently reuse Sacat-specific offsets.

## Fast repeatable route

1. **Reference lock** — approve front, back, side and three-quarter views before spending Hitem3D credits. Record the visual approval ID and license/source in the manifest.
2. **Canonical body** — create one neutral character in vest/boxers or body-safe base clothing. Keep the same scale, A/T pose, proportions and bind pose for a wardrobe family. Identity and user-approved manual work are locked.
3. **Blender cleanup** — use metres, feet at ground, clean topology/UVs, one canonical full-finger Humanoid skeleton, and no more than four bone influences. Clothing is separate geometry weight-transferred to this body; do not regenerate a fully clothed person for every outfit.
4. **Mobile derivation** — run `Tools/CharacterPipeline/Invoke-CharacterPipeline.ps1`. The manifest supplies LOD targets, texture cap and Unity paths. The approved Sacat baseline is 25k/12k/4.5k triangles, one material and 1024 Android texture max.
5. **Unity static gate** — validate a valid Humanoid, all 30 finger joints, weights/material/triangle budgets and upright fixed-camera screenshots. Keep axis correction on `VisualRig`; the gameplay wrapper remains position/rotation/scale identity.
6. **Motion gate** — sample owned idle/walk/run/jump/fist/grip clips on an isolated proof stage. Inspect the body and close hand evidence. Reject warped shoulders, knees, wrists, fingers, foot sliding, ground penetration or a 90-degree orientation error.
7. **Wardrobe/accessories** — garments share the canonical skeleton and bind pose. Hide covered body regions. Store chains, hats, watches, glasses and bike contact offsets in named per-character profiles. A user-approved manual transform is authoritative.
8. **Playable integration** — only after explicit visual approval. Swap through a character definition/prefab adapter, verify locomotion, bike, combat, camera, interactions and save compatibility, then run a mobile build gate.

## Automation contract

- `Docs/CharacterPipeline/System/CharacterManifest.schema.json` defines required data.
- `Docs/CharacterPipeline/<TASK>/<Name>.character.json` is the reusable character record.
- `Tools/CharacterPipeline/build_character_lods.py` generates derived LOD FBXs without changing the master.
- `Tools/CharacterPipeline/Invoke-CharacterPipeline.ps1` runs Blender and then Unity validation.
- Unity proof tools write evidence beneath `Logs/Tasks/<TASK>/`.

The wrapper stops at proof by default. `gates.playableIntegration` must remain `false` until the user approves motion evidence. This keeps character production fast without allowing an automated tool to overwrite a character, accessory or placement the user already approved.
