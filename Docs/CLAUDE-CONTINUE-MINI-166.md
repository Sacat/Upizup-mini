# Claude continuation: MINI-166 fitted selectable 3D outfits

## User request and honest status

Verified checkpoint: `Logs/mini166-build.log` reports BUILD SUCCEEDED, total build size 411,132,245 bytes. `Logs/mini166-smoke.log`: 15-second headless startup stayed running, no Exception/Error/Crash matches; repeated kinematic-body angular-velocity warnings. The EXE itself is 667,648 bytes (Unity launcher); the game data lives beside it. Outfit acceptance is still outstanding.

Continue implementing fitted, selectable 3D clothing for BOTH Franki and Sacat. User approved: Lacostes/Lacos polo, Mike crew tee, jeans, denim shorts, trousers, Lacos curved-brim cap, Mike 90 shoes and Mike 97 shoes. Eight designs, fitted separately to each character, with independent slot colours. Headphones are a removable head accessory, like a cap. Preserve the already implemented black wave hair when the cap/headphones are removed.

The user requested a Windows EXE checkpoint and this detailed handoff before continuing with Claude. MINI-166 is NOT complete. Only runtime selection/save foundations and a mesh audit have been added. No new fitted outfit meshes, no clothing selection UI, and no OutfitWardrobe component have been integrated into GrandBayProof. Do not tell the user the eight outfits are playable yet. Existing clothing appearance remains in the checkpoint build.

## 1. Establish the correct workspace and claim

- Work ONLY in `E:\Unity\Up Iz Up Mini`. `E:\Unity\Up iz up` is a different game and is read-only reference. The conversation's default working directory can be the wrong project; explicitly set working directory on every command.
- Read `AGENTS.md`, `Docs/CURRENT.md`, `Docs/AI-PRODUCTION-WORKFLOW.md`, `Docs/VISUAL-APPROVAL-REGISTER.md`, `Docs/WorkPackets/MINI-166.md`, and the current claim in `PROJECT-HANDOFF.md`. Read relevant Characters/UI ledgers, not every historical task.
- Claim MINI-166 as Claude in both the top Current owner line and the actual Current claim block; reserve outfit runtime/UI/save/editor assets and GrandBayProof. Update the existing work packet; do not duplicate task IDs.
- Run PowerShell: `& ./Tools/AIWorkflow/Invoke-Preflight.ps1 -Agent Claude -TaskId MINI-166`.
- Run `git status --short` before changing files. Many unrelated vehicle/gameplay/scene/docs changes predate this task. Never reset, clean, blanket-stage, or regenerate the entire scene. Prior scoped headphone checkpoint is `8b1e2d8`.

## 2. Tools and exact Unity build command

Use Claude's file Read/Edit/Write tools for source and documents, and PowerShell/terminal for commands. Prefer `rg` to search. Use an idempotent Unity Editor script to create and assign meshes/materials and save the scene; do not manually edit binary scene data. Blender is optional: inspect installed executable paths and existing `Tools/CharacterPipeline` scripts before using it. No paid generation, network upload, or bulk imports are authorized.

Unity executable: `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`.

From the Mini directory, build with PowerShell:

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' -ArgumentList '-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer -logFile "E:\Unity\Up Iz Up Mini\Logs\mini166-build.log"' -WindowStyle Hidden -PassThru
```

This returns a process immediately, NOT build completion. Check `Get-Process Unity -ErrorAction SilentlyContinue` and `Get-Content Logs/mini166-build.log -Tail 40`. Success requires the `MINI-001 BUILD SUCCEEDED` marker and successful Unity exit. Never launch two Editors against the same project. Build helper exits itself, so no `-quit` needed. Output is `Builds/GrandBayProof/UpIzUpMini.exe`; keep its adjacent data and DLL files. Total build size in the log is NOT the EXE file size.

For custom batch tools, substitute the fully qualified static editor method and use a unique log. Use `EditorApplication.Exit(0/1)` when done. Avoid `-nographics` for rendering. Hidden background windows are preferred. Do not kill a user's unrelated Editor/player process.

## 3. Existing new code to finish, not duplicate

- `Assets/UpIzUpMini/Scripts/Character/OutfitWardrobe.cs`: OutfitSlot (Shirt/Pants/Hat/Shoes), serializable OutfitPiece/OutfitBinding/OutfitChoice, Select, Capture, Restore, six colour swatches. Each binding expects compatible skinned mesh bones/bindposes. Mesh changes are real, but no assets/bindings are assigned yet. Tint material indices use indexed MaterialPropertyBlocks. Review null input resilience and selection defaults when integrating.
- `Assets/UpIzUpMini/Scripts/SaveLoadSystem.cs`: added `sacatOutfit`/`frankiOutfit` lists to GameSave and capture/restore per character. Legacy saves yield null and should use defaults. Validate actual save/load timing, not just JSON field presence.
- `Assets/UpIzUpMini/Scripts/Character/CharacterEquipment.cs`: old primitive cap and garment recolouring are bypassed ONLY when OutfitWardrobe exists. Until then old behaviour remains. Watch/shades/chain/headphones stay managed here. Adding OutfitWardrobe too early disables the old cap even if no fitted cap exists: only integrate once all bindings are ready.
- `Assets/UpIzUpMini/Editor/Mini166OutfitAudit.cs`: scene mesh/bone audit. Read its entry point before running. Results: `Logs/Tasks/MINI-166/audit.txt`; Unity log: `Logs/mini166-audit.log`.
- `Assets/UpIzUpMini/Scripts/UI/VisualWardrobePanel.cs`: still old UI. Clothing tabs say IN PRODUCTION. Replace those panels with actual piece selection and colour buttons. Capture OutfitWardrobe.Capture() on opening; restore on Cancel AND Restore opening outfit; retain selection on Apply. Keep existing accessory snapshot/restore working.

## 4. Approved references and protected assets

Read `Docs/WorkPackets/MINI-147.md` FINAL USER APPROVAL dated 2026-09-07. Its older awaiting-approval paragraph is historical, superseded. View these actual images before modelling:

- `Logs/Tasks/MINI-147/Wardrobe-Concept-v1.png` (navy polo with collar/buttons, white Mike tee, indigo jeans, denim shorts, charcoal trousers, green cap).
- `Logs/Tasks/MINI-147/Footwear-90-97-Concept.png` (this supersedes the original shoe concept).

Do not replace characters with generic models. Preserve skeletons, bodies, faces, hands, existing wave scalp, approved watch/chain. Current Franki source renderers:

- Ch28_Body and Ch28_Sneakers originate in Strong.fbx.
- Ch28_Hoody uses `Assets/UpIzUpMini/Art/Characters/Garments/Franki_ArmsRestored.fbx`.
- Ch28_Pants uses `.../Garments/Franki_ReshapedGarments_Colored.fbx`.
- Ch28_Hair uses `.../Garments/Waves/FrankiWaves.asset` with BlackWaves material.

Sacat's Ch06 uses `.../Garments/Headphones/SacatWithoutHeadphones.asset`: body, clothing, face and wave scalp are fused, with four submeshes/materials (Ch06_body_Reshaped, Ch06_eyelashes, Ch06_body1, BlackWaves). His separate HeadphonesAccessory uses `.../Garments/Headphones/SacatHeadphones.asset` (6,760 triangles). Do not source Sacat from the original fused-headphone FBX and accidentally reintroduce permanent headphones or erase waves.

## 5. Geometry work: critical pitfalls and recommended sequence

1. Snapshot the CURRENT scene and source mesh assignments into task-specific backup/evidence files before integration. Inspect existing source topology and UVs. First generate assets without modifying live scene assignments.
2. Use REST/BIND POSE geometry for fitting. Live Sacat bones are bent/posed; measuring current bone transforms against T-pose mesh vertices produces wrong sleeves/legs. Rest bone origin in mesh space is `mesh.bindposes[boneIndex].inverse.MultiplyPoint3x4(Vector3.zero)`. Transform through the renderer into the character root space when applying anatomical thresholds. Keep coordinate systems explicit.
3. Separate shirt, pants and shoes from source surfaces into fitted skinned renderers while preserving skin/body regions and all required bone weights. Franki has multiple source renderers with potentially different bone arrays/bindposes; do not assume one mesh can be swapped into an arbitrary binding. Build all alternatives for a slot against that binding's exact palette and transform.
4. Never simply delete sleeves or lower pants: the current models do NOT have complete bare limbs underneath. Franki's restored sleeves include repainted skin geometry that supplies missing arms. Sacat's clothes and body are fused. Read `Tools/CharacterPipeline/mini161_franki_arms.py` and `mini157_sacat_garment.py`. Preserve exposed arms; create complete properly weighted lower legs for shorts. Keep skin separate from cloth tint material slots.
5. If clipping source triangles at sleeve/hem boundaries, interpolate positions, normals, UVs and bone weights at intersections. Normalize weights and maintain winding; crude centroid deletion leaves jagged holes. Derive surfaces once from protected original source assets, not from your previous output on each rerun.
6. Make actual distinguishable shapes: polo collar and button placket versus crew-neck tee; knee-length denim shorts versus full jeans and trousers; distinct Mike 90 versus Mike 97 shoe panels/silhouettes; fitted curved cap brim and crown. Do not label colour-only substitutions as eight fitted 3D outfits. Match reference images within the character proportions.
7. Cap must sit on the skull without floating or covering eyes, and removal must reveal waves. Add a None choice for Hats. Keep Sacat headphones independently removable; if cap/headphone combination clips, handle the combination explicitly and document the choice instead of hiding it accidentally.
8. After static fitting passes for BOTH characters, attach OutfitWardrobe, populate pieces/defaults/bindings, replace only relevant source garment surfaces, then save GrandBayProof via EditorSceneManager. No full scene-builder regeneration.

Keep meshes bounded for mobile use, reuse small materials/textures, and add no runtime lighting just for clothes. Generate unique .asset/.mat files and preserve .meta GUIDs. On asset overwrite, Unity CopySerialized can leave stale GPU mesh data; assign mesh channels explicitly or use a fresh mesh, then reload saved scene and verify the saved result.

## 6. UI and persistence requirements

Keep free wardrobe access already requested by the user. Do not add purchase/ownership gates. Display actual piece names and selected colour, update the 3D preview immediately, and allow independent per-character choices. Existing tabs Accessories/Shirts/Pants/Hats/Shoes can remain.

VisualWardrobePanel.RebuildPreview currently copies only the global MaterialPropertyBlock. Copy indexed blocks for EACH material index too, or fabric colours will not appear in the preview. Preserve skinned BakeMesh transforms. Check initial yaw: existing yaw=180 may show backs; fit front-facing view using real renders rather than guessing.

Remove the duplicate old primitive-cap trial row from Accessories once fitted Hats work (or explicitly route it to the fitted cap). Preserve watch/shades/headphone controls. Update stale session-only/IN PRODUCTION wording to reflect real behaviour. Apply retains choices; game save/load persists them; Cancel and Restore opening outfit restore BOTH accessories and clothes. Verify switching characters does not copy one character's choices to the other.

## 7. Required evidence before claiming completion

- Unity compile; run available relevant tests plus a focused MINI-166 validation harness.
- Assert each of eight designs has a real mesh on BOTH characters, compatible bones/bindposes and valid materials. Hat None must work. Test slot independence and colour index validity.
- Play Mode: choose pieces/colours, Cancel, Apply, Restore opening, switch characters, and actual GameSave serialization/load including legacy null lists. Recheck Sacat headphones wear/remove and both wave scalps.
- For batch Play Mode callbacks surviving domain reload, follow `Mini165HeadphoneValidation.cs`: SessionState flag + InitializeOnLoad registration, enter Play Mode, wait initialization, execute checks, exit, finish from EnteredEditMode. Plain callbacks set before EnterPlaymode can be lost.
- Render front/side/back and close-ups for both characters; inspect waist, sleeves, neck, knees, ankle/foot and cap in standing/walk/run/crouch poses. Existing `Mini154GarmentMotionProof.cs` and `Mini164WaveHair.cs` provide pose/render patterns. Use Animator.Rebind where appropriate and SkinnedMeshRenderer.updateWhenOffscreen; render twice to warm GPU skinning. Reload SAVED scene for final evidence.
- Existing hidden player `-wardrobe-proof` screenshots are BLACK. These are NOT visual proof. Use Camera.Render into RenderTexture and read back PNGs for garment visuals; inspect actual files using an image viewer/tool. A GUI/user play-test remains necessary if UI capture still fails; report that limitation plainly. Do not repeatedly accept blank captures.
- Rebuild Windows EXE with the command above after final integration. Smoke-run it, record errors/logs and report any unverified hands-on behaviour.
- Update task packet, Characters/UI ledgers, CURRENT, TASKS, CHANGELOG and handoff with exact results. Stage only your own scoped files; checkpoint commit without swallowing pre-existing dirty scene/docs/gameplay edits. Release ownership to None.

## Suggested first reply to the user

"I’m continuing MINI-166 from the saved handoff. The EXE checkpoint preserves your current characters and accessories; I’ll finish the eight fitted clothing designs, connect the wardrobe selectors, and verify them on both characters before calling the outfits complete."
