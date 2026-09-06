# MINI-141: Houses, grass and crop previews

Status: evidence ready; awaiting user approval before gameplay integration. Ownership released.
Owner: Codex. Class C. External credits: 0.

Blender sample family: one/two-storey Caribbean houses, sparse grass, carrot, banana and visible cannabis buds. Render real meshes with fixed cameras outside Assets. Keep live scene and EXE untouched. Use user Lalay photographs for architectural proportions and character, preserve approved layout and manual placement. Existing Demo City buildings are previously rejected per asset ledger.

Targets: houses below 8000 triangles each; crops below 4000 each; shared palette/atlas and distant LODs during integration. No device performance claims before profiling. Studio lighting is presentation only. Grass should be sparse opaque blades with no colliders and distance culling at integration.

Preflight: clean tree, unclaimed, Unity not running, Blender 5.0 installed. District approved_graybox validation PASS.

Disconnected road: user identified Lalay near Dog Life block. Inspect exact live geometry before proposing repair; preserve block, shops and road alignment. Show images before changing playable scene.

Outputs: Logs/Tasks/MINI-141/01-Houses-Grass-Preview.png and 02-Crop-Preview.png, matching editable .blend files, mesh-budget.json; repeatable generator Tools/ArtPreview/mini141_preview.py. Blender 5.0.1 executed successfully; both images visually inspected. First banana was rejected internally for angular short leaves/loose fruit; second render has segmented curved leaves and a compact attached bunch. Gameplay compilation/build not applicable to standalone preview files. Unity integration/verification remains after approval.

Measured evaluated triangles: one-storey house 2282; two-storey 3426; carrot 348; banana 3696; each cannabis variant 1796. These are geometry counts only, not proof of phone performance. Blender candidates still have many modular objects: join by material, consolidate palette/atlas, create LODs, UVs/texture bakes where needed, and test collision/interiors/ground fit in Unity after approval. Exterior candidate doors/windows are decorative and do not yet provide playable interiors. The crop display intentionally exposes carrot root for review; the planted version should hide root below soil until harvest. Generated locally, no external assets or credits used.

Road read-only audit: documented Dog Life X/Z (-38.59,-152.24); candidate west join approximately (-63.29,8.91,-144.47), roads way/22917921 and way/23042701. Existing JunctionPatch_22917921_23042701 is present by binary name scan in live GrandBayProof; MBRoad object names are absent. Documentation confirms spline proof remains isolated. This suggests the live map may retain an older connection, but active geometry/height/collider continuity has NOT been verified in Unity. Next: capture live junction around X -65..-20, Z -165..-125, sample authoritative road/terrain collision, then show a repair preview before integration. Do not re-run full BuildScene.
