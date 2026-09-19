# MINI-171 — Playable expansion import and regression repairs

Owner: Codex (continued from Claude on 2026-09-19 by user request).

Scope: finish and validate `GrandBayProof_ExpansionImport.unity`; make road edges meet green terrain throughout the playable map; ensure NPCs use obstacle/stuck jumps; move the Dog Life NPC spawning on a Lalay building to safe ground; move player chains back toward the neck after wardrobe changes; separate driver and pillion bodies on both bikes. Preserve `GrandBayProof.unity`, the MINI-168 source copy, gameplay IDs, clothing selections, driver mount fit, and Claude's existing uncommitted import work.

Acceptance: clean Unity compile; saved-scene road/terrain clearance samples; Dog Life pool ground/roof check; focused NPC jump recovery test; chain before/after renders on both playable characters; driver/pillion side and rear renders with body-clearance measurement; MINI-171 import verifier; Windows build only after the fixes pass.

Known starting state: Claude created an uncommitted import scene and editor scripts. Its report says hard checks pass, but ground differences remain up to 3.96m in the old footprint and 11 existing houses are within 8m of new roads. No work packet existed. Unrelated dirty `Packages/packages-lock.json` and `ObjectiveMarker.mat` must not be absorbed.

## Completed 2026-09-19

- Preserved both protected source scenes byte-for-byte. The finished expansion remains `GrandBayProof_ExpansionImport.unity`, outside Build Settings until the user has driven it.
- Added `Mini171RepairPass`: it fits the green mesh terrain 3 cm below every active ground-road surface, refreshed the terrain collider, and grounded all four Dog Life actors plus all eight of their patrol points directly against the imported terrain collider. Result: 22 road colliders and 3,604 fitted terrain vertices.
- `PatrolNPC` now uses the existing `NpcObstacleJumpMotor`. A low obstacle triggers immediately; a navigation block lasting 0.6 seconds can also trigger a clearance-checked jump with an overhead/body-clear ray and a valid landing-ground probe. Movement remains through `CharacterController.Move`.
- Moved wearable chains 3.5 cm toward each character's neck after either manual-profile or fallback placement, before `AccessorySwing` captures its rest pose.
- Corrected TMAX pillion offsets from Z 0.32/0.28 to 0.02/-0.02 for seated/wheelie poses, while preserving X, Y, pitch and driver tuning. Updated the prefab and both authoritative rebuild tools. Increased the runtime SuperMoto pillion anchor rear spacing from 0.45 m to 0.55 m.
- Unity compile passed. `Mini171VerifyImport` passed all hard checks: zero new/live house overlap, zero live houses within 8 m of new roads, and key existing-road joins at 0.00 m vertical gap. Final overview/edge renders are in `Logs/Tasks/MINI-171/Renders`; `r1_road_edge_east.png` visually shows a flush road/grass edge. Windows build succeeded at `Builds/GrandBayProof/UpIzUpMini.exe`.
- Honest remaining check: passenger body clearance and chain depth need a hands-on camera inspection with both wardrobe-heavy characters. The measured placements and build are corrected, but batch proof did not exercise a two-rider animated frame. The expansion copy also still requires the user's walk/drive approval before it replaces the canonical scene.
