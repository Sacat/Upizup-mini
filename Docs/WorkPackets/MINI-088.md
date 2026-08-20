# MINI-088 — Boss C two-piece chain fit

```yaml
task_id: MINI-088
title: Boss C two-piece chain fit
request_owner: User
integrator: Codex
status: complete
approval_class: C
budget:
  codex_time: one preview, one approval revision loop, then capture/rebuild/validation/build
  claude_time: 0
  external_credits: 0
  stop_condition: stop at the Boss C visual gate before canonical integration
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini088BossCChainPlacement.cs
  - Assets/UpIzUpMini/Data/Equipment/BossCChainPlacement.asset
  - Assets/UpIzUpMini/Data/Character/BossCVisualProfile.asset
  - Assets/UpIzUpMini/Scripts/Character/CharacterVisualProfile.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini011PhaseBSetup.cs
  - Assets/UpIzUpMini/Editor/Mini088BossCValidation.cs
  - Docs/WorkPackets/MINI-088.md
  - Docs/VISUAL-APPROVAL-REGISTER.md
  - PROJECT-HANDOFF.md
  - TASKS.md
protected_files:
  - Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset
  - Sacat VA-002 root and fitted child transforms
depends_on:
  - MINI-083
  - MINI-087
```

## Intent

Boss C and Sacat already measure 1.85 m in the generated scene. Keep that height match, and match Boss C's measured shoulder/body width to Sacat through a reversible visual-root X-scale preview. Preserve his front-to-back depth. Reproduce Sacat's front-plus-nape chain presentation on Boss C using a separate Boss C profile because his Blender metarig has different bone axes and scale from Sacat's Mixamo rig.

## Non-goals

- Do not resize Sacat, Franki, other NPCs, or CharacterController capsules. Boss C may change only in X/Z visual-root scale; preserve his 1.85 m height.
- Do not modify Sacat's approved VA-002 placement.
- Do not unify or replace character rigs.
- Do not change the chain mesh source, material, price, or swing behavior.

## Acceptance scorecard

- [x] Confirm existing measured height truth: Sacat 1.85 m, Boss C 1.85 m.
- [x] Produce a reversible Boss C shoulder/body-width match preview while preserving height and depth.
- [x] Prepare Boss C preview from his existing baked chain root and Sacat's two-piece fitted geometry.
- [x] User manually revises and approves Boss C in Unity.
- [x] Capture separate Boss C body-scale and chain-placement profiles.
- [x] Rebuild, validate both character fits independently, and produce a Windows build.

## Evidence

- Manual body screenshot: `Logs/Snapshots/MINI-088-BossC-Body-Manual.png`.
- Manual chain close-up: `Logs/Snapshots/MINI-088-BossC-Chain-Manual.png`.
- Capture: `Logs/MINI-088-CaptureApproved.log` — body scale plus root and four child transforms.
- Canonical rebuild: `Logs/MINI-088-Rebuild.log` — success.
- Exact profile validation: `Logs/MINI-088-Validate.log` — pass; Boss C/Sacat shoulder widths both 0.4403 m and Sacat VA-002 unchanged.
- Windows build: `Logs/MINI-088-Build.log` — succeeded, 407,521,243 bytes.

Rollback: `ca7f198` restores the approved Sacat chain checkpoint with Boss C unchanged.
