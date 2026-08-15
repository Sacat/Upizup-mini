# Up Iz Up Mini — Corrective Grand Bay Mission Build V5 (Franki/Sacat, Help Screen, Animation and Farming Repair)

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

The original reference video is:

`https://youtu.be/gSP57P-IDio?si=BbmpUNeKae6rMkkI`

The video is useful for its **working method**: import finished art, construct a readable scene, test it in Game view, inspect what the player actually sees, and iterate. Its literal pixel-art/top-down appearance is not the target.

Use this additional AI-assisted 3D production workflow reference:

`https://youtu.be/Dto1QAh5gvE?si=V7dM9ewHPoQWxaCD`

Required project note: **"This workflow can be used to help build the Up Iz Up game."**

This second video demonstrates Tripo AI generating 3D characters, vehicles, track/environment props and vegetation, Blender/MCP preparation, Codex/Unity integration, and a playable loop. Use that sequence selectively for original assets when it improves speed. It is a workflow reference, not proof that an AI mesh is game-ready. Every result still needs licence/source recording, topology and scale review, URP materials, colliders, LOD/mobile review, correct hierarchy, rig/avatar validation, animation tests, and screenshots from the actual Unity build.

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

- The two protagonists are specifically named **Franki** and **Sacat**. These names supersede the temporary player-facing names Smart and Strong. Both must be full-body rigged characters with clearly different faces, clothing colours, body silhouettes, HUD portraits/icons, and gameplay identities. Never call them `Player1`, `Player2`, Smart, or Strong in player-facing UI.
- **Both Franki and Sacat must be controllable. Character switching is mandatory and may not be postponed.**
- Use a dedicated, modular `CharacterSwitchManager` (or equivalently focused component), not switch logic buried inside the movement controller, mission script, or HUD.
- Default keyboard/gamepad controls: `Q` or `Tab` switches character and the gamepad D-pad left/right or shoulder button performs the same action. Route this through Unity's Input System and expose a public action suitable for a future mobile portrait/button.
- Switching must smoothly blend the camera to the newly controlled boy, transfer player input, update the active-character outline/marker and HUD name/portrait, and prevent both boys from receiving player input simultaneously.
- The inactive boy must remain a real world character. When nearby, he follows the active boy at a sensible distance using navigation/waypoints without jittering, blocking doors, or walking into traffic. At a safehouse or scripted mission post, he may wait in a believable idle pose. He must never disappear merely because the player switches.
- Preserve each boy's world position, health, and stamina independently. Mission progress, money, inventory, unlocked crops, heat, and owned land are shared unless a later design decision explicitly changes them.
- Make their identities mechanically readable without creating two separate games:
  - **Franki (business/farming archetype):** normal health/stamina, faster farming/interaction speed, and a small legitimate-market selling bonus. HUD perk: `Franki — Better Deals & Farming`.
  - **Sacat (strength/carrying archetype):** higher maximum health, stamina, and carrying capacity with a modestly faster loaded movement speed. HUD perk: `Sacat — Tougher & Carries More`.
- Keep perk values data-driven. Start with Franki completing farm interactions about 15–20% faster and earning about 10% more from legal crops; Sacat receives about 25% more health/stamina and 50% more crop carrying capacity. Record values in `DECISIONS.md` for later balancing.
- The mission and interaction systems must query the active character instead of permanently referencing Franki or Sacat. Either boy can talk, buy, plant, clone, water, harvest, sell, shop, enter property/vehicles and trigger objectives.
- Block switching only during a short non-interruptible interaction, a transition/loading operation, or incapacitation. When blocked, show a brief readable reason instead of silently ignoring input. Do not use switching to escape police detection, reset heat, duplicate inventory, restore stamina, teleport through walls, or bypass mission triggers.
- Add a short first-use switching tutorial near the opening safehouse: `Press Q / Tab to switch between Franki and Sacat.` Require one successful switch, then allow either boy to continue.
- Working idle, walk, run, and interaction animations. Feet should contact ground; no sliding or floating.
- Character Controller/colliders must match the body and stop wall penetration.
- Use the existing Human Basic Motions/Mixamo-compatible animation source when technically suitable and licensed.

### Animation repair is a blocking gate

- Current walking NPCs move in a T-pose. Diagnose the actual cause before expanding missions: model Rig import type, valid Humanoid Avatar, bone mapping, Animator presence, controller assignment, idle/walk state clips, transitions, NavMesh/waypoint velocity parameters, layer weights, culling mode, and root-motion configuration.
- No NPC may translate in a T-pose. Every walking resident needs a visible idle, walk, turn and resume-walk cycle. Standing residents need a valid idle rather than bind pose.
- The current protagonist run looks poor. Replace or tune it with a compatible licensed clip and a locomotion blend tree driven by normalized movement speed. Match controller speed to animation stride and fix foot sliding, leaning, snapping and long crossfades.
- Add jumping now. Space must trigger the Input System `Jump` action. Implement grounded validation, one jump per grounding cycle, vertical velocity/gravity, takeoff, airborne/fall and landing animation states, and clean return to locomotion. No repeated air jumps or animation-only jump with no movement.
- Capture short Game-view video or sequential screenshots proving Franki run/jump/land, Sacat run/jump/land, one NPC idle-to-walk, one NPC turn, and no T-pose.

### Crop hierarchy, fruit attachment, seeds and cloning

- Current tomato growth can leave fruit suspended in the air. Treat this as a hierarchy/state bug: fruit must be a child of or explicitly anchored to the active plant growth-stage transform, use local offsets, and enable/disable with the correct stage.
- On stage change, harvest, plant removal, plot reset, load or replant, deactivate/destroy every fruit belonging to the old stage. There must be no floating fruit after the plant mesh changes.
- Inspect the original main game at `E:\Unity\Up iz up` read-only for its seed-return/plant propagation behaviour. Port the useful design rather than bulk-copying scripts or assets.
- Harvesting returns produce and can yield extra seeds based on crop data. The HUD and inventory must show seed gains clearly.
- Eligible plants support `[E] Clone` or a farming radial/menu action. Cloning consumes the required source/cutting/time or item and creates a clone seedling/item that can be replanted in a valid owned plot.
- Prevent same-frame/infinite duplication, cloning after harvest, cloning inactive stages, cloning without capacity, and save/load duplication. Seed and clone inventory must persist.
- Tests must cover stage attachment, harvest cleanup, seed yield, clone cost/result, capacity, plot ownership and save/load.

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
- Use the real conversational rhythm supplied by the user as a style reference, without copying private timestamps, links or real-person identities. Common patterns include short replies (`True`, `Facts`, `Alright`, `Good point`, `Yea`, `Yah wii`), friendly `bro`/`mn`, `di`, `dem`, `ppl`, `doe`, `Dat`, questions ending in `na`, and reactions such as `Weh weh weh`, `Chhh`, `Lolll` and `Hahaha`.
- Use `allu` for plural `you all` when natural. Code-switch between standard English and informal Dominican speech; not every line needs dialect spelling. Do not force `nuh` or `wii` into every sentence and never use dialect to make NPCs seem unintelligent.
- Sample tone: Franki: `Same thing I was saying. We need our own land.` Sacat: `Yah wii. Once we have the bike, nobody holding us back.` Buyer: `I can take the whole crate, but the small ones not getting full price, nuh.`
- Store dialogue as data with speaker, portrait, line, optional choices, mission/relationship conditions and repeat rules. Do not hard-code conversations into NPC navigation scripts.

## 9. Mandatory playable Mission 1

Build one complete, clear mission rather than disconnected mechanics.

Suggested title: **A Start in Montine**.

1. Franki and Sacat begin together near the Lalay safehouse. The HUD identifies the active boy.
2. Tutorial objective: `Press Q / Tab to switch between Franki and Sacat.` The camera and HUD must visibly transfer before progression.
3. Objective card changes to `Meet the seed seller on the main road.`
4. A subtle GTA-style world marker/minimap cue guides the player. Use a grounded ring, icon, compass/minimap marker, or breadcrumb system — no giant pole and no crude floating arrow.
5. Approach the seed seller with either boy; `[E] Talk` appears. The seller gives shared tomato seeds and directions toward Montine.
6. Objective changes to `Follow the dirt trail to the Montine farm.` The inactive boy follows unless deliberately told to wait at the safehouse.
7. At the farm, provide at least **six usable planting spots** or a small free-placement farming zone. One lone plot is forbidden.
8. Either boy can plant tomatoes, water them, wait through accelerated visible growth, and harvest them. Their perk difference should be observable but must not break mission timing.
9. Soil begins light/dry brown and changes to clearly darker wet brown when watered.
10. Tomato plants progress through at least three visual stages; mature fruit is visibly red, not green.
11. Objective changes to `Sell the tomatoes to the market buyer in Lalay.`
12. Buyer uses `[E] Sell`; shared inventory decreases and money increases. If Franki is active, his documented legal-crop price bonus is applied exactly once.
13. Mission-complete panel summarizes money earned, identifies which boy completed the sale, and unlocks the next mission placeholder.

The HUD must include working money, inventory/crop count, current objective, health, stamina, and heat. Stamina goes down while running and regenerates after the player slows or stops. Heat begins low but is functional.

### Mandatory H Help/Tutorial screen

- Add an Input System action named `Help` with keyboard binding `H`. Pressing `H` opens a polished Help/Tutorial screen; pressing `H` again closes it.
- Escape and a clearly visible `Close`/`Back` button must also close the tutorial. Expose the same action through a future mobile Help icon/button without coupling gameplay code directly to keyboard polling.
- When opened during gameplay, pause single-player simulation safely (`Time.timeScale` or the project's centralized pause service) while keeping UI navigation, input and transitions responsive through unscaled time.
- The Help screen must not open behind or overlap the title menu, pause menu, shop, dialogue, busted screen, mission-complete panel, or another modal. Define predictable modal priority and restore the previous safe gameplay state when it closes.
- Use a real Canvas/TextMeshPro layout, never `OnGUI`. It needs a darkened backdrop, bounded readable card, clear heading, sections/tabs or scrolling, readable text at supported resolutions, and no giant solid colour panel hiding missing text.
- Initially include:
  - Movement: move, camera, walk/run and `Space` jump.
  - Characters: `Q`/`Tab` switching, Franki's farming/deal role and Sacat's strength/carrying role.
  - Interaction: `[E]` prompts, talk, buy, sell, plant, water, harvest, clone, enter.
  - Farming: seeds, dry/wet soil, growth, fruit, harvest, extra seeds and cloning.
  - Missions/navigation: objectives and map/world markers.
  - Status: health, stamina, heat and police response.
  - Economy: inventory, shops, clothing, land/plots, houses and vehicle ownership.
  - Vehicles: entering/exiting and the currently implemented driving controls.
- Prefer contextual/unlocked tutorial sections: unavailable late systems can be visibly marked `Unlocks later` rather than teaching controls that do not exist.
- Show a short first-time hint such as `Press H anytime for Help`, then save that the hint was acknowledged. Opening Help must not reset a mission, clear heat, heal characters, regenerate stamina unfairly, or duplicate inventory.
- Add automated checks for H open/close, Escape close, close-button behavior, time pause/restoration, layout bounds, visible text, modal exclusion, repeated toggling, save/load compatibility and operation after switching between Franki and Sacat.

## 10. Police, farming progression, and vehicle presence

These systems may be simple but must be visibly represented:

- Police officer can be approached and shows `[E] Talk` while calm.
- Heat increases from a controlled debug/test action or carrying/selling an illegal crop in a test path.
- At high heat, police changes behaviour; at 100 heat, one additional officer can spawn. Do not build full combat unless required for safe testing.
- Crop data must support tomatoes, bananas, carrots, Bushers, Black Sugar, and Purple. Only tomatoes need the complete Mission 1 loop, but other crop prefabs/data must show distinct mature produce: red tomatoes, banana bunches, carrot tops/root indication, and visually distinct weed tiers.
- Include at least one parked motorcycle/bike and one ordinary road vehicle to establish the setting. A single simple drivable vehicle is desirable after Mission 1 works, but it must not block completion of the core visual/farming slice.
- Design farming land as data-driven zones so later missions can unlock or purchase additional lots and let the player place a permitted farm lot in suitable remote land.

## 10A. Full mission and ownership progression

After the animation and fruit/seed defects pass their visual gates, implement missions as data-driven chapters rather than disconnected test objects. The build should support this progression without requiring all late-game content to be finished in one pass:

1. **First Plot:** meet the farming contact, receive tomato seeds, plant, water, harvest, gain extra seeds, and sell to a separate buyer.
2. **Clone the Crop:** learn plant cloning/propagation, create a clone seedling, expand into a second valid spot, and prove persistence after save/load.
3. **Lalay Delivery:** carry/load produce, travel by foot or bike, sell before a timer expires, and update money/reputation.
4. **Buy Your Ground:** inspect a marked land parcel, see price/requirements, purchase it, and unlock additional farm plots. Ownership must persist and block planting before purchase.
5. **Market Pressure:** compete against a fictional rival through price, quality, delivery or dialogue choice.
6. **Boss Work:** meet a fictional boss/employer, accept work, complete a delivery, and show how deductions/exploitation affect payment.
7. **Heat:** introduce an illegal-crop test path, visible heat gain, calm police at low heat, pursuit at high heat, and one reinforcement at maximum heat.
8. **Wheels and Water:** acquire/borrow a vehicle, then later introduce a boat as transport/story content. Boats must use fictional missions and must not reproduce real trafficking routes or evasion methods.

Mission UI needs objective text, readable world/minimap guidance, optional dialogue choices, completion/failure conditions, rewards, unlocks, and save/load state. Verify each finished mission by playing it from its real starting state to completion.

## 10B. Shops, clothing, property and vehicles

- Add modular shop categories for seeds, farming tools, chains/accessories, tops, bottoms, shoes and upgrades.
- Use clearly fictional brands and original logo shapes. User examples include `Mike` and `Lacostes`, but do not copy Nike/Lacoste logos, exact designs, signature patterns or branded meshes. Prefer distinctive fictional labels such as `Mike Athletics`, `Bayline`, `Wavemaker`, `Crové`, `Highland Step` and `Lalay Gold`.
- Clothing purchases must preview the correct Franki/Sacat model, check money, prevent duplicate charges, save ownership, allow equip/unequip, and preserve animation rigging/material compatibility.
- Chains/accessories need proper bone attachment and must not float, clip severely, or remain behind after switching outfits/characters.
- Create purchasable land/plot definitions with price, boundary, allowed crop types, maximum planting positions, owner state and map marker.
- Houses/safehouses need purchase or mission unlock state, collision, usable doors/interiors where promised, spawn/save functions and map icons.
- Cars, motorcycles/bikes and boats need data-driven ownership/access state. Start with one properly tested drivable land vehicle before multiplying vehicle types.
- Do not use unverified third-party assets or AI-generated models without recording source/licence and completing scale, collider, material, LOD and mobile-performance checks.

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

### Phase C — people, prompts, Mission 1, HUD and Help

- Add the protagonists, seller, buyer, police, ambient NPCs, interactions, farming loop, objective guidance, working bars, the H Help/Tutorial screen, and mission completion.

Gate C screenshots:

1. `[E] Talk` over seller at correct range.
2. Buyer and police both visible in the Lalay area.
3. Dry versus watered soil.
4. Mature tomato plant with red fruit.
5. Walking residents along the road.
6. Mission-complete screen after selling.

### Phase D — verification and build

- Run batch-mode compile.
- Run Edit Mode and Play Mode tests for interaction targeting, stamina drain/recovery, crop state changes/fruit attachment, seeds/cloning, money/inventory transactions, land ownership, shop ownership/equipping, mission progression, heat reinforcement, Franki-to-Sacat switching, Sacat-to-Franki switching, input ownership, camera transfer, independent health/stamina persistence, shared inventory persistence, inactive-character following, jump grounding and prevention of perk/transaction duplication.
- Enter Play mode and complete Mission 1 from start to finish.
- Check Console for errors and important warnings.
- Make a Windows build and run it.
- Capture a final 20–45 second video or a screenshot sequence from the actual build, not only Scene view.

## 13. Definition of done

Do not say “done,” “production-ready,” or “matches Grand Bay” unless all conditions below are met:

- No visible primitive characters or cube houses remain in the playable route.
- The game visibly resembles a dense, tropical, mountainous Caribbean village rather than a generic test scene.
- Road placement, settlement density, and route relationships are demonstrably derived from the supplied Grand Bay references; approximations are labelled.
- Franki and Sacat are both controllable; switching works in both directions; the camera, HUD, input, perks, follower state, shared progression, and independent character statistics behave as specified.
- Franki and Sacat have visually acceptable run and jump animations; walking NPCs never move in T-pose.
- Tomato fruit remains attached to the active plant stage and disappears correctly on harvest/reset; extra seeds and cloning work without duplication exploits.
- Two boys, seller, buyer, police officer, six ambient NPCs, six farm spots/zone, safehouses, dirt trail, and Mission 1 are present.
- Mission 1 can be completed without using the Inspector or debug commands.
- All proximity prompts work and disappear outside range.
- Stamina, heat, money, and inventory visibly change.
- `H` opens the readable Help/Tutorial screen; `H`, Escape and Close dismiss it; gameplay pause/restoration and modal behavior pass verification.
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
