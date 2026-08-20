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
status: SUPERSEDED
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

```yaml
id: VA-002
status: APPROVED
date: 2026-08-20
task: MINI-087
subject: Sacat purchased GoldChain18k two-piece fitted placement
scene_or_prefab: Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\MINI-087-CaptureChain.log
  - E:\Unity\Up Iz Up Mini\Logs\MINI-087-Validate-2.log
  - E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Data\Equipment\SacatChainPlacement.asset
approved_aspects:
  - root local position (-0.047002427, 0.48330128, 0.07208218)
  - root local rotation (341.27582, 0, 0)
  - root local scale (0.9118362, 1.1559348, 0.9118362)
  - original front mesh fitted transform
  - duplicated mesh fitted around the nape/back of the neck
  - runtime recreation of both fitted pieces when Sacat owns chain_gold
still_editable:
  - runtime swing stiffness and damping after movement playtest
  - Boss C model scale and his separate chain placement
user_words: "Sacat chain done" / "i made a duplicate of the chain as well so save that too"
supersedes: VA-001
notes: This is a user-authored visual lock. Do not recalculate the root or either fitted child transform. The duplicated nape piece is profile data and is recreated at runtime; it is not a source-prefab edit.
```

```yaml
id: VA-003
status: APPROVED
date: 2026-08-20
task: MINI-088
subject: Boss C body width and two-piece GoldChain18k fit
scene_or_prefab:
  - Assets/UpIzUpMini/Data/Character/BossCVisualProfile.asset
  - Assets/UpIzUpMini/Data/Equipment/BossCChainPlacement.asset
evidence:
  - E:\Unity\Up Iz Up Mini\Logs\Snapshots\MINI-088-BossC-Body-Manual.png
  - E:\Unity\Up Iz Up Mini\Logs\Snapshots\MINI-088-BossC-Chain-Manual.png
  - E:\Unity\Up Iz Up Mini\Logs\MINI-088-Validate.log
approved_aspects:
  - Boss C visual-root scale (1.4496428, 1, 1)
  - 1.85 m height preserved
  - 0.4403 m shoulder width matching Sacat
  - front-to-back depth preserved
  - chain root local position (-0.035, 0.093, -0.423)
  - chain root local rotation (280.99878, 179.99992, 180.00008)
  - chain root local scale (0.7353191, 0.7353193, 0.73531926)
  - original front mesh and duplicated nape/back mesh fitted transforms
still_editable:
  - runtime animation deformation after hands-on movement observation
  - a future canonical shared rig/body project only with a new explicit approval
user_words: "i placed it manually so you can save it"
supersedes: null
notes: User manual placement is authoritative. Do not replace this with calculated bounds or Sacat's bone-local root numbers. Sacat VA-002 remains independently locked.
```
