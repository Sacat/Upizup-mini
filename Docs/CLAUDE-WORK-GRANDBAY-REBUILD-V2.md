# Up Iz Up Mini — Corrective Grand Bay Vertical Slice Rebuild

Copy everything below this line into Claude Work/Claude Code.

---

You are the lead Unity gameplay and environment agent for **Up Iz Up Mini** at:

`E:\Unity\Up Iz Up Mini`

This is a corrective rebuild. The existing `MINI-001` scene is not an acceptable visual or gameplay target. It is an engineering mock-up made from capsules, cubes, a single NPC, and one farm plot. Do not defend it as “meeting the previous checklist,” and do not extend it by adding more primitives. Replace its visible presentation and build a convincing vertical slice.

## 1. Mandatory startup and project boundaries

Before editing anything, read these files completely:

1. `AGENTS.md`
2. `CLAUDE.md`
3. `PROJECT-HANDOFF.md`
4. `TASKS.md`
5. `DECISIONS.md`
6. `Docs/GAME-DESIGN.md`
7. `Docs/MAP-STRATEGY.md`
8. `Docs/DEFINITION-OF-DONE.md`
9. this prompt

Treat the larger game at `E:\Unity\Up iz up` and the library at `E:\Assets` as **read-only references**. Never modify, move, rename, or delete their contents. Never copy an entire Assets, Packages, ProjectSettings, or Library directory. Import only deliberately selected files whose origin and licence are recorded.

Create and claim a new corrective task named:

`MINI-011 — Grand Bay Production Vertical Slice Rebuild`

Record ownership and reserved files in `PROJECT-HANDOFF.md`. This task supersedes the visual acceptance of `MINI-001`; do not change MINI-001 history. If another agent owns the project or scene, stop and report the conflict.

## 2. Understand the actual target

The reference video is:

`https://youtu.be/gSP57P-IDio?si=BbmpUNeKae6rMkkI`

The video is useful for its **working method**: import finished art, construct a readable scene, test it in Game view, inspect what the player actually sees, and iterate. Its literal pixel-art/top-down appearance is not the target.

Up Iz Up Mini must be a modern, mobile-friendly **three-quarter 3D game**:

- Real 3D characters, terrain, houses, vegetation, props, crops, and vehicles.
- A perspective three-quarter follow camera, not a flat tilemap and not a vertical overhead camera.
- A stylized low-poly visual language with coherent proportions, good lighting, readable silhouettes, warm Caribbean colour, lush greenery, and corrugated-roof village architecture.
- A compact GTA-like open-world feeling: walk around Lalay, meet people, follow the road and dirt trail, farm, sell, see police presence, and understand where to go.
- Geographic relationships based on Grand Bay, Dominica, compressed for playability rather than invented as a generic straight-road village.

The target is not AAA photorealism. The target is a polished indie vertical slice that looks intentional and could later run on phones.

## 3. Non-negotiable visual prohibitions

The playable view must contain none of the following as final visible content:

- Capsule, half-cylinder, cube, sphere, or mannequin characters.
- Plain cubes used as finished houses.
- A single flat green plane pretending to be Grand Bay.
- One straight road with repeated identical boxes.
- Default Unity materials, default sky, missing/pink materials, or unlit grey prototypes.
- OnGUI/debug text as the player-facing HUD.
- Giant yellow poles or crude floating arrows.
- A single farm plot presented as the whole farming feature.

Primitives may be hidden collision volumes only. If an art asset is temporarily unavailable, stop at the visual-planning gate and report the exact missing asset; do not declare the scene complete with geometry placeholders.

## 4. First inspect, then propose — do not code immediately

Perform a visual audit before implementation:

1. Open and run the current `GrandBayProof` scene.
2. Capture Game-view screenshots showing the player, road, buildings, NPC, and farm.
3. Inspect `Mini001SceneSetup.cs` and identify every generated placeholder that must be removed or hidden.
4. Inspect the Grand Bay reference material without modifying it:
   - `E:\Assets\GrandBayReference\Maps\Grandbay entire.jpg`
   - `E:\Assets\GrandBayReference\Maps\heightmapper-1774227992671.png`
   - `E:\Assets\GrandBayReference\Maps\heightmapper-1774227992671.raw`
   - `E:\Assets\GrandBayReference\Maps\Map_Index.md`
   - `E:\Assets\GrandBayReference\Maps\Google_Maps_Reference_Links.md`
   - `E:\Assets\GrandBayReference\Missions\Grand_Bay_Vertical_Slice_Missions.md`
   - the two Lalay aerial photographs supplied by the user if they are present in project/reference folders.
5. Inspect the larger project only for proven concepts, the two boys, animations, and already-vetted assets. Do not copy anything whose licence is unclear.
6. Produce `Docs/MINI-011-VISUAL-PLAN.md` containing:
   - before screenshots;
   - a scene blockout diagram;
   - art direction and palette;
   - exact asset packs/prefabs proposed;
   - licence/source for every imported pack;
   - a performance budget;
   - a list of what will be replaced;
   - risks or missing assets.

Do not continue to production scene work until the visual plan is coherent. If the user is available, show the plan and first environment screenshot for approval. If this is an unattended run, proceed only with the approved free packs listed below and record every assumption.

## 5. Asset strategy — coherent art beats many random packs

Use one main character style and one main environment style. Do not mix realistic PBR people, voxel trees, toy cars, and flat cartoon houses.

Recommended free starting set, subject to availability and import testing:

- **Low Poly Character Pack** — full rigged humanoids, Unity 6 and URP compatible:
  `https://assetstore.unity.com/packages/3d/characters/humanoids/low-poly-character-pack-357288`
- **Human Basic Motions FREE** — locomotion/idle animation source if already in My Assets:
  `https://assetstore.unity.com/packages/3d/animations/human-basic-motions-free-154271`
- **POLYGON Starter Pack — Art by Synty** — inspect for compatible neutral props and vehicles; use only pieces that match the selected character style:
  `https://assetstore.unity.com/packages/3d/props/polygon-starter-pack-low-poly-3d-art-by-synty-156819`
- **Low Poly Environment — Nature Free** — mobile-friendly trees, rocks, grass and terrain dressing; recolour/select tropical-looking pieces:
  `https://assetstore.unity.com/packages/3d/environments/low-poly-environment-nature-free-lowpoly-medieval-fantasy-series-187052`
- **Low Poly Tropical Beach** — palms, bushes, dock/boat/coastal props; Unity 6 URP compatible:
  `https://assetstore.unity.com/packages/3d/props/low-poly-tropical-beach-306153`
- **Cartoon Farm Crops** — candidate crop meshes only; it is old, so isolate it and test URP materials before use:
  `https://assetstore.unity.com/packages/3d/vegetation/plants/cartoon-farm-crops-79777`

Do not use Low Poly Brick Houses as the primary environment pack; its old custom shaders and reported URP issues make it a poor base. Do not use medieval or European village buildings unchanged. For Grand Bay houses, either use suitable existing licensed modular pieces or create modular houses from reusable wall/roof/window/door meshes with proper materials. These may be agent-built modular geometry, but they must look like finished houses, not scaled cubes.

Every imported asset needs an entry in `Docs/ASSET-REGISTER.md` with title, publisher, URL/source path, licence, render-pipeline status, purpose, and import date. Do not use content from `E:\Assets` merely because it exists.

## 6. Grand Bay map and environment requirements

Build a geographically informed, compressed gameplay district, not the full island and not a random map. Use the supplied map references for proportions and road relationships. Google Maps may be used only as visual/location research; do not ship Google imagery or textures.

Create a local coordinate/data layer for verified anchors. Do not invent exact latitude/longitude. Record unverified artistic placements as approximate. The playable connected route must read as:

`Lalay homes/safehouse → Lalay main road → shop and buyer area → police presence → Montine turnoff → rough dirt trail → farm clearing/safehouse`

Minimum environment content:

- A narrow paved Lalay road running between buildings, with believable bends or grade changes.
- 14–20 varied houses/buildings, mostly one storey with several two-storey buildings.
- Houses placed close to both sides of the road like the aerial references, with small yards, gaps, paths, fences, drains, steps, retaining edges, clotheslines, barrels/crates, and dense vegetation.
- Corrugated roofs in varied white/silver, red, blue, and green; Caribbean exterior colours without turning the scene into a toy rainbow.
- Correct human scale: roughly 2.7–3.2 m per storey, doors around 2 m, climbable steps around 0.16–0.20 m high and 0.28–0.35 m deep.
- At least one enterable safehouse near Lalay and one simple safehouse near the farm. Interior collision must prevent walking through walls; doors must work or clearly transition inside.
- A visible rough route toward Montine: brown soil, ruts, rocks, puddle/erosion hints, slope, grass edges, banana/palm/broadleaf vegetation. It must not be a smooth brown rectangle.
- Terrain elevation that reflects a mountainous tropical island. Avoid sheer heightmap spikes; establish sane height scale and smooth only rendering artefacts, not geographic structure.
- Landmarks or labelled stand-ins for the school, credit union, village council/community area, church/police area only where supported by references. Exact locations must be marked verified or approximate.
- Occlusion and camera-aware placement so roofs/trees do not constantly hide the player.

## 7. Camera, character, controls, and lighting quality

### Camera

- Perspective three-quarter camera with approximately 35–50° downward pitch and a slight yaw aligned to the village road.
- Player should occupy roughly 8–14% of screen height at 1080p.
- Smooth follow and look-ahead; no harsh snapping.
- Obstruction handling: fade or reposition for roofs/large vegetation between camera and player.
- Keep the character readable while still showing enough world to navigate.

### Characters

- Two recognizable full-body rigged boys remain the protagonists. They need distinct clothes/colour silhouettes and names or temporary identifiers.
- At least one boy is controllable; switching may be disabled for this slice if the second boy follows or waits at the safehouse.
- Working idle, walk, run, and interaction animations. Feet should contact ground; no sliding or floating.
- Character Controller/colliders must match the body and stop wall penetration.
- Use the existing Human Basic Motions/Mixamo-compatible animation source when technically suitable and licensed.

### Lighting

- Warm tropical daylight using one directional sun, soft shadows, sky/environment lighting, and a restrained URP volume.
- No point light attached in front of the player and no player-carried light that causes walls to glow.
- Maintain readable skin, roads, vegetation, and interiors without crushing shadows or washing colours out.
- Use baked/mixed lighting and probes where sensible for mobile; limit real-time shadow distance and extra lights.

## 8. Required people and interaction system

The slice is not acceptable without all of these visible in the scene:

- Two protagonist boys.
- One seed seller/employer near the Lalay shop.
- One separate crop buyer/market vendor.
- One police officer positioned near the Lalay road, not hidden at the farm.
- At least six ambient residents: minimum three walking along short NavMesh/waypoint routes and three standing/talking/working.

All interactable characters and objects must use the same modular proximity interaction system:

- At range, no prompt.
- Near the target, a polished world-space keycap appears above or beside it: `[E] Talk`, `[E] Buy`, `[E] Sell`, `[E] Plant`, `[E] Water`, `[E] Harvest`, `[E] Enter`.
- Prompt faces the camera, scales sensibly with distance, does not clip through heads, and selects only one target when several are nearby.
- Interaction opens TextMeshPro/canvas dialogue or a small shop panel, never `OnGUI` debug labels.
- Initial dialogue should be short Dominican-flavoured text such as “Yah man,” “allu,” “yah wii,” and sentence-final “nuh,” used naturally rather than in every sentence.

## 9. Mandatory playable Mission 1

Build one complete, clear mission rather than disconnected mechanics.

Suggested title: **A Start in Montine**.

1. Player begins near the Lalay safehouse with an objective card: `Meet the seed seller on the main road.`
2. A subtle GTA-style world marker/minimap cue guides the player. Use a grounded ring, icon, compass/minimap marker, or breadcrumb system — no giant pole and no crude floating arrow.
3. Approach the seed seller; `[E] Talk` appears. The seller gives tomato seeds and directions toward Montine.
4. Objective changes to `Follow the dirt trail to the Montine farm.`
5. At the farm, provide at least **six usable planting spots** or a small free-placement farming zone. One lone plot is forbidden.
6. Player plants tomatoes, waters them, waits through accelerated visible growth, and harvests them.
7. Soil begins light/dry brown and changes to clearly darker wet brown when watered.
8. Tomato plants progress through at least three visual stages; mature fruit is visibly red, not green.
9. Objective changes to `Sell the tomatoes to the market buyer in Lalay.`
10. Buyer uses `[E] Sell`; inventory decreases and money increases.
11. Mission-complete panel summarizes money earned and unlocks the next mission placeholder.

The HUD must include working money, inventory/crop count, current objective, health, stamina, and heat. Stamina goes down while running and regenerates after the player slows or stops. Heat begins low but is functional.

## 10. Police, farming progression, and vehicle presence

These systems may be simple but must be visibly represented:

- Police officer can be approached and shows `[E] Talk` while calm.
- Heat increases from a controlled debug/test action or carrying/selling an illegal crop in a test path.
- At high heat, police changes behaviour; at 100 heat, one additional officer can spawn. Do not build full combat unless required for safe testing.
- Crop data must support tomatoes, bananas, carrots, Bushers, Black Sugar, and Purple. Only tomatoes need the complete Mission 1 loop, but other crop prefabs/data must show distinct mature produce: red tomatoes, banana bunches, carrot tops/root indication, and visually distinct weed tiers.
- Include at least one parked motorcycle/bike and one ordinary road vehicle to establish the setting. A single simple drivable vehicle is desirable after Mission 1 works, but it must not block completion of the core visual/farming slice.
- Design farming land as data-driven zones so later missions can unlock or purchase additional lots and let the player place a permitted farm lot in suitable remote land.

## 11. Architecture and mobile constraints

- Unity 6 URP.
- Modular systems: interaction, dialogue, mission objectives, inventory/economy, crop definitions, farm plots/zones, heat, NPC behaviour, and HUD must not be one monolithic script.
- Prefer ScriptableObjects or serializable data for crops, products, NPC dialogue, mission steps, and prices.
- Pool ambient NPCs and police reinforcements.
- Use LODs or conservative mesh density, shared materials/atlases, GPU instancing, occlusion culling where useful, and bounded NPC simulation.
- Avoid per-object Update loops when a manager/event system will do.
- Target 60 FPS on development PC and design for a future 30 FPS mid-range Android profile. Record approximate draw calls, batches, SetPass calls, visible triangles, and memory concerns from the completed scene.
- Keep keyboard controls now but route input through Unity’s Input System so touch controls can be added later.

## 12. Implementation phases and hard visual gates

### Phase A — audit and art import

- Capture “before” screenshots.
- Write visual plan and asset register.
- Import only selected assets.
- Prove every material renders correctly in URP.

Gate A: submit one character lineup screenshot and one environment palette/asset lineup screenshot. Do not continue if characters are primitives, materials are pink, or packs visibly clash.

### Phase B — Grand Bay environment and camera

- Rebuild the visible district, road, houses, terrain, vegetation, dirt trail, safehouses, and camera.

Gate B: capture at least these Game-view images at 1920×1080:

1. Lalay road from player height showing buildings on both sides.
2. Three-quarter overview showing the road-to-Montine relationship.
3. Dirt trail and farm clearing.
4. Player beside a house/door proving human scale.

Visually inspect each screenshot. Record specific faults and fix them before Phase C. A hierarchy listing is not visual verification.

### Phase C — people, prompts, Mission 1, HUD

- Add the protagonists, seller, buyer, police, ambient NPCs, interactions, farming loop, objective guidance, working bars, and mission completion.

Gate C screenshots:

1. `[E] Talk` over seller at correct range.
2. Buyer and police both visible in the Lalay area.
3. Dry versus watered soil.
4. Mature tomato plant with red fruit.
5. Walking residents along the road.
6. Mission-complete screen after selling.

### Phase D — verification and build

- Run batch-mode compile.
- Run Edit Mode and Play Mode tests for interaction targeting, stamina drain/recovery, crop state changes, money/inventory transaction, mission progression, and heat reinforcement.
- Enter Play mode and complete Mission 1 from start to finish.
- Check Console for errors and important warnings.
- Make a Windows build and run it.
- Capture a final 20–45 second video or a screenshot sequence from the actual build, not only Scene view.

## 13. Definition of done

Do not say “done,” “production-ready,” or “matches Grand Bay” unless all conditions below are met:

- No visible primitive characters or cube houses remain in the playable route.
- The game visibly resembles a dense, tropical, mountainous Caribbean village rather than a generic test scene.
- Road placement, settlement density, and route relationships are demonstrably derived from the supplied Grand Bay references; approximations are labelled.
- Two boys, seller, buyer, police officer, six ambient NPCs, six farm spots/zone, safehouses, dirt trail, and Mission 1 are present.
- Mission 1 can be completed without using the Inspector or debug commands.
- All proximity prompts work and disappear outside range.
- Stamina, heat, money, and inventory visibly change.
- Tomato growth and watering have the required visual states.
- Final Game-view/build screenshots have been inspected, compared with the target, and included in `Docs/MINI-011-VISUAL-REPORT.md`.
- Compile, automated tests, full mission play test, and Windows build pass.
- `PROJECT-HANDOFF.md`, `TASKS.md`, `CHANGELOG.md`, `DECISIONS.md`, and `Docs/ASSET-REGISTER.md` are updated with exact evidence and files changed.
- Ownership is released by returning `Current owner` and `Active task` to `None`.

If any item fails, report the task as incomplete with a precise blocker. Do not quietly reduce scope, move required seller/buyer/police content to a later task, or substitute “the scripts exist” for a playable visual result.

## 14. Final response format

Return:

1. Plain-language summary of what the player can now do.
2. Exact scene and build path.
3. Links/paths to before-and-after screenshots and visual report.
4. Asset packs actually used and their licence records.
5. Test/build results and log paths.
6. Known visual or gameplay limitations.
7. One focused user play-test request covering movement, camera, Grand Bay resemblance, interactions, farming clarity, and performance.

Do not begin a new feature after this. Wait for the user’s play-test feedback.
