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

No formal visual locks have been backfilled yet. Add them prospectively, and only backfill an older approval when the supporting user statement and evidence can be identified reliably.
