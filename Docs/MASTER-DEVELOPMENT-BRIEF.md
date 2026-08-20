# UP IZ UP Mini — Master Development Brief
## Reuse-First Systems + Parallel Hi3D Asset Production Edition
**Project:** `E:\Unity\Up Iz Up Mini`  
**Unity:** `6000.3.10f1`  
**Read-only reference project:** `E:\Unity\Up iz up`  
**Primary first-bike asset:** `E:\Unity\Up Iz Up Mini\Assets tmax 560.glb`

---

## 1. Core Production Rule

UP IZ UP Mini must be developed as a sequence of small, testable MINI tasks.

For each task:

1. Read the current project state before editing.
2. Preserve working systems unless there is a demonstrated reason to replace them.
3. Research the best technical or visual approach where needed.
4. Define acceptance criteria.
5. Implement one cohesive feature.
6. Compile and validate.
7. Test in Unity Editor Play Mode whenever the feature can be tested there.
8. Render screenshots for visual changes and inspect them.
9. Fix visible problems before calling the task complete.
10. Update `PROJECT-HANDOFF.md`.
11. Only build a standalone Windows EXE at milestone gates or when standalone-specific behavior must be tested.

The master brief is a roadmap. It is NOT permission to implement every item at once.

---

## 2. Mandatory Files to Read Before Development

Before editing, read:

- `CLAUDE.md`
- `PROJECT-HANDOFF.md`
- `TASKS.md`
- `DECISIONS.md`
- `Docs/STORY.md`
- `Docs/DIALOGUE-REFERENCE.md`
- `Docs/ASSET-REGISTER.md`
- this file

Also inspect the latest relevant source code instead of assuming a system is missing.

The reference project at `E:\Unity\Up iz up` is READ ONLY. Assets, scripts, materials, animations, and ideas may be studied or copied into the Mini project when appropriate, but the reference project must never be modified.

---

# 3. Faster Verification Workflow

## Normal MINI task

Do NOT produce a Windows EXE automatically.

Use:

1. C# compile check.
2. Scene-builder/rebuild validation if the task touches generated content.
3. Editor/static validation scripts where useful.
4. Unity Editor Play Mode for interaction testing.
5. Unity rendered snapshots for visible features.
6. User hands-on test in the Unity Editor when feel/input matters.

This should be the default loop.

## Milestone build

Create a Windows standalone build only when one of these is true:

- a major system has reached a milestone;
- several MINI tasks have accumulated and need integration testing;
- testing behavior that differs from the Editor;
- testing save paths, resolution/window behavior, packaging, startup, or standalone input;
- preparing a version for another tester;
- the user explicitly asks for an EXE.

Suggested milestone cadence:

- after a major gameplay system;
- after roughly 4–6 smaller stable MINI tasks;
- before large architecture changes;
- before a tagged playable release.

## Play Mode performance

Unity Editor Play Mode is the primary rapid iteration target.

Do not disable Domain Reload or Scene Reload merely for speed until the project has been audited for static state. If faster Enter Play Mode settings are later enabled, first ensure all static gameplay state is explicitly reset correctly.

---

# 4. Research Before Building

Research is required for:

- real-world visual appearance;
- plants and strains;
- motorcycles and vehicles;
- clothing/jewelry/accessories;
- character animation;
- navigation/pathfinding;
- architecture;
- performance-sensitive systems.

Technical research priority:

1. Official Unity documentation.
2. Official Blender documentation.
3. Official tool documentation.
4. Reputable open-source projects with clear licenses.
5. Established/free Unity Asset Store solutions.

Visual research may use Google Images and other web references for comparison, but web images are reference material only unless their license clearly allows game use.

## Reuse-First System Rule

Before writing any substantial new system from scratch, Claude must search for a proven existing solution and classify it as:

- **ADOPT** — use mostly as-is;
- **ADAPT** — reuse the useful core and add a thin UP IZ UP integration layer;
- **REFERENCE ONLY** — study the implementation but do not import it;
- **REJECT** — too heavy, outdated, incompatible, badly licensed, or disruptive.

For every candidate, record:

- current maintenance/activity;
- license;
- Unity version compatibility;
- render-pipeline compatibility;
- package size;
- dependencies;
- mobile/runtime cost;
- how much of the current working project it would replace;
- exact feature it would solve.

Do not import a giant framework simply because it exists.

### Systems Claude must evaluate before rebuilding

**NPC navigation**
- First choice: Unity AI Navigation / NavMesh.
- Use custom UP IZ UP code only for role logic, stuck detection, gang behavior, mission targets, and recovery.
- Do not rebuild pathfinding/A* unless the official system genuinely cannot solve the need.

**Rider hand/foot placement**
- Evaluate Unity Animation Rigging for TMAX hand targets, foot targets, and later vehicle seating.
- Keep the existing `HumanoidAnimationManager` as the animation-state foundation.

**Wardrobe / clothing**
- Evaluate UMA 2 / Unity Multipurpose Avatar in an isolated test.
- UMA is a useful reference because it already implements concepts such as wardrobe slots/recipes and runtime avatar customization.
- Do NOT import it blindly into production; it is a large framework and UP IZ UP may only need a lightweight subset.
- Preferred outcome may be to reuse UMA design ideas while keeping the existing Humanoid characters and equipment foundation.

**Dialogue**
- Evaluate Yarn Spinner and ink before creating a complete custom dialogue language.
- Test ONE of them in a tiny throwaway conversation first.
- The proof must support: two speakers, one internal thought, one condition, one mission event, and save-state compatibility.
- Do not install both.

**TMAX motorcycle**
- Audit the user's already-owned/local vehicle packages first.
- Inspect reputable open-source motorcycle controllers with clear licenses.
- Use an isolated TMAX test scene before production integration.
- Prefer ADAPT or REFERENCE ONLY if the controller is old but its balance/steering math is useful.
- Do not blindly copy an entire repository.

### Current researched candidates

- Unity AI Navigation.
- Unity Animation Rigging.
- UMA 2 / Unity Multipurpose Avatar.
- Yarn Spinner for Unity.
- ink Unity Integration.
- MIT-licensed Unity motorcycle-controller projects.

The existence of these candidates means the default question is no longer "how do we code this from zero?" It is "which proven part can we reuse safely?"

---

# 5. Asset Acquisition Order

Always try the cheapest and most reusable source first:

1. Existing `Up Iz Up Mini` asset.
2. `E:\Unity\Up iz up` read-only reference project.
3. Existing local asset collection.
4. Free legally reusable assets.
5. Unity Asset Store free assets.
6. Mixamo for suitable Humanoid animation.
7. Blender creation/adaptation/optimization.
8. Hi3D for unique assets where image-to-3D offers a real advantage.
9. Other paid/AI generation tools only if Hi3D or existing assets cannot solve the problem well.

Never generate a replacement for a good asset that already exists.

Every external asset must be recorded in `Docs/ASSET-REGISTER.md` with source, license, purpose, and optimization details.

---

# 6. Hi3D Asset Queue

Hi3D should be treated as a parallel asset-production service, not as the main game-development engine.

Its main purpose in UP IZ UP is to create unique props or difficult shapes while Claude continues coding, dialogue work, mission logic, testing, or Blender preparation.

Hi3D may be used as a **parallel asset queue** when the user's plan/account supports concurrent jobs. The web subscription currently advertises concurrency limits that vary by plan, so Claude should use the actual account limit rather than assuming unlimited parallel generation.

Maintain:

`Docs/HI3D-ASSET-QUEUE.md`

Suggested fields:

- Asset ID
- Asset name
- Reference image approved?
- Priority
- Generation mode
- Status
- Credit cost
- Hi3D task/link if available
- Blender cleanup status
- Unity verification status

Statuses:

- IDEA
- RESEARCH
- NEED USER APPROVAL
- APPROVED
- GENERATING
- GENERATED
- REJECTED
- BLENDER CLEANUP
- UNITY TEST
- ACCEPTED

### Parallel-processing rule

When an approved Hi3D asset is generating, Claude should immediately switch to independent work instead of waiting.

Good work to do while generation runs:

- equipment sockets;
- Fit Rig tooling;
- item/shop data;
- dialogue;
- mission conditions;
- NPC navigation;
- UI;
- save/load extensions;
- vehicle interface code;
- rider IK targets;
- tests;
- documentation;
- Blender cleanup of a previously completed model.

Do not start unrelated large architecture changes merely to stay busy.

A good pattern is:

**Hi3D generates asset → Claude builds the receiving system → Blender cleans generated asset → Unity plugs it in.**

## Asset Generation Pipeline

### Gate A — Need

Before spending credits, answer:

- Does the Mini project already contain this?
- Does the large UP IZ UP project already contain it?
- Is a good free licensed asset available?
- Can Blender produce it more reliably and cheaply?
- Does AI generation actually add value?

If not, use the existing/free asset.

### Gate B — Reference

Before Hi3D generation:

1. gather strong visual references;
2. create a clean concept/reference image if needed;
3. remove confusing backgrounds;
4. use multiple views for complex objects when possible;
5. show the reference image(s) to the user for approval if the asset is visually important.

Do not spend expensive generation credits on an unapproved concept for major assets.

### Gate C — Cheap geometry proof

For important assets, first use a lower-cost/standard or geometry-first generation if available.

Check:

- silhouette;
- proportions;
- major shapes;
- missing parts;
- whether the object is actually usable.

Reject bad geometry before paying for higher-resolution texture/PBR stages.

### Gate D — Final generation

Only after geometry is approved:

- generate final texture/PBR version if needed;
- export GLB or FBX for Unity/Blender;
- keep the original generated asset outside the optimized runtime folder.

### Gate E — Blender cleanup

Generated models are not automatically game-ready.

Blender pass:

1. inspect scale and transforms;
2. inspect topology/poly count;
3. remove hidden/unnecessary geometry;
4. decimate/remesh/retopologize as appropriate;
5. preserve silhouette;
6. fix normals;
7. UV/bake textures where needed;
8. reduce material count;
9. make LODs if worthwhile;
10. export game-ready FBX/GLB.

### Gate F — Unity visual proof

Import and render the asset inside the real game.

Compare to the approved reference.

Only then mark it usable.

---

# 7. Parallel Work Strategy

Do not make Claude sit idle while an online model is generating.

Whenever a Hi3D job is running, Claude can continue with tasks that do not depend on the unfinished model, such as:

- dialogue data;
- mission logic;
- NPC behavior;
- navigation;
- UI;
- save data;
- editor tooling;
- asset sockets;
- vehicle interface code;
- Blender prep scripts;
- testing existing systems;
- documentation/handoff updates.

Do not start a second large architecture change merely to stay busy. Parallel work should be related and low-risk.

Example:

1. User approves a puffed Mariner chain reference.
2. Hi3D/Blender chain asset work begins.
3. While it is processing, Claude builds the standardized equipment socket/Fit Rig data structure.
4. When the model is ready, it plugs into a system that already exists.

---

# 8. Visual Verification Standard

Visible features are not complete because they compile.

For each important visual feature:

1. find 2–5 useful references;
2. list the visual characteristics being matched;
3. build/import the asset;
4. render it from the real Unity scene;
5. inspect close-up and gameplay-distance views;
6. compare;
7. correct major mismatch;
8. render again.

For character gear:

- front;
- side;
- three-quarter;
- normal gameplay camera;
- walking/running pose if clipping is possible.

For vehicles:

- side;
- front three-quarter;
- rear three-quarter;
- mounted rider;
- turning;
- wheelie;
- gameplay camera.

For plants:

- whole plant;
- close-up flowering section;
- young/ripe stage;
- multiple strains side-by-side.

---

# 9. Immediate Roadmap

## MINI-051 — Real strain flower/bud visuals

Fix the problem where the strain's designated bud color is not visible enough.

Do not recolor the complete Zeb plant.

Support separate visual concepts for:

- foliage;
- flowering buds/colas;
- primary bud color;
- optional secondary bud color;
- pistils;
- trichome/frost impression.

Research real strain/bud references first.

The plant must remain botanically believable rather than becoming a solid purple/blue/orange plant.

Reuse the existing crop visual/breeding architecture.

Visual proof is required.

No EXE build required unless the user requests it.

---

## MINI-052 — NPC navigation and stuck recovery

Replace/directly improve simplistic direct steering with proper navigation.

NPCs and companions must:

- path around houses;
- stop repeatedly walking into walls;
- avoid excessive jitter;
- recover if stuck;
- re-path when target changes.

Create reusable stuck recovery based on low displacement while desired movement remains high.

Recovery order:

1. recompute path;
2. try nearby valid path point;
3. rotate/recover;
4. only after repeated failure, safely relocate to a nearby valid navigation position.

Test in Editor Play Mode.

---

## MINI-053 — Persistent mission/objective UI + dialogue foundation

Mission details must not disappear before the player can read them.

Use:

- temporary mission-start/completion banner;
- persistent small objective card;
- progress count;
- optional distance;
- mission name.

Build a data-driven dialogue foundation that later supports:

- normal dialogue;
- inner thoughts;
- conditional lines;
- mission dialogue;
- shop dialogue;
- faction dialogue;
- story revelations.

---

## MINI-054 — Dialogue/dialect bible and opening conversation

Expand `Docs/DIALOGUE-REFERENCE.md` or create `Docs/DIALECT-LEXICON.md`.

Source-of-truth terms include:

- Zeb
- volehing
- Awtic
- fresh
- wah is di word
- what is di word diah
- Not Ah Word
- Domnicah
- diah
- awa
- awa wii
- Gwa Bay
- Gwada
- go up
- Zion
- scrub
- fadah
- gasah

Use slang naturally and sparingly.

Rasta uses a Jamaican speech style.

Most other local characters use Dominican/Gwa Bay speech based on the user's supplied dialogue.

Opening story:

- the two boys were kicked out of school;
- they are hungry and struggling;
- they need money;
- one suggests Zion/Zeb;
- the other is hesitant;
- they initially consider normal crops;
- story begins naturally through their conversation.

---

## MINI-055 — Boss consolidation

Move toward two major bosses:

### Boss J
- lower-level boss;
- Bushers;
- street connections;
- substantial chain;
- useful at first;
- later suspicious story behavior.

### Boss C
- bigger boss;
- more status;
- Strong;
- higher-value progression;
- larger/multiple chains;
- nice black SUVs nearby.

Migrate useful progression responsibilities from old placeholder bosses without destroying working systems.

---

## MINI-056 — Rasta mentor and strain progression

Older Rasta mentor teaches advanced strain work after missions.

Progression:

- Bushers from Boss J;
- Strong from Boss C;
- advanced strains learned through story/training;
- Black Sugar;
- Purple;
- Blue Cheese;
- later hybrids through the existing breeding system.

---

## MINI-057 — Normy

Create crooked police NPC Normy.

Normy is not representative of all police.

Support:

- side missions;
- information;
- relationship/reputation;
- self-interested assistance.

---

## MINI-058 — Factions

Player gang: **Not Ah Word**  
Rival gang: **Dog Life**

Dog Life controls Lalay initially.

Support up to roughly ten Dog Life members in major block encounters, but use pooling/activation/distance logic instead of leaving all NPCs fully active across the map.

Player gang can eventually recruit up to four members.

Assignments:

- follow;
- guard plantation;
- stay at block/home;
- unavailable/injured;
- later errands.

---

## MINI-059 — Plantation guarding/theft mystery

At low reputation, unattended Zeb can be volehed.

Rules:

- risk begins after player has been away;
- not guaranteed every time;
- reputation affects risk;
- assigned guard reduces/prevents risk.

Do not reveal Dog Life immediately.

The story should first create suspicion.

---

## MINI-060 — Gardey Zafeh reveal

Use Gwada/Gardey Zafeh story progression to discover the real source of the plantation theft.

Only after the reveal should Dog Life rivalry become openly active.

---

## MINI-061 — Gwa Bay Health Center + La Jol

Create:

- **Gwa Bay Health Center**
- **La Jol** police station/jail

Police knockdown/arrest should lead to La Jol when appropriate rather than always using the normal death flow.

---

## MINI-062 — Lalay house

Purchasable house.

First version supports:

- ownership;
- rest;
- save interaction;
- vitals recovery;
- selectable respawn.

A complex interior is optional until the gameplay loop is proven.

---

## MINI-063 — Visible Gwada boat cycle

The boat must visually leave the jetty, travel away, disappear naturally, remain away, then return near completion time.

Early courier trip pays only a fraction of the value the player later earns when going personally.

---

# 10. TMAX — First and Only Bike Prototype

The project already has:

`E:\Unity\Up Iz Up Mini\Assets tmax 560.glb`

Do not search for or generate a replacement TMAX unless that file is proven unusable.

Do not implement Tracer yet.

The TMAX is the single vehicle used to prove the bike architecture.

Before editing it:

1. inspect the GLB;
2. measure dimensions;
3. inspect hierarchy;
4. identify whether wheels are separate;
5. inspect pivot orientation;
6. inspect materials;
7. inspect polygon count;
8. render it;
9. compare proportions against real TMAX references.

If the model needs cleanup, use Blender.

## Manual motorcycle-controller reference location

A small reference package containing the open-source motorcycle controller scripts, README, and MIT license has been prepared separately. It intentionally excludes the repository's sample motorcycle and rider art.

The user should download and extract:

`Unity3D-Motorcycle-Controller-REFERENCE.zip`

to this exact Windows research location:

`E:\Assets\Research\Unity Systems\`

After extraction, the final folder should be:

`E:\Assets\Research\Unity Systems\Unity3D-Motorcycle-Controller-REFERENCE\`

**Do not place this folder into `E:\Unity\Up Iz Up Mini\Assets` yet.** It is research/reference material until MINI-064/065.

When the bike task begins, Claude must inspect that folder and explicitly choose:

- ADOPT;
- ADAPT;
- REFERENCE ONLY;
- REJECT.

If any code is eventually copied into the game, preserve the included MIT license/copyright notice and isolate third-party code under a clearly named third-party folder.

The actual motorcycle model remains:

`E:\Unity\Up Iz Up Mini\Assets tmax 560.glb`

Do not use the reference repository's motorcycle/rider art.

---

# 11. TMAX Development Sequence

## MINI-064 — TMAX asset preparation

Prepare `tmax 560.glb`.

Required:

- correct scale;
- correct orientation;
- separated/usable wheel transforms;
- usable steering root;
- reasonable poly count;
- simple collision representation;
- cleaned materials;
- optional LODs.

Create one reusable bike prefab.

No riding yet if asset preparation is substantial.

---

## MINI-065 — TMAX physics prototype

Build arcade-realistic bike physics.

Target:

- stable and fun open-world scooter behavior;
- believable acceleration/braking;
- steering;
- wheel rotation;
- visual lean;
- stable low-speed control;
- collision/crash recovery.

Do not turn it into a hardcore motorcycle simulator.

Use Rigidbody/appropriate wheel-grounding approach after research.

Test only this bike.

---

## MINI-066 — Rider mounting + IK

Use existing `HumanoidAnimationManager`.

Add reusable riding actions:

- mount;
- seated/riding;
- dismount.

Use IK/rig constraints for:

- left/right hands to handlebars;
- left/right feet to foot positions.

Do not author a unique rider animation system for every motorcycle.

---

## MINI-067 — Wheelie

Wheelie is intentional gameplay.

Reuse an existing context-sensitive key when mounted rather than permanently adding another key if possible.

Requirements:

- requires a reasonable speed/torque window;
- controllable;
- front comes up progressively;
- limited balance;
- rider pose reacts;
- steering remains possible but reduced;
- recovery when released;
- no instant 90-degree rotation.

---

## MINI-068 — Bike gameplay integration

Add:

- purchase/ownership;
- spawn/storage;
- mission/hint;
- save state where appropriate;
- phone/recall interaction later if useful.

Only after the TMAX works should the architecture be generalized for other bikes/vehicles.

---

# 12. Vehicle Architecture

Avoid filling `PlayerController.cs` with bike-specific branches.

Preferred reusable concepts:

- `IVehicle`
- `VehicleDefinition`
- `VehicleSeat`
- `VehicleInteractor`
- `VehicleController`
- `VehicleOwnership`

Player states:

- OnFoot
- EnteringVehicle
- RidingDriving
- ExitingVehicle
- Crashed/Recovery when necessary

Cars come later.

---

# 13. Clothing and Accessory Strategy

Clothing and accessories are now an active **Hi3D + Blender + Unity** production category.

Priority examples:

- puffed Mariner chains;
- scarves/bandanas;
- caps;
- shades;
- watches;
- shoes;
- shirts;
- polos;
- shorts;
- pants;
- boss jewelry.

Do not rebuild the existing Humanoid bone-attachment foundation. Extend it.

## Brand-inspired direction

The game can use a deliberately "familiar but off" streetwear style, but production assets should use **fictional/parody-style branding**, not exact unauthorized trademark logos.

Examples:

- **Mike** athletic shoes/shirts with an original abstract wing/check-style mark;
- **Lacostes** polo with an original fictional reptile/animal emblem;
- **Adibas** shorts with original stripe/triangle graphics;
- **Pumba** runners with an original cat/animal mark;
- **Ray-Bam** shades.

The fictional marks can be:

- upside down;
- mirrored;
- unusually positioned;
- oversized;
- recolored;
- stylized;

but rotating an exact Nike swoosh or exact Lacoste crocodile is NOT considered enough transformation by itself.

If the user supplies artwork they own or have permission to use, use it according to that permission.

## Hi3D clothing-generation workflow

For each important garment/accessory:

1. search current assets first;
2. research shape/material/style references;
3. create/select a clean concept/reference image;
4. get user approval before costly generation;
5. use geometry-first/cheaper generation where practical;
6. reject bad geometry before premium texture/PBR work;
7. use multi-view input for objects where front/back/side shape matters;
8. export GLB/FBX;
9. clean/retopologize/decimate in Blender;
10. fit to the UP IZ UP Fit Rig;
11. import into Unity;
12. test on both protagonists;
13. test walk/run/action clipping;
14. create LOD where useful;
15. record source/generation details.

## Small parallel generation batches

Do not launch dozens of models.

Start with 3–6 approved high-value items.

Suggested first batch:

1. player puffed Mariner chain;
2. Boss J heavier chain;
3. streetwear scarf/bandana;
4. fictional Lacostes-style polo;
5. fictional Mike-style athletic shirt;
6. fictional Mike-style shoes.

While those generate, Claude should work on the equipment/Fit Rig system, dialogue, navigation, mission logic, or another independent task.

## Equipment data

Create/extend standardized equipment definitions with:

- item id;
- display name;
- category;
- prefab;
- attachment mode;
- Humanoid bone/socket;
- local position;
- local rotation;
- local scale;
- left/right variant when required;
- body-part hiding rule;
- price;
- rarity/status;
- LOD;
- secondary motion;
- fictional brand/style metadata;
- source/license/generation metadata.

Categories:

- chain;
- scarf/bandana;
- watch;
- shades;
- hat;
- shirt/polo;
- pants/shorts;
- shoes.

## Attachment classes

**Rigid accessories**
- cap
- shades
- watch
- some jewelry

Use Humanoid bone/socket attachment.

**Neck/chest accessories**
- chain
- scarf/bandana

Use dedicated neck/chest fitting sockets. They must sit on the body, not float.

**Shoes**
- use left/right foot fitting separately;
- hide/replace original footwear geometry/material where necessary.

**Shirts/polos**
- should be skinned garments or modular body/clothing meshes that follow the Humanoid skeleton;
- never simply parent a rigid shirt mesh to the chest.

**Pants/shorts**
- same principle as shirts;
- use compatible skinned geometry.

Before building a custom wardrobe runtime, evaluate whether UMA 2 can provide the useful slot/recipe architecture without forcing the entire project onto UMA.

---

# 14. UP IZ UP Fit Rig

Create a developer-only standardized fitting mannequin.

Purpose:

- fit accessories once;
- store reusable offsets;
- avoid guessing placement separately for every character.

Define:

- head;
- neck;
- chest;
- waist;
- hips;
- wrists;
- feet;
- standard height;
- Humanoid sockets.

Provide editor gizmos or clear fitting markers.

---

# 15. Puffed Mariner Chain

Target:

- real puffed Mariner link shape;
- lies around neck and upper chest;
- believable thickness;
- subtle movement;
- not floating;
- not clipping badly.

Variants can include:

- regular player;
- upgraded player;
- Boss J;
- Boss C.

Boss C should visually exceed Boss J.

Research real 14K/18K pieces and current real-world prices when implementing economy values.

If no suitable licensed model exists, compare two production paths:

### Path A — Blender procedural chain
1. model one correct puffed Mariner link;
2. array along a curve;
3. tune chest/neck shape;
4. optimize;
5. create LODs;
6. fit using the Fit Rig.

### Path B — Hi3D
1. approve a clean chain reference image;
2. generate geometry first;
3. inspect the actual link shape and chest curve;
4. reject it early if links are malformed;
5. only then generate final texture/PBR;
6. Blender cleanup;
7. Fit Rig;
8. Unity render.

Use whichever produces the better chain with fewer credits and less cleanup.

Boss J and Boss C should share the same underlying equipment system. Boss C gets greater visual status through size, quality, or multiple pieces rather than an entirely separate code path.

---

# 16. Gameplay World Priorities

After the foundation and TMAX:

- better police heat presentation using fire emojis;
- stronger arrest system;
- Not Ah Word vs Dog Life progression;
- plantation guard assignments;
- character phone calls;
- Gwada travel progression;
- Granny/Moutey mission;
- stronger character development;
- better Awtic/accessories;
- music;
- precise Lalay/Gwa Bay visual refinement;
- final world polish.

Gameplay and story come before expensive cosmetic polishing.

---

# 17. Police Heat

Use fire icons rather than GTA stars:

- 🔥 low attention
- 🔥🔥 active search
- 🔥🔥🔥 stronger response
- 🔥🔥🔥🔥 severe response
- 🔥🔥🔥🔥🔥 maximum response

Difficulty should come from number, coordination, response speed, positioning, and pressure rather than only inflated health.

---

# 18. Character/World Visual Hierarchy

### Vagrant
Near shanty house:
- dirty/worn clothing;
- no shoes;
- visibly poor/disheveled.

### Boss J
- better Awtic;
- visible chain;
- street status.

### Boss C
- richer clothes;
- larger/multiple chains;
- black SUVs nearby;
- stronger presence.

Do not make every NPC share the same visual wealth level.

---

# 19. Manual User Approval

Ask the user to intervene only when needed.

Good approval gates:

- selecting one of several concept images;
- approving a major character/vehicle/accessory reference;
- logging into Hi3D;
- approving credit use;
- accepting a license;
- choosing between materially different visual directions.

When intervention is required, give exact steps:

1. site/application;
2. item/image;
3. button/menu;
4. settings;
5. export format;
6. destination path;
7. what the user should report back.

---

# 20. Definition of Done

A MINI task is complete when applicable checks pass:

- architecture respects existing systems;
- compile succeeds;
- no obvious Unity errors;
- Editor Play Mode behavior works;
- visual work has actual rendered proof;
- user-tested feel/input when required;
- no unnecessary EXE build;
- asset source/license recorded;
- performance impact considered;
- `PROJECT-HANDOFF.md` updated;
- ownership released.

Standalone EXE generation is a milestone verification step, not the default end of every MINI task.

---

# 21. Reuse/Research References

Claude should re-check these at implementation time because packages can change.

- Unity AI Navigation manual: `https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html`
- Unity Animation Rigging manual: `https://docs.unity3d.com/Manual/com.unity.animation.rigging.html`
- UMA GitHub: `https://github.com/umasteeringgroup/UMA`
- UMA 2 Asset Store: `https://assetstore.unity.com/packages/3d/characters/uma-2-35611`
- Yarn Spinner for Unity: `https://github.com/YarnSpinnerTool/YarnSpinner-Unity`
- ink Unity integration: `https://github.com/inkle/ink-unity-integration`
- MIT motorcycle-controller reference: `https://github.com/MuhammetFatihYilmaz/Unity3D-Motorcycle-Controller`
- Hi3D pricing/concurrency: `https://www.hi3d.ai/pricing`
- Hi3D Blender workflow: `https://www.hi3d.ai/blog/hi3d-blender-plugin-guide`

Do not treat a link in this list as automatic approval to import. Apply the ADOPT / ADAPT / REFERENCE / REJECT checklist first.

---

# 22. NEXT

Continue from the latest handoff.

Do **MINI-051 only** first:

**real visible Zeb strain flowering/bud treatment with strong visual-reference verification.**

Do not start TMAX work until MINI-051 is stable unless the user explicitly changes priority.

When TMAX work begins, use only:

`E:\Unity\Up Iz Up Mini\Assets tmax 560.glb`

as the first-bike target.

Do not generate another TMAX.
