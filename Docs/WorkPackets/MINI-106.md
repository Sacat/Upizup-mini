# MINI-106 — Sacat mobile LOD proof

```yaml
task_id: MINI-106
title: Mobile-ready Sacat LOD proof
request_owner: User
integrator: Codex
status: evidence_ready
approval_class: C
budget:
  external_credits: 0
  stop_condition: Three isolated rig-preserving LODs, Unity validation and fixed comparison evidence; no playable replacement.
reserved_files:
  - Docs/CharacterPipeline/MINI-106/
  - Docs/WorkPackets/MINI-106.md
  - Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/
  - Assets/UpIzUpMini/Editor/Mini106SacatMobileLodImport.cs
  - Docs/CURRENT.md
  - Docs/ASSET-REGISTER.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Prefabs/SacatModularBase_ImportProof.prefab
  - Existing playable Sacat prefab/model, Animator and VA-002 chain placement
depends_on:
  - MINI-105
  - VA-006
```

## Intent

Create visually faithful, mobile-conscious Sacat LOD meshes while retaining his approved appearance, Humanoid skeleton and full finger chains.

## Non-goals

- Do not replace playable Sacat.
- Do not change his approved face, proportions, hairline, vest, boxers or accessories.
- Do not alter gameplay scenes, Animator controllers, bike fitting or combat.
- Do not spend Hitem3D or other paid credits.

## Acceptance scorecard

- [ ] LOD0 roughly 30k–35k triangles.
- [ ] LOD1 roughly 14k–18k triangles.
- [ ] LOD2 roughly 5k–8k triangles.
- [ ] All levels retain one skinned mesh, UV/material and full Humanoid/finger hierarchy.
- [ ] Unity import enforces four maximum skin influences and Android texture cap.
- [ ] Fixed front/side/back comparison screenshots are visually inspected.
- [ ] User approval received before playable integration.

## Evidence

- Blender: exact 25,000 / 12,000 / 4,500 triangle targets; 101 bones; zero unweighted vertices; four maximum influences. Audit: `Docs/CharacterPipeline/MINI-106/BlenderOutput/Sacat-Mobile-LOD-Audit.json`.
- Unity: three valid Humanoid imports, 30/30 finger joints at every level, exact triangle budgets and one material each. Audit: `Logs/Tasks/MINI-106/Sacat-Mobile-LOD-Audit.json`.
- Combined isolated prefab: `Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/Prefabs/SacatModularBase_Mobile.prefab` with transitions 0.30 / 0.12 / 0.04 and no crossfade.
- Fixed visual comparison: `Logs/Tasks/MINI-106/Sacat-Mobile-LOD-Comparison.png`.
- The first two axis/bind attempts were rejected from their screenshots; the final mobile wrapper keeps a zero-rotation gameplay root while its visual rig corrects the Blender/Unity 90-degree axis difference.
- Unity log: `Logs/MINI-106-import.log`, return code 0.
- Human checks owed: user static approval; then isolated animation/finger/bike-grip deformation and Android device profiling.

Status: Evidence ready. Playable Sacat remains unchanged.
