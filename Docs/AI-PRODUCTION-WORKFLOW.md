# Up Iz Up Mini — AI Production Workflow

This is the canonical working agreement for the user, Codex, Claude Code/Work, Unity, Blender, QGIS, Mixamo, and Hitem3D. `AGENTS.md` remains the authority for repository safety and ownership.

## Production goal

Build a geographically recognizable, stylized low-poly Caribbean game that runs well on modest PCs now and is designed for mobile controls, flexible aspect ratios, bounded simulation, LODs, texture atlases, pooling, and limited dynamic lighting.

The workflow optimizes for four things in this order:

1. Preserve the user's visual intent.
2. Avoid duplicate or conflicting AI work.
3. Prove changes with evidence appropriate to the claim.
4. Spend subscriptions and generation credits only after a cheap approval gate.

## The production loop

```text
INTENT
  -> REFERENCES
  -> WORK PACKET + BUDGET
  -> READ-ONLY AUDIT
  -> CHEAP PREVIEW
  -> USER APPROVAL WHEN VISIBLE/IMPORTANT
  -> SINGLE-OWNER IMPLEMENTATION
  -> COMPILE + VALIDATE
  -> SCREENSHOT OR MOTION CAPTURE
  -> USER ACCEPT / REVISE
  -> VISUAL LOCK + HANDOFF
```

This is the project's Ralph-style harness: a bounded improvement loop with a clear scorecard and stopping condition. It is not an open-ended agent that keeps changing the game until tokens run out.

Before implementation, either agent should run:

```powershell
& .\Tools\AIWorkflow\Invoke-Preflight.ps1 -Agent Codex -TaskId MINI-###
```

Use `-Agent Claude` for Claude. The check fails on an ownership mismatch and reports missing workflow files, Unity version, a running Unity Editor, and dirty-tree risk. It is a guardrail, not permission to edit files outside the reservation.
Add `-Detailed` only when the full dirty-file list is needed; the default keeps agent context compact.

## Roles and parallel work

| Lane | Good parallel work | Single-integrator work |
|---|---|---|
| World | OSM/QGIS research, photo classification, anchor review, performance audit | Terrain, scene, road splines, generated world data, NavMesh bake |
| Assets | Reference sheets, provenance checks, Hitem3D candidate review, Blender inspection | Import settings, final prefab, material/pivot/collider/LOD integration |
| Characters | Rig diagnostics, animation search/review, wardrobe specification | Avatar/import changes, Animator, rig constraints, character prefab |
| Code | Non-overlapping scripts, tests, validators, documentation | Shared managers, generated-scene builder, package/project settings |
| QA | Log review, screenshot comparison, test-plan writing | Live Unity session, player build, final visual acceptance capture |

The main agent is the integrator. Parallel agents return short evidence-backed reports; they do not all edit the same feature. Codex and Claude may work at the same time only when their reserved files do not overlap and neither touches a protected Unity surface.

## Approval classes

### Class A — proceed and report

Documentation, diagnostics, tests, validators, and small code changes with no intended visible design change. Compile and test evidence is sufficient unless behavior is motion-dependent.

### Class B — preview, implement, visually verify

Reversible visible changes such as UI sizing, prop swaps, material tuning, or a new minor animation. Provide a reference and cheap preview when practical; final status requires an in-game screenshot or short motion capture.

### Class C — user decision before spending or integrating

- Lalay/Grand Bay layout or landmark movement.
- Franki/Sacat body, face, height, clothing silhouette, or signature accessories.
- Paid Hitem3D generation or purchase.
- Replacing an approved asset or manual placement.
- Render-pipeline migration, package changes with broad impact, destructive cleanup, or large architecture changes.
- Combat feel, bike rider pose/lean, camera behavior, or other subjective systems after a proposed direction is available.

Offer one recommended option and at most two meaningful alternatives. Record the accepted choice in `DECISIONS.md` and, for appearance/placement, `VISUAL-APPROVAL-REGISTER.md`.

## Visual evidence standard

Static appearance requires:

- reference image or written target;
- fixed camera name and resolution;
- before/after screenshots where a before exists;
- mobile-distance view and one close inspection view;
- note of lighting/time-of-day used.

Movement requires a short live capture showing the complete action at normal speed. Screenshots cannot prove locomotion, punch contact, ragdoll blending, wheelie balance, rider/bike synchronization, traffic, or NPC navigation.

An automated render proves only what it actually renders. A compile proves only that the project compiles. Headless runtime proves only that the exercised path produced no logged failure. The handoff must name any human look/feel check still owed.

## Visual locks

When the user says an appearance or placement is right, add a record to `Docs/VISUAL-APPROVAL-REGISTER.md` with the scene/prefab, object or stable ID, evidence path, approved aspects, and aspects still free to change.

A visual lock does not freeze bug fixes. It prevents an agent from silently changing the approved position, scale, silhouette, color, framing, or tuning for the sake of mathematical normalization or regeneration. A change requires a new Class C decision and new evidence.

## Accurate Grand Bay/Lalay world pipeline

The detailed reusable state machine, artifacts, commands, passability gates and migration contract live in `Docs/WORLD-EXPANSION-WORKFLOW.md`. That document is mandatory for every Dominica district, other island, or unrelated future map; this section remains the short policy summary.

1. Build Map Truth from licensed sources: OpenStreetMap/Geofabrik, public elevation data, user-owned photos/video, and user-confirmed landmarks. Do not copy Google imagery or Google 3D geometry into the shipped game.
2. Store verified anchors and sources in `Docs/MAP-ANCHORS.json`; mark artistic approximations honestly.
3. Use QGIS with a local metric origin (Dominica can start with WGS 84 / UTM Zone 20N, subject to source verification). One Unity unit equals one metre.
4. Preserve landmark order, road direction, coast/hill relationships, and recognizable spaces. Compress long travel distances deliberately rather than distorting each block independently.
5. First create a road/lot/coast graybox and three fixed comparison cameras. Obtain user approval before decorative population.
6. Generate repeatable roads, curbs, lots, fences, and scatter from data. Freeze an approved district revision before optimization and decoration.
7. Integrate in chunks with stable IDs, collision, NavMesh, LODs, and mobile-distance culling. The existing scene builder remains authoritative until a specific migration task replaces it safely.

First target: Lalay main street to the beach/jetty, with side lanes, market/institution anchors, safehouse relationship, and the Montine turnoff. Do not attempt the whole island first.

## Hitem3D asset pipeline

Hitem3D's subscription output is a candidate. It may save modeling time, but it does not replace topology, UV, rig, collider, performance, or visual checks.

Before generation, approve an asset card containing:

- purpose and replacement target;
- owned/licensed reference images and front/back/left/right views when possible;
- style target and silhouette notes;
- real-world dimensions and pivot;
- LOD0 triangle target, texture cap, material count, collider type;
- whether it must deform, be modular, or remain static;
- maximum generations/credits for the attempt.

Then:

```text
Hitem3D candidate
  -> archive source URL/prompt/settings/license evidence
  -> Blender inspection and cleanup
  -> correct scale/origin/transforms
  -> retopology/UV/atlas as needed
  -> LODs + collision
  -> neutral render approval
  -> Unity import staging
  -> prefab validation
  -> in-game screenshot approval
```

Best early Hitem3D uses: Caribbean props, furniture, market items, accessories, vegetation candidates, and recognizable static objects. Highest-risk uses: production-ready modular human bodies, hands/fingers, facial topology, skinned clothing, motorcycles with mechanical hierarchy, and animation-ready rigs.

## Modular character and wardrobe standard

Do not generate a complete new body for every outfit. Establish one canonical production skeleton and body proportions first.

- Franki and Sacat keep their identity, but future production bodies should share a compatible skeleton, bind pose, scale, bone naming, and attachment points.
- Use a modest base garment (boxer/short base), not a nude production asset.
- Shirts, trousers/shorts, shoes, hats, chains, and watches are separate assets. Deforming garments must be skinned to the canonical skeleton; rigid accessories use named sockets.
- Build a small test capsule first: one base body, one shirt, one shorts, one shoes, one chain, Idle/Walk/Run/Interact, then validate swaps and clipping before generating a wardrobe.
- Preserve four or fewer skin influences per vertex unless a measured exception is approved. Use atlases/material sharing and LODs.

Hitem3D can generate visual candidates for the body/clothes, but Blender must own the final topology, skin weights, bind pose, and export. Mixamo remains useful for motion prototypes; a valid Humanoid mapping alone is not proof of good deformation.

## Bike rider synchronization

Use a vehicle mount profile rather than hand-tuning each animation:

- stable targets for seat/pelvis, left/right grips, left/right foot pegs, and optional pillion targets;
- a tested seated base pose;
- two-bone IK for hands and feet with elbow/knee hints;
- pelvis/seat position and rotation constraint;
- bike lean/wheelie applied to one shared mount root so bike and rider do not diverge;
- baked finger grip poses or a small number of finger presets, not expensive per-finger physics on mobile;
- separate enter, idle, pull-away, turn/lean, wheelie, stop, and exit states with controlled blends.

Approve the neutral seated pose before tuning movement. Then approve ordinary turns before wheelies. Test Sacat and Franki separately because equal height does not guarantee identical bone scales or proportions.

## NPC movement, combat, and ragdoll order

Do not expand the current placeholder punch system into a large combat tree first.

1. Fix contact truth: animation-timed forward capsule/sphere casts, target layers, line-of-sight/angle checks, one hit per swing, and debug visualization. A fist merely passing near a character must not count.
2. Replace warped clips with validated Humanoid animations and correct Avatar/pose mapping.
3. Add locomotion-aware attack blending and recovery; test standing and moving separately.
4. Add hit reactions and a simple knockdown/get-up state.
5. Add pooled ragdoll only for hard knockdowns/KO, with controlled animator-to-ragdoll and ragdoll-to-recovery blending.
6. Add two or three readable combos before more moves. Shooting remains a later, separate input/aim/weapon task.

For NPCs, validate NavMesh coverage and then capture live footage of walking, turning, obstacle recovery, crowd spacing, and pooling. A static NavMesh check cannot prove natural movement.

## Mobile-first gates

PC keyboard controls may remain during development, but all new actions must go through a named input action/abstraction with an obvious touch equivalent. Do not hardwire more gameplay directly to random keys.

Each visible feature must be checked at a phone-like resolution and safe area. Each content task records triangle count, material count, texture cap, dynamic-light impact, collider complexity, and expected active-instance count. Prefer pooled NPCs/effects and bounded update distances.

Render-pipeline or input-system migration is a separate Class C task. Inspect existing packages and generated-scene dependencies first; do not combine migration with new content.

## Low-budget rules

- Use the already paid Hitem3D subscription in batches, after reference approval.
- Do not subscribe to another generator until a measured bottleneck proves the need.
- Use QGIS, Blender, Rigify, Mixamo, Unity packages, Git, and existing project validators first.
- One approved concept sheet should drive several related assets; do not pay multiple agents/services to solve the same asset simultaneously.
- Stop a generation attempt when its card's credit cap is reached. Review before spending again.
- Track source, license, prompt/settings, and cleanup status in `Docs/ASSET-REGISTER.md`.

## Recommended production order

1. Workflow/visual-lock foundation (this document).
2. Hands-on regression pass on the current build; classify what is truly accepted versus merely compiled.
3. Map Truth and Lalay-to-beach road/lot/coast graybox.
4. One canonical modular-character wardrobe test capsule.
5. Bike mount profile and IK on the existing bike.
6. Combat contact truth and one polished punch/hit/knockdown loop.
7. NPC locomotion/pooling live validation and polish.
8. First Android profiling pass before expanding world density.

This order gives visible progress while protecting the systems most expensive to redo.

## Tool/plugin decision

No new external plugin is required for the first phase. Local Git, Unity batch/build tools, fixed-camera snapshot tools, Codex subagents, browser control for signed-in Hitem3D work, and Computer Use for Unity/Blender live inspection cover the immediate workflow.

Install or connect GitHub only if the user chooses remote backup/review. Do not add Notion, Drive, project-management services, Houdini, Substance, or another 3D subscription just to make the workflow look sophisticated.

## Completion and handoff

Every work packet ends with:

- exact changed files;
- compile/test/validator result and log paths;
- screenshot/video evidence paths;
- measured mobile budgets where relevant;
- user approval status;
- known limitations and next decision;
- updated handoff/changelog/decision/asset/visual-lock records;
- ownership released.

If another agent's dirty work prevents a clean commit, do not absorb it. Record the reason and leave the user's changes untouched.
