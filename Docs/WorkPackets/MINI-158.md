# MINI-158 — real motion proof for both reshaped garments

```yaml
task_id: MINI-158
title: Get real idle/walk/run screenshots for Sacat's and Franki's reshaped garments
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: C
budget:
  claude_time: extend MINI-154's proof tool to both characters, retry render without -nographics
  external_credits: 0
  stop_condition: real motion screenshots or a confirmed environment block
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini154GarmentMotionProof.cs
  - Logs/Tasks/MINI-158/
  - Docs/WorkPackets/MINI-158.md
  - Docs/Systems/Characters.md
  - Docs/Systems/BuildAndVerification.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity (opened in-memory only, never saved - hash-verified unchanged)
depends_on:
  - MINI-154 (Franki reshape), MINI-157 (Sacat reshape + colour on both)
```

## Intent

MINI-154/MINI-157 both got stuck on "headless screenshot rendering comes
back blank in this environment" and told the user it needed an open Unity
Editor session. Before asking for that, retry with the fix
`Docs/Systems/BuildAndVerification.md` already documents for exactly this
symptom: drop `-nographics`, keep `-batchmode`.

## What happened

Extended `Mini154GarmentMotionProof.cs` to cover both reshaped assets
(Franki's `Franki_ReshapedGarments_Colored.fbx`, Sacat's
`Sacat_Ch06_Reshaped.fbx`) in one pass, each spawned in open air above the
canonical scene (never saved - file hash verified unchanged before/after),
driven by the shared `StarterAssetsThirdPerson.controller`, sampling
Idle/Walk/Run at 11 frames each via `PlayableGraph`/`AnimationClipPlayable`.

Ran it with `-batchmode -projectPath ... -executeMethod ...` (no
`-nographics`) instead of repeating the same `-nographics` invocation that
failed twice before. **This produced real images** (120-145KB PNGs with
actual visible content, vs. the ~1-3KB flat solid-color files from every
prior `-nographics` attempt) - confirmed by actually opening and looking
at them, not just checking file size.

## Result

- **Franki**: clean through Idle, Walk, and Run at every sampled frame -
  no shoulder gap, no tearing at the short-sleeve cuff, no seam at the
  waist where the shirt meets the pants, hand stays correctly attached to
  the wrist through the full range of motion. This is real motion
  evidence, not a static bind-pose render.
- **Sacat**: same structural result - no gaps, no tearing, the
  texture-repainted sleeve/collar boundary holds up correctly through
  motion (this specifically answers the open question from MINI-157: does
  a texture-only, zero-topology-change edit actually survive real
  deformation, since the geometry itself was never touched, only
  re-textured). **New issue found**: Sacat's render is overexposed/washed
  out (a lighting or material colour-space issue in this specific render
  setup, not a mesh defect - the silhouette and boundaries are still
  correctly readable through the blown-out lighting). Flagged as an open
  item, not chased further this pass.

## Acceptance scorecard

- [x] Real motion evidence (not a static/bind-pose render) for both characters
- [x] No shoulder/sleeve/collar/waist tearing observed at any sampled Idle/Walk/Run frame
- [x] Canonical scene file verified unchanged on disk (hash check)
- [ ] Sacat's overexposed render lighting/material issue - not yet fixed
- [ ] User visual approval of the motion evidence
- [ ] Unity gameplay integration - still not started, gated on approval

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| `-nographics` was the actual cause of the earlier blank renders | Same tool, same scene, only removing `-nographics` | `Logs/Tasks/MINI-158/*.png` (real content) vs `Logs/Tasks/MINI-154/Motion-*.png` (flat colour) | No |
| Franki survives idle/walk/run without tearing | 66 real screenshots (Front+Side x 11 frames x 3 clips) | `Logs/Tasks/MINI-158/Franki-*.png` | Yes - final acceptance |
| Sacat survives idle/walk/run without tearing | same, 66 screenshots | `Logs/Tasks/MINI-158/Sacat-*.png` | Yes - final acceptance |
| Canonical scene untouched | SHA-256 file hash before/after | `MINI158_MOTION_PROOF_PASS`, `Logs/MINI-158-motion-proof.log` | No |

## Handoff

- Files changed: `Assets/UpIzUpMini/Editor/Mini154GarmentMotionProof.cs` (extended to both characters, kept its original name/task references since it's the same tool evolving, not replaced); new `Logs/Tasks/MINI-158/*` evidence (132 screenshots + log). No scene/asset/accessory changes.
- Decisions made: none requiring user input this pass - this was a verification retry, not a design decision.
- Visual locks added/changed: none.
- Known limitations: Sacat's render is overexposed (lighting/material, not geometry) and needs a quick fix before it's a fair "final" screenshot; still no Unity gameplay integration.
- Next action: user reviews the motion evidence; if the shape/fit reads correctly despite Sacat's blown-out lighting, next step is Unity gameplay integration (wiring these into the playable characters / wardrobe system) - the last real gate before that was this motion proof, which now passes structurally.
- Ownership released: yes, at the end of this session's pass.
