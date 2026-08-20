# Up Iz Up Mini — Visual Approval Register

This file protects looks and placements the user has explicitly approved. Absence from this file does not mean an agent may ignore the design brief; it means the item has not yet received a formal visual lock.

## Status values

- `PROPOSED`: evidence exists; user has not decided.
- `APPROVED`: approved aspects are locked.
- `REVISE`: user rejected or requested changes.
- `SUPERSEDED`: replaced by a later approved record.

## Record template

```yaml
id: VA-###
status: PROPOSED | APPROVED | REVISE | SUPERSEDED
date: YYYY-MM-DD
task: MINI-###
subject: Stable object/prefab/scene ID
scene_or_prefab: path
evidence:
  - absolute screenshot/video path
approved_aspects:
  - placement
  - scale
  - silhouette
  - color/material
  - camera/framing
  - motion/tuning
still_editable:
  - clearly bounded aspects
user_words: Short exact confirmation or rejection
supersedes: null
notes: Important constraints
```

## Active records

```yaml
id: VA-001
status: APPROVED
date: 2026-08-20
task: MINI-086
subject: Sacat purchased GoldChain18k placement
scene_or_prefab: Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\MINI-086-CaptureApprovedChain.log
  - E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Data\Equipment\SacatChainPlacement.asset
approved_aspects:
  - local position (-0.03414066, 0.40609157, 0.06550227)
  - local rotation (337.00732, 0, 0)
  - local scale (1.066973, 1.066973, 1.066973)
  - GoldChain18k model on Sacat when Sacat owns chain_gold
still_editable:
  - runtime swing stiffness and damping after movement playtest
  - a separately approved Franki-specific fit profile
user_words: "ok the chain was done"
supersedes: null
notes: User placement is authoritative. Do not normalize or recalculate it. Boss C uses the same cleaned chain prefab with rig-specific placement; Boss J wears no chain.
```
