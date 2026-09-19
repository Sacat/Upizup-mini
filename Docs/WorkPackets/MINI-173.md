# MINI-173 — Lalay houses, Geneva cricket ground, coastal bay and chain inspection

User authorized replacing Lalay shanties using local architectural references, removing Geneva football features, making the roundabout surroundings coastal, and inspecting both playable-character chains. Work in the expansion import scene; preserve canonical scene and source copy. Use owned procedural geometry, no copied reference textures. Validate road clearance, colliders, original scene hashes and render actual results. Record uncertainty about current real-world details rather than claiming surveyed accuracy.

References: Safe Haven Historical Stone Structure in LaLay (main-road concrete/native-stone building, listing updated January 2026); Millenia MR255 Tete Lalay traditional two-storey concrete home (near intersection); LargeUp De Elf's Place (porch on Unity Block, Lallay; 2016). These establish types, not a complete 2026 street survey.

## Implementation and reproducible workflow (2026-09-19)

Project: E:\Unity\Up Iz Up Mini. Task MINI-173. Scene: Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity. Canonical GrandBayProof and source MapLab_GrandBayExpansionCopy were not edited. Unrelated ObjectiveMarker.mat and packages-lock.json changes are excluded.

### Research and accuracy
- https://www.safehavenrealestate.com/property/historical-stone-structure-in-lalay-grand-bay/ describes main-road concrete/native stone, formerly two storeys; listing updated Jan 2026.
- https://www.milleniarealtydominica.com/properties/grand-bay-two-storey-home-with-sea-views-business-potential/ search listing identifies a two-storey Tete Lalay home; page fetch unavailable.
- https://www.largeup.com/2016/10/28/pic-week-elfs-place-grand-bay/ describes a roadside porch on Unity Block, 2016.
These support architectural types, NOT a current measured street-by-street reconstruction. No downloaded imagery is shipped. Geneva and coastal shape are user-directed stylized approximations, not a verified 2026 survey.

### Tools and sequence for Claude
Read AGENTS.md, CURRENT.md, current claim, AI-PRODUCTION-WORKFLOW.md and WORLD-EXPANSION-WORKFLOW.md. Claim a task before editing. Run Tools/AIWorkflow/Invoke-Preflight.ps1. Use PowerShell rg for code discovery. Query scene objects through Unity editor APIs; this scene is binary, not YAML. No Blender, image generation or external asset import was needed in this pass.

Unity executable: C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe
Launch one editor at a time with -batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod METHOD -logFile LOG. Use -nographics for geometry-only work; omit it for renders. Do not add -quit to the Play Mode harness. On Windows launch hidden and check the log/process, because launching Unity does not itself prove completion.

1. Mini173Survey.Run: read-only inventory. Logs/Tasks/MINI-173/Survey.txt.
2. Mini173LalayRevision.Apply: disable exactly 31 Lalay_Shanty_* roots, retain their positions/orientations for rollback, create MINI173_LalayHomes. Remove 141 PitchLine/GoalPost/GoalCrossbar/GoalNet/MownStripe objects under GenevaField. Reverse stand-row offsets so higher tiers are farther from the field. Logs/mini173-apply.log.
3. Mini173BayRevision.Apply: snapshot source expansion terrain to TerrainBeforeCoast.asset once, clip triangles at authored coastline, grade shoreline, preserve road-hit heights, weld vertices, save distinct CoastalTerrain.asset used by both renderer and MeshCollider. Create narrow volcanic-sand shore and water band. Logs/mini173-coast.log.
4. Mini173Validation.Run: replacement counts, no active shanties/football, sampled house collider footprint vs road colliders, terrain mesh/collider identity, roundabout dry clearance. On success invokes map rendering. Logs/mini173-validation.log.
5. Mini173ChainProof.Run: actual Play Mode equipment on Sacat and Franki, front/side screenshots, two-second delayed settling screenshots. Only runtime alwaysEquipped injection; no save inventory modification. SetTrialItem does NOT support chain_gold; the first capture attempt failed and was corrected. Logs/mini173-chains-fit.log; latest delayed run is mini173-chains-final.log if present.
6. Inspect PNG pixels, not just logs. Existing test directories contain no test cases; task-specific editor and Play Mode harnesses provide the available checks. Do not claim live driving, all outfit combinations, or animated collision-free chain motion from static renders.

### Geometry, dimensions and elevations
House dimensions are COMPRESSED GAME dimensions: body 2.65m wide x 3.6m deep, single height2.7m or double5.1m. Original imported shanties reached 6-12m widths despite dense 3.6m street sampling. New geometry stays on the original lots. Porch depth0.7m, columns0.13m, roof17 degrees, floor interval2.55m; material palette five plaster colours, cream trim, dark windows, masonry plinth, metal roofing and rails. Porch faces local +Z, inherited road-facing rotation. Ground elevation inherits each original lot, approximately5-9m in the inspected section. Body/slabs/steps retain colliders; tiny decorative parts do not. Meshes combine by material per house. Geometry is owned procedural Unity primitive construction, with true roof slopes and window depth, no billboard camera tricks.

Coast is x=250 for z<-160; x=250+(z+160)*0.55 for -160<=z<-60; x=305+(z+60)*0.25 thereafter. This is artistic coastline compression. Roundabout remains (279.14, approximately2m,-66.62), road unchanged. Water0.25m, shore edge0.32m, sand6m wide, SmoothStep grade18m inland. Raycasts preserve road surface minus0.025m. Original terrain baseline is immutable for repeatability. Weld tolerance0.0001m reduces clipped mesh to36,857 vertices. Do not rerun MINI171 import or MINI172 broad refinement: those can reset this work or re-add football details.

Geneva keeps the prior cricket strip, wickets, oval boundary and two three-tier stands. The compressed field remains42x64m with an18.5x29.5m boundary ellipse. These are gameplay approximations; prior wicket/pitch dimensions have not been re-certified as regulation cricket dimensions.

### Chain diagnosis and rejected attempts
Before images showed middle chain portions buried inside both shirts while bottom links stood away from the chest. The existing global3.5cm rearward correction alone was insufficient. The GLB contains welded whole loops (6,127 vertices per mesh), not disconnected links. An attempted connected-component rigid-link fit therefore fitted zero components and was rejected. A radial surface projection stretched the upper necklace sideways toward the shoulders and was also rejected after viewing renders.

Final ChainGarmentFit uses48 samples around each loop's front-elevation oval; projects toward front/back torso along character forward only, preserving width and height. It bakes visible skinned garment/body meshes to temporary raycast surfaces, computes bounded +/-0.10m depth displacements with0.011m nominal clearance, smoothly interpolates displacement per vertex, clones meshes per equipped instance, then destroys temporary surfaces/meshes. Source GLB/prefab stay unchanged. CharacterEquipment calls it before AccessorySwing initialization, only for Sacat/Franki; BossC is excluded. Sacat fitted95 arc samples across two loops; Franki44 across one. Images show continuous visible necklaces after the width-preserving correction. This is equip-time fit, not dynamic cloth collision; unusual poses and later wardrobe changes still need gameplay review. Re-equipping recomputes from source. Do not describe screenshots as motion proof.

### Evidence and limitations
Renders: Logs/Tasks/MINI-173/Renders/Lalay.png, Geneva.png, Bay.png, FullMap.png, Sacat-chain-front.png, Sacat-chain-side.png, Franki-chain-front.png, Franki-chain-side.png; delayed settled captures when final harness completes. Camera definitions/resolutions are in Mini173LalayRevision.cs (1400x1000). Actual game scene geometry rendered under scene lighting, fog disabled for map overview. Logs record failed attempts as well as final successful runs; never cite a capture-pass marker as proof of visual acceptance without opening the images.
No EXE rebuilt in this task. User visual approval and hands-on movement/wardrobe switching checks remain outstanding. Do not mark a visual lock accepted on the user's behalf.

## Final verification
- mini173-validation.log: MINI173_VALIDATION_PASS.31 replacements;0 active shanties;0 football objects;0 sampled house-road footprint hits; terrain renderer/collider mesh identical; roundabout minimum shoreline clearance8.524689m.
- mini173-chains-final.log: MINI173_CHAIN_CAPTURE_PASS and MINI173_CHAIN_SETTLED_CAPTURE_PASS. Both delayed three-quarter images opened and inspected: necklaces remain visible over the shirts after two seconds of runtime updates. This is limited idle evidence, not exhaustive movement proof.
- Final Bay.png opened and inspected: the former grass wedge is replaced by water and a connected shoreline past the roundabout. Lalay/Geneva renders also inspected. Unity compilation completed without compiler errors. Empty project test folders contain no NUnit cases.
- Full Claude process documentation is this work packet plus the five Mini173 editor tools and ChainGarmentFit.cs. No external messages sent. No EXE built. User visual acceptance and hands-on driving/wardrobe-motion testing remain pending.
