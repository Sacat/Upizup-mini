# Prompt to paste into the new (VR game) thread

I'm building a VR game in Unity. Work the way my previous project ("Up Iz Up Mini", at `E:\Unity\Up Iz Up Mini`) worked. It is a finished workflow, so reuse it instead of reinventing it. Read before you start; don't copy code blindly.

## 1. Import my workflow, skills and memory (do this first)
1. Copy these into THIS project (don't modify the originals):
   - Skills: `E:\Unity\Up Iz Up Mini\.claude\skills\*` -> `<this project>\.claude\skills\`
     (reference-driven-game-asset-production, upizup-blender-modeling, upizup-building-modeling, upizup-shoe-modeling, upizup-wardrobe-repair, upizup-map-district-expansion, upizup-map-house-repair, upizup-grandbay-landmark-buildings)
   - Memory: read every file in `C:\Users\PCSS-PC\.claude\projects\E--Unity-Up-Iz-Up-Mini\memory\` (start with MEMORY.md). Save the ones that are general (how I like to work, modeling discipline, verification habits, tool-access rules) into this project's memory. Skip Up Iz Up Mini story/plot/character facts.
   - Docs to adapt: `Docs/WORK-PACKET-TEMPLATE.md`, `Docs/DEFINITION-OF-DONE.md`, `Docs/AI-PRODUCTION-WORKFLOW.md`, `Docs/WORLD-EXPANSION-WORKFLOW.md`, `Docs/Systems/README.md` (the per-system ledger idea).
2. Rename or generalise skills that mention Up Iz Up Mini / Grand Bay / Dominica so they fit this VR game. Keep the method, drop the game-specific facts.
3. Create this project's `CLAUDE.md`, `AGENTS.md`, `Docs/CURRENT.md` and `Docs/Systems/` ledger the same way.

## 2. Working rules
- Convert every request into a small task with acceptance criteria before building (MINI-### style IDs, one owner, reserved files, release at the end). Don't start broad work from an open-ended request.
- One integrator owns scenes, prefabs, packages, project settings and generated world data. Never edit the live/approved scene until I approve. Work in an isolated copy scene, hash-check live and source scenes before and after, and keep a git tag backup before big imports.
- Verify with real evidence: batch-mode Unity renders (do NOT use `-nographics`, it gives blank images), measured checks (heights, gaps, overlaps), Play Mode validators (don't pass `-quit`). Never claim success without looking at the output. Send me the images to confirm (SendUserFile).
- Don't drive the Unity Editor with computer-control. Use `-batchmode -executeMethod` editor tools, and give me menu steps when something must be done by hand.
- Manual placement I made is authoritative: never reinterpret values I tuned. Bug-hunt elsewhere first.
- Build data-driven, reusable systems (definition tables) rather than one-offs.
- After each phase, build a Windows exe / test scene for me to try before continuing.
- After each task: update the matching `Docs/Systems/*.md` (what worked, what didn't, open items), a one-line pointer in PROJECT-HANDOFF, CHANGELOG, then commit with `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`.
- Be direct: give a recommendation, don't over-ask. Ask only for genuinely mine-to-make decisions.

## 3. Model building (any asset: props, vehicles, houses, clothing, characters, vegetation)
Follow the mandatory 10-step sequence: define target -> analyse reference -> plan construction per component -> blockout -> establish construction -> prove one element -> inspect consistently -> fix largest errors first -> test in the game -> save evidence.
- Research real references first (WebSearch/WebFetch for facts, curl + Read for the actual images). Call out specific differences from the reference.
- Blender Python for modeling. Precise measured specs, staged part-to-whole inspection, checklist inspection against the reference before presenting.
- Organic shapes: real seam curves, one welded multi-material mesh so Subsurf works. If a detail fails 3 attempts, remove it rather than ship broken.
- Beware camera singularities in renders (explicit rotation, not `to_track_quat`); place surface decals with one flat local tangent frame.
- Optimise for the target platform. For VR: tight triangle budgets, few materials/draw calls, LODs, baked lighting, single-pass instanced stereo; check comfort (scale, frame rate).
- I approve visual locks; treat approved looks as protected.

## 4. Map building
Use upizup-map-district-expansion and WORLD-EXPANSION-WORKFLOW.md:
- New area = isolated hashed scene copy, idempotent generator scripts, curated real source data, terrain + roads + buildings generated with measured verification (seam gaps, ground shift at anchors, road-vs-building overlaps), authored grade blending, and a fixed-camera review gate before it goes anywhere near gameplay.
- Roads flush with terrain (flat pad under the road plus shoulder, smooth blend outward, no lip). Road joins: match heights at seams, cut or lift overlapping road triangles to stop z-fighting, keep ground below existing roads.
- Move objects off roads with a minimal-displacement search that avoids other buildings, then re-seat on the ground.
- Keep rejected-pass evidence. Never run historical whole-world builders.
- Make maps VR-friendly: walkable scale, teleport/locomotion-safe surfaces, comfortable slopes, occlusion/culling and LOD plan.

## 5. First task for this thread
Read the imported skills and docs, summarise how you'll apply them to a VR game (Quest-class standalone or PC VR: ask me which if it isn't in the project), then propose the first task with acceptance criteria and wait for my go-ahead.

My VR project is at: <PASTE PATH HERE>. Its state / what I've already built: <PASTE NOTES HERE>.
