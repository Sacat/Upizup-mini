# MINI-107 — Reusable character pipeline and Sacat motion proof

```yaml
task_id: MINI-107
title: Reusable modular-character pipeline and Sacat deformation proof
request_owner: User
integrator: Codex
status: paused_by_user
approval_class: C
budget:
  external_credits: 0
  stop_condition: Reusable manifest/workflow plus isolated Sacat motion evidence; no playable replacement.
reserved_files:
  - Docs/CharacterPipeline/System/
  - Docs/CharacterPipeline/MINI-107/
  - Docs/CHARACTER-PRODUCTION-WORKFLOW.md
  - Docs/WorkPackets/MINI-107.md
  - Tools/CharacterPipeline/
  - Assets/UpIzUpMini/Editor/Mini107CharacterMotionProof.cs
  - Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/Proof/
  - Docs/VISUAL-APPROVAL-REGISTER.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Existing playable Sacat, Animator and VA-002 chain
depends_on:
  - MINI-105
  - MINI-106
  - VA-006
```

## Intent

Prove the approved mobile Sacat under representative Humanoid motion and encode every successful step into a reusable low-cost character-production system for future heads, bodies, accessories and clothing.

## Non-goals

- Do not replace playable Sacat in this packet.
- Do not modify gameplay scenes, controls, bike fitting, combat or locked accessories.
- Do not generate or buy another character.
- Do not claim Android performance without a real Android/device profiling pass.

## Acceptance scorecard

- [ ] Existing legal Humanoid idle/walk/run/jump clips are identified and sampled on mobile Sacat.
- [ ] Representative full-finger/fist or grip poses prove separate finger motion.
- [ ] Fixed screenshots plus a short motion file are created and inspected.
- [ ] A reusable character manifest records source, identity lock, rig, LOD, texture, axis and evidence settings.
- [ ] One command or documented short sequence repeats Blender LOD generation and Unity validation for another character.
- [ ] Existing playable Sacat remains unchanged.

## Pause handoff — 2026-08-21

The user paused this task to playtest simpler game systems and conserve tokens. Do not resume MINI-107 until requested.

Completed and reusable:

- Character manifest schema and Sacat reference manifest.
- Reusable character-production workflow.
- Manifest-driven Blender LOD builder and one-command wrapper.
- Unity runtime-style Humanoid proof builder using a PlayableGraph.
- Static Sacat LODs remain approved at 25k/12k/4.5k triangles.

Critical motion finding:

- The original 100K AccuRIG Sacat deforms correctly under owned idle/walk/run/jump clips.
- The decimated mobile LOD mesh tears badly at animated joints and stretches across the frame. The failure is in the derived mesh/weights, not the original rig or the +90-degree wrapper correction.
- Therefore the mobile LOD prefab is **static-proof only** and must not replace playable Sacat.
- On resume, rebuild LODs with a deformation-preserving workflow (preferred: rig-aware retopology or carefully transferred weights), then repeat the same PlayableGraph screenshots before playable integration.
- `gates.motionApproved` and `gates.playableIntegration` correctly remain false.

Diagnostic evidence/logs:

- `Logs/MINI-107-motion-proof.log` — failed mobile deformation visual despite structural checks passing.
- `Logs/MINI-107-source-diagnostic.log` — original 100K rig samples successfully.
- `Logs/Tasks/MINI-107/Sacat-Motion-Pose-Matrix.png` — most recent source-rig diagnostic; do not label it as mobile LOD approval.
