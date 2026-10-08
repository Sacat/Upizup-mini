# MINI-197 bare bodies — note for the local Unity machine

Cloud design lane, branch `cloud/sacat-legs` (2026-10-08). Blender work only: no scene, prefab or
generated garment asset was touched. Nothing here has been opened in Unity yet.

## What to import

Use the **final** files; stages 01–04 are kept as history only.

| Character | FBX | Base colour | Blender source |
|---|---|---|---|
| Sacat | `04-NeckTorso/SacatBare_04NeckTorso.fbx` | `04-NeckTorso/SacatBare_04NeckTorso_BaseColor.png` | `04-NeckTorso/SacatBare_04NeckTorso.blend` |
| Franki | `05-Franki/FrankiBare.fbx` | `05-Franki/FrankiBare_BaseColor.png` | `05-Franki/FrankiBare.blend` |

Copy them to an isolated candidate folder, e.g. `Assets/UpIzUpMini/Art/Characters/Modular/Bare/`.
Do not replace the live Sacat/Franki or edit `GrandBayProof.unity` until the checks below pass.

Each FBX holds one armature (101 `CC_Base_*` bones, AccuRig names unchanged) and six skinned
meshes sharing one material: `<Name>Bare_Head`, `_Torso`, `_Arms`, `_Hands`, `_Legs`, `_Feet`
(20,000 triangles total, 4 bone influences max, no unweighted vertices).

Suggested import settings:
- Model: Scale Factor 1, Convert Units on; Normals **Import** (custom normals keep the piece seams invisible).
- Rig: Humanoid, Create From This Model.
- Texture: sRGB, Max Size 1024 for mobile (source is 2048).

## What changed from the MINI-105 AccuRig donor

1. **Legs/hips**: boxers removed as geometry. Knee-to-hip cross-sections are within 0–7 mm of the donor
   (`03-Hands/leg_sections_vs_donor.txt`); only the removed groin bulge differs.
2. **Skin tone**: arms were lighter than face and legs; the large-scale tone is evened out, detail kept.
3. **Hands**: the donor fingers were fused and the **hand rig was one finger off** (Index chain in the
   thumb, no bones in the pinky, Thumb a 13 mm stub). New separated fingers; the **30 finger bones were
   moved** into the right digits. No other bone moved (max 0.001 mm).
4. **Torso/neck**: vest removed as geometry. The torso keeps its volume (about 10 mm of girth = fabric).
5. **Rig frame fix**: in the MINI-105 FBX (as Blender imports it) the armature object was rotated 90° X
   against its upright mesh, so posing any bone tore the skin. The armature object is now a plain ×0.01
   scale, with the meshes parented at identity. All pose tests ran on a plain re-import of the exported FBX.
6. **Franki**: the same body and rig with his warmer skin tone (target sRGB 0.50/0.32/0.20, from the Ch28 face).
   The face is the same as Sacat's; hair is meant to come from his hair piece.

Generator: `Tools/CharacterPipeline/mini197_bare_body/build_bare_body.py`
(`python build_bare_body.py -- <donor.fbx> <basecolor.png> neck <out_prefix> [r,g,b skin target]`, with
env `BODY_NAME=Franki`). Checks: `verify_fbx.py`, `pose_hands.py`, `legs_inspect.py`, `leg_measure.py`, `torso_measure.py`.

## What to check in Unity

1. **Orientation and scale**: stands upright, about 1.85 m tall, facing +Z, feet on the ground. This is the
   first thing the rig frame fix could get wrong in Unity.
2. **Avatar**: Configure Avatar passes with no red bones; the T-pose looks right; **finger mapping** is
   Thumb/Index/Middle/Ring/Little → `CC_Base_*_Thumb/Index/Mid/Ring/Pinky 1-3`, each on the correct digit.
3. **Animations**: idle, walk, run, punch, wheelie ride. No candy-wrapper twists at elbows, knees or wrists.
4. **MINI-190 pistol**: aim and reload with the procedural finger curls. Curl direction and the trigger
   finger should now match the real digits. In Blender, −X on the finger bones curls toward the palm.
5. **Wardrobe**: `Mini166Repair` finds bones by Mixamo suffix (`:Hips`, `:LeftArm`), but these are
   `CC_Base_*`. Garments will not attach until bones are renamed on import or the lookup is generalised
   (MINI-197 step 6). This is a code decision for the local session.
6. **Look in game light**: skin tone of both characters next to each other, hands vs arms, no visible
   outline where the vest or boxers used to be.

## Known limits (not fixed)

- No fingernails. The fingers are clean tubes at 1.8k triangles for both hands.
- The bare chest and back are smooth filled skin with fine variation and subtle areolae (user: good enough).
- One faint corner on the pinky side of each knuckle line.
- No LOD1/LOD2 yet (budget about 8k / 3k triangles); the same generator can produce them with `BODY_TRIS`.
- Franki and Sacat share a face.

Evidence renders: `0x-*/renders/`.
