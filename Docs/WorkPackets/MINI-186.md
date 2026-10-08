# MINI-186 — ImageGen and Hitem3D tool model

```yaml
task_id: MINI-186
title: Replace the temporary Lalay Tool model with an ImageGen-led Hitem3D asset
request_owner: User
integrator: Codex
status: implemented; built; live aim-pose review pending
approval_class: C
external_credits: one Hitem3D generation attempt unless user authorizes another
stop_condition: one reviewed candidate, imported at the correct scale and pivot in GrandBayProof, visual proof and Windows build
reserved_files:
  - Assets/UpIzUpMini/Art/Weapons/Mini186/
  - Tools/ArtPreview/Mini186Source/
  - Assets/UpIzUpMini/Editor/Mini186*.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Docs/Systems/Combat.md
  - Docs/CURRENT.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
  - Assets/UpIzUpMini/Art/Environment/Mini182/
```

## Asset card

Purpose: replace only the temporary `Mini183_Sidearm` visual meshes. Fictional, compact low-poly Caribbean game sidearm called the Lalay Tool, with a clear slide, barrel and hand grip; no real manufacturer or markings. ImageGen produces a clean visual reference for Hitem3D. Target length 0.19 m, grip pivot at the right hand and barrel along Unity local +Z. One rigid asset, no deformation, at most one 1024px texture set and one material; the initial 1,500-triangle budget was treated as a target, and the accepted PC mesh is 2,998 triangles to preserve the trigger guard and slide cuts. The gameplay ray and NPC damage remain driven by existing code, so no gun collider is needed. One Hitem3D generation attempt was authorized and used.

## Acceptance

- [x] ImageGen concept saved at `Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool-ImageGen-Concept.png` and inspected (1536x1024 RGBA, transparent background).
- [x] Hitem3D asset generated, downloaded and provenance/points recorded.
- [x] Asset inspected, scaled, pivoted and imported without changing the protected scene.
- [x] In-engine static render, scene verification and Windows build pass.
- [x] Source-image and exported-mesh measurements, topology, materials, orientation and cleanup decisions recorded below.

## Generation record

Initial ImageGen prompt: "Use case: stylized-concept. Asset type: source concept for a low-poly 3D game prop to be generated in Hitem3D. Create ONE isolated fictional compact sidearm called the Lalay Tool, inspired by stylized Caribbean open-world game art. Clean three-quarter product view, entire object fully visible, barrel pointing toward upper right, grip toward lower left. Strong readable silhouette with a dark graphite slide, muted steel barrel tip, and a simple warm dark-brown grip panel. Low-poly faceted surfaces, game-ready proportions, about 19 cm long in-world. No real brand, no logos, no lettering, no serial numbers, no accessories, no hands, no ammunition, no environment. Crisp edges and even neutral studio lighting. Genuine transparent background. This image will be supplied to image-to-3D generation, so avoid dramatic perspective, motion blur, shadows, and reflections."

On 2026-10-02 the user requested a more modern, Glock-like visual direction. A second ImageGen concept was saved at `Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool-Modern-Concept.png` (1536x1024 RGBA, transparent background). This was the image-to-3D source: original fictional polymer-frame profile, black/charcoal body and a restrained teal line, without real brand markings.

On 2026-10-02, the user uploaded `LalayTool-Modern-Concept.png` into the signed-in Hi3D Chrome tab. The preview visually matched the modern polymer-frame concept. I started one Quality v3.0 Geometry + Stylized Texture generation under Private license. Hi3D charged 65 credits: balance changed from 1,040 to 975. The task entered the queue with two tasks ahead. The user uploaded manually because the ChatGPT Chrome extension still reported that local file access was unavailable.

## Measurement and modeling notes

The user asked for a reusable record of how this generation behaves. The site exposed v3.0 Quality, Geometry + Stylized Texture, Private license, a single-file FBX export and the 65-credit cost. The service's internal model and training process are not observable from the export; the steps below are measured and reproducible local processing, not a claim about its private algorithm.

1. Save a single clean 1536x1024 transparent ImageGen concept with an entire silhouette and one clear material scheme. The generated model copied the squared slide, textured grip and teal line, but invented tiny unreadable marks on the slide. Inspect both sides before acceptance.
2. Export FBX from Hi3D and preserve it outside Unity `Assets` at `Tools/ArtPreview/Mini186Source/Hi3D_LalayTool_Modern_raw.fbx` (89,998,512 bytes). Blender 5 inspection (`inspect_hi3d.py`) measured one mesh with 994,171 vertices, 1,988,434 triangles, one UV layer, one material and one embedded 8192x8192 texture. Its source bounding box was about 0.00138 x 0.01000 x 0.00627 Blender units; the export's nominal scale was not suitable for gameplay.
3. Run `build_game_mesh.py`: apply a 19x scale to reach the 0.19 m game length, decimate to approximately 3,000 triangles, remove an isolated three-vertex sliver, and downsample the texture to 1024x1024. `check_topology.py` confirmed the final mesh has 1,451 vertices, 2,998 triangles, one connected component, zero boundary edges, zero nonmanifold edges and zero loose vertices. Final bounds are 0.0260 x 0.1187 x 0.1899 m. The game FBX is 110,716 bytes and texture 1,015,973 bytes. `render_game_mesh.py` saved side, reverse and three-quarter renders; the broad silhouette and details remain readable. This PC budget exceeds the initial 1,500-triangle target; a lower mobile LOD would be a separate quality pass.
4. The exported model's muzzle is local +Z and grip is toward -Z. `Mini186ToolModelSetup.Apply` puts its visual root at `(0, 0.03, 0.055)` relative to the existing weapon holder, and moves `ShotOrigin` to `(0, 0.055, 0.165)`. It replaces only the three primitive visual parts for both player characters and refreshes `FirearmController.heldWeaponRenderers`; firearm input, damage, ammo and mission code are unchanged.
5. The first Unity render exposed a material mapping mistake: this project resolved `Standard`, so `_BaseMap`/`_BaseColor` had no effect. Mapping the 1024 texture through `_MainTex` and tint through `_Color` produced the dark textured tool. Future prop importers should check the actual shader before choosing property names. The static hand render is an editor rest-pose check, not proof of the runtime aiming hand pose.

Evidence: `Tools/ArtPreview/Mini186Source/raw_inspection.json`, `game_mesh_report.json`, `topology_report.json`, `LalayTool_three_quarter.png`, and `Logs/Tasks/MINI-186/Unity-Held-Preview.png`. `Mini186ToolModelSetup.Verify` passed for both players with no placeholder cubes and the final <=3,000-triangle budget. Windows build passed at `Builds/GrandBayProof/UpIzUpMini.exe` (reported total 442,474,501 bytes in `Logs/Tasks/MINI-186/build-final.log`); updated `UpIzUpMini_Data` content is timestamped 2026-10-02. The protected `GrandBayProof_HouseEnhance.unity` SHA-256 remained `CF0E4E8A06140F3DA0B65989F9C791D77E80590E9A4F9CF748E5DC242ED07CF0`.

Remaining review: run the EXE interactively, acquire the tool mission, aim, shoot and inspect the right-hand grip alignment from player view. The static editor render shows the model near a resting hand, but cannot validate the animated aim pose or player feel.
