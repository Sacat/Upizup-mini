# MINI-087 — Sacat chain inward-fit revision gate

```yaml
task_id: MINI-087
title: Sacat chain inward-fit revision gate
request_owner: User
integrator: Codex
status: evidence_ready_waiting_movement_playtest
approval_class: C
budget:
  codex_time: one manual placement setup and capture/rebuild after approval
  claude_time: 0
  external_credits: 0
  stop_condition: stop before resizing or changing Boss C
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini086ChainPlacement.cs
  - Assets/UpIzUpMini/Editor/Mini086ChainValidation.cs
  - Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scripts/Character/AccessoryPlacementProfile.cs
  - Assets/UpIzUpMini/Scripts/Character/CharacterEquipment.cs
  - Docs/WorkPackets/MINI-087.md
  - Docs/VISUAL-APPROVAL-REGISTER.md
  - PROJECT-HANDOFF.md
  - TASKS.md
protected_files:
  - Boss C model, scale, rig, and chain placement
depends_on:
  - MINI-086
```

## Intent

Reopen Sacat's current approved chain placement and let the user push it slightly inward. Preserve the current position/rotation/scale as the starting point. Capture and rebuild only after the user approves the revised fit in Unity.

## Non-goals

- Do not resize Boss C yet.
- Do not copy Sacat's bone-local numbers to Boss C's different rig.
- Do not change the source chain mesh, material, swing, price, or ownership. Preserve the user's scene-made duplicate as fitted profile data.

## Acceptance scorecard

- [x] Manual preview starts from VA-001, not the old default constants.
- [x] User moves the chain slightly inward and explicitly approves it.
- [x] Revised root plus both fitted mesh transforms supersede VA-001 and are captured exactly.
- [x] Scene rebuild, fitted-duplicate runtime validation, and Windows build pass.
- [ ] User performs the final walking/running visibility and clipping check in the updated build.

## Evidence

- User approval: `"Sacat chain done"` and confirmation that the duplicate covers the nape/back of the neck.
- Capture: `Logs/MINI-087-CaptureChain.log`.
- Rebuild: `Logs/MINI-087-Rebuild.log`.
- Runtime equipment validation: `Logs/MINI-087-Validate-2.log` — purchase recreates both fitted pieces exactly.
- Windows build: `Logs/MINI-087-Build.log` — succeeded, 407,520,555 bytes.

Rollback: `d45724c` restores the first approved placement and runtime visibility fix.
